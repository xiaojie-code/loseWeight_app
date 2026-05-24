using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.Combat;
using LoseWeight.PoseDetection;
using LoseWeight.PoseDetection.Providers;

namespace LoseWeight.Game
{
    public class GameController : MonoBehaviour
    {
        [Header("UI (Optional)")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _actionText;

        private Text _opponentHpText;
        private Text _poseBroadcastText;
        private bool _controllerHudLogged;

        private MediaPipePoseProvider _poseProvider;
        private ActionDetector _actionDetector;
        private CombatManager _combatManager;
        private GameManager _gameManager;
        private CombatSceneBuilder _sceneBuilder;
        private PoseOverlayUI _poseOverlay;
        private GameObject _gameHudRoot;

        private bool _poseDetected;
        private int _calibrationFrames;
        private const int CALIBRATION_FRAMES_NEEDED = 15;

        private enum GamePhase { WaitingForPose, Calibrating, Fighting, RoundEnd, MatchEnd }
        private GamePhase _phase = GamePhase.WaitingForPose;

        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            _gameManager = GameManager.Instance;

            if (CombatManager.Instance == null)
            {
                var go = new GameObject("CombatManager");
                go.AddComponent<CombatManager>();
                go.AddComponent<ComboSystem>();
                go.AddComponent<CombatStatistics>();
            }
            _combatManager = CombatManager.Instance;

            // \u521d\u59cb\u5316 UI \u7ba1\u7406\u5668
            if (LoseWeight.UI.UIManager.Instance == null)
            {
                var uiGo = new GameObject("UIManager");
                uiGo.AddComponent<LoseWeight.UI.UIManager>();
            }
        }

        private void Start()
        {
            var poseGo = new GameObject("PoseSystem");
            _poseProvider = poseGo.AddComponent<MediaPipePoseProvider>();
            _actionDetector = poseGo.AddComponent<ActionDetector>();
            _poseProvider.OnPoseFrame += OnPoseFrame;

            // 启动姿态检测
            _poseProvider.StartDetection(new PoseRuntimeOptions
            {
                TargetFps = 15,
                UseFrontCamera = true,
                MirrorOutput = true
            });
            Debug.Log("[GameController] Pose detection started");

            _sceneBuilder = gameObject.AddComponent<CombatSceneBuilder>();

            EventBus.Subscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Subscribe<DefendEvent>(OnDefend);
            EventBus.Subscribe<DodgeEvent>(OnDodge);
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<RoundEndEvent>(OnRoundEnd);
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
            EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);

            // 进入加载状态
            _gameManager.ChangeState(GameState.Loading);
        }

        /// <summary>
        /// 禁用 MediaPipe Unity Plugin（在 Android 上不工作）
        /// </summary>
        private void DisableMediaPipeOnAndroid()
        {
            var solution = GameObject.Find("Solution");
            if (solution != null) solution.SetActive(false);
            var bootstrap = GameObject.Find("Bootstrap");
            if (bootstrap != null) bootstrap.SetActive(false);
            var mainCanvas = GameObject.Find("Main Canvas");
            if (mainCanvas != null) mainCanvas.SetActive(false);
            ScreenDebugLog.Log("MediaPipe disabled on Android");
        }

        /// <summary>
        /// Android 上的摄像头+姿态检测启动路径
        /// </summary>
        private void StartAndroidCameraPath()
        {
            ScreenDebugLog.Log("Starting Android camera path");
            CreateMobileCameraPreview();
            _cameraPreviewFixed = true;
            // 服务端姿态检测延迟启动（等摄像头就绪）
            Invoke(nameof(StartServerPoseDetection), 2f);
        }

