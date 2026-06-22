using UnityEngine;
using LoseWeight.PoseDetection;

namespace LoseWeight.CannonGame
{
    /// <summary>
    /// Pose-to-cannon controls for the cannon game only.
    /// Reads raw pose frames and converts arm motion into horizontal aim + downward-fire.
    /// </summary>
    public class CannonPoseInput
    {
        private const float MinConfidence = 0.18f;
        private const float RelativeAimScale = 5.45f;
        private const float ControlHoldDeadZone = 0.0032f;
        private const float ControlReleaseDeadZone = 0.009f;
        private const float ControlSoftMoveDelta = 0.012f;
        private const float ControlFastMoveDelta = 0.034f;
        private const float ControlSnapDelta = 0.060f;
        private const float ControlSlowAlpha = 0.46f;
        private const float ControlFastAlpha = 0.90f;
        private const float ControlMaxStep = 0.26f;
        private const float RawStillDelta = 0.0032f;
        private const float AimDeadZone = 0.0035f;
        private const float AimMaxStep = 1f;
        private const float CorePointConfidence = 0.22f;
        private const float StickyHandMinScore = 0.18f;
        private const int AimCalibrationFrames = 12;
        private const float ShoulderAimSpanScale = 1.15f;
        private const float MinShoulderAimHalfSpan = 0.15f;
        private const float MaxShoulderAimHalfSpan = 0.32f;
        private const float RaiseWindow = 0.11f;
        private const float DropDistance = 0.068f;
        private const float DropVelocity = 0.52f;
        private const float BigDropDistance = 0.118f;
        private const float FirePreLockDropDistance = 0.038f;
        private const float AimLockDropDistance = 0.050f;
        private const float SingleFrameDropDistance = 0.050f;
        private const float FireAimLockDuration = 0.58f;
        private const float FireCooldown = 0.28f;
        private const float FireReadyDelay = 0.14f;
        private const float ActiveHandRaiseWindow = 0.34f;
        private const float ActiveHandMinExtension = 0.055f;
        private const float StrongHandScore = 0.32f;

        private bool _hasAim;
        private bool _armed;
        private bool _lastHandValid;
        private bool _aimLockActive;
        private bool _calibratedAimCenter;
        private bool _controlUsesRightHand;
        private float _lastY;
        private float _lastTime;
        private float _peakY = 1f;
        private float _cooldownUntil;
        private float _lockedAimX = 0.5f;
        private float _lockUntil;
        private float _centerControlX;
        private float _calibrationControlSum;
        private float _filteredControlX;
        private float _lastRawControlX;
        private float _armedSince;
        private int _calibrationSamples;
        private int _dropFrames;
        private int _stillControlFrames;
        private int _selectedHandWeakFrames;
        private int _rawMoveDirection;
        private int _rawMoveFrames;
        private bool _hasFilteredControlX;
        private bool _hasLastRawControlX;

#if UNITY_ANDROID && !UNITY_EDITOR
        private const bool UsePortraitFrontCameraMapping = true;
#else
        private const bool UsePortraitFrontCameraMapping = false;
#endif

        public float AimX { get; private set; } = 0.5f;
        public float AimY { get; private set; } = 0.62f;
        public float Charge { get; private set; }
        public bool HasPose { get; private set; }
        public bool FireTriggered { get; private set; }
        public bool AimFrozen => _aimLockActive;
        public bool IsCalibrated => _calibratedAimCenter;
        public string Hint { get; private set; } = "\u62ac\u9ad8\u624b\u81c2\uff0c\u5de6\u53f3\u79fb\u52a8\u7784\u51c6";

        public void Reset()
        {
            _hasAim = false;
            _armed = false;
            _lastHandValid = false;
            _aimLockActive = false;
            _calibratedAimCenter = false;
            _controlUsesRightHand = false;
            _lastY = 0f;
            _lastTime = 0f;
            _peakY = 1f;
            _cooldownUntil = 0f;
            _lockedAimX = 0.5f;
            _lockUntil = 0f;
            _centerControlX = 0f;
            _calibrationControlSum = 0f;
            _filteredControlX = 0f;
            _lastRawControlX = 0f;
            _armedSince = 0f;
            _calibrationSamples = 0;
            _dropFrames = 0;
            _stillControlFrames = 0;
            _selectedHandWeakFrames = 0;
            _rawMoveDirection = 0;
            _rawMoveFrames = 0;
            _hasFilteredControlX = false;
            _hasLastRawControlX = false;
            Charge = 0f;
            HasPose = false;
            FireTriggered = false;
            Hint = "\u62ac\u9ad8\u624b\u81c2\uff0c\u5de6\u53f3\u79fb\u52a8\u7784\u51c6";
            AimX = 0.5f;
            AimY = 0.62f;
        }

