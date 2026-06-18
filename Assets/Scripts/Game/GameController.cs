using UnityEngine;
using LoseWeight.Core;
using LoseWeight.Combat;
using LoseWeight.PoseDetection;
using LoseWeight.PoseDetection.Providers;

namespace LoseWeight.Game
{
    public class GameController : MonoBehaviour
    {
        private IPoseProvider _poseProvider;
        private MLKitPoseProvider _mlKitPoseProvider;
        private ActionDetector _actionDetector;
        private CombatManager _combatManager;
        private GameManager _gameManager;
        private CombatSceneBuilder _sceneBuilder;
        private bool _combatRuntimeStarted;
        private bool _poseRuntimeStarted;
        private bool _poseDetected;
        private int _calibrationFrames;
        private string _calibrationHint = "请站在摄像头前...";

        private const int CalibrationFramesNeeded = 20;

        private enum GamePhase { WaitingForPose, Calibrating, Fighting, RoundEnd, MatchEnd }
        private GamePhase _phase = GamePhase.WaitingForPose;

        private void Awake()
        {
            _gameManager = GameManager.Instance ?? SceneNodeResolver.FindRequiredComponent<GameManager>("AppSceneRoot/RuntimeSystems/GameManager");
            _combatManager = CombatManager.Instance ?? SceneNodeResolver.FindRequiredComponent<CombatManager>("AppSceneRoot/RuntimeSystems/CombatManager");
            _sceneBuilder = GetComponent<CombatSceneBuilder>() ?? SceneNodeResolver.FindRequiredComponent<CombatSceneBuilder>("AppSceneRoot/RuntimeSystems/CombatManager");

            var poseGo = SceneNodeResolver.FindRequired("AppSceneRoot/RuntimeSystems/PoseSystem");
            if (poseGo != null)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                _mlKitPoseProvider = poseGo.GetComponent<MLKitPoseProvider>();
                _poseProvider = _mlKitPoseProvider;
#else
                _poseProvider = poseGo.GetComponent<MediaPipePoseProvider>();
#endif
                _actionDetector = poseGo.GetComponent<ActionDetector>();
            }
        }

        private void Start()
        {
            if (_poseProvider == null || _actionDetector == null || _gameManager == null || _combatManager == null || _sceneBuilder == null)
            {
                Debug.LogError("[GameController] MCP runtime systems are incomplete.");
                return;
            }

            _poseProvider.OnPoseFrame += OnPoseFrame;
            _poseProvider.OnPoseError += OnPoseError;
            EventBus.Subscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Subscribe<DefendEvent>(OnDefend);
            EventBus.Subscribe<DodgeEvent>(OnDodge);
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<RoundEndEvent>(OnRoundEnd);
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
            EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);

