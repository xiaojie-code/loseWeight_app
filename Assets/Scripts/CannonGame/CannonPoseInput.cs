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
        // One Euro filter on the final aim signal: low jitter when still, low lag
        // when moving. Lower MinCutoff => steadier at rest; higher Beta => snappier
        // follow when the hand moves. Tuned aggressively for noisy on-device MLKit.
        private const float AimFilterMinCutoff = 0.4f;
        private const float AimFilterBeta = 2.8f;
        private const float AimFilterDCutoff = 1.0f;
        private const float CorePointConfidence = 0.30f;
        // Hand-selection hysteresis: once a control hand is locked, hold it firmly so
        // the other arm / shoulders / head in frame can't steal control and yank the
        // aim sideways. Only switch when the held hand stays lost for several frames
        // AND the other hand is clearly stronger.
        private const float HandKeepMinScore = 0.10f;
        private const float HandSwitchMargin = 0.20f;
        private const int HandSwitchLostFrames = 8;
        // Anchor the wrist to the (smoothed) shoulder midline so swaying the whole
        // body left/right doesn't drift the aim.
        private const float ShoulderCenterSmooth = 0.35f;
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
        private float _armedSince;
        private int _calibrationSamples;
        private int _dropFrames;
        private int _selectedHandLostFrames;
        private float _smoothShoulderCenterX;
        private bool _hasShoulderCenter;
        private readonly OneEuroFilter _aimFilter = new OneEuroFilter(AimFilterMinCutoff, AimFilterBeta, AimFilterDCutoff);

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
            _armedSince = 0f;
            _calibrationSamples = 0;
            _dropFrames = 0;
            _selectedHandLostFrames = 0;
            _smoothShoulderCenterX = 0f;
            _hasShoulderCenter = false;
            _aimFilter.Reset();
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
            _selectedHandLostFrames = 0;
            _smoothShoulderCenterX = 0f;
            _hasShoulderCenter = false;
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
                _selectedHandLostFrames = 0;
                _dropFrames = 0;
                Charge = 0f;
                AimX = _hasAim ? Mathf.Lerp(AimX, 0.5f, 0.35f) : 0.5f;
                AimY = 0.62f;
                _hasAim = true;
                _aimFilter.Reset(AimX);
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
                ApplyAim(0.5f, now);
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
                _aimFilter.Reset(targetAimX);
                ApplyAim(targetAimX, now);
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
                {
                    _lockedAimX = AimX;
                    // Snap the filter to the locked aim so the downward punch can't drag it.
                    _aimFilter.Reset(_lockedAimX);
                }
                _aimLockActive = true;
                _lockUntil = now + FireAimLockDuration;
            }

            if (_aimLockActive && now > _lockUntil)
                _aimLockActive = false;

            if (_aimLockActive)
                targetAimX = _lockedAimX;

            ApplyAim(targetAimX, now);

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
                _aimLockActive = false;
                _lockUntil = 0f;
                if (_calibratedAimCenter)
                {
                    // Seamless hand-over: re-anchor the new hand's center so the aim
                    // stays exactly where it is instead of snapping back to the middle.
                    _centerControlX = controlX - (AimX - 0.5f) / RelativeAimScale;
                }
                else
                {
                    _calibrationControlSum = 0f;
                    _calibrationSamples = 0;
                }
            }

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

        private float GetControlRight(PoseFrame frame, PoseLandmark wrist, bool usingRightHand)
        {
            float armCenterX = GetArmCenterRight(frame, wrist, usingRightHand);

            // Anchor against the shoulder midline so swaying the whole body sideways
            // (or the non-control arm / head drifting) doesn't move the aim. The
            // shoulder center is itself smoothed because shoulder landmarks jitter too.
            var leftShoulder = frame.GetLandmark(PoseLandmarkIndex.LeftShoulder);
            var rightShoulder = frame.GetLandmark(PoseLandmarkIndex.RightShoulder);
            if (leftShoulder.Confidence >= MinConfidence && rightShoulder.Confidence >= MinConfidence)
            {
                float center = (GetScreenRight(leftShoulder.Position) + GetScreenRight(rightShoulder.Position)) * 0.5f;
                _smoothShoulderCenterX = _hasShoulderCenter
                    ? Mathf.Lerp(_smoothShoulderCenterX, center, ShoulderCenterSmooth)
                    : center;
                _hasShoulderCenter = true;
            }

            return _hasShoulderCenter ? armCenterX - _smoothShoulderCenterX : armCenterX;
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

        private void ApplyAim(float targetAimX, float now)
        {
            if (_hasAim)
            {
                AimX = Mathf.Clamp01(_aimFilter.Filter(targetAimX, now));
            }
            else
            {
                AimX = Mathf.Clamp01(targetAimX);
                _aimFilter.Reset(AimX);
                _hasAim = true;
            }
            AimY = 0.62f;
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

            // Already locked onto a control hand: hold it firmly (hysteresis) so the
            // other arm / shoulders / head cannot steal control and jerk the aim.
            if (_lastHandValid)
            {
                float selectedScore = _controlUsesRightHand ? rightScore : leftScore;
                float otherScore = _controlUsesRightHand ? leftScore : rightScore;

                if (selectedScore >= HandKeepMinScore)
                {
                    _selectedHandLostFrames = 0;
                    AssignHand(_controlUsesRightHand, leftWrist, rightWrist, leftShoulder, rightShoulder,
                        out wrist, out shoulder, out usingRightHand);
                    return true;
                }

                // Control hand is momentarily weak. Don't switch immediately.
                _selectedHandLostFrames++;
                bool lostLongEnough = _selectedHandLostFrames >= HandSwitchLostFrames;
                bool otherClearlyBetter = otherScore >= StrongHandScore && otherScore >= selectedScore + HandSwitchMargin;

                if (lostLongEnough && otherClearlyBetter)
                {
                    _selectedHandLostFrames = 0;
                    AssignHand(!_controlUsesRightHand, leftWrist, rightWrist, leftShoulder, rightShoulder,
                        out wrist, out shoulder, out usingRightHand);
                    return true;
                }

                // Keep holding the current hand while its wrist is still tracked at all;
                // otherwise report no pose so the aim simply freezes (never jumps).
                if (selectedScore > 0f)
                {
                    AssignHand(_controlUsesRightHand, leftWrist, rightWrist, leftShoulder, rightShoulder,
                        out wrist, out shoulder, out usingRightHand);
                    return true;
                }

                wrist = default(PoseLandmark);
                shoulder = default(PoseLandmark);
                usingRightHand = false;
                return false;
            }

            // First lock: pick the stronger hand.
            _selectedHandLostFrames = 0;
            AssignHand(rightScore >= leftScore, leftWrist, rightWrist, leftShoulder, rightShoulder,
                out wrist, out shoulder, out usingRightHand);
            return true;
        }

        private static void AssignHand(bool useRight, PoseLandmark leftWrist, PoseLandmark rightWrist,
            PoseLandmark leftShoulder, PoseLandmark rightShoulder,
            out PoseLandmark wrist, out PoseLandmark shoulder, out bool usingRightHand)
        {
            wrist = useRight ? rightWrist : leftWrist;
            shoulder = useRight ? rightShoulder : leftShoulder;
            usingRightHand = useRight;
        }

        private void ResetControlFilter()
        {
            _aimFilter.Reset();
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

    /// <summary>
    /// One Euro Filter (Casiez et al. 2012): adaptive low-pass filter for noisy
    /// interactive signals. It smooths jitter when the value is nearly still and
    /// reduces lag when the value moves quickly, which is exactly the trade-off
    /// needed for camera/pose driven aiming.
    /// </summary>
    internal sealed class OneEuroFilter
    {
        private readonly float _minCutoff;
        private readonly float _beta;
        private readonly float _dCutoff;
        private float _xPrev;
        private float _dxPrev;
        private float _lastTime;
        private bool _hasPrev;

        public OneEuroFilter(float minCutoff, float beta, float dCutoff = 1f)
        {
            _minCutoff = Mathf.Max(0.0001f, minCutoff);
            _beta = beta;
            _dCutoff = Mathf.Max(0.0001f, dCutoff);
        }

        /// <summary>Forget history; next Filter call seeds from the incoming value.</summary>
        public void Reset()
        {
            _hasPrev = false;
            _dxPrev = 0f;
        }

        /// <summary>Snap the filter state to a known value (e.g. a locked aim).</summary>
        public void Reset(float value)
        {
            _xPrev = value;
            _dxPrev = 0f;
            _hasPrev = true;
            _lastTime = -1f;
        }

        public float Filter(float x, float timestamp)
        {
            if (!_hasPrev)
            {
                _xPrev = x;
                _dxPrev = 0f;
                _lastTime = timestamp;
                _hasPrev = true;
                return x;
            }

            float dt = _lastTime >= 0f ? timestamp - _lastTime : 1f / 30f;
            dt = Mathf.Clamp(dt, 0.001f, 0.1f);
            _lastTime = timestamp;

            float dx = (x - _xPrev) / dt;
            float edx = LowPass(dx, _dxPrev, Alpha(_dCutoff, dt));
            _dxPrev = edx;

            float cutoff = _minCutoff + _beta * Mathf.Abs(edx);
            float xHat = LowPass(x, _xPrev, Alpha(cutoff, dt));
            _xPrev = xHat;
            return xHat;
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }

        private static float LowPass(float x, float xPrev, float alpha)
        {
            return alpha * x + (1f - alpha) * xPrev;
        }
    }
}
