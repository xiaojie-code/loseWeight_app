using System;
using System.Collections.Concurrent;
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace LoseWeight.PoseDetection.Providers
{
    public class MediaPipePoseProvider : MonoBehaviour, IPoseProvider
    {
        public event Action<PoseFrame> OnPoseFrame;
        public event Action<string> OnPoseError;
        public bool IsRunning { get; private set; }

        private PoseRuntimeOptions _options;
        private int _frameCount;
        private ConcurrentQueue<PoseLandmarkerResult> _resultQueue = new ConcurrentQueue<PoseLandmarkerResult>();

        private void Awake()
        {
            PoseEventBridge.OnPoseDetected += EnqueueResult;
            Debug.Log("[MediaPipePose] Subscribed to PoseEventBridge in Awake");
        }

        public void StartDetection(PoseRuntimeOptions options)
        {
            _options = options;
            _frameCount = 0;
            IsRunning = true;
            Debug.Log("[MediaPipePose] StartDetection - IsRunning=true");
        }

        public void StopDetection() { IsRunning = false; }

        private void OnDestroy()
        {
            PoseEventBridge.OnPoseDetected -= EnqueueResult;
        }

        // 后台线程调用 - 不检查 IsRunning，直接入队
        private void EnqueueResult(PoseLandmarkerResult result)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;
            _resultQueue.Enqueue(result);
        }

        // 主线程 - 这里检查 IsRunning
        private void Update()
        {
            while (_resultQueue.TryDequeue(out var result))
            {
                if (!IsRunning) continue; // 丢弃 IsRunning 之前的数据
                ProcessResult(result);
            }
        }

        private void ProcessResult(PoseLandmarkerResult result)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;
            var landmarks = result.poseLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count == 0) return;

            _frameCount++;
            if (_frameCount <= 5)
                Debug.Log($"[MediaPipePose] Frame #{_frameCount}, {landmarks.Count} landmarks");

            var frame = new PoseFrame();
            frame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            frame.Source = "mediapipe";

            int count = Mathf.Min(landmarks.Count, 33);
            for (int i = 0; i < count; i++)
            {
                var lm = landmarks[i];
                float x = (_options != null && _options.MirrorOutput) ? (1f - lm.x) : lm.x;
                frame.Landmarks[i] = new PoseLandmark
                {
                    Position = new Vector3(x, lm.y, lm.z),
                    Confidence = lm.visibility ?? 0.5f
                };
            }

            OnPoseFrame?.Invoke(frame);
        }
    }
}
