using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.Game
{
    /// <summary>
    /// ��ɫ���������� - �� CrossFade ֱ�Ӳ��Ŷ���״̬
    /// ���� Trigger �������¶���ѭ��
    /// </summary>
    public class CharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] private bool _enableDefendAnimations = false;
        [SerializeField] private bool _enableDodgeAnimations = false;

        private Animator _playerAnimator;
        private Animator _opponentAnimator;
        private Transform _playerModel;
        private Transform _opponentModel;
        private Vector3 _playerBaseLocalPosition;
        private Quaternion _playerBaseLocalRotation;
        private Vector3 _opponentBaseLocalPosition;
        private Quaternion _opponentBaseLocalRotation;
        private Coroutine _playerPoseRoutine;
        private Coroutine _opponentPoseRoutine;

        private bool _playerBusy;
        private float _playerBusyUntil;
        private bool _playerBusyFromPunch;
        private bool _opponentBusy;
        private float _opponentBusyUntil;

        // ����״̬����ӳ��
        private static readonly string[] ACTION_STATES = {
            "LeftStraight_Light",    // 0
            "LeftStraight_Heavy",    // 1
            "RightStraight_Light",   // 2
            "RightStraight_Heavy",   // 3
            "LeftUppercut_Light",    // 4
            "LeftUppercut_Heavy",    // 5
            "RightUppercut_Light",   // 6
            "RightUppercut_Heavy",   // 7
            "Block",                 // 8
            "Duck"                   // 9
        };

        public void Setup(GameObject player, GameObject opponent)
        {
            if (_playerPoseRoutine != null)
            {
                StopCoroutine(_playerPoseRoutine);
                _playerPoseRoutine = null;
            }
            if (_opponentPoseRoutine != null)
            {
                StopCoroutine(_opponentPoseRoutine);
                _opponentPoseRoutine = null;
            }

            _playerBusy = false;
            _opponentBusy = false;
            _playerBusyFromPunch = false;
            _playerBusyUntil = 0f;
            _opponentBusyUntil = 0f;

            _playerModel = player?.transform;
            _opponentModel = opponent?.transform;
            if (_playerModel != null)
            {
                _playerBaseLocalPosition = _playerModel.localPosition;
                _playerBaseLocalRotation = _playerModel.localRotation;
            }
            if (_opponentModel != null)
            {
                _opponentBaseLocalPosition = _opponentModel.localPosition;
                _opponentBaseLocalRotation = _opponentModel.localRotation;
            }

            if (player != null)
            {
                _playerAnimator = player.GetComponent<Animator>() ?? player.GetComponentInChildren<Animator>();
                if (_playerAnimator != null)
                {
                    _playerAnimator.applyRootMotion = false;
                    _playerAnimator.enabled = true;
                    _playerAnimator.Play("Idle", 0, 0);
                    _playerAnimator.speed = 0f;
                }
            }

            if (opponent != null)
            {
                _opponentAnimator = opponent.GetComponent<Animator>() ?? opponent.GetComponentInChildren<Animator>();
                if (_opponentAnimator != null)
                {
                    _opponentAnimator.applyRootMotion = false;
                    _opponentAnimator.enabled = true;
                    _opponentAnimator.Play("Idle", 0, 0);
                }
            }

            Debug.Log($"[AnimDriver] Setup. Player={_playerAnimator != null}, Opponent={_opponentAnimator != null}");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PunchDetectedEvent>(OnPlayerPunch);
            EventBus.Subscribe<DefendEvent>(OnPlayerDefend);
            EventBus.Subscribe<DodgeEvent>(OnPlayerDodge);
            EventBus.Subscribe<AIActionEvent>(OnAIAction);
            EventBus.Subscribe<DamageEvent>(OnDamage);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPlayerPunch);
            EventBus.Unsubscribe<DefendEvent>(OnPlayerDefend);
            EventBus.Unsubscribe<DodgeEvent>(OnPlayerDodge);
            EventBus.Unsubscribe<AIActionEvent>(OnAIAction);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
        }

        private void Update()
        {
            // ������������Զ��ص� Idle
            if (_playerBusy && Time.time > _playerBusyUntil)
            {
                _playerBusy = false;
                _playerBusyFromPunch = false;
                PlayPlayerState("Idle");
                ResetPlayerPose();
            }

            if (_opponentBusy && Time.time > _opponentBusyUntil)
            {
                _opponentBusy = false;
                PlayOpponentState("Idle");
            }
        }

        private void OnPlayerPunch(PunchDetectedEvent evt)
        {
            int idx = GetPunchIndex(evt.Type, evt.Power);
            if (idx >= 0 && idx < ACTION_STATES.Length)
            {
                PlayPlayerState(ACTION_STATES[idx], true);
                StopPlayerPoseFeedback();
                ResetPlayerPose();
                _playerBusy = true;
                _playerBusyFromPunch = true;
                _playerBusyUntil = Time.time + 0.5f; // ��������ʱ��
                Debug.Log($"[AnimDriver] Player punch {evt.Type} {evt.Power}");
            }
        }

        private void OnAIAction(AIActionEvent evt)
        {
            if (_opponentBusy) return;

            int idx = GetPunchIndex(evt.Type, evt.Power);
            if (idx >= 0 && idx < ACTION_STATES.Length)
            {
                PlayOpponentState(ACTION_STATES[idx]);
                StopOpponentPoseFeedback();
                ResetOpponentPose();
                _opponentBusy = true;
                _opponentBusyUntil = Time.time + 0.5f;
                Debug.Log($"[AnimDriver] AI punch {evt.Type} {evt.Power}");
            }
        }

        private void OnPlayerDefend(DefendEvent evt)
        {
            if (!_enableDefendAnimations) return;
            if (_playerBusyFromPunch) return;

            if (evt.IsActive)
            {
                PlayPlayerState("Block");
                StartPlayerPoseFeedback(new Vector3(0f, 0f, -0.12f), Quaternion.Euler(-6f, 0f, 0f), 0.8f);
                _playerBusy = true;
                _playerBusyUntil = Time.time + 0.8f;
            }
            else
            {
                _playerBusy = false;
                PlayPlayerState("Idle");
                ResetPlayerPose();
            }
        }

        private void OnPlayerDodge(DodgeEvent evt)
        {
            if (!_enableDodgeAnimations) return;
            if (_playerBusy) return;
            PlayPlayerState("Duck");
            StartPlayerPoseFeedback(GetDodgeOffset(evt.Direction), GetDodgeRotation(evt.Direction), 0.5f);
            _playerBusy = true;
            _playerBusyFromPunch = false;
            _playerBusyUntil = Time.time + 0.5f;
        }

        private void OnDamage(DamageEvent evt)
        {
            if (evt.TargetId == "opponent" && _opponentModel != null)
            {
                StartCoroutine(OpponentHitReaction());
            }
        }

        private void PlayPlayerState(string stateName, bool forceRestart = false)
        {
            if (_playerAnimator == null || !_playerAnimator.enabled) return;
            if (stateName == "Idle")
            {
                _playerAnimator.speed = 1f;
                _playerAnimator.Play("Idle", 0, 0f);
                _playerAnimator.Update(0f);
                _playerAnimator.speed = 0f;
                return;
            }

            _playerAnimator.speed = 1f;
            if (forceRestart)
            {
                _playerAnimator.Play(stateName, 0, 0f);
                _playerAnimator.Update(0f);
                return;
            }

            _playerAnimator.CrossFadeInFixedTime(stateName, 0.05f, 0, 0f);
        }

        private void PlayOpponentState(string stateName)
        {
            if (_opponentAnimator == null || !_opponentAnimator.enabled) return;
            _opponentAnimator.speed = 1f;
            _opponentAnimator.CrossFadeInFixedTime(stateName, 0.05f, 0, 0f);
        }

        private int GetPunchIndex(PunchType type, PunchPower power)
        {
            return (type, power) switch
            {
                (PunchType.LeftStraight, PunchPower.Light) => 0,
                (PunchType.LeftStraight, PunchPower.Heavy) => 1,
                (PunchType.RightStraight, PunchPower.Light) => 2,
                (PunchType.RightStraight, PunchPower.Heavy) => 3,
                (PunchType.LeftUppercut, PunchPower.Light) => 4,
                (PunchType.LeftUppercut, PunchPower.Heavy) => 5,
                (PunchType.RightUppercut, PunchPower.Light) => 6,
                (PunchType.RightUppercut, PunchPower.Heavy) => 7,
                _ => 0
            };
        }

        private Vector3 GetDodgeOffset(DodgeDirection direction)
        {
            return direction switch
            {
                DodgeDirection.Left => new Vector3(-0.35f, 0f, 0f),
                DodgeDirection.Right => new Vector3(0.35f, 0f, 0f),
                DodgeDirection.Down => new Vector3(0f, -0.25f, 0f),
                _ => Vector3.zero
            };
        }

        private Quaternion GetDodgeRotation(DodgeDirection direction)
        {
            return direction switch
            {
                DodgeDirection.Left => Quaternion.Euler(0f, 0f, 10f),
                DodgeDirection.Right => Quaternion.Euler(0f, 0f, -10f),
                DodgeDirection.Down => Quaternion.Euler(-10f, 0f, 0f),
                _ => Quaternion.identity
            };
        }

        private Vector3 GetPunchOffset(PunchType type)
        {
            float side = type == PunchType.LeftStraight || type == PunchType.LeftUppercut ? -0.08f : 0.08f;
            float lift = type == PunchType.LeftUppercut || type == PunchType.RightUppercut ? 0.08f : 0f;
            return new Vector3(side, lift, 0.18f);
        }

        private Quaternion GetPunchRotation(PunchType type)
        {
            float roll = type == PunchType.LeftStraight || type == PunchType.LeftUppercut ? 8f : -8f;
            float pitch = type == PunchType.LeftUppercut || type == PunchType.RightUppercut ? -8f : -4f;
            return Quaternion.Euler(pitch, 0f, roll);
        }

        private void StartPlayerPoseFeedback(Vector3 offset, Quaternion localRotationOffset, float duration)
        {
            if (_playerModel == null) return;
            StopPlayerPoseFeedback();
            _playerPoseRoutine = StartCoroutine(PlayerPoseFeedback(offset, localRotationOffset, duration));
        }

        private void StartOpponentPoseFeedback(Vector3 offset, Quaternion localRotationOffset, float duration)
        {
            if (_opponentModel == null) return;
            StopOpponentPoseFeedback();
            _opponentPoseRoutine = StartCoroutine(OpponentPoseFeedback(offset, localRotationOffset, duration));
        }

        private void StopPlayerPoseFeedback()
        {
            if (_playerPoseRoutine == null) return;
            StopCoroutine(_playerPoseRoutine);
            _playerPoseRoutine = null;
        }

        private void StopOpponentPoseFeedback()
        {
            if (_opponentPoseRoutine == null) return;
            StopCoroutine(_opponentPoseRoutine);
            _opponentPoseRoutine = null;
        }

        private System.Collections.IEnumerator PlayerPoseFeedback(Vector3 offset, Quaternion localRotationOffset, float duration)
        {
            _playerModel.localPosition = _playerBaseLocalPosition + offset;
            _playerModel.localRotation = _playerBaseLocalRotation * localRotationOffset;
            yield return new WaitForSeconds(duration);
            ResetPlayerPose();
        }

        private System.Collections.IEnumerator OpponentPoseFeedback(Vector3 offset, Quaternion localRotationOffset, float duration)
        {
            _opponentModel.localPosition = _opponentBaseLocalPosition + offset;
            _opponentModel.localRotation = _opponentBaseLocalRotation * localRotationOffset;
            yield return new WaitForSeconds(duration);
            ResetOpponentPose();
        }

        private void ResetPlayerPose()
        {
            if (_playerModel == null) return;
            _playerModel.localPosition = _playerBaseLocalPosition;
            _playerModel.localRotation = _playerBaseLocalRotation;
        }

        private void ResetOpponentPose()
        {
            if (_opponentModel == null) return;
            _opponentModel.localPosition = _opponentBaseLocalPosition;
            _opponentModel.localRotation = _opponentBaseLocalRotation;
        }

        private System.Collections.IEnumerator OpponentHitReaction()
        {
            var orig = _opponentModel.localPosition;
            _opponentModel.localPosition = orig + new Vector3(0, 0, 0.12f);
            yield return new WaitForSeconds(0.12f);
            _opponentModel.localPosition = orig;
        }
    }
}
