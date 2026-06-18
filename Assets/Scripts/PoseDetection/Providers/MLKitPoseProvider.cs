using System;
using System.Collections;
using UnityEngine;

namespace LoseWeight.PoseDetection.Providers
{
    /// <summary>
    /// ML Kit Pose Detection (Bundled) - 端侧姿态识别
    /// 通过 Java 桥接调用 Google ML Kit，不依赖 GMS，不需要网络
    /// </summary>
    public class MLKitPoseProvider : MonoBehaviour, IPoseProvider
    {
        public event Action<PoseFrame> OnPoseFrame;
        public event Action<string> OnPoseError;
        public bool IsRunning { get; private set; }

        [SerializeField] private float _detectInterval = 0.055f;
        [SerializeField] private int _targetInputSize = 192;
        [SerializeField] private int _jpegQuality = 50;
        [SerializeField] private float _frameTimeoutSeconds = 1.2f;

        private WebCamTexture _camTexture;
        private bool _ownsCamera;
        private bool _processingFrame;
        private float _lastDetectTime;
        private float _processingStartedTime;
        private PoseRuntimeOptions _options;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _detector;
#endif

        public static int FrameCount { get; private set; }
        public static int LastReceivedFrame { get; private set; }

        public WebCamTexture CameraTexture => _camTexture;
        public bool IsCameraReady => _camTexture != null && _camTexture.isPlaying && _camTexture.width > 16;

        public void StartDetection(PoseRuntimeOptions options)
        {
            if (IsRunning) return;

            _options = options ?? new PoseRuntimeOptions();
            _detectInterval = 1f / Mathf.Max(1, _options.TargetFps);
            FrameCount = 0;
            LastReceivedFrame = 0;
            _processingFrame = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(StartOwnedCamera());
#else
            ReportError("ML Kit provider only runs on Android device builds");
#endif
        }

        public void Initialize(WebCamTexture camTexture)
        {
            Initialize(camTexture, new PoseRuntimeOptions());
        }

        public void Initialize(WebCamTexture camTexture, PoseRuntimeOptions options)
        {
            StopDetection();
            _options = options ?? new PoseRuntimeOptions();
            _detectInterval = 1f / Mathf.Max(1, _options.TargetFps);
            _camTexture = camTexture;
            _ownsCamera = false;
            FrameCount = 0;
            LastReceivedFrame = 0;
            _processingFrame = false;

            if (InitializeDetector())
            {
                IsRunning = true;
            }
        }

        public void StopDetection()
        {
            IsRunning = false;
            _processingFrame = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            _detector = null;
#endif

            if (_ownsCamera && _camTexture != null)
            {
                if (_camTexture.isPlaying) _camTexture.Stop();
                Destroy(_camTexture);
            }

            _camTexture = null;
            _ownsCamera = false;
        }

        public void Stop()
        {
            StopDetection();
        }

        private IEnumerator StartOwnedCamera()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                float permissionWait = 0f;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    yield return new WaitForSeconds(0.25f);
                    permissionWait += 0.25f;
                    if (permissionWait > 15f)
                    {
                        ReportError("Camera permission denied");
                        yield break;
                    }
                }
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                ReportError("No camera device found");
                yield break;
            }

            WebCamDevice selected = devices[0];
            if (_options == null || _options.UseFrontCamera)
            {
                foreach (var device in devices)
                {
                    if (device.isFrontFacing)
                    {
                        selected = device;
                        break;
                    }
                }
            }

            int requestFps = Mathf.Clamp(_options?.TargetFps ?? 15, 10, 30);
            _camTexture = new WebCamTexture(selected.name, 480, 640, requestFps);
            _ownsCamera = true;
            _camTexture.Play();

            float startWait = 0f;
            while (_camTexture.width <= 16)
            {
                yield return new WaitForSeconds(0.1f);
                startWait += 0.1f;
                if (startWait > 8f)
                {
                    ReportError("Camera start timeout");
                    yield break;
                }
            }

            if (!InitializeDetector())
            {
                yield break;
            }

            IsRunning = true;
            LoseWeight.Game.ScreenDebugLog.Log($"MLKit cam {_camTexture.width}x{_camTexture.height} rot={_camTexture.videoRotationAngle} vM={_camTexture.videoVerticallyMirrored}");
#else
            yield break;
#endif
        }

        private bool InitializeDetector()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var clazz = new AndroidJavaClass("com.ycykj.mlkit.MLKitPoseDetector");
                _detector = clazz.CallStatic<AndroidJavaObject>("getInstance");
                // 重新初始化（再玩一局时单例可能已被关闭）
                _detector.Call("reinit");
                // 设置 Unity 回调目标
                _detector.Call("setUnityCallback", gameObject.name, nameof(OnMLKitPoseResult));
                LoseWeight.Game.ScreenDebugLog.Log("MLKit init OK");
                Debug.Log("[MLKitPose] Detector initialized");
                return true;
            }
            catch (Exception e)
            {
                ReportError($"MLKit init failed: {e.Message}");
                return false;
            }
