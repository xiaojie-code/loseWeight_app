using UnityEngine;

namespace LoseWeight.PoseDetection
{
    /// <summary>
    /// 一帧姿态数据（33 个关键点）
    /// </summary>
    public class PoseFrame
    {
        public PoseLandmark[] Landmarks = new PoseLandmark[33];
        public long Timestamp;
        public float LatencyMs;
        public string Source; // "mediapipe", "movenet", "mock"

        public PoseFrame()
        {
            for (int i = 0; i < 33; i++)
                Landmarks[i] = new PoseLandmark();
        }

        /// <summary>
        /// 获取指定关键点
        /// </summary>
        public PoseLandmark GetLandmark(PoseLandmarkIndex index)
        {
            return Landmarks[(int)index];
        }
    }

    /// <summary>
    /// 单个关键点
    /// </summary>
    public struct PoseLandmark
    {
        public Vector3 Position; // x, y 归一化 [0,1]，z 为深度
        public float Confidence; // 置信度 [0,1]

        public bool IsVisible => Confidence >= 0.4f;
    }

    /// <summary>
    /// MediaPipe 33 点关键点索引
    /// </summary>
    public enum PoseLandmarkIndex
    {
        Nose = 0,
        LeftEyeInner = 1,
        LeftEye = 2,
        LeftEyeOuter = 3,
        RightEyeInner = 4,
        RightEye = 5,
        RightEyeOuter = 6,
        LeftEar = 7,
        RightEar = 8,
        MouthLeft = 9,
        MouthRight = 10,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftElbow = 13,
        RightElbow = 14,
        LeftWrist = 15,
        RightWrist = 16,
        LeftPinky = 17,
        RightPinky = 18,
        LeftIndex = 19,
        RightIndex = 20,
        LeftThumb = 21,
        RightThumb = 22,
        LeftHip = 23,
        RightHip = 24,
        LeftKnee = 25,
        RightKnee = 26,
        LeftAnkle = 27,
        RightAnkle = 28,
        LeftHeel = 29,
        RightHeel = 30,
        LeftFootIndex = 31,
        RightFootIndex = 32
    }
}
