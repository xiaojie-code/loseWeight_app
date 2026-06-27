using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.CannonGame
{
    public class Cannonball : MonoBehaviour
    {
        private Vector2 _startScreen;
        private Vector2 _endScreen;
        private RectTransform _rt;
        private Image _image;
        private float _startTime;
        private bool _arrived;
        private System.Action<Vector2, Vector2> _onArrived;

        private const float FlyDuration = 0.38f;

        public void Initialize(Vector2 startScreen, Vector2 endScreen, RectTransform canvas, System.Action<Vector2, Vector2> onArrived)
        {
            _startScreen = startScreen;
            _endScreen = endScreen;
            _onArrived = onArrived;
            _startTime = Time.time;
            _arrived = false;

            _rt = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            if (_rt == null || _image == null)
            {
                Debug.LogError("[CannonGame] Cannonball pool object is missing RectTransform/Image.");
                gameObject.SetActive(false);
                return;
            }

            _rt.sizeDelta = new Vector2(44, 44);
            _rt.localScale = Vector3.one;
            _image.color = new Color(1f, 0.8f, 0f, 1f);
            _image.raycastTarget = false;
            SetPositionFromScreen(_startScreen);
        }

        private void Update()
        {
            if (_arrived) return;

            float t = (Time.time - _startTime) / FlyDuration;
            if (t >= 1f)
            {
                t = 1f;
                _arrived = true;
            }

            SetPositionFromScreen(Vector2.Lerp(_startScreen, _endScreen, t));

            if (_arrived)
            {
                _onArrived?.Invoke(_startScreen, _endScreen);
                gameObject.SetActive(false);
            }
        }

        private void SetPositionFromScreen(Vector2 screenPos)
        {
            if (_rt == null) return;
            float ax = screenPos.x / Mathf.Max(1f, Screen.width);
            float ay = screenPos.y / Mathf.Max(1f, Screen.height);
            _rt.anchorMin = new Vector2(ax, ay);
            _rt.anchorMax = new Vector2(ax, ay);
            _rt.anchoredPosition = Vector2.zero;
        }
    }
}