        private void OnStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Combat)
            {
                // 清理上一局可能残留的对象（再玩一次时）
                CleanupCombatResources();

                // 从菜单进入对战，构建场景和 HUD
                _sceneBuilder.BuildScene();
                EnsureControllerHUD();
                _poseDetected = false;
                _phase = GamePhase.WaitingForPose;
                ShowStatus("\u8bf7\u7ad9\u5728\u6444\u50cf\u5934\u524d...");

                // 初始化屏幕调试日志
                var debugLog = _gameHudRoot.GetComponent<ScreenDebugLog>();
                if (debugLog == null) debugLog = _gameHudRoot.AddComponent<ScreenDebugLog>();
                debugLog.Initialize(_gameHudRoot.transform);
                ScreenDebugLog.Log("Combat started");
                ScreenDebugLog.Log($"Platform: {Application.platform}");

                // Android 上启动独立摄像头 + 服务端姿态检测
                if (Application.platform == RuntimePlatform.Android)
                {
                    ScreenDebugLog.Log("Path: Android (Mobile cam + Server pose)");
                    DisableMediaPipeOnAndroid();
                    Invoke(nameof(StartAndroidCameraPath), 0.5f);
                }
                else
                {
                    ScreenDebugLog.Log("Path: Editor (MediaPipe Plugin)");
                    InvokeRepeating(nameof(TryFixCameraPreview), 1f, 2f);
                }
            }
            else if (evt.NewState == GameState.MatchEnd)
            {
                // \u5bf9\u6218\u7ed3\u675f\uff0c\u9690\u85cf GameHUD
                if (_gameHudRoot != null) _gameHudRoot.SetActive(false);
            }
            else if (evt.NewState == GameState.MainMenu)
            {
                // \u8fd4\u56de\u83dc\u5355\uff0c\u6e05\u7406\u573a\u666f\u548c HUD
                CleanupCombatResources();
                _sceneBuilder.DestroyScene();
                if (_gameHudRoot != null) _gameHudRoot.SetActive(false);
                _phase = GamePhase.WaitingForPose;
            }
        }

        /// <summary>
        /// 清理对战相关资源（摄像头预览、姿态覆盖层、服务端检测器）
        /// </summary>
        private void CleanupCombatResources()
        {
            // 清理摄像头预览（连带子节点的 PoseOverlay 也会销毁）
            var existingPreview = _gameHudRoot != null
                ? _gameHudRoot.transform.Find("CameraPreview")
                : null;
            if (existingPreview != null) Destroy(existingPreview.gameObject);

            // 清理服务端姿态检测器
            var existingDetector = GetComponent<LoseWeight.PoseDetection.Providers.ServerPoseDetector>();
            if (existingDetector != null)
            {
                existingDetector.Stop();
                Destroy(existingDetector);
            }

            // 重置引用
            _cameraPreview = null;
            _poseOverlay = null;
            _cameraPreviewFixed = false;
            CancelInvoke(nameof(TryFixCameraPreview));
            CancelInvoke(nameof(StartAndroidCameraPath));
            CancelInvoke(nameof(StartServerPoseDetection));
        }

        private void Update()
        {
#if UNITY_EDITOR
            HandleDebugInput();
#endif
            // \u53ea\u6709\u5728 Combat \u72b6\u6001\u4e0b\u624d\u8fd0\u884c\u5bf9\u6218\u903b\u8f91
            if (_gameManager.CurrentState != GameState.Combat) return;

            switch (_phase)
            {
                case GamePhase.WaitingForPose:
                    if (_poseDetected)
                    {
                        _phase = GamePhase.Calibrating;
                        _calibrationFrames = 0;
                        ShowStatus("\u68c0\u6d4b\u5230\u59ff\u6001\uff0c\u8bf7\u4fdd\u6301\u4e0d\u52a8...");
                    }
                    break;

                case GamePhase.Calibrating:
                    string calibrationStatus = $"\u6821\u51c6\u4e2d ({_calibrationFrames}/{CALIBRATION_FRAMES_NEEDED})";
                    ShowStatus(calibrationStatus);
                    if (_calibrationFrames >= CALIBRATION_FRAMES_NEEDED)
                    {
                        _phase = GamePhase.Fighting;
                        StartFight();
                    }
                    break;

                case GamePhase.Fighting:
                    EnsureControllerHUD();
                    UpdateHUD();
                    break;
            }
        }

