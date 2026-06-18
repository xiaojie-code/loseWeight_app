using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace LoseWeight.Game
{
    public class MobileCameraPreview : MonoBehaviour
    {
        private WebCamTexture _webCamTexture;
        private RawImage _rawImage;
        private RectTransform _rawImageRT;
        private bool _started;
        private bool _isFrontFacing;
        private Vector2 _lastPreviewSize;
        private int _lastRotation = int.MinValue;
        private bool _lastMirrored;

        public WebCamTexture CamTexture => _webCamTexture;
        public bool IsRunning => _started && _webCamTexture != null && _webCamTexture.isPlaying;

        public void Initialize(RawImage targetImage)
        {
            _rawImage = targetImage;
            _rawImageRT = targetImage.GetComponent<RectTransform>();
            StartCoroutine(StartCamera());
        }

        private IEnumerator StartCamera()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                yield return new WaitForSeconds(1.5f);
                float w = 0;
                while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    yield return new WaitForSeconds(0.5f);
                    w += 0.5f;
                    if (w > 15f) { ScreenDebugLog.Log("Cam permission DENIED"); yield break; }
                }
            }
            ScreenDebugLog.Log("Cam permission OK");
#endif
            yield return new WaitForSeconds(0.3f);

            WebCamDevice? frontCam = null;
            var devices = WebCamTexture.devices;
            ScreenDebugLog.Log($"Cameras: {devices.Length}");

            foreach (var d in devices)
            {
                if (d.isFrontFacing) { frontCam = d; break; }
            }
            if (frontCam == null && devices.Length > 0) frontCam = devices[0];
            if (frontCam == null) { ScreenDebugLog.Log("No camera!"); yield break; }

            _isFrontFacing = frontCam.Value.isFrontFacing;
            _webCamTexture = new WebCamTexture(frontCam.Value.name, 360, 480, 24);
            _webCamTexture.Play();

            float sw = 0;
            while (_webCamTexture.width <= 16)
            {
                yield return new WaitForSeconds(0.1f);
                sw += 0.1f;
                if (sw > 8f) { ScreenDebugLog.Log("Cam start timeout"); yield break; }
            }

            ScreenDebugLog.Log($"Cam OK {_webCamTexture.width}x{_webCamTexture.height} rot={_webCamTexture.videoRotationAngle} vM={_webCamTexture.videoVerticallyMirrored}");

            if (_rawImage != null)
            {
                _rawImage.texture = _webCamTexture;
            }

            _started = true;
        }

        /// <summary>
        /// 每帧更新摄像头方向（因为有些设备旋转值可能变化）
        /// </summary>
        private void Update()
        {
            if (!_started || _rawImage == null || _webCamTexture == null) return;

            int angle = _webCamTexture.videoRotationAngle;
            bool vMirrored = _webCamTexture.videoVerticallyMirrored;

            // rot=270, front=true, vM=false 的正确处理：
            // 实测：90度倒，-90度（=270度）正确
            _rawImageRT.localEulerAngles = new Vector3(0, 0, -90);
            // 前置摄像头水平镜像
            _rawImageRT.localScale = new Vector3(-1, 1, 1);

            // 修正宽高比
            var parent = _rawImageRT.parent as RectTransform;
            if (parent == null) return;
            float parentW = parent.rect.width;
            float parentH = parent.rect.height;
            if (parentW <= 0 || parentH <= 0) return;

            float camW = _webCamTexture.width;
            float camH = _webCamTexture.height;

            // rot=270 时实际显示宽高交换
            float displayW = camH;
            float displayH = camW;

            float scaleToFit = Mathf.Min(parentW / displayW, parentH / displayH);
            Vector2 nextSize = new Vector2(displayW * scaleToFit, displayH * scaleToFit);
            if (Vector2.Distance(_lastPreviewSize, nextSize) > 0.5f
                || _lastRotation != angle
                || _lastMirrored != vMirrored)
            {
                _rawImageRT.sizeDelta = nextSize;
                _lastPreviewSize = nextSize;
                _lastRotation = angle;
                _lastMirrored = vMirrored;
            }
            _rawImageRT.anchorMin = new Vector2(0.5f, 0.5f);
            _rawImageRT.anchorMax = new Vector2(0.5f, 0.5f);
            _rawImageRT.anchoredPosition = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_webCamTexture != null && _webCamTexture.isPlaying)
                _webCamTexture.Stop();
        }

        private void OnApplicationPause(bool pause)
        {
            if (_webCamTexture == null) return;
            if (pause) _webCamTexture.Pause();
            else if (_started) _webCamTexture.Play();
        }
    }
}
