using UnityEngine;
using LoseWeight.Core;
using LoseWeight.Data;

namespace LoseWeight.Combat
{
    /// <summary>
    /// 对战管理器 - 管理整场比赛的生命周期
    /// BO3 回合制，每回合60秒，HP=100
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        // Runtime State
        public CombatState State { get; private set; } = CombatState.Idle;
        public int CurrentRound { get; private set; }
        public float RoundTimer { get; private set; }
        public int[] RoundWins { get; private set; } = new int[2]; // [player, opponent]
        public bool IsWaitingForPose => State == CombatState.WaitingForPose;
        public bool IsPausedForPose => State == CombatState.PoseLost;

        // 玩家数据
        private PlayerCombatData _player;
        private PlayerCombatData _opponent;
        private AICombatController _aiController;

        // 对战模式
        private CombatMode _mode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PunchDetectedEvent>(OnPunchDetected);
            EventBus.Subscribe<DefendEvent>(OnDefend);
            EventBus.Subscribe<DodgeEvent>(OnDodge);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPunchDetected);
            EventBus.Unsubscribe<DefendEvent>(OnDefend);
            EventBus.Unsubscribe<DodgeEvent>(OnDodge);
        }

        private void Update()
        {
            if (State != CombatState.Fighting) return;

            _player?.UpdateDodge();
            _opponent?.UpdateDodge();

            RoundTimer -= Time.deltaTime;
            if (RoundTimer <= 0)
            {
                EndRound();
            }
        }

        /// <summary>
        /// 开始一场比赛
        /// </summary>
        public void StartMatch(CombatMode mode)
        {
            CancelInvoke(nameof(StartNextRound));
            CancelInvoke(nameof(BeginFight));

            _mode = mode;
            _player = new PlayerCombatData("player");
            _opponent = new PlayerCombatData("opponent");
            RoundWins = new int[2];
            CurrentRound = 0;

            SetupModeControllers(mode);

            State = CombatState.WaitingForPose;
            Debug.Log($"[Combat] Match prepared in {mode}, waiting for pose validation...");
        }

        public void BeginMatchAfterPoseReady()
        {
            if (_player == null || _opponent == null)
            {
                Debug.LogWarning("[Combat] Cannot begin match: combat data is not prepared");
                return;
            }

            if (State != CombatState.WaitingForPose)
            {
                Debug.Log($"[Combat] BeginMatchAfterPoseReady ignored in state {State}");
                return;
            }

            StartNextRound();
        }

        public void PauseForPoseLost()
        {
            if (State != CombatState.Fighting) return;
            State = CombatState.PoseLost;
            Debug.Log("[Combat] Pose lost, pausing fight...");
        }

        public void ResumeAfterPoseReady()
        {
            if (State != CombatState.PoseLost) return;
            State = CombatState.Fighting;
            Debug.Log("[Combat] Pose restored, resuming fight...");
        }

        private void SetupModeControllers(CombatMode mode)
        {
            if (mode == CombatMode.AI)
            {
                _aiController = FindObjectOfType<AICombatController>();
                if (_aiController == null)
                    _aiController = gameObject.AddComponent<AICombatController>();
                _aiController.Initialize(_opponent);
            }
            else
            {
                _aiController?.Stop();
            }
        }

        /// <summary>
        /// 开始下一回合
        /// </summary>
        private void StartNextRound()
        {
            CurrentRound++;
            _player.ResetHp();
            _opponent.ResetHp();
            RoundTimer = CombatConfig.RoundDuration;
            State = CombatState.Countdown;

            // 倒计时后进入战斗
            Invoke(nameof(BeginFight), CombatConfig.PreCombatCountdown);
        }

        private void BeginFight()
        {
            State = CombatState.Fighting;
            Debug.Log($"[Combat] Round {CurrentRound} FIGHT!");
        }

        private void EndRound()
        {
            State = CombatState.RoundEnd;

            // 判定回合胜负
            string winnerId = null;
            if (_player.Hp > _opponent.Hp)
            {
                winnerId = _player.Id;
                RoundWins[0]++;
            }
            else if (_opponent.Hp > _player.Hp)
            {
                winnerId = _opponent.Id;
                RoundWins[1]++;
            }
            // HP相同 = 平局不计

            EventBus.Publish(new RoundEndEvent
            {
                WinnerId = winnerId,
                Score = RoundWins
            });

            Debug.Log($"[Combat] Round {CurrentRound} end. Winner: {winnerId ?? "Draw"}, Score: {RoundWins[0]}-{RoundWins[1]}");

            // 检查是否比赛结束
            if (RoundWins[0] >= CombatConfig.WinRoundsNeeded ||
                RoundWins[1] >= CombatConfig.WinRoundsNeeded ||
                CurrentRound >= CombatConfig.MaxRounds)
            {
                EndMatch();
            }
            else
            {
                // 2秒后开始下一回合
                Invoke(nameof(StartNextRound), 2f);
            }
        }

        private void EndMatch()
        {
            State = CombatState.MatchEnd;
            _aiController?.Stop();

            string winnerId = null;
            if (RoundWins[0] > RoundWins[1])
                winnerId = _player.Id;
            else if (RoundWins[1] > RoundWins[0])
                winnerId = _opponent.Id;

            EventBus.Publish(new MatchEndEvent
            {
                WinnerId = winnerId,
                Result = $"{RoundWins[0]}:{RoundWins[1]}"
            });

            Debug.Log($"[Combat] Match end. Winner: {winnerId ?? "Draw"}, Result: {RoundWins[0]}:{RoundWins[1]}");
        }

        // ========== 事件处理 ==========

        private void OnPunchDetected(PunchDetectedEvent evt)
        {
            if (State != CombatState.Fighting) return;
            if (_opponent.Hp <= 0) return; // \u9632\u6b62\u91cd\u590d KO

            int damage = CalculateDamage(evt.Type, evt.Power);

            // \u68c0\u67e5\u5bf9\u624b\u662f\u5426\u5728\u9632\u5fa1/\u95ea\u907f
            if (_opponent.IsDodging)
            {
                damage = 0;
            }
            else if (_opponent.IsDefending)
            {
                damage = Mathf.RoundToInt(damage * (1f - CombatConfig.DefendDamageReduction));
            }

            if (damage > 0)
            {
                _opponent.TakeDamage(damage);
                EventBus.Publish(new DamageEvent
                {
                    TargetId = _opponent.Id,
                    Damage = damage,
                    RemainingHp = _opponent.Hp
                });

                if (_opponent.Hp <= 0)
                {
                    EndRound();
                }
            }
        }

        private void OnDefend(DefendEvent evt)
        {
            if (State != CombatState.Fighting) return;
            _player.IsDefending = evt.IsActive;
        }

        private void OnDodge(DodgeEvent evt)
        {
            if (State != CombatState.Fighting) return;
            _player.StartDodge();
        }

        private int CalculateDamage(PunchType type, PunchPower power)
        {
            return (type, power) switch
            {
                (PunchType.LeftStraight, PunchPower.Light) => CombatConfig.LightStraightDamage,
                (PunchType.LeftStraight, PunchPower.Heavy) => CombatConfig.HeavyStraightDamage,
                (PunchType.RightStraight, PunchPower.Light) => CombatConfig.LightStraightDamage,
                (PunchType.RightStraight, PunchPower.Heavy) => CombatConfig.HeavyStraightDamage,
                (PunchType.LeftUppercut, PunchPower.Light) => CombatConfig.LightUppercutDamage,
                (PunchType.LeftUppercut, PunchPower.Heavy) => CombatConfig.HeavyUppercutDamage,
                (PunchType.RightUppercut, PunchPower.Light) => CombatConfig.LightUppercutDamage,
                (PunchType.RightUppercut, PunchPower.Heavy) => CombatConfig.HeavyUppercutDamage,
                _ => 0
            };
        }

        // ========== \u516c\u5f00\u63a5\u53e3 ==========

        public PlayerCombatData GetPlayerData() => _player;
        public PlayerCombatData GetOpponentData() => _opponent;

        /// <summary>
        /// AI \u653b\u51fb\u73a9\u5bb6\uff08\u7531 AICombatController \u8c03\u7528\uff09
        /// </summary>
        public void AIAttackPlayer(int damage)
        {
            if (State != CombatState.Fighting) return;
            if (_player == null || _player.Hp <= 0) return;

            if (_player.IsDodging)
            {
                damage = 0;
            }
            else if (_player.IsDefending)
            {
                damage = Mathf.RoundToInt(damage * (1f - CombatConfig.DefendDamageReduction));
            }

            if (damage > 0)
            {
                _player.TakeDamage(damage);
                EventBus.Publish(new DamageEvent
                {
                    TargetId = _player.Id,
                    Damage = damage,
                    RemainingHp = _player.Hp
                });

                if (_player.Hp <= 0)
                {
                    EndRound();
                }
            }
        }
    }

    public enum CombatState
    {
        Idle,
        WaitingForPose,
        Countdown,
        Fighting,
        PoseLost,
        RoundEnd,
        MatchEnd
    }

    public enum CombatMode
    {
        OnlineMatch,    // 在线匹配
        Room,           // 创建房间
        AI              // 人机对打
    }
}