        public void PrepareForRound()
        {
            _armed = false;
            _lastHandValid = false;
            _aimLockActive = false;
            _lastY = 0f;
            _lastTime = 0f;
            _peakY = 1f;
            _lockedAimX = AimX;
            _lockUntil = 0f;
            _armedSince = 0f;
            _dropFrames = 0;
            _calibratedAimCenter = false;
            _calibrationControlSum = 0f;
            _calibrationSamples = 0;
            ResetControlFilter();
            AimX = 0.5f;
            _hasAim = false;
            Charge = 0f;
            FireTriggered = false;
        }

        public void Process(PoseFrame frame, float now)
        {
            FireTriggered = false;
            HasPose = false;

            if (!TrySelectHand(frame, out PoseLandmark wrist, out PoseLandmark shoulder, out bool usingRightHand))
            {
                _armed = false;
                _lastHandValid = false;
                _aimLockActive = false;
                _calibratedAimCenter = false;
                _hasFilteredControlX = false;
                _hasLastRawControlX = false;
                _stillControlFrames = 0;
                _selectedHandWeakFrames = 0;
                _rawMoveDirection = 0;
                _rawMoveFrames = 0;
                _dropFrames = 0;
                Charge = 0f;
                AimX = _hasAim ? Mathf.Lerp(AimX, 0.5f, 0.35f) : 0.5f;
                AimY = 0.62f;
                _hasAim = true;
                Hint = "\u8bf7\u7ad9\u5230\u955c\u5934\u5185\uff0c\u5e76\u9732\u51fa\u80a9\u8180\u548c\u624b\u8155";
                return;
            }

            HasPose = true;

            float wristDown = GetScreenDown(wrist.Position);
            float shoulderDown = GetScreenDown(shoulder.Position);
            bool handRaised = wristDown < shoulderDown + RaiseWindow;
            float targetAimX = GetTargetAimX(frame, wrist, usingRightHand, now);

            if (!_calibratedAimCenter)
            {
                ApplyAim(0.5f);
                _lastY = wristDown;
                _lastTime = now;
                _peakY = wristDown;
                _armed = false;
                _lastHandValid = true;
                _dropFrames = 0;
                Charge = 0f;
                Hint = "\u8bf7\u4fdd\u6301\u624b\u81c2\u7a33\u5b9a\uff0c\u6b63\u5728\u6821\u51c6\u7784\u51c6";
                return;
            }

            if (!_lastHandValid)
            {
                ApplyAim(targetAimX);
                _lastY = wristDown;
                _lastTime = now;
                _peakY = wristDown;
                _armed = handRaised;
                _armedSince = _armed ? now : 0f;
                _lastHandValid = true;
                _dropFrames = 0;
                Charge = 0f;
                Hint = handRaised
                    ? "\u624b\u81c2\u7a33\u5b9a\u540e\uff0c\u5feb\u901f\u4e0b\u843d\u5f00\u70ae"
                    : "\u5148\u628a\u624b\u62ac\u5230\u80a9\u8180\u9644\u8fd1";
                return;
            }

            float dt = Mathf.Max(0.016f, now - _lastTime);
            float downwardVelocity = (wristDown - _lastY) / dt;

            if (handRaised || (_armed && wristDown < _peakY))
            {
                bool wasArmed = _armed;
                _armed = true;
                if (!wasArmed)
                    _armedSince = now;
                _peakY = Mathf.Min(_peakY, wristDown);
            }

            float dropAmount = Mathf.Max(0f, wristDown - _peakY);
            Charge = _armed ? Mathf.Clamp01(dropAmount / DropDistance) : 0f;

            float frameDrop = wristDown - _lastY;
            bool singleFrameDrop = frameDrop >= SingleFrameDropDistance && downwardVelocity >= DropVelocity * 0.75f;
            bool downwardMotion = frameDrop > 0.012f && downwardVelocity >= DropVelocity * 0.55f;
            _dropFrames = downwardMotion ? Mathf.Min(_dropFrames + 1, 8) : 0;
            bool likelyDrop = _armed && _dropFrames >= 2 && dropAmount >= FirePreLockDropDistance;
            bool startingDrop = _armed && _dropFrames >= 2 && (dropAmount >= AimLockDropDistance || singleFrameDrop);
            if (likelyDrop || startingDrop)
            {
                if (!_aimLockActive)
                    _lockedAimX = AimX;
                _aimLockActive = true;
                _lockUntil = now + FireAimLockDuration;
                SyncFilteredControlToAim(_lockedAimX);
            }

            if (_aimLockActive && now > _lockUntil)
                _aimLockActive = false;

            if (_aimLockActive)
                targetAimX = _lockedAimX;

            ApplyAim(targetAimX);

            bool fireReady = _armed && _armedSince > 0f && now - _armedSince >= FireReadyDelay;
            bool quickDrop = dropAmount >= DropDistance
                && ((downwardVelocity >= DropVelocity && _dropFrames >= 2) || _dropFrames >= 3 || singleFrameDrop);
            bool bigDrop = dropAmount >= BigDropDistance && _dropFrames >= 1 && downwardVelocity >= DropVelocity * 0.45f;
            if (fireReady && now >= _cooldownUntil && (quickDrop || bigDrop))
            {
                FireTriggered = true;
                _cooldownUntil = now + FireCooldown;
                _lockUntil = now + FireAimLockDuration;
                _aimLockActive = true;
                _armed = false;
                _armedSince = 0f;
                _peakY = wristDown;
                _dropFrames = 0;
                Charge = 0f;
                Hint = "\u5f00\u70ae!";
            }
            else if (now < _cooldownUntil)
            {
                Hint = "\u88c5\u586b\u4e2d";
            }
            else
            {
                Hint = _armed
                    ? "\u5927\u5e45\u5feb\u901f\u4e0b\u843d\u624b\u81c2\u5f00\u70ae"
                    : "\u62ac\u9ad8\u624b\u81c2\u51c6\u5907\u5f00\u70ae";
            }

            if (now >= _lockUntil)
                _aimLockActive = false;

            _lastY = wristDown;
            _lastTime = now;
        }

