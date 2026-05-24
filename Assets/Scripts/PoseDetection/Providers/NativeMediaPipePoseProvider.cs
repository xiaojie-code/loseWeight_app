using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LoseWeight.PoseDetection.Providers
{
    /// <summary>
    /// Android 原生 MediaPipe 姿态识�?Provider
    /// 通过 JNI 调用 Android 原生层的 CameraX + MediaPipe
    /// </summary>
    public class NativeMediaPipePoseProvider : MonoBehaviour, IPoseProvider
    {
        public event Action<PoseFrame> OnPoseFrame;
        public event Action<string> OnPoseError;
        public bool IsRunning { get; private set; }

        private PoseRuntimeOptions _options;

        public void StartDetection(PoseRuntimeOptions options)
        {
            if (IsRunning) return;
            _options = options;
            IsRunning = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var posePlugin = new AndroidJavaClass("com.loseweight.pose.MediaPipePosePlugin"))
            {
                posePlugin.CallStatic("startPoseDetection", activity, options.TargetFps, options.UseFrontCamera);
            }
            Debug.Log("[NativePose] Started MediaPipe on Android");
#else
            Debug.Log("[NativePose] Not on Android, skipping native start");
#endif
        }

        public void StopDetection()
        {
            if (!IsRunning) return;
            IsRunning = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var posePlugin = new AndroidJavaClass("com.loseweight.pose.MediaPipePosePlugin"))
            {
                posePlugin.CallStatic("stopPoseDetection");
            }
#endif
            Debug.Log("[NativePose] Stopped");
        }

        /// <summary>
        /// �?Android 原生层通过 UnitySendMessage 调用
        /// GameObject 名称必须�?"PoseProvider"
        /// </summary>
        public void OnNativePoseData(string jsonData)
        {
            if (!IsRunning) return;

            try
            {
                var data = JsonUtility.FromJson<NativePoseResult>(jsonData);
                if (data == null || data.landmarks == null) return;

                var frame = new PoseFrame();
                frame.Timestamp = data.timestamp;
                frame.LatencyMs = data.latencyMs;
                frame.Source = "mediapipe";

                for (int i = 0; i < data.landmarks.Length && i < 33; i++)
                {
                    var lm = data.landmarks[i];
                    float x = _options.MirrorOutput ? (1f - lm.x) : lm.x;
                    frame.Landmarks[i] = new PoseLandmark
                    {
                        Position = new Vector3(x, lm.y, lm.z),
                        Confidence = lm.v
                    };
                }

                OnPoseFrame?.Invoke(frame);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NativePose] Parse error: {e.Message}");
                OnPoseError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// �?Android 原生层调用，报告错误
        /// </summary>
        public void OnNativePoseError(string error)
        {
            Debug.LogError($"[NativePose] Error: {error}");
            OnPoseError?.Invoke(error);
        }

        private void OnDestroy()
        {
            StopDetection();
        }
    }

    [Serializable]
    public class NativePoseResult
    {
        public long timestamp;
        public float latencyMs;
        public NativeLandmark[] landmarks;
    }

    [Serializable]
    public class NativeLandmark
    {
        public float x;
        public float y;
        public float z;
        public float v; // visibility/confidence
    }
}
