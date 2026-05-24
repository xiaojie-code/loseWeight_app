using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.Character
{
    /// <summary>
    /// 角色动画事件 - 在动画关键帧触发
    /// 用于精确控制命中判定时机和特效播放
    /// </summary>
    public class CharacterAnimEvents : MonoBehaviour
    {
        [SerializeField] private DressupSystem _dressupSystem;
        [SerializeField] private Transform _leftFistPoint;
        [SerializeField] private Transform _rightFistPoint;

        /// <summary>
        /// 动画事件：左拳命中帧
        /// </summary>
        public void OnLeftPunchHit()
        {
            if (_dressupSystem != null && _leftFistPoint != null)
            {
                _dressupSystem.PlayHitEffect(_leftFistPoint.position);
            }
        }

        /// <summary>
        /// 动画事件：右拳命中帧
        /// </summary>
        public void OnRightPunchHit()
        {
            if (_dressupSystem != null && _rightFistPoint != null)
            {
                _dressupSystem.PlayHitEffect(_rightFistPoint.position);
            }
        }

        /// <summary>
        /// 动画事件：出拳音效
        /// </summary>
        public void OnPunchSwing()
        {
            AudioManager.Instance?.PlayClick(); // TODO: 替换为挥拳音效
        }

        /// <summary>
        /// 动画事件：受击音效
        /// </summary>
        public void OnHitImpact()
        {
            // 由 AudioManager 通过事件处理
        }
    }
}
