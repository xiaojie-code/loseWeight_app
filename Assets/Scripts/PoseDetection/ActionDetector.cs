using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.PoseDetection
{
    public class ActionDetector : MonoBehaviour
    {
        private Vector3 _calibLeftWrist, _calibRightWrist;
        private Vector3 _calibLeftShoulder, _calibRightShoulder;
        private Vector3 _calibLeftElbow, _calibRightElbow;
        private float _calibBodyCenterX;
        private float _calibUpperBodyY;
        private float _shoulderWidth;
        private bool _isCalibrated;

        private float _lastLeftPunchTime, _lastRightPunchTime;
        private float _lastPunchTime;
        private bool _lastPunchWasLeft;
        private float _lastDefendTime, _lastDodgeTime;
        private bool _punchGestureLocked;
        private bool _lockedPunchIsLeft;
        private float _punchGestureLockStartedTime;
        private bool _leftPunchArmed;
        private bool _rightPunchArmed;
        private float _leftPunchPeakGuardDistance;
        private float _rightPunchPeakGuardDistance;
        private float _leftPunchPeakReachGain;
        private float _rightPunchPeakReachGain;
        private float _leftArmedGuardDistance;
        private float _rightArmedGuardDistance;
        private float _leftArmedReachGain;
        private float _rightArmedReachGain;

        [Header("Cooldowns")]
        public float PunchCooldown = 0.32f;
        public float PunchGlobalCooldown = 0.22f;
        public float DefendCooldown = 0.4f;
        public float DodgeCooldown = 0.6f;

        [Header("Punch - ������ֵ���������")]
        public float MinKeypointConfidence = 0.25f;
        public float MinPunchSpeed = 0.16f;       // ��ȭ����ٶ�
        public float MinPunchScore = 1.15f;
        public float HeavyPunchSpeed = 0.45f;     // ��ȭ�ٶ�
        public float HeavyPunchScore = 2.35f;
        public float MinPunchExtend = 0.05f;      // ����ǰ����С����
        public float MinPunchForwardExtend = 0.04f;
        public float MinPunchImpulse = 0.02f;
        public float FastPunchFrameMove = 0.028f;
        public float HeavyPunchFrameMove = 0.08f;
        public float MaxPunchGestureLockSeconds = 0.38f;
        public float PunchResetDistance = 0.10f;
        public float PunchResetReach = 0.055f;
        public float PunchRetractDistance = 0.04f;
        public float PunchRetractReach = 0.03f;
        public float PunchLaunchFromArmedDistance = 0.022f;
        public float PunchSideDominanceRatio = 1.2f;
        public float PunchSideDominanceMargin = 0.35f;
        public float PunchSideImpulseRatio = 1.1f;
        public float PunchSideImpulseMargin = 0.012f;
        public float OppositeSideGuardSeconds = 0.55f;
        public float OppositeSideLaunchMultiplier = 2.0f;
        public float OppositeSideImpulseMultiplier = 1.8f;
        public float OppositeSideSpeedMultiplier = 1.2f;

        [Header("Uppercut Detection")]
        public float UppercutYSpeed = 0.2f;       // ��ȭ��y���������ٶ�
        public float UppercutMinRise = 0.045f;

        [Header("Defend")]
        public float DefendAboveShoulder = 0.03f;
        public float DefendCloseRatio = 0.65f;

        [Header("Dodge")]
        public bool EnableDodgeDetection = false;
        public float DodgeShift = 0.06f;
        public float DuckShift = 0.05f;

        // ��ʷ֡
        private Vector3[] _leftHistory = new Vector3[8];
        private Vector3[] _rightHistory = new Vector3[8];
        private Vector3[] _leftShoulderHistory = new Vector3[8];
        private Vector3[] _rightShoulderHistory = new Vector3[8];
        private float[] _timeHistory = new float[8];
        private int _hIdx;
        private int _hCount;

        private bool _isDefending;
        private int _warmupFrames;
        private const int WARMUP = 10;

        public void Calibrate(PoseFrame frame)
        {
            var nose = frame.GetLandmark(PoseLandmarkIndex.Nose);
            var ls = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rs = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);
            var le = frame.GetLandmark(PoseLandmarkIndex.LeftElbow);
            var re = frame.GetLandmark(PoseLandmarkIndex.RightElbow);
            var lw = frame.GetLandmark(PoseLandmarkIndex.LeftWrist);
            var rw = frame.GetLandmark(PoseLandmarkIndex.RightWrist);
            var lh = frame.GetLandmark(PoseLandmarkIndex.LeftHip);
            var rh = frame.GetLandmark(PoseLandmarkIndex.RightHip);

            _calibLeftShoulder = ls.Position;
            _calibRightShoulder = rs.Position;
            _calibLeftElbow = le.Position;
            _calibRightElbow = re.Position;
            _calibLeftWrist = lw.Position;
            _calibRightWrist = rw.Position;
            _calibBodyCenterX = GetBodyCenterX(ls, rs, lh, rh);
            _calibUpperBodyY = GetUpperBodyY(nose, ls, rs);
            _shoulderWidth = Mathf.Max(GetShoulderDistance(ls, rs), 0.12f);
            _hCount = 0; _hIdx = 0; _warmupFrames = 0;
            _punchGestureLocked = false;
            _punchGestureLockStartedTime = 0f;
            _leftPunchArmed = false;
            _rightPunchArmed = false;
            _leftPunchPeakGuardDistance = 0f;
            _rightPunchPeakGuardDistance = 0f;
            _leftPunchPeakReachGain = 0f;
            _rightPunchPeakReachGain = 0f;
            _leftArmedGuardDistance = 0f;
            _rightArmedGuardDistance = 0f;
            _leftArmedReachGain = 0f;
            _rightArmedReachGain = 0f;
            _lastLeftPunchTime = 0f;
            _lastRightPunchTime = 0f;
            _lastPunchTime = 0f;
            _lastPunchWasLeft = false;
            _isCalibrated = true;
            Debug.Log($"[ActionDetector] Calibrated. SW={_shoulderWidth:F3}, LW={_calibLeftWrist}, RW={_calibRightWrist}");
        }

        public bool CanCalibrate(PoseFrame frame, out string reason)
        {
            reason = "请站在摄像头前";
            if (frame == null || frame.Landmarks == null || frame.Landmarks.Length < 33) return false;

            float requiredConfidence = Mathf.Max(0.25f, MinKeypointConfidence);
            if (!HasConfidence(frame, PoseLandmarkIndex.Nose, requiredConfidence)
                || !HasConfidence(frame, PoseLandmarkIndex.LeftShoulder, requiredConfidence)
                || !HasConfidence(frame, PoseLandmarkIndex.RightShoulder, requiredConfidence))
            {
                reason = "请露出头部和双肩";
                return false;
            }

            if (!HasConfidence(frame, PoseLandmarkIndex.LeftElbow, requiredConfidence)
                || !HasConfidence(frame, PoseLandmarkIndex.RightElbow, requiredConfidence)
                || !HasConfidence(frame, PoseLandmarkIndex.LeftWrist, requiredConfidence)
                || !HasConfidence(frame, PoseLandmarkIndex.RightWrist, requiredConfidence))
            {
                reason = "请露出双臂和双手";
                return false;
            }

            var ls = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rs = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);
            float shoulderWidth = GetShoulderDistance(ls, rs);
            if (shoulderWidth < 0.025f)
            {
                reason = "请正对摄像头，并让双肩入镜";
                return false;
            }
            if (shoulderWidth > 0.85f)
            {
                reason = "请离摄像头远一点";
                return false;
            }

            float centerX = (ls.Position.x + rs.Position.x) * 0.5f;
            if (centerX < 0.05f || centerX > 0.95f)
            {
                reason = "请站到画面中央";
                return false;
            }

            float shoulderY = (ls.Position.y + rs.Position.y) * 0.5f;
            if (shoulderY < 0.04f || shoulderY > 0.94f)
            {
                reason = "请调整手机角度，让上半身入镜";
                return false;
            }

            reason = "姿态有效，请保持不动";
            return true;
        }

        public void ProcessFrame(PoseFrame frame)
        {
            if (!_isCalibrated) { Calibrate(frame); return; }

            var lw = frame.GetLandmark(PoseLandmarkIndex.LeftWrist);
            var rw = frame.GetLandmark(PoseLandmarkIndex.RightWrist);
            var ls = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rs = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);
            var le = frame.GetLandmark(PoseLandmarkIndex.LeftElbow);
            var re = frame.GetLandmark(PoseLandmarkIndex.RightElbow);
            var nose = frame.GetLandmark(PoseLandmarkIndex.Nose);
            var lh = frame.GetLandmark(PoseLandmarkIndex.LeftHip);
            var rh = frame.GetLandmark(PoseLandmarkIndex.RightHip);

            if (!ls.IsVisible || !rs.IsVisible) return;

            bool hasLeftWrist = IsTracked(lw);
            bool hasRightWrist = IsTracked(rw);
            int previousIdx = (_hIdx - 1 + 8) % 8;

            // ��¼��ʷ
            _leftHistory[_hIdx] = hasLeftWrist || _hCount == 0 ? lw.Position : _leftHistory[previousIdx];
            _rightHistory[_hIdx] = hasRightWrist || _hCount == 0 ? rw.Position : _rightHistory[previousIdx];
            _leftShoulderHistory[_hIdx] = ls.Position;
            _rightShoulderHistory[_hIdx] = rs.Position;
            _timeHistory[_hIdx] = Time.time;
            _hIdx = (_hIdx + 1) % 8;
            if (_hCount < 8) _hCount++;

            _warmupFrames++;
            if (_warmupFrames < WARMUP) return;
            if (_hCount < 4) return;

            // �� 2 ֡ǰ���������ٶȣ�ƽ�������Ⱥ��ȶ��ԣ�
            int oldIdx = (_hIdx - 2 + 8) % 8;
            float dt = Time.time - _timeHistory[oldIdx];
            if (dt < 0.03f) return;

            Vector3 leftVel = ((lw.Position - ls.Position) - (_leftHistory[oldIdx] - _leftShoulderHistory[oldIdx])) / dt;
            Vector3 rightVel = ((rw.Position - rs.Position) - (_rightHistory[oldIdx] - _rightShoulderHistory[oldIdx])) / dt;

            // �� 4 ֡ǰ�������жϹ�ȭ�켣
            int olderIdx = (_hIdx - 4 + 8) % 8;

            float now = Time.time;

            UpdatePunchGestureLock(lw, rw, ls, rs, leftVel, rightVel, now);
            UpdatePunchArming(lw, rw, ls, rs, leftVel, rightVel);

            var leftCandidate = BuildPunchCandidate(lw, leftVel, _leftHistory[olderIdx], _leftShoulderHistory[olderIdx], true, le, ls, now);
            var rightCandidate = BuildPunchCandidate(rw, rightVel, _rightHistory[olderIdx], _rightShoulderHistory[olderIdx], false, re, rs, now);
            if (PublishBestPunch(leftCandidate, rightCandidate, now)) return;

            float lastPunchTime = Mathf.Max(_lastLeftPunchTime, _lastRightPunchTime);
            if (lastPunchTime > 0f && now - lastPunchTime < PunchCooldown * 0.75f) return;

            if (hasLeftWrist && hasRightWrist)
            {
                DetectDefend(lw, rw, ls, rs, now);
            }
            else if (_isDefending)
            {
                _isDefending = false;
                EventBus.Publish(new DefendEvent { IsActive = false });
            }

            if (EnableDodgeDetection)
            {
                DetectDodge(ls, rs, nose, lh, rh, now);
            }
        }

        private struct PunchCandidate
        {
            public bool IsValid;
            public bool IsLeft;
            public PunchType Type;
            public PunchPower Power;
            public float Score;
            public float Speed;
            public float FrameMove;
            public float ForwardExtend;
            public float ForwardSpeed;
            public float ReachGain;
            public float ReachIncrease;
            public float GuardDistance;
            public float GuardDistanceGain;
            public float SideImpulse;
            public float UpRise;
            public float UpSpeed;
            public float Activity;
        }

        private PunchCandidate BuildPunchCandidate(PoseLandmark wrist, Vector3 velocity, Vector3 olderPosition, Vector3 olderShoulderPosition, bool isLeft, PoseLandmark elbow, PoseLandmark shoulder, float now)
        {
            var candidate = new PunchCandidate { IsLeft = isLeft };
            if (now - _lastPunchTime < PunchGlobalCooldown) return candidate;
            float lastTime = isLeft ? _lastLeftPunchTime : _lastRightPunchTime;
            if (now - lastTime < PunchCooldown) return candidate;
            if (!(isLeft ? _leftPunchArmed : _rightPunchArmed)) return candidate;
            if (!IsTracked(wrist) || !IsTracked(elbow) || !IsTracked(shoulder)) return candidate;

            Vector3 calibrationWrist = isLeft ? _calibLeftWrist : _calibRightWrist;
            Vector3 calibrationShoulder = isLeft ? _calibLeftShoulder : _calibRightShoulder;
            Vector3 wristLocal = wrist.Position - shoulder.Position;
            Vector3 olderWristLocal = olderPosition - olderShoulderPosition;
            Vector3 calibrationWristLocal = calibrationWrist - calibrationShoulder;

            Vector2 wristPosition = new Vector2(wristLocal.x, wristLocal.y);
            Vector2 olderWristPosition = new Vector2(olderWristLocal.x, olderWristLocal.y);
            Vector2 calibrationWristPosition = new Vector2(calibrationWristLocal.x, calibrationWristLocal.y);
            Vector2 calibrationShoulderPosition = Vector2.zero;

            float speed = velocity.magnitude;
            float frameMove = Vector2.Distance(wristPosition, olderWristPosition);
            float guardBreakMove = Vector2.Distance(wristPosition, calibrationWristPosition);
            float olderGuardBreakMove = Vector2.Distance(olderWristPosition, calibrationWristPosition);
            float guardDistanceGain = guardBreakMove - olderGuardBreakMove;
            float reach = new Vector2(wristLocal.x, wristLocal.y).magnitude;
            float olderReach = new Vector2(olderWristLocal.x, olderWristLocal.y).magnitude;
            float calibrationReach = Vector2.Distance(calibrationWristPosition, calibrationShoulderPosition);
            float reachGain = reach - calibrationReach;
            float reachIncrease = reach - olderReach;
            float armedGuardDistance = isLeft ? _leftArmedGuardDistance : _rightArmedGuardDistance;
            float armedReachGain = isLeft ? _leftArmedReachGain : _rightArmedReachGain;
            float guardLaunch = guardBreakMove - armedGuardDistance;
            float reachLaunch = reachGain - armedReachGain;
            float forwardExtend = calibrationWristLocal.z - wristLocal.z;
            float forwardFrameMove = olderWristLocal.z - wristLocal.z;
            float forwardSpeed = -velocity.z;
            float upRise = olderWristLocal.y - wristLocal.y;
            float upSpeed = -velocity.y;
            float sideImpulse = Mathf.Max(guardDistanceGain, reachIncrease);
            float launchImpulse = Mathf.Max(guardLaunch, reachLaunch);
            float motionImpulse = Mathf.Max(sideImpulse, frameMove * 0.45f);
            float activity = Mathf.Max(
                PositiveRatio(speed, MinPunchSpeed),
                PositiveRatio(frameMove, FastPunchFrameMove),
                PositiveRatio(motionImpulse, MinPunchImpulse));
            candidate.Activity = activity;

            float extensionScore = Mathf.Max(
                PositiveRatio(forwardExtend, MinPunchForwardExtend),
                PositiveRatio(reachGain, MinPunchExtend),
                PositiveRatio(guardBreakMove, MinPunchExtend));
            float speedScore = Mathf.Max(
                PositiveRatio(speed, MinPunchSpeed),
                PositiveRatio(forwardSpeed, MinPunchSpeed * 0.6f),
                PositiveRatio(upSpeed, UppercutYSpeed * 0.7f));
            float impulseScore = Mathf.Max(
                PositiveRatio(sideImpulse, MinPunchImpulse),
                PositiveRatio(forwardFrameMove, MinPunchForwardExtend * 0.7f),
                PositiveRatio(upRise, UppercutMinRise * 0.75f));

            float score = extensionScore * 0.55f + speedScore * 0.35f + impulseScore * 0.5f;
            bool hasExtension = forwardExtend >= MinPunchForwardExtend
                || reachGain >= MinPunchExtend
                || guardBreakMove >= MinPunchExtend;
            bool hasPlanarCue = reachGain >= MinPunchExtend * 0.6f
                || guardBreakMove >= MinPunchExtend * 0.8f
                || forwardExtend >= MinPunchForwardExtend * 0.75f;
            bool hasPlanarImpulse = sideImpulse >= MinPunchImpulse
                || (frameMove >= FastPunchFrameMove && sideImpulse >= MinPunchImpulse * 0.45f);
            bool launchedFromArmedPose = launchImpulse >= PunchLaunchFromArmedDistance
                && sideImpulse >= MinPunchImpulse * 0.5f;
            bool isOppositeSideAfterRecentPunch = IsOppositeSideAfterRecentPunch(isLeft, now);
            bool hasDepthImpulse = forwardFrameMove >= MinPunchForwardExtend * 0.7f
                || upRise >= UppercutMinRise * 0.75f;
            bool hasAttackImpulse = hasPlanarImpulse
                || (hasDepthImpulse && frameMove >= FastPunchFrameMove * 0.55f);
            bool hasSpeed = speed >= MinPunchSpeed
                || forwardSpeed >= MinPunchSpeed * 0.6f
                || upSpeed >= UppercutYSpeed * 0.7f;

            if (!hasExtension || !hasPlanarCue || !hasAttackImpulse || !hasSpeed || !launchedFromArmedPose || score < MinPunchScore) return candidate;
            if (isOppositeSideAfterRecentPunch && !IsDeliberateOppositeSidePunch(launchImpulse, sideImpulse, speed, frameMove, reachGain)) return candidate;

            bool isUppercut = upRise >= UppercutMinRise
                && upSpeed >= UppercutYSpeed
                && upRise >= Mathf.Max(0.035f, Mathf.Abs(forwardExtend) * 0.45f);

            float heavyScore = score
                + PositiveRatio(speed, HeavyPunchSpeed) * 0.35f
                + PositiveRatio(frameMove, HeavyPunchFrameMove) * 0.3f
                + PositiveRatio(forwardExtend, _shoulderWidth * 0.55f) * 0.35f;
            var power = speed >= HeavyPunchSpeed
                || frameMove >= HeavyPunchFrameMove
                || forwardExtend >= _shoulderWidth * 0.55f
                || heavyScore >= HeavyPunchScore
                    ? PunchPower.Heavy
                    : PunchPower.Light;

            candidate.IsValid = true;
            candidate.Type = isLeft
                ? (isUppercut ? PunchType.LeftUppercut : PunchType.LeftStraight)
                : (isUppercut ? PunchType.RightUppercut : PunchType.RightStraight);
            candidate.Power = power;
            candidate.Score = score;
            candidate.Speed = speed;
            candidate.FrameMove = frameMove;
            candidate.ForwardExtend = forwardExtend;
            candidate.ForwardSpeed = forwardSpeed;
            candidate.ReachGain = reachGain;
            candidate.ReachIncrease = reachIncrease;
            candidate.GuardDistance = guardBreakMove;
            candidate.GuardDistanceGain = guardDistanceGain;
            candidate.SideImpulse = sideImpulse;
            candidate.UpRise = upRise;
            candidate.UpSpeed = upSpeed;
            candidate.Activity = activity;
            return candidate;
        }

        private bool PublishBestPunch(PunchCandidate leftCandidate, PunchCandidate rightCandidate, float now)
        {
            if (leftCandidate.IsValid && IsDominatedByOtherSide(leftCandidate, rightCandidate)) leftCandidate.IsValid = false;
            if (rightCandidate.IsValid && IsDominatedByOtherSide(rightCandidate, leftCandidate)) rightCandidate.IsValid = false;
            if (!leftCandidate.IsValid && !rightCandidate.IsValid) return false;

            PunchCandidate selected;
            if (leftCandidate.IsValid && rightCandidate.IsValid)
            {
                if (HasClearlyStrongerSideImpulse(leftCandidate, rightCandidate)) selected = leftCandidate;
                else if (HasClearlyStrongerSideImpulse(rightCandidate, leftCandidate)) selected = rightCandidate;
                else
                {
                    float leftSelectionScore = GetSelectionScore(leftCandidate);
                    float rightSelectionScore = GetSelectionScore(rightCandidate);
                    selected = leftSelectionScore > rightSelectionScore ? leftCandidate : rightCandidate;
                    if (Mathf.Abs(leftSelectionScore - rightSelectionScore) < 0.15f)
                        selected = leftCandidate.Activity >= rightCandidate.Activity ? leftCandidate : rightCandidate;
                }
            }
            else
            {
                selected = leftCandidate.IsValid ? leftCandidate : rightCandidate;
            }

            if (selected.IsLeft)
            {
                _lastLeftPunchTime = now;
                _leftPunchArmed = false;
                _leftPunchPeakGuardDistance = selected.GuardDistance;
                _leftPunchPeakReachGain = Mathf.Max(0f, selected.ReachGain);
            }
            else
            {
                _lastRightPunchTime = now;
                _rightPunchArmed = false;
                _rightPunchPeakGuardDistance = selected.GuardDistance;
                _rightPunchPeakReachGain = Mathf.Max(0f, selected.ReachGain);
            }
            _lastPunchTime = now;
            _lastPunchWasLeft = selected.IsLeft;
            _punchGestureLocked = true;
            _lockedPunchIsLeft = selected.IsLeft;
            _punchGestureLockStartedTime = now;

            if (_isDefending)
            {
                _isDefending = false;
                EventBus.Publish(new DefendEvent { IsActive = false });
            }

            EventBus.Publish(new PunchDetectedEvent { Type = selected.Type, Power = selected.Power, Speed = selected.Speed });
            LoseWeight.Game.ScreenDebugLog.Log($"[Action] {(selected.IsLeft ? "L" : "R")} {selected.Type} {selected.Power} score={selected.Score:F2} act={selected.Activity:F2} sideImp={selected.SideImpulse:F3} spd={selected.Speed:F2} frame={selected.FrameMove:F3} reach={selected.ReachGain:F3}/{selected.ReachIncrease:F3} guardGain={selected.GuardDistanceGain:F3} zExt={selected.ForwardExtend:F3} zSpd={selected.ForwardSpeed:F2} up={selected.UpRise:F3}/{selected.UpSpeed:F2}");
            return true;
        }

        private bool HasClearlyStrongerSideImpulse(PunchCandidate candidate, PunchCandidate other)
        {
            return candidate.SideImpulse >= other.SideImpulse * PunchSideImpulseRatio
                && candidate.SideImpulse - other.SideImpulse >= PunchSideImpulseMargin;
        }

        private bool IsDominatedByOtherSide(PunchCandidate candidate, PunchCandidate other)
        {
            return other.Activity >= candidate.Activity * PunchSideDominanceRatio
                && other.Activity - candidate.Activity >= PunchSideDominanceMargin
                && HasClearlyStrongerSideImpulse(other, candidate);
        }

        private static float GetSelectionScore(PunchCandidate candidate)
        {
            return candidate.Score + candidate.Activity * 0.55f;
        }

        private bool IsOppositeSideAfterRecentPunch(bool isLeft, float now)
        {
            return _lastPunchTime > 0f
                && isLeft != _lastPunchWasLeft
                && now - _lastPunchTime <= OppositeSideGuardSeconds;
        }

        private bool IsDeliberateOppositeSidePunch(float launchImpulse, float sideImpulse, float speed, float frameMove, float reachGain)
        {
            return launchImpulse >= PunchLaunchFromArmedDistance * OppositeSideLaunchMultiplier
                && sideImpulse >= MinPunchImpulse * OppositeSideImpulseMultiplier
                && speed >= MinPunchSpeed * OppositeSideSpeedMultiplier
                && frameMove >= FastPunchFrameMove * 1.15f
                && reachGain >= MinPunchExtend * 0.75f;
        }

        private static bool HasConfidence(PoseFrame frame, PoseLandmarkIndex index, float requiredConfidence)
        {
            return frame.GetLandmark(index).Confidence >= requiredConfidence;
        }

        private static float GetShoulderDistance(PoseLandmark leftShoulder, PoseLandmark rightShoulder)
        {
            return Vector2.Distance(
                new Vector2(leftShoulder.Position.x, leftShoulder.Position.y),
                new Vector2(rightShoulder.Position.x, rightShoulder.Position.y));
        }

        private void UpdatePunchArming(PoseLandmark leftWrist, PoseLandmark rightWrist, PoseLandmark leftShoulder, PoseLandmark rightShoulder, Vector3 leftVelocity, Vector3 rightVelocity)
        {
            bool wasLeftArmed = _leftPunchArmed;
            bool wasRightArmed = _rightPunchArmed;
            _leftPunchArmed = UpdatePunchArmState(leftWrist, leftShoulder, leftVelocity, true, _leftPunchArmed);
            _rightPunchArmed = UpdatePunchArmState(rightWrist, rightShoulder, rightVelocity, false, _rightPunchArmed);
            if (!wasLeftArmed && _leftPunchArmed) ArmPunch(true, leftWrist, leftShoulder);
            if (!wasRightArmed && _rightPunchArmed) ArmPunch(false, rightWrist, rightShoulder);
        }

        private void ArmPunch(bool isLeft, PoseLandmark wrist, PoseLandmark shoulder)
        {
            GetPunchGuardMetrics(wrist, shoulder, isLeft, out float guardDistance, out float reachGain, out _);
            if (isLeft)
            {
                _leftPunchPeakGuardDistance = 0f;
                _leftPunchPeakReachGain = 0f;
                _leftArmedGuardDistance = guardDistance;
                _leftArmedReachGain = reachGain;
            }
            else
            {
                _rightPunchPeakGuardDistance = 0f;
                _rightPunchPeakReachGain = 0f;
                _rightArmedGuardDistance = guardDistance;
                _rightArmedReachGain = reachGain;
            }
        }

        private bool UpdatePunchArmState(PoseLandmark wrist, PoseLandmark shoulder, Vector3 velocity, bool isLeft, bool isAlreadyArmed)
        {
            if (!IsTracked(wrist) || !IsTracked(shoulder)) return false;
            return isAlreadyArmed || IsPunchReadyToRearm(wrist, shoulder, velocity, isLeft);
        }

        private bool IsPunchReadyToRearm(PoseLandmark wrist, PoseLandmark shoulder, Vector3 velocity, bool isLeft)
        {
            GetPunchGuardMetrics(wrist, shoulder, isLeft, out float guardDistance, out float reachGain, out float forwardExtend);
            float peakGuardDistance = isLeft ? _leftPunchPeakGuardDistance : _rightPunchPeakGuardDistance;
            float peakReachGain = isLeft ? _leftPunchPeakReachGain : _rightPunchPeakReachGain;

            bool nearGuard = guardDistance <= PunchResetDistance
                && reachGain <= PunchResetReach
                && forwardExtend <= MinPunchForwardExtend * 1.25f;
            bool retractedFromPeak = (peakGuardDistance > 0f && peakGuardDistance - guardDistance >= PunchRetractDistance)
                || (peakReachGain > 0f && peakReachGain - reachGain >= PunchRetractReach);
            bool notFullyExtended = reachGain <= Mathf.Max(PunchResetReach * 1.7f, peakReachGain - PunchRetractReach);

            return nearGuard || (retractedFromPeak && notFullyExtended);
        }

        private void GetPunchGuardMetrics(PoseLandmark wrist, PoseLandmark shoulder, bool isLeft, out float guardDistance, out float reachGain, out float forwardExtend)
        {
            Vector3 calibrationWrist = isLeft ? _calibLeftWrist : _calibRightWrist;
            Vector3 calibrationShoulder = isLeft ? _calibLeftShoulder : _calibRightShoulder;
            Vector3 wristLocal = wrist.Position - shoulder.Position;
            Vector3 calibrationWristLocal = calibrationWrist - calibrationShoulder;

            Vector2 wristPosition = new Vector2(wristLocal.x, wristLocal.y);
            Vector2 calibrationWristPosition = new Vector2(calibrationWristLocal.x, calibrationWristLocal.y);
            guardDistance = Vector2.Distance(wristPosition, calibrationWristPosition);
            reachGain = wristPosition.magnitude - calibrationWristPosition.magnitude;
            forwardExtend = calibrationWristLocal.z - wristLocal.z;
        }

        private void UpdatePunchGestureLock(PoseLandmark leftWrist, PoseLandmark rightWrist, PoseLandmark leftShoulder, PoseLandmark rightShoulder, Vector3 leftVelocity, Vector3 rightVelocity, float now)
        {
            if (!_punchGestureLocked) return;

            if (now - _punchGestureLockStartedTime >= MaxPunchGestureLockSeconds)
            {
                _punchGestureLocked = false;
                return;
            }

            var wrist = _lockedPunchIsLeft ? leftWrist : rightWrist;
            var shoulder = _lockedPunchIsLeft ? leftShoulder : rightShoulder;
            var velocity = _lockedPunchIsLeft ? leftVelocity : rightVelocity;

            if (!IsTracked(wrist) || !IsTracked(shoulder))
            {
                _punchGestureLocked = false;
                return;
            }

            if (IsPunchReadyToRearm(wrist, shoulder, velocity, _lockedPunchIsLeft))
            {
                _punchGestureLocked = false;
            }
        }

        private bool IsTracked(PoseLandmark landmark)
        {
            return landmark.Confidence >= MinKeypointConfidence;
        }

        private static float PositiveRatio(float value, float threshold)
        {
            return threshold > 0f ? Mathf.Max(0f, value) / threshold : 0f;
        }

        private void DetectDefend(PoseLandmark lw, PoseLandmark rw, PoseLandmark ls, PoseLandmark rs, float now)
        {
            bool leftHigh = lw.Position.y < ls.Position.y - DefendAboveShoulder;
            bool rightHigh = rw.Position.y < rs.Position.y - DefendAboveShoulder;
            float wristDist = Mathf.Abs(lw.Position.x - rw.Position.x);
            bool close = wristDist < _shoulderWidth * DefendCloseRatio;
            bool shouldDefend = leftHigh && rightHigh && close;

            if (shouldDefend && !_isDefending && now - _lastDefendTime > DefendCooldown)
            {
                _isDefending = true;
                _lastDefendTime = now;
                EventBus.Publish(new DefendEvent { IsActive = true });
            }
            else if (!shouldDefend && _isDefending)
            {
                _isDefending = false;
                EventBus.Publish(new DefendEvent { IsActive = false });
            }
        }

        private void DetectDodge(PoseLandmark ls, PoseLandmark rs, PoseLandmark nose, PoseLandmark lh, PoseLandmark rh, float now)
        {
            if (now - _lastDodgeTime < DodgeCooldown) return;

            float shift = GetBodyCenterX(ls, rs, lh, rh) - _calibBodyCenterX;
            float drop = GetUpperBodyY(nose, ls, rs) - _calibUpperBodyY;

            if (drop > DuckShift && drop > Mathf.Abs(shift) * 0.6f)
            {
                _lastDodgeTime = now;
                EventBus.Publish(new DodgeEvent { Direction = DodgeDirection.Down });
                Debug.Log($"[Action] Dodge Down drop={drop:F2} shift={shift:F2}");
                return;
            }

            if (Mathf.Abs(shift) > DodgeShift)
            {
                _lastDodgeTime = now;
                var direction = shift < 0 ? DodgeDirection.Left : DodgeDirection.Right;
                EventBus.Publish(new DodgeEvent { Direction = direction });
                Debug.Log($"[Action] Dodge {direction} shift={shift:F2} drop={drop:F2}");
            }
        }

        private static float GetBodyCenterX(PoseLandmark ls, PoseLandmark rs, PoseLandmark lh, PoseLandmark rh)
        {
            bool hasLeftShoulder = ls.IsVisible;
            bool hasRightShoulder = rs.IsVisible;
            bool hasLeftHip = lh.IsVisible;
            bool hasRightHip = rh.IsVisible;

            if (hasLeftShoulder && hasRightShoulder)
            {
                float shoulderCenter = (ls.Position.x + rs.Position.x) * 0.5f;
                if (hasLeftHip && hasRightHip)
                {
                    float hipCenter = (lh.Position.x + rh.Position.x) * 0.5f;
                    return Mathf.Lerp(shoulderCenter, hipCenter, 0.25f);
                }
                return shoulderCenter;
            }

            if (hasLeftHip && hasRightHip)
                return (lh.Position.x + rh.Position.x) * 0.5f;

            return 0.5f;
        }

        private static float GetUpperBodyY(PoseLandmark nose, PoseLandmark ls, PoseLandmark rs)
        {
            float total = 0f;
            int count = 0;
            if (nose.IsVisible) { total += nose.Position.y; count++; }
            if (ls.IsVisible) { total += ls.Position.y; count++; }
            if (rs.IsVisible) { total += rs.Position.y; count++; }
            return count > 0 ? total / count : 0.5f;
        }

        public void ResetCalibration()
        {
            _isCalibrated = false;
            _hCount = 0;
            _punchGestureLocked = false;
            _leftPunchArmed = false;
            _rightPunchArmed = false;
            _leftPunchPeakGuardDistance = 0f;
            _rightPunchPeakGuardDistance = 0f;
            _leftPunchPeakReachGain = 0f;
            _rightPunchPeakReachGain = 0f;
            _leftArmedGuardDistance = 0f;
            _rightArmedGuardDistance = 0f;
            _leftArmedReachGain = 0f;
            _rightArmedReachGain = 0f;
            _lastPunchTime = 0f;
            _lastPunchWasLeft = false;
        }
    }
}