#if UNITY_EDITOR
        private void HandleDebugInput()
        {
            if (_phase != GamePhase.Fighting) return;

            if (Input.GetKeyDown(KeyCode.J))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftStraight, Power = PunchPower.Light, Speed = 0.5f });
            if (Input.GetKeyDown(KeyCode.K))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Light, Speed = 0.5f });
            if (Input.GetKeyDown(KeyCode.U))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftUppercut, Power = PunchPower.Heavy, Speed = 0.8f });
            if (Input.GetKeyDown(KeyCode.I))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightUppercut, Power = PunchPower.Heavy, Speed = 0.8f });
            if (Input.GetKeyDown(KeyCode.G))
                EventBus.Publish(new DefendEvent { IsActive = true });
            if (Input.GetKeyUp(KeyCode.G))
                EventBus.Publish(new DefendEvent { IsActive = false });
            if (Input.GetKeyDown(KeyCode.A))
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Left });
            if (Input.GetKeyDown(KeyCode.D))
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Right });
            if (Input.GetKeyDown(KeyCode.S))
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Down });
        }
#endif

        private void OnPoseFrame(PoseFrame frame)
        {
            if (!_poseDetected)
            {
                ScreenDebugLog.Log($"First pose frame! landmarks={frame.Landmarks?.Length}");
            }
            _poseDetected = true;

            // 更新关键点覆盖层
            if (_poseOverlay != null) _poseOverlay.UpdatePose(frame);

            if (_phase == GamePhase.Calibrating)
            {
                _calibrationFrames++;
                if (_calibrationFrames == CALIBRATION_FRAMES_NEEDED)
                {
                    _actionDetector.Calibrate(frame);
                    ScreenDebugLog.Log("Calibration done, starting fight");
                }
            }
            else if (_phase == GamePhase.Fighting)
            {
                _actionDetector.ProcessFrame(frame);
            }
        }

        private void StartFight()
        {
            ShowStatus("Fight!");
            _gameManager.ChangeState(GameState.Combat);
            _combatManager.StartMatch(CombatMode.AI);
            _combatManager.BeginMatchAfterPoseReady();
            Invoke(nameof(ClearStatus), 1.5f);
            Debug.Log("[GameController] Fight started!");
        }

        private void ClearStatus()
        {
            ShowStatus("");
        }

        private void UpdateHUD()
        {
            if (_combatManager == null) return;
            var player = _combatManager.GetPlayerData();
            var opponent = _combatManager.GetOpponentData();
            if (player == null || opponent == null) return;

            _sceneBuilder.UpdateHUD(player.Hp, opponent.Hp, _combatManager.CurrentRound, _combatManager.RoundTimer);
            UpdateControllerHUD(player.Hp, opponent.Hp, _combatManager.CurrentRound, _combatManager.RoundTimer);
        }

        private void OnPunch(PunchDetectedEvent evt)
        {
            string actionName = $"{GetPunchName(evt.Type)} / {GetPowerName(evt.Power)}";
            Debug.Log($"[GameController] Player action {actionName}, speed={evt.Speed:F2}");
            _sceneBuilder.ShowActionFeedback(actionName);
            ShowControllerAction(actionName);
            ShowControllerBroadcast(actionName, $"Speed: {evt.Speed:F2}");
        }

        private void OnDefend(DefendEvent evt)
        {
            string actionName = evt.IsActive ? "\u9632\u5fa1" : "\u89e3\u9664\u9632\u5fa1";
            Debug.Log($"[GameController] Player action {actionName}");
            _sceneBuilder.ShowActionFeedback(evt.IsActive ? actionName : "");
            ShowControllerAction(evt.IsActive ? actionName : "");
            ShowControllerBroadcast(actionName, null);
        }

        private void OnDodge(DodgeEvent evt)
        {
            string actionName = $"\u95ea\u907f / {GetDodgeName(evt.Direction)}";
            Debug.Log($"[GameController] Player action {actionName}");
            _sceneBuilder.ShowActionFeedback(actionName);
            ShowControllerAction(actionName);
            ShowControllerBroadcast(actionName, null);
        }

        private void ShowStatus(string text)
        {
            _sceneBuilder.ShowStatus(text);
            ShowControllerStatus(text);
        }

        private void EnsureControllerHUD()
        {
            if (_gameHudRoot == null)
            {
                _gameHudRoot = GameObject.Find("GameHUD");
                if (_gameHudRoot == null)
                {
                    _gameHudRoot = new GameObject("GameHUD");
                    _gameHudRoot.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }
            _gameHudRoot.SetActive(true);
            var canvas = _gameHudRoot.GetComponent<Canvas>(); if (canvas == null) { canvas = _gameHudRoot.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;

            // \u8bbe\u7f6e CanvasScaler \u9002\u914d\u7ad6\u5c4f
            var scaler = _gameHudRoot.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = _gameHudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (_gameHudRoot.GetComponent<GraphicRaycaster>() == null) _gameHudRoot.AddComponent<GraphicRaycaster>();

            // \u5de6\u4e0a\uff1a\u73a9\u5bb6HP\uff08\u52a0\u5927\u5b57\u53f7\uff0c\u4e0b\u79fb\u907f\u5f00\u72b6\u6001\u680f\uff09
            _hpText = EnsureControllerText(_hpText, _gameHudRoot.transform, "PlayerHP", "\u6211\u65b9 HP: 100", new Vector2(0, 1), new Vector2(20, -90), new Vector2(350, 50), TextAnchor.UpperLeft, 36, Color.white, FontStyle.Bold);
            // \u53f3\u4e0a\uff1a\u5bf9\u624bHP
            _opponentHpText = EnsureControllerText(_opponentHpText, _gameHudRoot.transform, "OpponentHP", "\u5bf9\u624b HP: 100", new Vector2(1, 1), new Vector2(-370, -90), new Vector2(350, 50), TextAnchor.UpperRight, 36, Color.white, FontStyle.Bold);
            // \u9876\u90e8\u4e2d\u95f4\uff1a\u56de\u5408/\u65f6\u95f4
            _timerText = EnsureControllerText(_timerText, _gameHudRoot.transform, "Timer", "R1 | 60s", new Vector2(0.5f, 1), new Vector2(-100, -90), new Vector2(280, 50), TextAnchor.UpperCenter, 34, Color.white, FontStyle.Normal);
            // \u4e2d\u95f4\uff1a\u72b6\u6001\u63d0\u793a
            _statusText = EnsureControllerText(_statusText, _gameHudRoot.transform, "Status", "", new Vector2(0.5f, 0.5f), new Vector2(-250, 0), new Vector2(500, 80), TextAnchor.MiddleCenter, 40, Color.white, FontStyle.Bold);
            // \u53f3\u4e2d\uff1a\u52a8\u4f5c\u64ad\u62a5
            _poseBroadcastText = EnsureControllerText(_poseBroadcastText, _gameHudRoot.transform, "PoseBroadcast", "", new Vector2(1, 0.5f), new Vector2(-350, 40), new Vector2(330, 80), TextAnchor.MiddleRight, 32, new Color(0.4f, 1f, 1f), FontStyle.Bold);
            // \u53f3\u4e2d\u504f\u4e0b\uff1a\u52a8\u4f5c\u53cd\u9988
            _actionText = EnsureControllerText(_actionText, _gameHudRoot.transform, "Action", "", new Vector2(1, 0.5f), new Vector2(-350, -50), new Vector2(330, 60), TextAnchor.MiddleRight, 36, Color.yellow, FontStyle.Bold);

            if (!_controllerHudLogged)
            {
                Debug.Log($"[GameControllerHUD] Ensured HUD children: {_gameHudRoot.transform.childCount}");
                _controllerHudLogged = true;
            }

            // \u6682\u505c\u6309\u94ae\uff08\u53f3\u4e0a\u89d2\uff0c\u52a0\u5927\u4e0b\u79fb\uff09
            EnsurePauseButton();
        }

        private GameObject _pauseBtn;
        private GameObject _pausePanel;

        private void EnsurePauseButton()
        {
            if (_pauseBtn != null) return;

            _pauseBtn = new GameObject("PauseBtn");
            _pauseBtn.transform.SetParent(_gameHudRoot.transform, false);
            var rt = _pauseBtn.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -90);
            rt.sizeDelta = new Vector2(100, 60);
            var img = _pauseBtn.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);
            var btn = _pauseBtn.AddComponent<Button>();
            btn.onClick.AddListener(ShowPauseMenu);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(_pauseBtn.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = "\u2016";
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            txt.fontSize = 36;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private void ShowPauseMenu()
        {
            if (_pausePanel != null) { _pausePanel.SetActive(true); Time.timeScale = 0; return; }

            _pausePanel = new GameObject("PausePanel");
            _pausePanel.transform.SetParent(_gameHudRoot.transform, false);
            var rt = _pausePanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // \u534a\u900f\u660e\u80cc\u666f
            var bg = _pausePanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            // \u5f39\u6846
            var card = new GameObject("Card");
            card.transform.SetParent(_pausePanel.transform, false);
            var crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.15f, 0.35f); crt.anchorMax = new Vector2(0.85f, 0.65f);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            card.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            // \u6807\u9898
            var title = new GameObject("Title");
            title.transform.SetParent(card.transform, false);
            var trt = title.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.7f); trt.anchorMax = new Vector2(1, 0.95f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var ttxt = title.AddComponent<Text>();
            ttxt.text = "\u6682\u505c";
            ttxt.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            ttxt.fontSize = 32; ttxt.color = Color.white; ttxt.alignment = TextAnchor.MiddleCenter;
            ttxt.fontStyle = FontStyle.Bold;

            // \u7ee7\u7eed\u6e38\u620f
            CreatePauseButton(card.transform, "\u7ee7\u7eed\u6e38\u620f", new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.60f),
                new Color(0.2f, 0.6f, 1f), () => { _pausePanel.SetActive(false); Time.timeScale = 1; });

            // \u8fd4\u56de\u5927\u5385
            CreatePauseButton(card.transform, "\u8fd4\u56de\u5927\u5385", new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.30f),
                new Color(0.4f, 0.4f, 0.5f), () => { _pausePanel.SetActive(false); Time.timeScale = 1; _gameManager.ChangeState(GameState.MainMenu); });

            Time.timeScale = 0;
        }

        private void CreatePauseButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
            txt.fontSize = 26; txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }

        private Text EnsureControllerText(Text current, Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, TextAnchor alignment, int fontSize, Color color, FontStyle fontStyle)
        {
            if (current == null)
            {
                var existing = parent.Find(name);
                current = existing != null ? existing.GetComponent<Text>() : null;
            }

            if (current == null)
            {
                var textObject = new GameObject(name);
                textObject.transform.SetParent(parent, false);
                var rect = textObject.AddComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                current = textObject.AddComponent<Text>();
                current.text = text;
                current.font = Font.CreateDynamicFontFromOSFont("Arial", 14) ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            var currentRect = current.GetComponent<RectTransform>();
            currentRect.anchorMin = anchor;
            currentRect.anchorMax = anchor;
            currentRect.anchoredPosition = position;
            currentRect.sizeDelta = size;
            current.fontSize = fontSize;
            current.color = color;
            current.fontStyle = fontStyle;
            current.alignment = alignment;
            current.raycastTarget = false;
            current.horizontalOverflow = HorizontalWrapMode.Overflow;
            current.verticalOverflow = VerticalWrapMode.Overflow;
            return current;
        }

        private void UpdateControllerHUD(int playerHp, int opponentHp, int round, float timer)
        {
            EnsureControllerHUD();
            if (_hpText != null) _hpText.text = $"\u6211\u65b9 HP: {playerHp}";
            if (_opponentHpText != null) _opponentHpText.text = $"\u5bf9\u624b HP: {opponentHp}";
            if (_timerText != null) _timerText.text = $"R{round} | {Mathf.CeilToInt(timer)}s";
        }

        private void ShowControllerStatus(string text)
        {
            EnsureControllerHUD();
            if (_statusText != null) _statusText.text = text;
        }

        private void ShowControllerAction(string text)
        {
            EnsureControllerHUD();
            if (_actionText == null) return;
            _actionText.text = text;
            CancelInvoke(nameof(ClearControllerAction));
            Invoke(nameof(ClearControllerAction), 0.8f);
        }

        private void ShowControllerBroadcast(string actionName, string detail)
        {
            EnsureControllerHUD();
            if (_poseBroadcastText == null) return;
            _poseBroadcastText.text = string.IsNullOrEmpty(detail)
                ? $"\u52a8\u4f5c: {actionName}"
                : $"\u52a8\u4f5c: {actionName}\n{detail}";
            CancelInvoke(nameof(ClearControllerBroadcast));
            Invoke(nameof(ClearControllerBroadcast), 1.8f);
        }

        private void ClearControllerAction()
        {
            if (_actionText != null) _actionText.text = "";
        }

        private void ClearControllerBroadcast()
        {
            if (_poseBroadcastText != null) _poseBroadcastText.text = "";
        }

        private static string GetPunchName(PunchType type)
        {
            return type switch
            {
                PunchType.LeftStraight => "\u5de6\u76f4\u62f3",
                PunchType.RightStraight => "\u53f3\u76f4\u62f3",
                PunchType.LeftUppercut => "\u5de6\u52fe\u62f3",
                PunchType.RightUppercut => "\u53f3\u52fe\u62f3",
                _ => type.ToString()
            };
        }

        private static string GetPowerName(PunchPower power)
        {
            return power == PunchPower.Heavy ? "\u91cd" : "\u8f7b";
        }

        private static string GetDodgeName(DodgeDirection direction)
        {
            return direction switch
            {
                DodgeDirection.Left => "\u5de6\u95ea",
                DodgeDirection.Right => "\u53f3\u95ea",
                DodgeDirection.Down => "\u4e0b\u8e72",
                _ => direction.ToString()
            };
        }

        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId == "opponent")
            {
                _sceneBuilder.OpponentHit();
            }
            else if (evt.TargetId == "player")
            {
                // \u73a9\u5bb6\u53d7\u51fb\u7ea2\u8272\u95ea\u70c1\u6548\u679c
                ShowHitFlash();
            }
        }

        private GameObject _hitFlashPanel;

        private void ShowHitFlash()
        {
            if (_hitFlashPanel == null)
            {
                // \u521b\u5efa\u5168\u5c4f\u7ea2\u8272\u534a\u900f\u660e\u906e\u7f69
                var hudRoot = GameObject.Find("GameHUD");
                if (hudRoot == null) return;
                _hitFlashPanel = new GameObject("HitFlash");
                _hitFlashPanel.transform.SetParent(hudRoot.transform, false);
                var rt = _hitFlashPanel.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = _hitFlashPanel.AddComponent<Image>();
                img.color = new Color(1f, 0f, 0f, 0f);
                img.raycastTarget = false;
            }

            _hitFlashPanel.SetActive(true);
            var flashImg = _hitFlashPanel.GetComponent<Image>();
            flashImg.color = new Color(1f, 0f, 0f, 0.3f);
            StartCoroutine(FadeHitFlash(flashImg));
        }

        private System.Collections.IEnumerator FadeHitFlash(Image img)
        {
            float t = 0.3f;
            while (t > 0)
            {
                t -= Time.deltaTime * 2f;
                img.color = new Color(1f, 0f, 0f, Mathf.Max(0, t));
                yield return null;
            }
            img.color = new Color(1f, 0f, 0f, 0f);
        }

        private void OnRoundEnd(RoundEndEvent evt)
        {
            _phase = GamePhase.RoundEnd;
            ShowStatus($"Round End {evt.Score[0]}-{evt.Score[1]}");
            Invoke(nameof(ResumeRound), 2f);
        }

        private void ResumeRound()
        {
            _phase = GamePhase.Fighting;
            ShowStatus("");
        }

        private void OnMatchEnd(MatchEndEvent evt)
        {
            _phase = GamePhase.MatchEnd;
            // \u901a\u77e5 GameState \u53d8\u66f4\uff0cUIManager \u4f1a\u663e\u793a\u7ed3\u7b97\u9875
            _gameManager.ChangeState(GameState.MatchEnd);
        }

        private void HideMediaPipeCanvas()
        {
            var mainCanvas = GameObject.Find("Main Canvas");
            if (mainCanvas != null)
            {
                mainCanvas.SetActive(false);
                Debug.Log("[GameController] MediaPipe Main Canvas hidden on device");
            }
        }

        /// <summary>
        /// 真机上延迟检查：如果 MediaPipe 没有成功启动（没有收到姿态帧），就启动独立摄像头
        /// </summary>
        private void CheckAndStartMobileCamera()
        {
            ScreenDebugLog.Log($"CheckCamera: poseDetected={_poseDetected}");

            if (_poseDetected)
            {
                ScreenDebugLog.Log("MediaPipe OK");
                return;
            }

            // MediaPipe 没工作，才启动独立摄像头（避免摄像头冲突）
            ScreenDebugLog.Log("MediaPipe FAILED after 20s, starting mobile cam");
            HideMediaPipeCanvas();

            if (_cameraPreview == null)
            {
                CreateMobileCameraPreview();
                _cameraPreviewFixed = true;
                CancelInvoke(nameof(TryFixCameraPreview));
            }

            // 启动服务端姿态检测
            StartServerPoseDetection();
        }

        private void StartServerPoseDetection()
        {
            if (_cameraPreview == null || !_cameraPreview.IsRunning)
            {
                ScreenDebugLog.Log("ServerPose: cam not ready, retry");
                Invoke(nameof(StartServerPoseDetection), 1f);
                return;
            }

            // 局域网服务器地址（电脑 IP，手机和电脑必须在同一 WiFi）
            var serverUrl = "http://192.168.1.4:3000";

            var detector = gameObject.AddComponent<LoseWeight.PoseDetection.Providers.ServerPoseDetector>();
            detector.Initialize(_cameraPreview.CamTexture, serverUrl);
            detector.OnPoseFrame += OnPoseFrame;
            ScreenDebugLog.Log($"ServerPose started: {serverUrl}");
        }

        private bool _cameraPreviewFixed;
        private MobileCameraPreview _cameraPreview;

        private void TryFixCameraPreview()
        {
            if (_cameraPreviewFixed) { CancelInvoke(nameof(TryFixCameraPreview)); return; }

            // 尝试找到并调整 MediaPipe 的 Main Canvas
            var mainCanvas = GameObject.Find("Main Canvas");
            if (mainCanvas != null)
            {
                FixCameraPreviewPosition();
                _cameraPreviewFixed = true;
                CancelInvoke(nameof(TryFixCameraPreview));
                Debug.Log("[GameController] Camera preview fixed (MediaPipe Canvas)");
            }
        }

        /// <summary>
        /// 真机上直接创建摄像头预览（不依赖 MediaPipe UI）
        /// </summary>
        private void CreateMobileCameraPreview()
        {
            if (_gameHudRoot == null) EnsureControllerHUD();

            // 在 GameHUD 上创建预览区域
            var previewGo = new GameObject("CameraPreview");
            previewGo.transform.SetParent(_gameHudRoot.transform, false);
            var rt = previewGo.AddComponent<RectTransform>();
            // 左上角小窗
            rt.anchorMin = new Vector2(0, 0.75f);
            rt.anchorMax = new Vector2(0.25f, 1f);
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-5, -80);

            // 背景透明（去掉黑框）
            var bgImg = previewGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = Color.clear;

            // RawImage 显示摄像头画面
            var rawImgGo = new GameObject("CamTexture");
            rawImgGo.transform.SetParent(previewGo.transform, false);
            var rawImgRT = rawImgGo.AddComponent<RectTransform>();
            rawImgRT.anchorMin = Vector2.zero;
            rawImgRT.anchorMax = Vector2.one;
            rawImgRT.offsetMin = Vector2.zero;
            rawImgRT.offsetMax = Vector2.zero;
            var rawImage = rawImgGo.AddComponent<UnityEngine.UI.RawImage>();
            rawImage.color = Color.white;

            // 启动摄像头
            _cameraPreview = previewGo.AddComponent<MobileCameraPreview>();
            _cameraPreview.Initialize(rawImage);

            // 关键点覆盖层 - 作为 RawImage 的子节点，自动跟随同样的旋转/镜像
            if (_poseOverlay == null)
            {
                var overlayGo = new GameObject("PoseOverlay");
                overlayGo.transform.SetParent(rawImgGo.transform, false);
                var overlayRT = overlayGo.AddComponent<RectTransform>();
                overlayRT.anchorMin = Vector2.zero;
                overlayRT.anchorMax = Vector2.one;
                overlayRT.offsetMin = Vector2.zero;
                overlayRT.offsetMax = Vector2.zero;
                _poseOverlay = overlayGo.AddComponent<PoseOverlayUI>();
                _poseOverlay.Initialize(overlayRT);
            }
        }

        private void FixCameraPreviewPosition()
        {
            var mainCanvas = GameObject.Find("Main Canvas");
            if (mainCanvas != null)
            {
                var canvas = mainCanvas.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 50;
                }

                // \u4fee\u6b63 CanvasScaler \u4e3a\u7ad6\u5c4f\u9002\u914d
                var scaler = mainCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                var container = mainCanvas.transform.Find("Container Panel");
                if (container != null)
                {
                    var rt = container.GetComponent<RectTransform>();
                    // \u5de6\u4e0a\u89d2\u5c0f\u7a97
                    rt.anchorMin = new Vector2(0, 0.78f);
                    rt.anchorMax = new Vector2(0.22f, 1f);
                    rt.offsetMin = new Vector2(10, 10);
                    rt.offsetMax = new Vector2(-5, -80); // \u907f\u5f00\u72b6\u6001\u680f

                    // \u9690\u85cfHeader\u548cFooter
                    var header = container.Find("Header");
                    if (header != null) header.gameObject.SetActive(false);
                    var footer = container.Find("Footer");
                    if (footer != null) footer.gameObject.SetActive(false);
                }

                // \u7981\u7528 AutoFit\uff0c\u8ba9 Annotatable Screen stretch \u586b\u6ee1 Container Panel
                var annotatableScreen = mainCanvas.GetComponentInChildren<Mediapipe.Unity.Screen>();
                if (annotatableScreen != null)
                {
                    var autoFit = annotatableScreen.GetComponent<Mediapipe.Unity.AutoFit>();
                    if (autoFit != null) autoFit.enabled = false;

                    var screenRT = annotatableScreen.GetComponent<RectTransform>();
                    screenRT.anchorMin = Vector2.zero;
                    screenRT.anchorMax = Vector2.one;
                    screenRT.offsetMin = Vector2.zero;
                    screenRT.offsetMax = Vector2.zero;
                    screenRT.sizeDelta = Vector2.zero;

                    // \u68c0\u67e5\u7eb9\u7406\u72b6\u6001
                    var rawImg = annotatableScreen.GetComponent<UnityEngine.UI.RawImage>();
                    ScreenDebugLog.Log($"Screen tex: {(rawImg?.texture != null ? rawImg.texture.name : "NULL")}");

                    // \u521b\u5efa\u5173\u952e\u70b9\u8986\u76d6\u5c42
                    if (_poseOverlay == null)
                    {
                        var overlayGo = new GameObject("PoseOverlay");
                        overlayGo.transform.SetParent(screenRT, false);
                        var overlayRT = overlayGo.AddComponent<RectTransform>();
                        overlayRT.anchorMin = Vector2.zero;
                        overlayRT.anchorMax = Vector2.one;
                        overlayRT.offsetMin = Vector2.zero;
                        overlayRT.offsetMax = Vector2.zero;
                        _poseOverlay = overlayGo.AddComponent<PoseOverlayUI>();
                        _poseOverlay.Initialize(overlayRT);
                    }
                }
                else
                {
                    ScreenDebugLog.Log("Annotatable Screen NOT FOUND");
                }
            }
        }

        private void OnDestroy()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseFrame -= OnPoseFrame;
                _poseProvider.StopDetection();
            }
            EventBus.Clear();
        }
    }
}
