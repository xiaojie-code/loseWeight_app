using UnityEngine;
using UnityEngine.UI;
using LoseWeight.PoseDetection;

namespace LoseWeight.Game
{
    public class PoseOverlayUI : MonoBehaviour
    {
        private RectTransform _parentRT;
        private RectTransform[] _dots;
        private RectTransform[] _lines;
        private PoseFrame _lastFrame;
        private bool _initialized;

        private const int LandmarkCount = 33;

        private static readonly int[,] Connections =
        {
            {11,12}, {11,13}, {13,15}, {12,14}, {14,16},
            {11,23}, {12,24}, {23,24},
            {23,25}, {25,27}, {24,26}, {26,28},
            {15,17}, {15,19}, {17,19},
            {16,18}, {16,20}, {18,20},
            {27,29}, {27,31}, {29,31},
            {28,30}, {28,32}, {30,32},
        };

        public void Initialize(RectTransform parent)
        {
            _parentRT = parent;
            _dots = new RectTransform[LandmarkCount];
            for (int i = 0; i < LandmarkCount; i++)
            {
                _dots[i] = FindChild(parent, $"Dot_{i}")?.GetComponent<RectTransform>();
                if (_dots[i] != null) _dots[i].gameObject.SetActive(false);
            }

            int lineCount = Connections.GetLength(0);
            _lines = new RectTransform[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                _lines[i] = FindChild(parent, $"Line_{i}")?.GetComponent<RectTransform>();
                if (_lines[i] != null) _lines[i].gameObject.SetActive(false);
            }
            _initialized = true;
        }

        public void UpdatePose(PoseFrame frame)
        {
            _lastFrame = frame;
        }

        private void LateUpdate()
        {
            if (!_initialized || _lastFrame == null || _parentRT == null) return;
            DrawLandmarks();
        }

        private void DrawLandmarks()
        {
            var rect = _parentRT.rect;
            for (int i = 0; i < LandmarkCount && i < _lastFrame.Landmarks.Length; i++)
            {
                var dot = _dots[i];
                if (dot == null) continue;
                var lm = _lastFrame.Landmarks[i];
                if (lm.Confidence < 0.3f)
                {
                    dot.gameObject.SetActive(false);
                    continue;
                }

                dot.gameObject.SetActive(true);
                float x = lm.Position.x * rect.width + rect.xMin;
                float y = (1f - lm.Position.y) * rect.height + rect.yMin;
                dot.anchoredPosition = new Vector2(x, y);
            }

            for (int i = 0; i < Connections.GetLength(0); i++)
            {
                var line = _lines[i];
                if (line == null) continue;
                int a = Connections[i, 0];
                int b = Connections[i, 1];
                if (a >= _lastFrame.Landmarks.Length || b >= _lastFrame.Landmarks.Length)
                {
                    line.gameObject.SetActive(false);
                    continue;
                }

                var lmA = _lastFrame.Landmarks[a];
                var lmB = _lastFrame.Landmarks[b];
                if (lmA.Confidence < 0.3f || lmB.Confidence < 0.3f)
                {
                    line.gameObject.SetActive(false);
                    continue;
                }

                line.gameObject.SetActive(true);
                Vector2 from = new Vector2(lmA.Position.x * rect.width + rect.xMin, (1f - lmA.Position.y) * rect.height + rect.yMin);
                Vector2 to = new Vector2(lmB.Position.x * rect.width + rect.xMin, (1f - lmB.Position.y) * rect.height + rect.yMin);
                DrawLine(line, from, to);
            }
        }

        private static void DrawLine(RectTransform rt, Vector2 from, Vector2 to)
        {
            var dir = to - from;
            rt.anchoredPosition = from;
            rt.sizeDelta = new Vector2(dir.magnitude, 4f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
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
