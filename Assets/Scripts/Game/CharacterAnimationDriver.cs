using UnityEngine;
using LoseWeight.Core;
using LoseWeight.Combat;
using LoseWeight.PoseDetection;
using System.Collections;

namespace LoseWeight.Game
{
    /// <summary>
    /// \u89d2\u8272\u52a8\u753b\u9a71\u52a8\u5668
    /// \u73a9\u5bb6\u6a21\u578b: \u7531\u73a9\u5bb6\u51fa\u62f3\u4e8b\u4ef6\u9a71\u52a8
    /// \u5bf9\u624b\u6a21\u578b: \u7531 AI \u4e8b\u4ef6\u9a71\u52a8
    /// \u4e25\u683c\u533a\u5206\uff0c\u4e92\u4e0d\u5e72\u6270
    /// </summary>
    public class CharacterAnimationDriver : MonoBehaviour
    {
        private Animator _playerAnimator;
        private Animator _opponentAnimator;
        private Transform _playerModel;
        private Transform _opponentModel;
        private bool _playerBusy;
        private float _busyUntil;
        private Vector3 _playerOriginalPos;
        private Vector3 _opponentOriginalPos;

        public void Setup(GameObject player, GameObject opponent)
        {
            if (player != null)
            {
                _playerModel = player.transform;
                _playerAnimator = player.GetComponentInChildren<Animator>();
                if (_playerAnimator != null) _playerAnimator.applyRootMotion = false;
                _playerOriginalPos = player.transform.localPosition;
            }
            if (opponent != null)
            {
                _opponentModel = opponent.transform;
                _opponentAnimator = opponent.GetComponentInChildren<Animator>();
                if (_opponentAnimator != null) _opponentAnimator.applyRootMotion = false;
                _opponentOriginalPos = opponent.transform.localPosition;
            }
            Debug.Log($"[AnimDriver] Setup done. PlayerAnim={_playerAnimator != null}, OpponentAnim={_opponentAnimator != null}, Same={_playerAnimator == _opponentAnimator}");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PunchDetectedEvent>(OnPlayerPunch);
            EventBus.Subscribe<DefendEvent>(OnPlayerDefend);
            EventBus.Subscribe<DodgeEvent>(OnPlayerDodge);
            EventBus.Subscribe<AIActionEvent>(OnOpponentAction);
            EventBus.Subscribe<DamageEvent>(OnDamage);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPlayerPunch);
            EventBus.Unsubscribe<DefendEvent>(OnPlayerDefend);
            EventBus.Unsubscribe<DodgeEvent>(OnPlayerDodge);
            EventBus.Unsubscribe<AIActionEvent>(OnOpponentAction);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
        }

        private void Update()
        {
            if (_playerBusy && Time.time > _busyUntil)
            {
                _playerBusy = false;
                SafePlay(_playerAnimator, "Idle");
            }
        }

        // ========== \u73a9\u5bb6\u52a8\u4f5c\uff08\u53ea\u9a71\u52a8\u73a9\u5bb6\u6a21\u578b\uff09 ==========

        private void OnPlayerPunch(PunchDetectedEvent evt)
        {
            if (_playerBusy || _playerAnimator == null) return;
            string state = GetPunchState(evt.Type, evt.Power);
            SafePlay(_playerAnimator, state);
            _playerBusy = true;
            _busyUntil = Time.time + 0.5f;
        }

        private void OnPlayerDefend(DefendEvent evt)
        {
            if (_playerAnimator == null) return;
            if (evt.IsActive)
            {
                SafePlay(_playerAnimator, "Block");
                _playerBusy = true;
                _busyUntil = Time.time + 0.8f;
            }
        }

        private void OnPlayerDodge(DodgeEvent evt)
        {
            if (_playerBusy || _playerAnimator == null) return;
            SafePlay(_playerAnimator, "Duck");
            float dir = evt.Direction == DodgeDirection.Left ? -0.5f : 0.5f;
            StartCoroutine(MoveAndReturn(_playerModel, _playerOriginalPos, new Vector3(dir, 0, 0), 0.3f));
            _playerBusy = true;
            _busyUntil = Time.time + 0.5f;
        }

        // ========== \u5bf9\u624b\u52a8\u4f5c\uff08\u53ea\u9a71\u52a8\u5bf9\u624b\u6a21\u578b\uff09 ==========

        private void OnOpponentAction(AIActionEvent evt)
        {
            if (_opponentAnimator == null) return;
            string state = GetPunchState(evt.Type, evt.Power);
            SafePlay(_opponentAnimator, state);
        }

        // ========== \u53d7\u51fb ==========

        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId == "opponent" && _opponentModel != null)
            {
                StartCoroutine(MoveAndReturn(_opponentModel, _opponentOriginalPos, Vector3.forward * 0.15f, 0.12f));
            }
            else if (evt.TargetId == "player" && _playerAnimator != null)
            {
                SafePlay(_playerAnimator, "Duck");
                _playerBusy = true;
                _busyUntil = Time.time + 0.3f;
            }
        }

        // ========== \u5de5\u5177\u65b9\u6cd5 ==========

        private void SafePlay(Animator anim, string stateName)
        {
            if (anim == null) return;
            // \u68c0\u67e5\u72b6\u6001\u662f\u5426\u5b58\u5728\uff0c\u907f\u514d\u62a5\u9519
            if (anim.HasState(0, Animator.StringToHash(stateName)))
            {
                anim.CrossFadeInFixedTime(stateName, 0.1f, 0);
            }
        }

        private string GetPunchState(PunchType type, PunchPower power)
        {
            return (type, power) switch
            {
                (PunchType.LeftStraight, PunchPower.Light) => "LeftStraight_Light",
                (PunchType.LeftStraight, PunchPower.Heavy) => "LeftStraight_Heavy",
                (PunchType.RightStraight, PunchPower.Light) => "RightStraight_Light",
                (PunchType.RightStraight, PunchPower.Heavy) => "RightStraight_Heavy",
                (PunchType.LeftUppercut, PunchPower.Light) => "LeftUppercut_Light",
                (PunchType.LeftUppercut, PunchPower.Heavy) => "LeftUppercut_Heavy",
                (PunchType.RightUppercut, PunchPower.Light) => "RightUppercut_Light",
                (PunchType.RightUppercut, PunchPower.Heavy) => "RightUppercut_Heavy",
                _ => "RightStraight_Light"
            };
        }

        private IEnumerator MoveAndReturn(Transform t, Vector3 origin, Vector3 offset, float duration)
        {
            if (t == null) yield break;
            t.localPosition = origin + offset;
            yield return new WaitForSeconds(duration);
            if (t != null) t.localPosition = origin;
        }
    }
}
