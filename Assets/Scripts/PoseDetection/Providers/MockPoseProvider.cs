using System;
using UnityEngine;

namespace LoseWeight.PoseDetection.Providers
{
    /// <summary>
    /// Mock 姿�?Provider - 用于 Editor 调试
    /// 模拟关键点数据，可通过键盘触发动作
    /// </summary>
    public class MockPoseProvider : MonoBehaviour, IPoseProvider
    {
        public event Action<PoseFrame> OnPoseFrame;
        public event Action<string> OnPoseError;
        public bool IsRunning { get; private set; }

        private PoseRuntimeOptions _options;
        private float _frameInterval;
        private float _lastFrameTime;

        // 模拟的中立姿�?
        private PoseFrame _neutralFrame;

        public void StartDetection(PoseRuntimeOptions options)
        {
            if (IsRunning) return;
            _options = options;
            IsRunning = true;
            _frameInterval = 1f / options.TargetFps;
            _neutralFrame = CreateNeutralPose();
            Debug.Log("[MockPose] Started");
        }

        public void StopDetection()
        {
            IsRunning = false;
            Debug.Log("[MockPose] Stopped");
        }

        private void Update()
        {
            if (!IsRunning) return;

            if (Time.time - _lastFrameTime < _frameInterval) return;
            _lastFrameTime = Time.time;

            var frame = CreateNeutralPose();
            frame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            frame.Source = "mock";

            // 键盘模拟动作
            if (Input.GetKey(KeyCode.Q)) SimulateLeftPunch(frame);
            if (Input.GetKey(KeyCode.E)) SimulateRightPunch(frame);
            if (Input.GetKey(KeyCode.Space)) SimulateDefend(frame);
            if (Input.GetKey(KeyCode.A)) SimulateDodgeLeft(frame);
            if (Input.GetKey(KeyCode.D)) SimulateDodgeRight(frame);

            OnPoseFrame?.Invoke(frame);
        }

        private PoseFrame CreateNeutralPose()
        {
            var frame = new PoseFrame();
            // 模拟站立姿态的关键点位�?
            frame.Landmarks[11] = new PoseLandmark { Position = new Vector3(0.4f, 0.4f, 0), Confidence = 0.9f }; // LeftShoulder
            frame.Landmarks[12] = new PoseLandmark { Position = new Vector3(0.6f, 0.4f, 0), Confidence = 0.9f }; // RightShoulder
            frame.Landmarks[13] = new PoseLandmark { Position = new Vector3(0.35f, 0.55f, 0), Confidence = 0.9f }; // LeftElbow
            frame.Landmarks[14] = new PoseLandmark { Position = new Vector3(0.65f, 0.55f, 0), Confidence = 0.9f }; // RightElbow
            frame.Landmarks[15] = new PoseLandmark { Position = new Vector3(0.38f, 0.45f, 0), Confidence = 0.9f }; // LeftWrist
            frame.Landmarks[16] = new PoseLandmark { Position = new Vector3(0.62f, 0.45f, 0), Confidence = 0.9f }; // RightWrist
            frame.Landmarks[23] = new PoseLandmark { Position = new Vector3(0.45f, 0.7f, 0), Confidence = 0.9f }; // LeftHip
            frame.Landmarks[24] = new PoseLandmark { Position = new Vector3(0.55f, 0.7f, 0), Confidence = 0.9f }; // RightHip
            return frame;
        }

        private void SimulateLeftPunch(PoseFrame frame)
        {
            // 左手腕快速前�?
            frame.Landmarks[15] = new PoseLandmark { Position = new Vector3(0.5f, 0.35f, -0.3f), Confidence = 0.95f };
        }

        private void SimulateRightPunch(PoseFrame frame)
        {
            frame.Landmarks[16] = new PoseLandmark { Position = new Vector3(0.5f, 0.35f, -0.3f), Confidence = 0.95f };
        }

        private void SimulateDefend(PoseFrame frame)
        {
            // 双手抬到面前
            frame.Landmarks[15] = new PoseLandmark { Position = new Vector3(0.45f, 0.3f, -0.1f), Confidence = 0.9f };
            frame.Landmarks[16] = new PoseLandmark { Position = new Vector3(0.55f, 0.3f, -0.1f), Confidence = 0.9f };
        }

        private void SimulateDodgeLeft(PoseFrame frame)
        {
            // 身体重心左移
            frame.Landmarks[11] = new PoseLandmark { Position = new Vector3(0.3f, 0.4f, 0), Confidence = 0.9f };
            frame.Landmarks[12] = new PoseLandmark { Position = new Vector3(0.5f, 0.4f, 0), Confidence = 0.9f };
        }

        private void SimulateDodgeRight(PoseFrame frame)
        {
            frame.Landmarks[11] = new PoseLandmark { Position = new Vector3(0.5f, 0.4f, 0), Confidence = 0.9f };
            frame.Landmarks[12] = new PoseLandmark { Position = new Vector3(0.7f, 0.4f, 0), Confidence = 0.9f };
        }
    }
}