            if (_gameManager.CurrentState == GameState.None)
                _gameManager.ChangeState(GameState.Loading);
        }

        private void OnStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Combat)
            {
                StartCombatRuntime();
            }
            else if (evt.NewState == GameState.MatchEnd || evt.NewState == GameState.MainMenu)
            {
                StopPoseRuntime();
                _sceneBuilder.DestroyScene();
                _combatRuntimeStarted = false;
                _phase = GamePhase.WaitingForPose;
            }
        }

        private void StartCombatRuntime()
        {
            if (_combatRuntimeStarted) return;
            _combatRuntimeStarted = true;
            StartPoseRuntime();
            _sceneBuilder.ActivateScene();
            _poseDetected = false;
            _calibrationFrames = 0;
            _calibrationHint = "请站在摄像头前...";
            _phase = GamePhase.WaitingForPose;
            ShowStatus(_calibrationHint);
        }

        private void StartPoseRuntime()
        {
            if (_poseRuntimeStarted || _poseProvider == null) return;
            _poseProvider.StartDetection(new PoseRuntimeOptions
            {
                TargetFps = 15,
                UseFrontCamera = true,
                MirrorOutput = true
            });
            _poseRuntimeStarted = true;
        }

        private void StopPoseRuntime()
        {
            if (!_poseRuntimeStarted || _poseProvider == null) return;
            _poseProvider.StopDetection();
            _poseRuntimeStarted = false;
        }

        private void Update()
        {
#if UNITY_EDITOR
            HandleDebugInput();
#endif
            if (_gameManager == null || _gameManager.CurrentState != GameState.Combat) return;
            if (!_combatRuntimeStarted)
            {
                StartCombatRuntime();
                return;
            }

            if (_phase == GamePhase.WaitingForPose && _poseDetected)
            {
                _phase = GamePhase.Calibrating;
                _calibrationFrames = 0;
                ShowStatus("检测到有效姿态，请保持不动...");
            }
            else if (_phase == GamePhase.Calibrating)
            {
                ShowStatus($"{_calibrationHint}\n校准中 ({_calibrationFrames}/{CalibrationFramesNeeded})");
                if (_calibrationFrames >= CalibrationFramesNeeded)
                {
                    _phase = GamePhase.Fighting;
                    StartFight();
                }
            }
            else if (_phase == GamePhase.Fighting)
            {
                UpdateHUD();
            }
        }

        private void OnPoseFrame(PoseFrame frame)
        {
            if (_phase == GamePhase.WaitingForPose)
            {
                if (_actionDetector.CanCalibrate(frame, out string waitReason))
                {
                    _poseDetected = true;
                    _calibrationHint = "检测到有效姿态，请保持不动";
                }
                else
                {
                    _poseDetected = false;
                    _calibrationHint = waitReason;
                    ShowStatus(waitReason);
                }
                return;
            }

            if (_phase == GamePhase.Calibrating)
            {
                if (_actionDetector.CanCalibrate(frame, out string reason))
                {
                    _calibrationFrames++;
                    _calibrationHint = reason;
                    if (_calibrationFrames == CalibrationFramesNeeded)
                        _actionDetector.Calibrate(frame);
                }
                else
                {
                    _calibrationFrames = Mathf.Max(0, _calibrationFrames - 1);
                    _calibrationHint = reason;
                }
            }
            else if (_phase == GamePhase.Fighting)
            {
                _actionDetector.ProcessFrame(frame);
            }
        }

        private void OnPoseError(string error)
        {
            Debug.LogWarning($"[GameController] Pose error: {error}");
            ShowStatus($"Pose error: {error}");
        }

        private void StartFight()
        {
            ShowStatus("Fight!");
            _combatManager.StartMatch(CombatMode.AI);
            _combatManager.BeginMatchAfterPoseReady();
            Invoke(nameof(ClearStatus), 1.5f);
        }

        private void ClearStatus() => ShowStatus("");

        private void UpdateHUD()
        {
            var player = _combatManager.GetPlayerData();
            var opponent = _combatManager.GetOpponentData();
            if (player == null || opponent == null) return;
            _sceneBuilder.UpdateHUD(player.Hp, opponent.Hp, _combatManager.CurrentRound, _combatManager.RoundTimer);
        }

        private void OnPunch(PunchDetectedEvent evt)
        {
            if (_gameManager.CurrentState != GameState.Combat) return;
            string actionName = $"{evt.Type} / {evt.Power}";
            _sceneBuilder.ShowActionFeedback(actionName);
            _sceneBuilder.ShowPoseBroadcast(actionName, $"Speed: {evt.Speed:F2}");
        }

        private void OnDefend(DefendEvent evt)
        {
            if (_gameManager.CurrentState != GameState.Combat) return;
            string actionName = evt.IsActive ? "防守" : "解除防守";
            _sceneBuilder.ShowActionFeedback(evt.IsActive ? actionName : "");
            _sceneBuilder.ShowPoseBroadcast(actionName, null);
        }

        private void OnDodge(DodgeEvent evt)
        {
            if (_gameManager.CurrentState != GameState.Combat) return;
            string actionName = $"闪避 / {evt.Direction}";
            _sceneBuilder.ShowActionFeedback(actionName);
            _sceneBuilder.ShowPoseBroadcast(actionName, null);
        }

        private void OnDamage(DamageEvent evt)
        {
            if (_gameManager.CurrentState == GameState.Combat && evt.TargetId == "opponent")
                _sceneBuilder.OpponentHit();
        }

        private void ShowStatus(string text)
        {
            _sceneBuilder?.ShowStatus(text);
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
            _gameManager.ChangeState(GameState.MatchEnd);
        }

#if UNITY_EDITOR
        private void HandleDebugInput()
        {
            if (_phase != GamePhase.Fighting) return;
            if (Input.GetKeyDown(KeyCode.J))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftStraight, Power = PunchPower.Light, Speed = 0.5f });
            if (Input.GetKeyDown(KeyCode.K))
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Light, Speed = 0.5f });
            if (Input.GetKeyDown(KeyCode.G))
                EventBus.Publish(new DefendEvent { IsActive = true });
            if (Input.GetKeyUp(KeyCode.G))
                EventBus.Publish(new DefendEvent { IsActive = false });
            if (Input.GetKeyDown(KeyCode.A))
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Left });
            if (Input.GetKeyDown(KeyCode.D))
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Right });
        }
#endif

        private void OnDestroy()
        {
            if (_poseProvider != null)
            {
                _poseProvider.OnPoseFrame -= OnPoseFrame;
                _poseProvider.OnPoseError -= OnPoseError;
                StopPoseRuntime();
            }
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Unsubscribe<DefendEvent>(OnDefend);
            EventBus.Unsubscribe<DodgeEvent>(OnDodge);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
            EventBus.Unsubscribe<RoundEndEvent>(OnRoundEnd);
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }
    }
}
