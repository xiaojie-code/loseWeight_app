using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.PoseDetection.Providers
{
    /// <summary>
    /// 服务端姿态检测 - 把摄像头帧发给服务端 MediaPipe 做推理
    /// 用 System.Net.Http.HttpClient（绕过 UnityWebRequest 在某些 Android 设备的问题）
    /// </summary>
    public class ServerPoseDetector : MonoBehaviour
    {
        public event Action<PoseFrame> OnPoseFrame;

        [SerializeField] private string _serverUrl = "http://192.168.1.4:3000";
        [SerializeField] private float _detectInterval = 0.2f; // 5fps

        private WebCamTexture _camTexture;
        private bool _isRunning;
        private bool _isDetecting;
        private float _lastDetectTime;

        private HttpClient _httpClient;
        private readonly Queue<PoseFrame> _resultQueue = new Queue<PoseFrame>();
        private readonly object _queueLock = new object();

        private int _logFailCount = 0;
        private int _logSuccessCount = 0;
        private int _logSendCount = 0;

        public void Initialize(WebCamTexture camTexture, string serverUrl)
        {
            _camTexture = camTexture;
            _serverUrl = serverUrl;
            _isRunning = true;

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);

            Debug.Log($"[ServerPose] Initialized, server={serverUrl}");
            LoseWeight.Game.ScreenDebugLog.Log($"ServerPose init: {serverUrl}");

            StartCoroutine(TestConnection());
        }

        public void Stop()
        {
            _isRunning = false;
            _httpClient?.Dispose();
            _httpClient = null;
        }

        private IEnumerator TestConnection()
        {
            yield return new WaitForSeconds(0.5f);
            var task = Task.Run(async () =>
            {
                try
                {
                    var resp = await _httpClient.GetAsync($"{_serverUrl}/pose/status");
                    return $"OK status={resp.StatusCode}";
                }
                catch (Exception e)
                {
                    return $"FAIL: {e.Message}";
                }
            });

            float t = Time.time;
            while (!task.IsCompleted)
            {
                yield return null;
                if (Time.time - t > 8f)
                {
                    LoseWeight.Game.ScreenDebugLog.Log("ServerPose conn: timeout 8s");
                    yield break;
                }
            }

            LoseWeight.Game.ScreenDebugLog.Log($"ServerPose conn: {task.Result}");
        }

        private void Update()
        {
            // 处理后台线程返回的姿态结果
            lock (_queueLock)
            {
                while (_resultQueue.Count > 0)
                {
                    var frame = _resultQueue.Dequeue();
                    OnPoseFrame?.Invoke(frame);
                }
            }

            if (!_isRunning || _isDetecting || _camTexture == null || !_camTexture.isPlaying) return;
            if (Time.time - _lastDetectTime < _detectInterval) return;
            if (_camTexture.width <= 16) return;

            _lastDetectTime = Time.time;
            StartCoroutine(DetectPose());
        }

        private IEnumerator DetectPose()
        {
            _isDetecting = true;

            // 保持摄像头原始宽高比（避免拉伸导致姿态识别坐标错位）
            int srcW = _camTexture.width;
            int srcH = _camTexture.height;
            int targetMax = 320;
            int dstW, dstH;
            if (srcW > srcH)
            {
                dstW = targetMax;
                dstH = Mathf.RoundToInt(srcH * (float)targetMax / srcW);
            }
            else
            {
                dstH = targetMax;
                dstW = Mathf.RoundToInt(srcW * (float)targetMax / srcH);
            }

            var rt = RenderTexture.GetTemporary(dstW, dstH);
            Graphics.Blit(_camTexture, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var resized = new Texture2D(dstW, dstH, TextureFormat.RGB24, false);
            resized.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
            resized.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] jpegBytes = resized.EncodeToJPG(60);
            Destroy(resized);

            string base64 = Convert.ToBase64String(jpegBytes);
            string json = $"{{\"image\":\"data:image/jpeg;base64,{base64}\"}}";

            if (_logSendCount < 2)
            {
                LoseWeight.Game.ScreenDebugLog.Log($"ServerPose sending {jpegBytes.Length}B");
                _logSendCount++;
            }

            // 后台线程发请求
            var task = Task.Run(async () =>
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await _httpClient.PostAsync($"{_serverUrl}/pose/detect", content);
                    var body = await resp.Content.ReadAsStringAsync();
                    return new TaskResult { ok = resp.IsSuccessStatusCode, body = body, error = null };
                }
                catch (Exception e)
                {
                    return new TaskResult { ok = false, body = null, error = e.Message };
                }
            });

            float t = Time.time;
            while (!task.IsCompleted)
            {
                yield return null;
                if (Time.time - t > 8f)
                {
                    if (_logFailCount < 3)
                    {
                        LoseWeight.Game.ScreenDebugLog.Log("ServerPose: req timeout 8s");
                        _logFailCount++;
                    }
                    _isDetecting = false;
                    yield break;
                }
            }

            var result = task.Result;
            if (result.ok && !string.IsNullOrEmpty(result.body))
            {
                ParseAndEnqueue(result.body);
            }
            else
            {
                if (_logFailCount < 3)
                {
                    LoseWeight.Game.ScreenDebugLog.Log($"ServerPose fail: {result.error}");
                    _logFailCount++;
                }
            }

            _isDetecting = false;
        }

        private void ParseAndEnqueue(string json)
        {
            try
            {
                var response = JsonUtility.FromJson<PoseResponse>(json);
                if (response == null || !response.success || response.landmarks == null || response.landmarks.Length == 0) return;

                var frame = new PoseFrame();
                frame.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                frame.Source = "server";

                int count = Mathf.Min(response.landmarks.Length, 33);
                for (int i = 0; i < count; i++)
                {
                    var lm = response.landmarks[i];
                    frame.Landmarks[i] = new PoseLandmark
                    {
                        Position = new Vector3(lm.x, lm.y, lm.z),
                        Confidence = lm.v
                    };
                }

                if (_logSuccessCount < 3)
                {
                    LoseWeight.Game.ScreenDebugLog.Log($"ServerPose OK: {count} landmarks");
                    _logSuccessCount++;
                }

                lock (_queueLock) { _resultQueue.Enqueue(frame); }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ServerPose] Parse error: {e.Message}");
            }
        }

        private class TaskResult
        {
            public bool ok;
            public string body;
            public string error;
        }

        [Serializable]
        private class PoseResponse
        {
            public bool success;
            public LandmarkData[] landmarks;
        }

        [Serializable]
        private class LandmarkData
        {
            public float x, y, z, v;
        }
    }
}
