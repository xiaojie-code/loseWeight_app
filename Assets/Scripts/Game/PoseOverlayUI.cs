using UnityEngine;
using UnityEngine.UI;
using LoseWeight.PoseDetection;

namespace LoseWeight.Game
{
    /// <summary>
    /// 在摄像头预览窗口上绘制姿态关键点和连接线
    /// </summary>
    public class PoseOverlayUI : MonoBehaviour
    {
        private RectTransform _parentRT;
        private RectTransform[] _dots;
        private Image[] _dotImages;
        private RectTransform[] _lines;
        private Image[] _lineImages;
        private PoseFrame _lastFrame;
        private bool _initialized;

        // 关键点数量（MediaPipe Pose 33点）
        private const int LANDMARK_COUNT = 33;

        // 连接线定义（MediaPipe Pose 骨骼连接）
        private static readonly int[,] CONNECTIONS = {
            {11,12}, {11,13}, {13,15}, {12,14}, {14,16},  // 上肢
            {11,23}, {12,24}, {23,24},                      // 躯干
            {23,25}, {25,27}, {24,26}, {26,28},            // 下肢
            {15,17}, {15,19}, {17,19},                      // 左手
            {16,18}, {16,20}, {18,20},                      // 右手
            {27,29}, {27,31}, {29,31},                      // 左脚
            {28,30}, {28,32}, {30,32},                      // 右脚
        };

        // 左侧关键点（青色）
        private static readonly int[] LEFT_INDICES = { 1,2,3,7,9,11,13,15,17,19,21,23,25,27,29,31 };
        // 右侧关键点（橙色）
        private static readonly int[] RIGHT_INDICES = { 4,5,6,8,10,12,14,16,18,20,22,24,26,28,30,32 };

        public void Initialize(RectTransform parent)
        {
            _parentRT = parent;
            CreateDots();
            CreateLines();
            _initialized = true;
        }

        public void UpdatePose(PoseFrame frame)
        {
            _lastFrame = frame;
        }

        private void LateUpdate()
        {
            if (!_initialized || _lastFrame == null) return;
            DrawLandmarks();
        }

        private void DrawLandmarks()
        {
            var rect = _parentRT.rect;

            // 更新点位
            for (int i = 0; i < LANDMARK_COUNT; i++)
            {
                var lm = _lastFrame.Landmarks[i];
                if (lm.Confidence < 0.3f)
                {
                    _dots[i].gameObject.SetActive(false);
                    continue;
                }

                _dots[i].gameObject.SetActive(true);
                float x = lm.Position.x * rect.width + rect.xMin;
                float y = (1f - lm.Position.y) * rect.height + rect.yMin;
                _dots[i].anchoredPosition = new Vector2(x, y);
            }

            // 更新连接线
            for (int i = 0; i < CONNECTIONS.GetLength(0); i++)
            {
                int a = CONNECTIONS[i, 0];
                int b = CONNECTIONS[i, 1];

                var lmA = _lastFrame.Landmarks[a];
                var lmB = _lastFrame.Landmarks[b];

                if (lmA.Confidence < 0.3f || lmB.Confidence < 0.3f)
                {
                    _lines[i].gameObject.SetActive(false);
                    continue;
                }

                _lines[i].gameObject.SetActive(true);
                float ax = lmA.Position.x * rect.width + rect.xMin;
                float ay = (1f - lmA.Position.y) * rect.height + rect.yMin;
                float bx = lmB.Position.x * rect.width + rect.xMin;
                float by = (1f - lmB.Position.y) * rect.height + rect.yMin;

                DrawLine(_lines[i], _lineImages[i], new Vector2(ax, ay), new Vector2(bx, by));
            }
        }

        private void CreateDots()
        {
            _dots = new RectTransform[LANDMARK_COUNT];
            _dotImages = new Image[LANDMARK_COUNT];

            for (int i = 0; i < LANDMARK_COUNT; i++)
            {
                var go = new GameObject($"Dot_{i}");
                go.transform.SetParent(_parentRT, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(10, 10);
                var img = go.AddComponent<Image>();
                img.color = GetDotColor(i);
                img.raycastTarget = false;
                _dots[i] = rt;
                _dotImages[i] = img;
                go.SetActive(false);
            }
        }

        private void CreateLines()
        {
            int count = CONNECTIONS.GetLength(0);
            _lines = new RectTransform[count];
            _lineImages = new Image[count];

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Line_{i}");
                go.transform.SetParent(_parentRT, false);
                go.transform.SetAsFirstSibling(); // 线在点下面
                var rt = go.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0, 0.5f);
                var img = go.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.6f);
                img.raycastTarget = false;
                _lines[i] = rt;
                _lineImages[i] = img;
                go.SetActive(false);
            }
        }

        private void DrawLine(RectTransform rt, Image img, Vector2 from, Vector2 to)
        {
            var dir = to - from;
            float dist = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = from;
            rt.sizeDelta = new Vector2(dist, 4f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private Color GetDotColor(int index)
        {
            // 中心点白色
            if (index == 0) return Color.white;

            foreach (var l in LEFT_INDICES)
                if (l == index) return new Color(0f, 0.9f, 1f, 1f); // 青色

            foreach (var r in RIGHT_INDICES)
                if (r == index) return new Color(1f, 0.6f, 0f, 1f); // 橙色

            return Color.white;
        }
    }
}