        private float GetTargetAimX(PoseFrame frame, PoseLandmark wrist, bool usingRightHand, float now)
        {
            float controlX = GetControlRight(frame, wrist, usingRightHand);
            if (_controlUsesRightHand != usingRightHand)
            {
                _controlUsesRightHand = usingRightHand;
                _calibrationControlSum = 0f;
                _calibrationSamples = 0;
                _calibratedAimCenter = false;
                _aimLockActive = false;
                _lockUntil = 0f;
                AimX = 0.5f;
                _hasAim = true;
                ResetControlFilter();
            }

            controlX = GetFilteredControlX(controlX);
            if (!_calibratedAimCenter)
            {
                _calibrationControlSum += controlX;
                _calibrationSamples++;
                if (_calibrationSamples >= AimCalibrationFrames)
                {
                    _centerControlX = _calibrationControlSum / Mathf.Max(1, _calibrationSamples);
                    _calibratedAimCenter = true;
                }
                return 0.5f;
            }

            float delta = controlX - _centerControlX;
            float targetAimX = 0.5f + delta * RelativeAimScale;
            return Mathf.Clamp(targetAimX, 0.02f, 0.98f);
        }

        private float GetFilteredControlX(float controlX)
        {
            if (!_hasFilteredControlX)
            {
                _filteredControlX = controlX;
                _hasFilteredControlX = true;
                _lastRawControlX = controlX;
                _hasLastRawControlX = true;
                return controlX;
            }

            float rawDelta = _hasLastRawControlX ? controlX - _lastRawControlX : 0f;
            _lastRawControlX = controlX;
            _hasLastRawControlX = true;

            float delta = controlX - _filteredControlX;
            float absDelta = Mathf.Abs(delta);
            int rawDirection = Mathf.Abs(rawDelta) > RawStillDelta ? (rawDelta > 0f ? 1 : -1) : 0;
            if (rawDirection == 0)
            {
                _rawMoveFrames = 0;
            }
            else if (rawDirection == _rawMoveDirection)
            {
                _rawMoveFrames = Mathf.Min(_rawMoveFrames + 1, 8);
            }
            else
            {
                _rawMoveDirection = rawDirection;
                _rawMoveFrames = 1;
            }

            if (Mathf.Abs(rawDelta) <= RawStillDelta && absDelta <= ControlReleaseDeadZone)
            {
                _stillControlFrames = Mathf.Min(_stillControlFrames + 1, 12);
                return _filteredControlX;
            }

            if (absDelta < ControlHoldDeadZone)
            {
                _stillControlFrames = Mathf.Min(_stillControlFrames + 1, 12);
                return _filteredControlX;
            }

            if (absDelta < ControlSoftMoveDelta && _rawMoveFrames < 2)
                return _filteredControlX;

            float followRatio = Mathf.InverseLerp(ControlSoftMoveDelta, ControlFastMoveDelta, absDelta);
            float alpha = Mathf.Lerp(ControlSlowAlpha, ControlFastAlpha, followRatio);
            if (absDelta >= ControlSnapDelta)
                alpha = 1f;

            if (_stillControlFrames >= 3 && Mathf.Abs(rawDelta) <= RawStillDelta && absDelta < ControlSoftMoveDelta)
                alpha *= 0.65f;

            _stillControlFrames = 0;
            _filteredControlX += Mathf.Clamp(delta * alpha, -ControlMaxStep, ControlMaxStep);
            return _filteredControlX;
        }

