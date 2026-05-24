using UnityEngine;
using LoseWeight.Data;

namespace LoseWeight.Combat
{
    /// <summary>
    /// 玩家对战运行时数据
    /// </summary>
    public class PlayerCombatData
    {
        public string Id { get; private set; }
        public int Hp { get; private set; }
        public bool IsDefending { get; set; }
        public bool IsDodging { get; private set; }
        public int Combo { get; private set; }
        public int TotalDamageDealt { get; private set; }
        public int TotalHits { get; private set; }

        private float _dodgeEndTime;

        public PlayerCombatData(string id)
        {
            Id = id;
            ResetHp();
        }

        public void ResetHp()
        {
            Hp = CombatConfig.MaxHp;
            IsDefending = false;
            IsDodging = false;
            Combo = 0;
        }

        public void TakeDamage(int damage)
        {
            Hp = Mathf.Max(0, Hp - damage);
        }

        public void DealDamage(int damage)
        {
            TotalDamageDealt += damage;
            TotalHits++;
            Combo++;
        }

        public void ResetCombo()
        {
            Combo = 0;
        }

        public void StartDodge()
        {
            IsDodging = true;
            _dodgeEndTime = Time.time + PoseConfig.DodgeActiveDuration;
        }

        public void UpdateDodge()
        {
            if (IsDodging && Time.time >= _dodgeEndTime)
            {
                IsDodging = false;
            }
        }
    }
}
