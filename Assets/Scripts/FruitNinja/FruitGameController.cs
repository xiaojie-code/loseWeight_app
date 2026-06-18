using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.PoseDetection;
using LoseWeight.PoseDetection.Providers;
using LoseWeight.Game;

namespace LoseWeight.FruitNinja
{
    public class FruitGameController : MonoBehaviour
    {
        private enum Phase { Init, Countdown, Playing, GameOver }

        private Phase _phase = Phase.Init;
        private float _gameTimer = 60f;
        private int _score;
        private int _combo;
        private float _lastSliceTime;
        private int _lives = 3;
        private float _countdownTimer;

        private SliceDetector _sliceDetector;
        private FruitSpawner _spawner;
        private FruitNinjaHUD _hud;
        private BladeTrail _leftBlade;
        private GameObject _rootGo;
        private RectTransform _canvasRT;
        private MLKitPoseProvider _poseProvider;
        private MobileCameraPreview _cameraPreview;
        private PoseOverlayUI _poseOverlay;
        private bool _poseSubscribed;
        private bool _bound;

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
            if (evt.NewState == GameState.FruitNinja)
                StartGame();
            else if (evt.OldState == GameState.FruitNinja || evt.OldState == GameState.FruitNinjaEnd)
                Cleanup();
        }

        private void StartGame()
        {
            CleanupSessionOnly();
            _rootGo = SceneNodeResolver.FindRequired("AppSceneRoot/FruitNinjaRoot");
            if (_rootGo != null) _rootGo.SetActive(true);
            if (!BindScene())
            {
                if (_rootGo != null) _rootGo.SetActive(false);
                return;
            }

            _phase = Phase.Countdown;
            _countdownTimer = 3f;
            _score = 0;
            _combo = 0;
            _lives = 3;
            _gameTimer = 60f;
            _hud.UpdateScore(0);
            _hud.UpdateLives(_lives);
            _hud.UpdateCombo(0);
            _hud.ShowStatus("3");
            if (_sliceDetector != null) _sliceDetector.Reset();
            StartPoseDetection();
        }

        private bool BindScene()
        {
            if (_bound && _rootGo != null) return true;

            _rootGo = SceneNodeResolver.FindRequired("AppSceneRoot/FruitNinjaRoot");
            if (_rootGo == null) return false;
            _canvasRT = _rootGo.GetComponent<RectTransform>();
            _sliceDetector = GetComponent<SliceDetector>();
            _spawner = GetComponent<FruitSpawner>();
            _hud = GetComponent<FruitNinjaHUD>();
            _leftBlade = GetComponent<BladeTrail>();

            var gameArea = FindChild(_rootGo.transform, "GameArea")?.GetComponent<RectTransform>();
            if (_canvasRT == null || gameArea == null || _sliceDetector == null || _spawner == null || _hud == null || _leftBlade == null)
            {
                Debug.LogError("[FruitNinja] MCP scene is incomplete.");
                return false;
            }

            _spawner.SpawnArea = gameArea;
            _spawner.BindPool(_rootGo.transform);
            _spawner.OnFruitMissed = OnFruitMissed;
            _hud.Initialize(_canvasRT);
            _leftBlade.Initialize(_canvasRT, FindChild(_rootGo.transform, "LeftBlade"), new Color(0f, 0.9f, 1f, 0.9f));

            var back = FindChild(_rootGo.transform, "BackBtn")?.GetComponent<Button>();
            if (back != null)
            {
                back.onClick.RemoveListener(OnBackClicked);
                back.onClick.AddListener(OnBackClicked);
            }

            BindCameraPreview();
            _bound = true;
            return true;
        }

        private void BindCameraPreview()
        {
            var preview = FindChild(_rootGo.transform, "CameraPreview");
            var rawImage = FindChild(_rootGo.transform, "CamTexture")?.GetComponent<RawImage>();
            _poseOverlay = FindChild(_rootGo.transform, "PoseOverlay")?.GetComponent<PoseOverlayUI>();
            _cameraPreview = preview != null ? preview.GetComponent<MobileCameraPreview>() : null;
            if (_cameraPreview != null && rawImage != null) _cameraPreview.Initialize(rawImage);
            if (_poseOverlay != null) _poseOverlay.Initialize(_poseOverlay.GetComponent<RectTransform>());
        }

        private void StartPoseDetection()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