        private void SyncFilteredControlToAim(float aimX)
        {
            if (!_calibratedAimCenter)
                return;

            _filteredControlX = _centerControlX + (Mathf.Clamp01(aimX) - 0.5f) / RelativeAimScale;
            _hasFilteredControlX = true;
            _lastRawControlX = _filteredControlX;
            _hasLastRawControlX = true;
            _stillControlFrames = 0;
            _rawMoveDirection = 0;
            _rawMoveFrames = 0;
        }

        private float GetControlRight(PoseFrame frame, PoseLandmark wrist, bool usingRightHand)
        {
            return GetArmCenterRight(frame, wrist, usingRightHand);
        }

        private float GetArmCenterRight(PoseFrame frame, PoseLandmark wrist, bool usingRightHand)
        {
            float sum = 0f;
            float weight = 0f;
            AddControlPoint(wrist, 0.70f, ref sum, ref weight);
            AddControlPoint(frame.GetLandmark(usingRightHand ? PoseLandmarkIndex.RightElbow : PoseLandmarkIndex.LeftElbow), 0.30f, ref sum, ref weight);
            return weight > 0f ? sum / weight : GetScreenRight(wrist.Position);
        }

        private void AddControlPoint(PoseLandmark landmark, float pointWeight, ref float sum, ref float weight)
        {
            if (landmark.Confidence < CorePointConfidence)
                return;

            float confidenceWeight = pointWeight * Mathf.Clamp01(landmark.Confidence);
            sum += GetScreenRight(landmark.Position) * confidenceWeight;
            weight += confidenceWeight;
        }

        private static float GetScreenRight(Vector3 raw)
        {
            return UsePortraitFrontCameraMapping
                ? Mathf.Clamp01(1f - raw.y)
                : Mathf.Clamp01(1f - raw.x);
        }

        private static float GetScreenDown(Vector3 raw)
        {
            return UsePortraitFrontCameraMapping
                ? Mathf.Clamp01(raw.x)
                : Mathf.Clamp01(raw.y);
        }

        private void ApplyAim(float targetAimX)
        {
            float aimDelta = Mathf.Abs(targetAimX - AimX);
            if (_hasAim && aimDelta < AimDeadZone)
                targetAimX = AimX;

            if (_hasAim)
            {
                float delta = targetAimX - AimX;
                AimX += Mathf.Clamp(delta, -AimMaxStep, AimMaxStep);
            }
            else
            {
                AimX = targetAimX;
            }
            AimY = 0.62f;
            _hasAim = true;
        }

        private bool TrySelectHand(PoseFrame frame, out PoseLandmark wrist, out PoseLandmark shoulder, out bool usingRightHand)
        {
            var leftWrist = frame.GetLandmark(PoseLandmarkIndex.LeftWrist);
            var rightWrist = frame.GetLandmark(PoseLandmarkIndex.RightWrist);
            var leftShoulder = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rightShoulder = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);

            float leftScore = GetHandScore(leftWrist, leftShoulder);
            float rightScore = GetHandScore(rightWrist, rightShoulder);

