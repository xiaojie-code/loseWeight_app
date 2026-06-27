using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.PoseDetection;
using LoseWeight.PoseDetection.Providers;
using LoseWeight.Game;
using LoseWeight.UI;
#if UNITY_EDITOR || !UNITY_ANDROID
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
#endif

namespace LoseWeight.CannonGame
{
    /// <summary>
    /// Standalone cannon shooter mode. All scene/UI nodes are MCP-authored in
    /// AppSceneRoot/CannonGameRoot; runtime code only binds and reuses them.
    /// </summary>
    public class CannonGameController : MonoBehaviour
    {
        private enum Phase { Init, Tutorial, WaitingPose, Countdown, Playing, GameOver }

        private Phase _phase = Phase.Init;
        private float _gameTimer = 60f;
        private float _countdownTimer;
        private int _score;
        private int _kills;
        private int _shots;
        private int _combo;
        private int _lives = 3;

        private GameObject _rootGo;
        private GameObject _poseRootGo;
        private RectTransform _canvasRT;
        private RectTransform _gameAreaRT;
        private GameObject _tutorialPanel;
        private GameObject _endActions;
        private readonly List<Cannonball> _bulletPool = new List<Cannonball>();

        private CannonAimController _aimController;
        private ZombieSpawner _zombieSpawner;
        private CannonHUD _hud;
        private readonly CannonPoseInput _poseInput = new CannonPoseInput();

        [SerializeField] private bool _enableEditorMouseFallback;

        private IPoseProvider _poseProvider;
        private MLKitPoseProvider _mlKitPoseProvider;
        private MobileCameraPreview _cameraPreview;
        private RawImage _cameraRawImage;
        private PoseOverlayUI _poseOverlay;
        private bool _poseSubscribed;
        private int _poseReadyFrames;
        private float _lastPoseFrameTime;
        private Vector2 _recentLockedTarget;
        private float _recentLockedTargetTime = -10f;
        private float _manualFireCooldownUntil;
        private Zombie _manualTarget;

#if UNITY_EDITOR || !UNITY_ANDROID
        private GameObject _mediaPipeSolutionGo;
        private PoseLandmarkerRunner _mediaPipeRunner;
        private bool _mediaPipeSolutionStarted;
#endif