            var poseGo = SceneNodeResolver.FindRequired("AppSceneRoot/RuntimeSystems/PoseSystem");
            _poseProvider = poseGo != null ? poseGo.GetComponent<MLKitPoseProvider>() : null;
            if (_poseProvider == null)
            {
                Debug.LogError("[FruitNinja] Missing MCP MLKitPoseProvider.");
                return;
            }
            _poseProvider.OnPoseFrame += OnPoseFrame;
            _poseSubscribed = true;
            Invoke(nameof(InitMLKit), 1.5f);
#endif
        }

        private void InitMLKit()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_cameraPreview != null && _cameraPreview.IsRunning && _poseProvider != null)
                _poseProvider.Initialize(_cameraPreview.CamTexture);
            else
                Invoke(nameof(InitMLKit), 0.5f);
#endif
        }

        private void OnPoseFrame(PoseFrame frame)
        {
            if (_phase != Phase.Playing && _phase != Phase.Countdown) return;
            if (_sliceDetector == null) return;
            if (_poseOverlay != null) _poseOverlay.UpdatePose(frame);

            var lw = frame.GetLandmark(PoseLandmarkIndex.LeftWrist);
            var rw = frame.GetLandmark(PoseLandmarkIndex.RightWrist);
            _sliceDetector.UpdateHands(lw.Position, rw.Position, lw.Confidence, rw.Confidence);
            _leftBlade?.UpdatePosition(_sliceDetector.LeftHandScreen, _sliceDetector.LeftHandValid);
        }

        private void Update()
        {
            if (_phase == Phase.Init) return;

#if UNITY_EDITOR || !UNITY_ANDROID
            if (Input.GetMouseButton(0) && _sliceDetector != null)
            {
                _sliceDetector.UpdateFromMouse(Input.mousePosition);
                _leftBlade?.UpdatePosition(Input.mousePosition, true);
            }
#endif

            if (_phase == Phase.Countdown)
            {
                _countdownTimer -= Time.deltaTime;
                if (_countdownTimer > 0)
                {
                    _hud.ShowStatus(Mathf.CeilToInt(_countdownTimer).ToString());
                }
                else
                {
                    _hud.ShowStatus("GO!");
                    _phase = Phase.Playing;
                    _spawner.StartSpawning();
                    Invoke(nameof(ClearGoText), 0.5f);
                }
            }
            else if (_phase == Phase.Playing)
            {
                _gameTimer -= Time.deltaTime;
                _hud.UpdateTimer(_gameTimer);
                if (_gameTimer <= 0 || _lives <= 0)
                {
                    EndGame();
                    return;
                }
                CheckCollisions();
            }
        }

        private void ClearGoText() => _hud.ShowStatus("");

        private void CheckCollisions()
        {
            var fruits = _spawner.GetActiveFruits();
            foreach (var fruit in fruits)
            {
                if (fruit == null || fruit.IsSliced) continue;
                RectTransform fruitRT = fruit.GetComponent<RectTransform>();
                Vector2 screenPos = fruitRT.position;
                bool hitLeft = _sliceDetector.CheckSlice(screenPos, fruit.CollisionRadius, true);
                bool hitRight = _sliceDetector.CheckSlice(screenPos, fruit.CollisionRadius, false);
                if (!hitLeft && !hitRight) continue;

                fruit.Slice();
                if (fruit.IsBomb)
                {
                    _lives--;
                    _combo = 0;
                    _hud.UpdateLives(_lives);
                    _hud.UpdateCombo(0);
                }
                else
                {
                    _combo = Time.time - _lastSliceTime < 0.5f ? _combo + 1 : 1;
                    _lastSliceTime = Time.time;
                    _score += fruit.Score + (_combo > 1 ? 5 * _combo : 0);
                    _hud.UpdateScore(_score);
                    _hud.UpdateCombo(_combo);
                }
            }
        }

        public void OnFruitMissed()
        {
            if (_phase != Phase.Playing || _lives <= 0) return;
            _lives--;
            _combo = 0;
            _hud.UpdateLives(_lives);
            _hud.UpdateCombo(0);
        }

        private void EndGame()
        {
            _phase = Phase.GameOver;
            _spawner.StopSpawning();
            _spawner.ClearAll();
            _leftBlade?.Hide();
            _hud.ShowStatus($"游戏结束\n得分: {_score}");
        }

        private void OnBackClicked()
        {
            Cleanup();
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        private void CleanupSessionOnly()
        {
            CancelInvoke();
            if (_poseSubscribed && _poseProvider != null)
            {
                _poseProvider.OnPoseFrame -= OnPoseFrame;
                _poseProvider.StopDetection();
                _poseSubscribed = false;
            }
            _spawner?.StopSpawning();
            _spawner?.ClearAll();
            _leftBlade?.Hide();
            _poseProvider = null;
            _phase = Phase.Init;
        }

        private void Cleanup()
        {
            CleanupSessionOnly();
            if (_rootGo != null) _rootGo.SetActive(false);
        }

        private void OnDestroy()
        {
            CleanupSessionOnly();
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
