using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.FruitNinja
{
    public class BladeTrail : MonoBehaviour
    {
        private RectTransform _bladeRT;
        private Image _bladeImage;
        private RectTransform[] _trailDots;
        private Vector2[] _history;
        private int _historyIdx;
        private int _historyCount;

        private const int TrailLength = 6;

        public void Initialize(RectTransform parent, Transform bladeRoot, Color bladeColor)
        {
            _bladeRT = FindChild(bladeRoot, "Blade")?.GetComponent<RectTransform>();
            _bladeImage = _bladeRT != null ? _bladeRT.GetComponent<Image>() : null;
            if (_bladeImage != null)
            {
                _bladeImage.color = bladeColor;
                _bladeImage.raycastTarget = false;
            }

            _trailDots = new RectTransform[TrailLength];
            _history = new Vector2[TrailLength];
            for (int i = 0; i < TrailLength; i++)
            {
                var dot = FindChild(bladeRoot, $"T{i}")?.GetComponent<RectTransform>();
                if (dot != null)
                {
                    var img = dot.GetComponent<Image>();
                    if (img != null) img.color = new Color(bladeColor.r, bladeColor.g, bladeColor.b, Mathf.Lerp(0.6f, 0.1f, (float)i / TrailLength));
                    dot.gameObject.SetActive(false);
                }
                _trailDots[i] = dot;
            }
            Hide();
        }

        public void UpdatePosition(Vector2 screenPos, bool valid)
        {
            if (!valid)
            {
                Hide();
                return;
            }

            SetScreenPosition(_bladeRT, screenPos);
            if (_bladeRT != null) _bladeRT.gameObject.SetActive(true);

            _history[_historyIdx] = screenPos;
            _historyIdx = (_historyIdx + 1) % TrailLength;
            if (_historyCount < TrailLength) _historyCount++;

            for (int i = 0; i < _historyCount; i++)
            {
                int idx = (_historyIdx - 1 - i + TrailLength) % TrailLength;
                SetScreenPosition(_trailDots[i], _history[idx]);
                if (_trailDots[i] != null) _trailDots[i].gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_bladeRT != null) _bladeRT.gameObject.SetActive(false);
            if (_trailDots != null)
            {
                for (int i = 0; i < _trailDots.Length; i++)
                    if (_trailDots[i] != null) _trailDots[i].gameObject.SetActive(false);
            }
            _historyCount = 0;
            _historyIdx = 0;
        }

        private static void SetScreenPosition(RectTransform rt, Vector2 screenPos)
        {
            if (rt == null) return;
            float ax = screenPos.x / Mathf.Max(1f, Screen.width);
            float ay = screenPos.y / Mathf.Max(1f, Screen.height);
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