        private const float ShotHitWidth = 72f;
        private const float TargetLockWidth = 128f;
        private const float FireTargetLockGrace = 0.48f;
        private const int PoseReadyFramesNeeded = 8;
        private const bool ManualTapControl = true;
        private const float ManualFireCooldown = 0.42f;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.CannonGame)
            {
                StartGame();
            }
            else if (evt.OldState == GameState.CannonGame || evt.OldState == GameState.CannonGameEnd)
            {
                Cleanup();
            }
        }

        private void StartGame()
        {
            CleanupSessionOnly();
            _rootGo = SceneNodeResolver.FindRequired("AppSceneRoot/CannonGameRoot");
            if (_rootGo != null) _rootGo.SetActive(true);
            if (!BindScene())
            {
                if (_rootGo != null) _rootGo.SetActive(false);
                return;
            }

            _phase = Phase.Tutorial;
            _score = 0;
            _kills = 0;
            _shots = 0;
            _combo = 0;
            _lives = 3;
            _gameTimer = 60f;
            _poseReadyFrames = 0;
            _lastPoseFrameTime = 0f;
            _recentLockedTarget = Vector2.zero;
            _recentLockedTargetTime = -10f;
            _manualFireCooldownUntil = 0f;
            _manualTarget = null;
            _poseInput.Reset();

            HideEndButtons();
            HideBulletPool();
            _zombieSpawner.StopSpawning();
            if (!ManualTapControl)
                StartPoseDetection();

            _hud.UpdateScore(_score);
            _hud.UpdateLives(_lives);
            _hud.UpdateTimer(_gameTimer);
            _hud.UpdateCombo(_combo);
            _hud.UpdateWave(1, 1);
            ShowTutorial();

            Debug.Log("[CannonGame] StartGame using MCP scene nodes");
        }

        private bool BindScene()
        {
            _rootGo = SceneNodeResolver.FindRequired("AppSceneRoot/CannonGameRoot");
            if (_rootGo == null) return false;

            _canvasRT = _rootGo.GetComponent<RectTransform>();
            _gameAreaRT = FindRect(_rootGo.transform, "GameArea");
            _tutorialPanel = FindTransform(_rootGo.transform, "Tutorial")?.gameObject;
            _poseRootGo = FindTransform(_rootGo.transform, "CannonPoseSystem")?.gameObject;
            _endActions = FindTransform(_rootGo.transform, "EndActions")?.gameObject;

            _aimController = _rootGo.GetComponent<CannonAimController>();
            _zombieSpawner = _rootGo.GetComponent<ZombieSpawner>();
            _hud = _rootGo.GetComponent<CannonHUD>();

            if (_canvasRT == null || _gameAreaRT == null || _poseRootGo == null
                || _aimController == null || _zombieSpawner == null || _hud == null)
            {
                Debug.LogError("[CannonGame] MCP scene is incomplete. Rebuild CannonGameRoot via Unity MCP.");
                return false;
            }

            _aimController.Initialize(_canvasRT, _gameAreaRT);
            _zombieSpawner.Initialize(_gameAreaRT);
            _zombieSpawner.OnZombieReachedBottom = OnZombieReachedBottom;
            _zombieSpawner.OnWaveChanged = OnWaveChanged;
            _hud.Initialize(_canvasRT);
            BindButtons();
            BindBulletPool();
            BindCameraPreview();
            NormalizeStaticLabels();
            return true;
        }

        private void BindButtons()
        {
            var back = FindTransform(_rootGo.transform, "BackBtn")?.GetComponent<Button>();
            if (back != null)
            {
                back.onClick.RemoveAllListeners();
                back.interactable = true;
                back.onClick.AddListener(OnBackClicked);
            }

            var retry = FindTransform(_rootGo.transform, "Retry")?.GetComponent<Button>();
            if (retry != null)
            {
                retry.onClick.RemoveAllListeners();
                retry.onClick.AddListener(StartGame);
            }

            var home = FindTransform(_rootGo.transform, "Home")?.GetComponent<Button>();
            if (home != null)
            {
                home.onClick.RemoveAllListeners();
                home.onClick.AddListener(OnBackClicked);
            }
        }

        private void BindBulletPool()
        {
            _bulletPool.Clear();
            var poolRoot = FindTransform(_rootGo.transform, "BulletPool");
            if (poolRoot == null)
            {
                Debug.LogError("[CannonGame] Missing MCP BulletPool.");
                return;
            }

            foreach (var bullet in poolRoot.GetComponentsInChildren<Cannonball>(true))
            {
                bullet.gameObject.SetActive(false);
                _bulletPool.Add(bullet);
            }
        }

        private void BindCameraPreview()
        {
            var preview = FindTransform(_rootGo.transform, "CannonCameraPreview");
            var rawImage = FindTransform(_rootGo.transform, "CamTexture")?.GetComponent<RawImage>();
            var overlay = FindTransform(_rootGo.transform, "PoseOverlay")?.GetComponent<PoseOverlayUI>();
            _cameraPreview = preview != null ? preview.GetComponent<MobileCameraPreview>() : null;
            _cameraRawImage = rawImage;
            _poseOverlay = overlay;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_cameraPreview != null && rawImage != null)
                _cameraPreview.Initialize(rawImage);
