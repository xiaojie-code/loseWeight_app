using UnityEngine;
using LoseWeight.Core;
using LoseWeight.Data;

namespace LoseWeight.Combat
{
    /// <summary>
    /// AI 对手控制器 - 人机对打模式
    /// 难度：出拳频率低、不闪避、偶尔防御
    /// </summary>
    public class AICombatController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float _minAttackInterval = 2f;
        [SerializeField] private float _maxAttackInterval = 4f;
        [SerializeField] private float _defendChance = 0.2f;
        [SerializeField] private float _defendDuration = 1f;

        private float _nextAttackTime;
        private float _defendEndTime;
        private bool _isActive;
        private PlayerCombatData _aiData;

        public void Initialize(PlayerCombatData aiData)
        {
            _aiData = aiData;
            _isActive = true;
            ScheduleNextAttack();
        }

        public void Stop()
        {
            _isActive = false;
        }

        private void Update()
        {
            if (!_isActive) return;
            if (CombatManager.Instance.State != CombatState.Fighting) return;

            // 防御结束
            if (_aiData.IsDefending && Time.time >= _defendEndTime)
            {
                _aiData.IsDefending = false;
            }

            // 攻击计时
            if (Time.time >= _nextAttackTime)
            {
                PerformAction();
                ScheduleNextAttack();
            }
        }

        private void PerformAction()
        {
            // \u5076\u5c14\u9632\u5fa1
            if (Random.value < _defendChance)
            {
                _aiData.IsDefending = true;
                _defendEndTime = Time.time + _defendDuration;
                return;
            }

            // \u968f\u673a\u51fa\u62f3
            var punchTypes = new[] { PunchType.LeftStraight, PunchType.RightStraight };
            var type = punchTypes[Random.Range(0, punchTypes.Length)];
            var power = Random.value > 0.7f ? PunchPower.Heavy : PunchPower.Light;

            EventBus.Publish(new AIActionEvent
            {
                Type = type,
                Power = power
            });

            int damage = CalculateAIDamage(type, power);

            // \u901a\u8fc7 CombatManager \u7edf\u4e00\u5904\u7406\u4f24\u5bb3\uff08\u5305\u542b\u9632\u5fa1/\u95ea\u907f/KO\u5224\u5b9a\uff09
            CombatManager.Instance?.AIAttackPlayer(damage);
        }

        private void ScheduleNextAttack()
        {
            _nextAttackTime = Time.time + Random.Range(_minAttackInterval, _maxAttackInterval);
        }

        private int CalculateAIDamage(PunchType type, PunchPower power)
        {
            return (type, power) switch
            {
                (_, PunchPower.Light) => CombatConfig.LightStraightDamage,
                (_, PunchPower.Heavy) => CombatConfig.HeavyStraightDamage,
                _ => CombatConfig.LightStraightDamage
            };
        }

        private PlayerCombatData GetPlayerData()
        {
            // 通过 CombatManager 获取玩家数据
            if (CombatManager.Instance != null)
                return CombatManager.Instance.GetPlayerData();
            return null;
        }
    }
}