            if (leftScore <= 0f && rightScore <= 0f)
            {
                wrist = default(PoseLandmark);
                shoulder = default(PoseLandmark);
                usingRightHand = false;
                return false;
            }

            bool leftStrong = leftScore >= StrongHandScore;
            bool rightStrong = rightScore >= StrongHandScore;

            if (_lastHandValid)
            {
                float selectedScore = _controlUsesRightHand ? rightScore : leftScore;
                float otherScore = _controlUsesRightHand ? leftScore : rightScore;
                if (selectedScore >= StickyHandMinScore || (selectedScore > 0f && _selectedHandWeakFrames < 3 && otherScore < StrongHandScore + 0.12f))
                {
                    _selectedHandWeakFrames = selectedScore >= StickyHandMinScore ? 0 : _selectedHandWeakFrames + 1;
                    wrist = _controlUsesRightHand ? rightWrist : leftWrist;
                    shoulder = _controlUsesRightHand ? rightShoulder : leftShoulder;
                    usingRightHand = _controlUsesRightHand;
                    return true;
                }
            }

            if (leftStrong && rightStrong)
            {
                wrist = rightWrist;
                shoulder = rightShoulder;
                usingRightHand = true;
            }
            else if (_lastHandValid && _controlUsesRightHand && rightScore >= StickyHandMinScore)
            {
                wrist = rightWrist;
                shoulder = rightShoulder;
                usingRightHand = true;
            }
            else if (_lastHandValid && !_controlUsesRightHand && leftScore >= StickyHandMinScore)
            {
                wrist = leftWrist;
                shoulder = leftShoulder;
                usingRightHand = false;
            }
            else if (leftStrong)
            {
                wrist = leftWrist;
                shoulder = leftShoulder;
                usingRightHand = false;
            }
            else if (rightStrong)
            {
                wrist = rightWrist;
                shoulder = rightShoulder;
                usingRightHand = true;
            }
            else if (rightScore >= leftScore)
            {
                wrist = rightWrist;
                shoulder = rightShoulder;
                usingRightHand = true;
            }
            else
            {
                wrist = leftWrist;
                shoulder = leftShoulder;
                usingRightHand = false;
            }

            _selectedHandWeakFrames = 0;
            return true;
        }

        private void ResetControlFilter()
        {
            _hasFilteredControlX = false;
            _hasLastRawControlX = false;
            _stillControlFrames = 0;
            _rawMoveDirection = 0;
            _rawMoveFrames = 0;
        }

        private bool TryGetShoulderRelativeAim(PoseFrame frame, PoseLandmark wrist, out float aimX)
        {
            aimX = 0.5f;

            var leftShoulder = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rightShoulder = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);
            if (leftShoulder.Confidence < MinConfidence || rightShoulder.Confidence < MinConfidence)
                return false;

            float wristX = GetScreenRight(wrist.Position);
            float leftX = GetScreenRight(leftShoulder.Position);
            float rightX = GetScreenRight(rightShoulder.Position);
            float shoulderCenterX = (leftX + rightX) * 0.5f;
            float shoulderWidth = Mathf.Abs(leftX - rightX);
            if (shoulderWidth < 0.035f)
                return false;

            float halfSpan = Mathf.Clamp(
                shoulderWidth * ShoulderAimSpanScale,
                MinShoulderAimHalfSpan,
                MaxShoulderAimHalfSpan);
            float normalized = Mathf.Clamp((wristX - shoulderCenterX) / halfSpan, -1f, 1f);
            aimX = 0.5f + normalized * 0.5f;
            return true;
        }

        private float GetHandScore(PoseLandmark wrist, PoseLandmark shoulder)
        {
            if (wrist.Confidence < MinConfidence)
                return 0f;

            if (shoulder.Confidence < MinConfidence)
                return wrist.Confidence;

            float extension = Mathf.Abs(GetScreenRight(wrist.Position) - GetScreenRight(shoulder.Position));
            bool activeEnough = GetScreenDown(wrist.Position) < GetScreenDown(shoulder.Position) + ActiveHandRaiseWindow
                || extension >= ActiveHandMinExtension;
            if (!activeEnough)
                return wrist.Confidence * 0.75f;

            float raiseBonus = GetScreenDown(wrist.Position) < GetScreenDown(shoulder.Position) + RaiseWindow ? 0.35f : 0f;
            return wrist.Confidence + extension * 1.4f + raiseBonus;
        }
    }
}