#endif
            if (_poseOverlay != null)
                _poseOverlay.Initialize(_poseOverlay.GetComponent<RectTransform>());
        }

        private void NormalizeStaticLabels()
        {
            SetText("BackBtn/Text", "<返回");
            SetText("Tutorial/Tip", ManualTapControl
                ? "点击僵尸锁定目标\n大炮会自动开火"
                : "抬高手臂左右移动瞄准\n快速下落手臂开炮");
            SetText("Retry/Text", "再来一局");
            SetText("Home/Text", "主菜单");
        }

        private void SetText(string relativePath, string value)
        {
            if (_rootGo == null) return;
            var node = _rootGo.transform.Find(relativePath);
            if (node == null)
            {
                int slash = relativePath.IndexOf('/');
                if (slash > 0 && slash < relativePath.Length - 1)
                {
                    string parentName = relativePath.Substring(0, slash);
                    string childPath = relativePath.Substring(slash + 1);
                    var parent = FindTransform(_rootGo.transform, parentName);
                    node = parent != null ? parent.Find(childPath) : null;
                }
            }

            var text = (node != null ? node : FindTransform(_rootGo.transform, relativePath))?.GetComponent<Text>();
            if (text != null) text.text = value;
        }

        private void ShowTutorial()
        {
            if (_tutorialPanel != null)
                _tutorialPanel.SetActive(true);
            Invoke(nameof(EndTutorial), 2.2f);
        }

        private void EndTutorial()
        {
            if (_tutorialPanel != null)
                _tutorialPanel.SetActive(false);

            if (ManualTapControl)
            {
                _phase = Phase.Countdown;
                _countdownTimer = 3f;
                _hud.ShowStatus("3");
                _aimController.ShowInputHint("点击僵尸开炮");
                return;
            }

            _phase = Phase.WaitingPose;
            _hud.ShowStatus("请抬高手臂\n左右移动瞄准");
            _aimController.ShowInputHint("等待手臂姿态检测...");
        }

        private void StartPoseDetection()
        {
            if (_poseRootGo == null) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

            _mlKitPoseProvider = _poseRootGo.GetComponent<MLKitPoseProvider>();
            _poseProvider = _mlKitPoseProvider;
            if (_poseProvider == null)
            {
                Debug.LogError("[CannonGame] Missing MCP MLKitPoseProvider on CannonPoseSystem.");
                return;
            }

            _poseProvider.OnPoseFrame += OnPoseFrame;
            _poseProvider.OnPoseError += OnPoseError;
            _poseSubscribed = true;
            Invoke(nameof(InitMLKit), 0.25f);
#else
            _poseProvider = _poseRootGo.GetComponent<MediaPipePoseProvider>();
            if (_poseProvider == null)
            {
                Debug.LogError("[CannonGame] Missing MCP MediaPipePoseProvider on CannonPoseSystem.");
                return;
            }

            _poseProvider.OnPoseFrame += OnPoseFrame;
            _poseProvider.OnPoseError += OnPoseError;
            _poseProvider.StartDetection(new PoseRuntimeOptions
            {
                TargetFps = 15,
                UseFrontCamera = true,
                MirrorOutput = false
            });
            _poseSubscribed = true;
            StartMediaPipeProducer();
            Debug.Log("[CannonGame] Pose controls started (MediaPipe provider)");
#endif
        }

#if UNITY_EDITOR || !UNITY_ANDROID
        private void StartMediaPipeProducer()
        {
            _mediaPipeSolutionGo = SceneNodeResolver.FindRequired("Solution");
            if (_mediaPipeSolutionGo == null)
            {
                OnPoseError("未找到 MediaPipe 姿态推理节点 Solution");
                return;
            }

            _mediaPipeRunner = _mediaPipeSolutionGo.GetComponent<PoseLandmarkerRunner>();
            if (_mediaPipeRunner == null)
            {
                OnPoseError("Solution 缺少 PoseLandmarkerRunner");
                return;
            }

            if (!_mediaPipeSolutionGo.activeSelf)
                _mediaPipeSolutionGo.SetActive(true);

            _mediaPipeRunner.Play();
            _mediaPipeSolutionStarted = true;
        }

        private void UpdateMediaPipePreview()
        {
            if (_cameraRawImage == null) return;
            var imageSource = ImageSourceProvider.ImageSource;
            if (imageSource == null || !imageSource.isPrepared) return;

            var texture = imageSource.GetCurrentTexture();
            if (texture != null && _cameraRawImage.texture != texture)
                _cameraRawImage.texture = texture;
        }
