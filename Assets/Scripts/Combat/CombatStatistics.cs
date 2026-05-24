using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.Combat
{
    /// <summary>
    /// 对战统计 - 记录单场比赛的详细数据
    /// 用于结算页面展示
    /// </summary>
    public class CombatStatistics : MonoBehaviour
    {
        public int TotalPunches { get; private set; }
        public int SuccessfulHits { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int TotalDamageTaken { get; private set; }
        public int DodgeCount { get; private set; }
        public int DefendCount { get; private set; }
        public int MaxCombo { get; private set; }
        public float MatchDuration { get; private set; }

        private float _matchStartTime;
        private ComboSystem _comboSystem;

        private void Awake()
        {
            _comboSystem = GetComponent<ComboSystem>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<DefendEvent>(OnDefend);
            EventBus.Subscribe<DodgeEvent>(OnDodge);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
            EventBus.Unsubscribe<DefendEvent>(OnDefend);
            EventBus.Unsubscribe<DodgeEvent>(OnDodge);
        }

        public void StartTracking()
        {
            _matchStartTime = Time.time;
            Reset();
        }

        public void StopTracking()
        {
            MatchDuration = Time.time - _matchStartTime;
            if (_comboSystem != null)
                MaxCombo = _comboSystem.MaxCombo;
        }

        public void Reset()
        {
            TotalPunches = 0;
            SuccessfulHits = 0;
            TotalDamageDealt = 0;
            TotalDamageTaken = 0;
            DodgeCount = 0;
            DefendCount = 0;
            MaxCombo = 0;
        }

        /// <summary>
        /// 命中率
        /// </summary>
        public float HitRate => TotalPunches > 0 ? (float)SuccessfulHits / TotalPunches : 0f;

        private void OnPunch(PunchDetectedEvent evt)
        {
            TotalPunches++;
        }

        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId == "opponent")
            {
                SuccessfulHits++;
                TotalDamageDealt += evt.Damage;
            }
            else if (evt.TargetId == "player")
            {
                TotalDamageTaken += evt.Damage;
            }
        }

        private void OnDefend(DefendEvent evt)
        {
            if (evt.IsActive)
                DefendCount++;
        }

        private void OnDodge(DodgeEvent evt)
        {
            DodgeCount++;
        }
    }
}
