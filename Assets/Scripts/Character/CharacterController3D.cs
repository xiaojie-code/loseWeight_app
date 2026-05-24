using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.Character
{
    /// <summary>
    /// 3D 角色控制器 - 驱动角色动画和表现
    /// 接收动作事件，播放对应动画
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterController3D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _hitEffectPoint; // 命中特效挂点

        [Header("Settings")]
        [SerializeField] private bool _isPlayer = true;

        // Animator 参数名
        private static readonly int AnimPunch = Animator.StringToHash("Punch");
        private static readonly int AnimPunchType = Animator.StringToHash("PunchType");
        private static readonly int AnimDefend = Animator.StringToHash("IsDefending");
        private static readonly int AnimDodge = Animator.StringToHash("Dodge");
        private static readonly int AnimDodgeDir = Animator.StringToHash("DodgeDirection");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimKO = Animator.StringToHash("KO");
        private static readonly int AnimVictory = Animator.StringToHash("Victory");
        private const float PunchAnimationCooldown = 0.34f;
        private float _lastPunchAnimationAt = -999f;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (_isPlayer)
            {
                EventBus.Subscribe<PunchDetectedEvent>(OnPunch);
                EventBus.Subscribe<DefendEvent>(OnDefend);
                EventBus.Subscribe<DodgeEvent>(OnDodge);
            }
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void OnDisable()
        {
            if (_isPlayer)
            {
                EventBus.Unsubscribe<PunchDetectedEvent>(OnPunch);
                EventBus.Unsubscribe<DefendEvent>(OnDefend);
                EventBus.Unsubscribe<DodgeEvent>(OnDodge);
            }
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void OnPunch(PunchDetectedEvent evt)
        {
            if (Time.unscaledTime - _lastPunchAnimationAt < PunchAnimationCooldown)
                return;

            _lastPunchAnimationAt = Time.unscaledTime;
            _animator.ResetTrigger(AnimPunch);
            _animator.SetInteger(AnimPunchType, (int)evt.Type);
            _animator.SetTrigger(AnimPunch);
        }

        private void OnDefend(DefendEvent evt)
        {
            _animator.SetBool(AnimDefend, evt.IsActive);
        }

        private void OnDodge(DodgeEvent evt)
        {
            _animator.SetInteger(AnimDodgeDir, (int)evt.Direction);
            _animator.SetTrigger(AnimDodge);
        }

        private void OnDamage(DamageEvent evt)
        {
            // 对手角色播放出拳动画，自己播放受击动画
            bool isTarget = (_isPlayer && evt.TargetId == "player") ||
                            (!_isPlayer && evt.TargetId == "opponent");

            if (isTarget)
            {
                _animator.SetTrigger(AnimHit);
                PlayHitEffect();
            }
        }

        private void OnMatchEnd(MatchEndEvent evt)
        {
            bool isWinner = (_isPlayer && evt.WinnerId == "player") ||
                            (!_isPlayer && evt.WinnerId == "opponent");

            if (isWinner)
                _animator.SetTrigger(AnimVictory);
            else
                _animator.SetTrigger(AnimKO);
        }

        private void PlayHitEffect()
        {
            // TODO: 根据拳套类型播放不同特效
            // 训练拳套 -> 白色冲击波
            // 烈焰拳套 -> 火焰爆裂
            // 雷霆拳套 -> 电弧闪烁
            if (_hitEffectPoint != null)
            {
                Debug.Log("[Character] Hit effect at " + _hitEffectPoint.position);
            }
        }
    }
}
