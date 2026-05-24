using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.Combat
{
    /// <summary>
    /// 连击系统 - 追踪连续命中并提供加成
    /// </summary>
    public class ComboSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _comboResetTime = 2f; // 超过2秒无命中重置连击

        public int CurrentCombo { get; private set; }
        public int MaxCombo { get; private set; }

        private float _lastHitTime;

        private void OnEnable()
        {
            EventBus.Subscribe<DamageEvent>(OnDamage);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
        }

        private void Update()
        {
            // 超时重置连击
            if (CurrentCombo > 0 && Time.time - _lastHitTime > _comboResetTime)
            {
                ResetCombo();
            }
        }

        private void OnDamage(DamageEvent evt)
        {
            // 只统计玩家造成的伤害
            if (evt.TargetId == "opponent" && evt.Damage > 0)
            {
                CurrentCombo++;
                _lastHitTime = Time.time;

                if (CurrentCombo > MaxCombo)
                    MaxCombo = CurrentCombo;
            }
            else if (evt.TargetId == "player" && evt.Damage > 0)
            {
                // 被打中重置连击
                ResetCombo();
            }
        }

        /// <summary>
        /// 获取连击伤害加成倍率
        /// </summary>
        public float GetComboMultiplier()
        {
            if (CurrentCombo < 3) return 1f;
            if (CurrentCombo < 5) return 1.1f;
            if (CurrentCombo < 8) return 1.2f;
            return 1.3f; // 8连击以上 30% 加成
        }

        public void ResetCombo()
        {
            CurrentCombo = 0;
        }

        public void ResetAll()
        {
            CurrentCombo = 0;
            MaxCombo = 0;
        }
    }
}
