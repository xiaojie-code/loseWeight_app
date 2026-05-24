using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.PoseDetection
{
    public class ActionDetector : MonoBehaviour
    {
        [Header("出拳参数")]
        public float PunchSpeedMin = 0.05f;       // 最小速度阈值
        public float HeavyPunchSpeed = 0.10f;     // 重拳速度阈值
        public float PunchCooldown = 0.6f;        // 单手冷却（防止连续触发）
        public float MinConfidence = 0.4f;
        public float MinForwardDistance = 0.08f;  // 拳头距离肩膀最小前推距离

        [Header("��ȭ�ж�")]
        public float UppercutYRatio = 0.65f;      // yλ��ռ�� > ��ֵ = ��ȭ

        [Header("��������")]
        public float DefendWristAboveShoulder = 0.03f;
        public float DefendWristCloseRatio = 0.6f;
        public float DefendCooldown = 0.4f;

        [Header("��������")]
        public float DodgeShift = 0.12f;     // 闪避偏移阈值（提高，避免误触）
        public float DodgeCooldown = 1.5f;   // 闪避冷却（提高，避免连续触发）
        public float DodgeStableDuration = 0.15f; // 偏移需持续这么久才算闪避

        // ��ʷ֡����3֡ǰ�����ݼ����ٶȣ�ƽ��������
        private Vector3[] _leftHistory = new Vector3[6];
        private Vector3[] _rightHistory = new Vector3[6];
        private int _hIdx;
        private int _hCount;

        private Vector3 _calibLeftShoulder, _calibRightShoulder;
        private float _shoulderWidth;
        private bool _isCalibrated;

        private float _lastPunchTime;
        private float _lastLeftPunchTime;
        private float _lastRightPunchTime;
        private float _lastDefendTime, _lastDodgeTime;
        private float _dodgeStartTime;       // 偏移开始时间
        private DodgeDirection _pendingDodgeDir;
        private bool _isDefending;
        private int _frameCount;
        private const int WARMUP = 12;

        public void Calibrate(PoseFrame frame)
        {
            _calibLeftShoulder = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder).Position;
            _calibRightShoulder = frame.GetLandmark(PoseLandmarkIndex.RightShoulder).Position;
            _shoulderWidth = Mathf.Abs(_calibLeftShoulder.x - _calibRightShoulder.x);
            _hCount = 0; _hIdx = 0; _frameCount = 0;
            _isCalibrated = true;
            Debug.Log($"[ActionDetector] Calibrated. SW={_shoulderWidth:F3}");
        }

        public void ProcessFrame(PoseFrame frame)
        {
            if (!_isCalibrated) { Calibrate(frame); return; }

            var lw = frame.GetLandmark(PoseLandmarkIndex.LeftWrist);
            var rw = frame.GetLandmark(PoseLandmarkIndex.RightWrist);
            var ls = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rs = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);

            if (!lw.IsVisible || !rw.IsVisible) return;

            // ��¼��ʷ
            _leftHistory[_hIdx] = lw.Position;
            _rightHistory[_hIdx] = rw.Position;
            _hIdx = (_hIdx + 1) % 6;
            if (_hCount < 6) _hCount++;
            _frameCount++;

            if (_frameCount < WARMUP || _hCount < 3) return;

            // �� 3 ֡ǰ�����ݼ����ٶȣ�ƽ����
            int oldIdx = (_hIdx - 3 + 6) % 6;
            Vector3 leftOld = _leftHistory[oldIdx];
            Vector3 rightOld = _rightHistory[oldIdx];

            float now = Time.time;

            // ����
            if (ls.IsVisible && rs.IsVisible)
                DetectDefend(lw, rw, ls, rs, now);

            // ��ȭ - \u5de6\u53f3\u624b\u72ec\u7acb\u51b7\u5374
            if (!_isDefending)
            {
                if (now - _lastLeftPunchTime > PunchCooldown)
                    TryDetectPunch(lw, leftOld, true, now, ls.Position);
                if (now - _lastRightPunchTime > PunchCooldown)
                    TryDetectPunch(rw, rightOld, false, now, rs.Position);
            }

            // ����
            if (ls.IsVisible && rs.IsVisible)
                DetectDodge(ls, rs, now);
        }

        private void TryDetectPunch(PoseLandmark wrist, Vector3 old, bool isMediaPipeLeft, float now, Vector3 shoulderPos)
        {
            if (wrist.Confidence < MinConfidence) return;

            Vector3 current = wrist.Position;
            float dx = current.x - old.x;
            float dy = current.y - old.y;
            float speed = Mathf.Sqrt(dx * dx + dy * dy);

            if (speed < PunchSpeedMin) return;

            // 检查拳头是否真的"伸出去了"——和肩膀的距离要够
            float wristToShoulder = Vector2.Distance(
                new Vector2(current.x, current.y),
                new Vector2(shoulderPos.x, shoulderPos.y));
            if (wristToShoulder < MinForwardDistance) return;

            // \u5de6\u53f3\u624b\u72ec\u7acb\u8bb0\u5f55\u51b7\u5374
            if (isMediaPipeLeft)
                _lastLeftPunchTime = now;
            else
                _lastRightPunchTime = now;

            // ����ȭ
            var power = speed >= HeavyPunchSpeed ? PunchPower.Heavy : PunchPower.Light;

            // ֱȭ vs ��ȭ
            float totalDisp = Mathf.Abs(dx) + Mathf.Abs(dy) + 0.001f;
            float yRatio = Mathf.Abs(dy) / totalDisp;
            bool isUppercut = dy < 0 && yRatio > UppercutYRatio;

            // 用户视角：用户左手 = 左拳，用户右手 = 右拳
            PunchType type;
            if (isMediaPipeLeft)
                type = isUppercut ? PunchType.LeftUppercut : PunchType.LeftStraight;
            else
                type = isUppercut ? PunchType.RightUppercut : PunchType.RightStraight;

            EventBus.Publish(new PunchDetectedEvent { Type = type, Power = power, Speed = speed });
        }

        private void DetectDefend(PoseLandmark lw, PoseLandmark rw, PoseLandmark ls, PoseLandmark rs, float now)
        {
            bool leftHigh = lw.Position.y < ls.Position.y - DefendWristAboveShoulder;
            bool rightHigh = rw.Position.y < rs.Position.y - DefendWristAboveShoulder;
            float dist = Mathf.Abs(lw.Position.x - rw.Position.x);
            bool close = dist < _shoulderWidth * DefendWristCloseRatio;
            bool shouldDefend = leftHigh && rightHigh && close;

            if (shouldDefend && !_isDefending && now - _lastDefendTime > DefendCooldown)
            {
                _isDefending = true; _lastDefendTime = now;
                EventBus.Publish(new DefendEvent { IsActive = true });
            }
            else if (!shouldDefend && _isDefending)
            {
                _isDefending = false;
                EventBus.Publish(new DefendEvent { IsActive = false });
            }
        }

        private void DetectDodge(PoseLandmark ls, PoseLandmark rs, float now)
        {
            if (now - _lastDodgeTime < DodgeCooldown)
            {
                _dodgeStartTime = 0; // 冷却中重置
                return;
            }

            float cx = (ls.Position.x + rs.Position.x) / 2f;
            float calibCx = (_calibLeftShoulder.x + _calibRightShoulder.x) / 2f;
            float shift = cx - calibCx;

            if (Mathf.Abs(shift) < DodgeShift)
            {
                // 不够偏移，重置
                _dodgeStartTime = 0;
                return;
            }

            // 反方向：身体向用户左方移动 = 用户左闪避
            var dir = shift > 0 ? DodgeDirection.Left : DodgeDirection.Right;

            if (_dodgeStartTime == 0 || _pendingDodgeDir != dir)
            {
                _dodgeStartTime = now;
                _pendingDodgeDir = dir;
                return;
            }

            // 必须持续偏移一段时间才触发
            if (now - _dodgeStartTime < DodgeStableDuration) return;

            _lastDodgeTime = now;
            _dodgeStartTime = 0;
            EventBus.Publish(new DodgeEvent { Direction = dir });
        }

        public void ResetCalibration() { _isCalibrated = false; _hCount = 0; _frameCount = 0; }
    }
}