#else
            ReportError("ML Kit provider only works on Android device");
            return false;
#endif
        }

        private void Update()
        {
            if (!IsRunning || _camTexture == null || !_camTexture.isPlaying) return;

            if (_processingFrame)
            {
                if (Time.time - _processingStartedTime > _frameTimeoutSeconds)
                {
                    _processingFrame = false;
                    ReportError("MLKit frame timeout");
                }
                return;
            }

            if (Time.time - _lastDetectTime < _detectInterval) return;
            if (_camTexture.width <= 16) return;
            if (!_camTexture.didUpdateThisFrame) return;

            _lastDetectTime = Time.time;
            DetectFrame();
        }

        private Texture2D _rgbCache;

        private void DetectFrame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_detector == null) return;

            int srcW = _camTexture.width;
            int srcH = _camTexture.height;
            int target = Mathf.Max(64, _targetInputSize);
            int dstW, dstH;
            if (srcW > srcH) { dstW = target; dstH = Mathf.RoundToInt(srcH * (float)target / srcW); }
            else { dstH = target; dstW = Mathf.RoundToInt(srcW * (float)target / srcH); }

            RenderTexture rt = null;
            RenderTexture prev = RenderTexture.active;
            try
            {
                rt = RenderTexture.GetTemporary(dstW, dstH);
                Graphics.Blit(_camTexture, rt);
                RenderTexture.active = rt;

                if (_rgbCache == null || _rgbCache.width != dstW || _rgbCache.height != dstH)
                {
                    if (_rgbCache != null) Destroy(_rgbCache);
                    _rgbCache = new Texture2D(dstW, dstH, TextureFormat.RGB24, false);
                }

                _rgbCache.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
                _rgbCache.Apply();

                byte[] jpeg = _rgbCache.EncodeToJPG(Mathf.Clamp(_jpegQuality, 35, 90));
                _processingFrame = true;
                _processingStartedTime = Time.time;
                // rotation=0 keeps ML Kit coordinates in the same image space as the Unity texture sent here.
                _detector.Call("detectFromJpeg", jpeg, 0);
            }
            catch (Exception e)
            {
                _processingFrame = false;
                ReportError($"MLKit detect failed: {e.Message}");
            }
            finally
            {
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
#endif
        }

        /// <summary>
        /// Unity 主线程回调 - 由 Java 通过 UnitySendMessage 调用
        /// 必须命名为 OnMLKitPoseResult 且参数是 string
        /// </summary>
        public void OnMLKitPoseResult(string json)
        {
            _processingFrame = false;
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var response = JsonUtility.FromJson<PoseResponse>(json);
                if (response == null) return;
                if (!string.IsNullOrEmpty(response.error))
                {
                    ReportError(response.error);
                    return;
                }
                if (response.landmarks == null || response.landmarks.Length == 0) return;

                FrameCount++;
                LastReceivedFrame++;

                var frame = new PoseFrame();
                frame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                frame.Source = "mlkit";

                int count = Mathf.Min(response.landmarks.Length, 33);
                for (int i = 0; i < count; i++)
                {
                    var lm = response.landmarks[i];
                    // Keep the same image space that was sent to ML Kit. The preview UI applies rotation/mirror separately.
                    frame.Landmarks[i] = new PoseLandmark
                    {
                        Position = new Vector3(Mathf.Clamp01(lm.x), Mathf.Clamp01(lm.y), lm.z),
                        Confidence = Mathf.Clamp01(lm.v)
                    };
                }

                if (FrameCount <= 3)
                    LoseWeight.Game.ScreenDebugLog.Log($"MLKit frame #{FrameCount}: {count} raw landmarks");

                OnPoseFrame?.Invoke(frame);
            }
            catch (Exception e)
            {
                ReportError($"MLKit parse error: {e.Message}");
            }
        }

        private void ReportError(string message)
        {
            Debug.LogWarning($"[MLKitPose] {message}");
            LoseWeight.Game.ScreenDebugLog.Log($"MLKit: {message}");
            OnPoseError?.Invoke(message);
        }

        [Serializable]
        private class PoseResponse
        {
            public string error;
            public LandmarkData[] landmarks;
        }

        [Serializable]
        private class LandmarkData
        {
            public float x, y, z, v;
        }

        private void OnDestroy()
        {
            StopDetection();
            if (_rgbCache != null) Destroy(_rgbCache);
        }

        private void OnApplicationPause(bool pause)
        {
            if (!_ownsCamera || _camTexture == null) return;
            if (pause) _camTexture.Pause();
            else if (IsRunning) _camTexture.Play();
        }
    }
}