#endif

        private void InitMLKit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_phase == Phase.Init) return;

            if (_cameraPreview != null && _cameraPreview.IsRunning && _mlKitPoseProvider != null)
            {
                _mlKitPoseProvider.Initialize(_cameraPreview.CamTexture, new PoseRuntimeOptions
                {
                    TargetFps = 30,
                    UseFrontCamera = true,
                    MirrorOutput = true
                });
                Debug.Log("[CannonGame] MLKit initialized");
            }
            else
            {
                Invoke(nameof(InitMLKit), 0.15f);
            }
#endif
        }

        private void OnPoseFrame(PoseFrame frame)
        {
            if (_phase == Phase.Init || _phase == Phase.GameOver) return;
            if (_poseOverlay != null) _poseOverlay.UpdatePose(frame);

            _poseInput.Process(frame, Time.time);
            _lastPoseFrameTime = Time.time;
            _aimController.UpdateAim(_poseInput.AimX, _poseInput.AimY);
            RefreshTargetLock();
            _aimController.ShowCharge(_poseInput.Charge);
            _aimController.ShowInputHint(_poseInput.Hint);

            if (_phase == Phase.WaitingPose)
            {
                UpdatePoseReadiness();
                return;
            }

            if (_phase == Phase.Playing && _poseInput.FireTriggered)
                Fire();
        }

        private void OnPoseError(string error)
        {
            Debug.LogWarning($"[CannonGame] Pose error: {error}");
            if (_aimController != null) _aimController.ShowInputHint("摄像头识别异常，请稍后重试");
        }

        private void UpdatePoseReadiness()
        {
            if (_poseInput.HasPose && _poseInput.IsCalibrated)
                _poseReadyFrames++;
            else
                _poseReadyFrames = Mathf.Max(0, _poseReadyFrames - 1);

            _hud.ShowStatus($"检测手臂姿态\n{_poseReadyFrames}/{PoseReadyFramesNeeded}");

            if (_poseReadyFrames >= PoseReadyFramesNeeded)
            {
                _phase = Phase.Countdown;
                _countdownTimer = 3f;
                _hud.ShowStatus("3");
                _aimController.ShowInputHint("姿态已就绪，快速下落手臂开炮");
            }
        }

        private void Update()
        {
            if (_phase == Phase.Init || _phase == Phase.Tutorial) return;

#if UNITY_EDITOR || !UNITY_ANDROID
            UpdateMediaPipePreview();
            if (_enableEditorMouseFallback)
                UpdateEditorControls();
#endif

            switch (_phase)
            {
                case Phase.WaitingPose:
                    if (Time.time - _lastPoseFrameTime > 1.2f)
                    {
                        _poseReadyFrames = 0;
                        _hud.ShowStatus("等待手臂姿态检测\n请确认摄像头和 MediaPipe 已启动");
                        _aimController.ShowInputHint(_enableEditorMouseFallback ? "调试模式：鼠标兜底已启用" : "请抬高手臂，露出肩膀和手腕");
                    }
                    break;

                case Phase.Countdown:
                    _countdownTimer -= Time.deltaTime;
                    if (_countdownTimer > 0f)
                    {
                        _hud.ShowStatus(Mathf.CeilToInt(_countdownTimer).ToString());
                    }
                    else
                    {
                        _hud.ShowStatus("GO!");
                        _poseInput.PrepareForRound();
                        _aimController.UpdateAim(_poseInput.AimX, _poseInput.AimY);
                        _phase = Phase.Playing;
                        _zombieSpawner.StartSpawning();
                        Invoke(nameof(ClearStatus), 0.45f);
                    }
                    break;

                case Phase.Playing:
                    if (ManualTapControl)
                        UpdateManualTapControls();
                    else
                        RefreshTargetLock();
                    _gameTimer -= Time.deltaTime;
                    _hud.UpdateTimer(_gameTimer);
                    if (_gameTimer <= 0f || _lives <= 0)
                        EndGame();
                    break;
            }
        }

        private void UpdateEditorControls()
        {
            if (ManualTapControl) return;
            if (_aimController == null) return;
            float mx = Mathf.Clamp01(Input.mousePosition.x / Mathf.Max(1f, Screen.width));
            _aimController.UpdateAim(mx, 0.62f);
            RefreshTargetLock();
            _aimController.ShowInputHint("Editor：移动鼠标瞄准，点击开炮");
            if (_phase == Phase.Playing && Input.GetMouseButtonDown(0))
                Fire();
        }

        private void UpdateManualTapControls()
        {
            if (_aimController == null || _zombieSpawner == null) return;

            if (_manualTarget != null)
            {
                if (_manualTarget.IsDead || !_manualTarget.gameObject.activeInHierarchy)
                {
                    ClearManualTarget(true);
                    return;
                }

                Vector2 lockedTargetPos = _manualTarget.GetComponent<RectTransform>().position;
                _recentLockedTarget = lockedTargetPos;
                _recentLockedTargetTime = Time.time;
                _aimController.SetLockedTarget(true, lockedTargetPos);
                _aimController.ShowInputHint("自动攻击中");

                if (Time.time >= _manualFireCooldownUntil)
                    FireAtZombie(_manualTarget);

                return;
            }

            if (!TryGetManualTapPosition(out Vector2 tapPosition))
                return;

            if (_zombieSpawner.TryFindZombieAtScreenPoint(tapPosition, out Zombie target, out Vector2 targetPos))
            {
                _manualTarget = target;
                _manualFireCooldownUntil = 0f;
                _recentLockedTarget = targetPos;
                _recentLockedTargetTime = Time.time;
                _aimController.SetLockedTarget(true, targetPos);
                _aimController.ShowInputHint("已锁定目标");
                Debug.Log($"[CannonGame] Manual tap hit {target.name} at {tapPosition}, target={targetPos}");
                FireAtZombie(target);
            }
            else
            {
                Debug.Log($"[CannonGame] Manual tap missed at {tapPosition}");
                _aimController.ShowInputHint("点击僵尸开炮");
            }
        }

        private bool TryGetManualTapPosition(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began)
                    return false;

                screenPosition = touch.position;
                return true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            return false;
        }

        private void ClearManualTarget(bool returnToCenter)
        {
            _manualTarget = null;
            _manualFireCooldownUntil = 0f;
            _recentLockedTarget = Vector2.zero;
            _recentLockedTargetTime = -10f;

            if (_aimController == null)
                return;

            _aimController.SetLockedTarget(false, Vector2.zero);
            if (returnToCenter)
                _aimController.UpdateAim(0.5f, 0.62f);
            _aimController.ShowInputHint("点击僵尸选择目标");
        }

        private void RefreshTargetLock()
        {
            if (_aimController == null || _zombieSpawner == null)
                return;

            if (_phase != Phase.Playing)
            {
                _aimController.SetLockedTarget(false, Vector2.zero);
                return;
            }

            if (_poseInput.AimFrozen && Time.time - _recentLockedTargetTime <= FireTargetLockGrace)
            {
                _aimController.SetLockedTarget(true, _recentLockedTarget);
                return;
            }

            Vector2 start = _aimController.GetCannonScreenPosition();
            Vector2 end = _aimController.GetAimRayEndScreenPosition();
            bool hasTarget = _zombieSpawner.TryFindTargetAlongShot(start, end, TargetLockWidth, out Vector2 targetPos);
            if (hasTarget)
            {
                _recentLockedTarget = targetPos;
                _recentLockedTargetTime = Time.time;
            }
            _aimController.SetLockedTarget(hasTarget, targetPos);
        }

        private void Fire()
        {
            if (_phase != Phase.Playing || _aimController == null) return;

            var bullet = GetAvailableBullet();
            if (bullet == null)
            {
                Debug.LogWarning("[CannonGame] BulletPool exhausted. Increase MCP BulletPool size.");
                return;
            }

            _shots++;
            if (Time.time - _recentLockedTargetTime <= FireTargetLockGrace)
                _aimController.SetLockedTarget(true, _recentLockedTarget);

            Vector2 start = _aimController.GetCannonScreenPosition();
            Vector2 end = _aimController.GetShotEndScreenPosition();
            _aimController.ShowFireEffect();

            bullet.transform.SetParent(_rootGo.transform, false);
            bullet.gameObject.SetActive(true);
            bullet.Initialize(start, end, _canvasRT, OnBulletArrived);

#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void FireAtZombie(Zombie target)
        {
            if (_phase != Phase.Playing || _aimController == null || target == null) return;
            if (target.IsDead || !target.gameObject.activeInHierarchy)
            {
                ClearManualTarget(true);
                return;
            }

            var bullet = GetAvailableBullet();
            if (bullet == null)
            {
                Debug.LogWarning("[CannonGame] BulletPool exhausted. Increase MCP BulletPool size.");
                return;
            }

            _shots++;
            _manualFireCooldownUntil = Time.time + ManualFireCooldown;

            Vector2 start = _aimController.GetCannonScreenPosition();
            Vector2 end = target.GetComponent<RectTransform>().position;
            Debug.Log($"[CannonGame] Manual fire target={target.name}, start={start}, end={end}");
            _aimController.SetLockedTarget(true, end);
            _aimController.ShowFireEffect();

            bullet.transform.SetParent(_rootGo.transform, false);
            bullet.gameObject.SetActive(true);
            bullet.Initialize(start, end, _canvasRT, (startScreen, endScreen) => OnManualBulletArrived(startScreen, endScreen, target));

#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private Cannonball GetAvailableBullet()
        {
            foreach (var bullet in _bulletPool)
            {
                if (bullet != null && !bullet.gameObject.activeInHierarchy)
                    return bullet;
            }
            return null;
        }

        private void OnBulletArrived(Vector2 startScreen, Vector2 endScreen)
        {
            if (_zombieSpawner == null) return;

            bool hit = _zombieSpawner.TryHitAlongShot(startScreen, endScreen, ShotHitWidth, out Vector2 hitPos, out int killScore);
            if (hit)
            {
                _aimController.ShowExplosion(hitPos, true);
                if (killScore > 0)
                {
                    _kills++;
                    _combo++;
                    int comboBonus = Mathf.Max(0, _combo - 1) * 40;
                    int points = killScore + comboBonus;
                    _score += points;
                    _hud.UpdateScore(_score);
                    _hud.UpdateCombo(_combo);
                    _hud.ShowHitFeedback(true, points);
                }
                else
                {
                    _hud.ShowHitFeedback(true, 0);
                }
            }
            else
            {
                _combo = 0;
                _aimController.ShowExplosion(endScreen, false);
                _hud.UpdateCombo(_combo);
                _hud.ShowHitFeedback(false, 0);
            }
        }

        private void OnManualBulletArrived(Vector2 startScreen, Vector2 endScreen, Zombie target)
        {
            if (_zombieSpawner == null) return;

            bool hit = _zombieSpawner.TryHitZombie(target, out Vector2 hitPos, out int killScore);
            if (hit)
            {
                _aimController.ShowExplosion(hitPos, true);
                if (killScore > 0)
                {
                    _kills++;
                    _combo++;
                    int comboBonus = Mathf.Max(0, _combo - 1) * 40;
                    int points = killScore + comboBonus;
                    _score += points;
                    _hud.UpdateScore(_score);
                    _hud.UpdateCombo(_combo);
                    _hud.ShowHitFeedback(true, points);
                    ClearManualTarget(true);
                }
                else
                {
                    _hud.ShowHitFeedback(true, 0);
                    if (target != null && target.gameObject.activeInHierarchy && !target.IsDead)
                    {
                        _manualTarget = target;
                        _aimController.SetLockedTarget(true, target.GetComponent<RectTransform>().position);
                    }
                }
            }
            else
            {
                _combo = 0;
                _aimController.ShowExplosion(endScreen, false);
                _hud.UpdateCombo(_combo);
                _hud.ShowHitFeedback(false, 0);
                ClearManualTarget(true);
            }
        }

        private void OnZombieReachedBottom(Zombie zombie)
        {
            if (_phase != Phase.Playing || _lives <= 0) return;

            int damage = zombie != null && zombie.Type == ZombieType.Boss ? 2 : 1;
            if (ManualTapControl && zombie != null && zombie == _manualTarget)
                ClearManualTarget(true);

            _lives = Mathf.Max(0, _lives - damage);
            _combo = 0;
            _hud.UpdateLives(_lives);
            _hud.UpdateCombo(_combo);
            _aimController.ShowDamageFlash();
        }

        private void OnWaveChanged(int wave, int stage)
        {
            if (_hud != null) _hud.UpdateWave(wave, stage);
        }

        private void ClearStatus()
        {
            if (_hud != null) _hud.ShowStatus("");
        }

        private void EndGame()
        {
            if (_phase == Phase.GameOver) return;

            _phase = Phase.GameOver;
            _zombieSpawner.StopSpawning();
            float accuracy = _shots > 0 ? (float)_kills / _shots * 100f : 0f;
            _hud.ShowStatus($"游戏结束\n分数: {_score}\n击杀: {_kills}\n命中率: {accuracy:F0}%");
            Invoke(nameof(ShowEndButtons), 1.2f);
        }

        private void ShowEndButtons()
        {
            if (_endActions != null)
                _endActions.SetActive(true);
        }

        private void HideEndButtons()
        {
            if (_endActions != null)
                _endActions.SetActive(false);
        }

        private void OnBackClicked()
        {
            Debug.Log("[CannonGame] Back clicked");
            Cleanup();
            if (UIManager.Instance != null)
                UIManager.Instance.OnClickBackToMenu();
            else if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        private void CleanupSessionOnly()
        {
            CancelInvoke();
            if (_poseSubscribed && _poseProvider != null)
            {
                _poseProvider.OnPoseFrame -= OnPoseFrame;
                _poseProvider.OnPoseError -= OnPoseError;
                _poseProvider.StopDetection();
                _poseSubscribed = false;
            }

            if (_zombieSpawner != null) _zombieSpawner.StopSpawning();
            _manualTarget = null;
            HideBulletPool();
            HideEndButtons();
            if (_tutorialPanel != null) _tutorialPanel.SetActive(false);

#if UNITY_EDITOR || !UNITY_ANDROID
            if (_mediaPipeSolutionStarted && _mediaPipeRunner != null)
            {
                try { _mediaPipeRunner.Stop(); }
                catch (System.Exception e) { Debug.LogWarning($"[CannonGame] MediaPipe stop ignored: {e.Message}"); }
            }
            if (_mediaPipeSolutionGo != null)
                _mediaPipeSolutionGo.SetActive(false);
            _mediaPipeSolutionStarted = false;
            _mediaPipeRunner = null;
            _mediaPipeSolutionGo = null;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            // Release the camera so the next round can re-open it. Without this the
            // WebCamTexture stays bound to the device and the second session's
            // camera never produces frames, leaving the game stuck on WaitingPose.
            if (_cameraPreview != null)
                _cameraPreview.StopCamera();
#endif

            _poseProvider = null;
            _mlKitPoseProvider = null;
            _poseReadyFrames = 0;
            _lastPoseFrameTime = 0f;
        }

        private void Cleanup()
        {
            CleanupSessionOnly();
            _phase = Phase.Init;
            if (_rootGo != null) _rootGo.SetActive(false);
        }

        private void HideBulletPool()
        {
            foreach (var bullet in _bulletPool)
            {
                if (bullet != null)
                    bullet.gameObject.SetActive(false);
            }
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            return FindTransform(root, name)?.GetComponent<RectTransform>();
        }

        private static Transform FindTransform(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindTransform(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void OnDestroy()
        {
            CleanupSessionOnly();
        }
    }
}
