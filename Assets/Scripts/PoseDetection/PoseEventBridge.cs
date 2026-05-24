using System;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace LoseWeight.PoseDetection
{
    /// <summary>
    /// 姿态检测事件桥接
    /// PoseLandmarkerRunner 检测到结果时通过此类广播
    /// </summary>
    public static class PoseEventBridge
    {
        /// <summary>
        /// 当检测到姿态时触发
        /// </summary>
        private static int _frameCount = 0;
        public static event Action<PoseLandmarkerResult> OnPoseDetected;

        public static void Publish(PoseLandmarkerResult result)
        {
            _frameCount++;
            if (_frameCount <= 3) UnityEngine.Debug.Log($"[PoseEventBridge] Publishing frame #{_frameCount}");
            OnPoseDetected?.Invoke(result);
        }
    }
}
