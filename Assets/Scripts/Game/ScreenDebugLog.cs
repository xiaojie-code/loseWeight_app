using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace LoseWeight.Game
{
    /// <summary>
    /// 屏幕调试日志 - 在对局画面上显示关键状态信息
    /// 用于真机调试，发布时可禁用
    /// </summary>
    public class ScreenDebugLog : MonoBehaviour
    {
        public static bool Enabled = false; // 关闭屏幕日志
        private static ScreenDebugLog _instance;
        private Text _logText;
        private List<string> _lines = new List<string>();
        private const int MAX_LINES = 12;

        public static void Log(string msg)
        {
            if (Enabled && _instance != null) _instance.AddLine(msg);
            Debug.Log(msg);
        }

        private void Awake()
        {
            _instance = this;
        }

        public void Initialize(Transform parent)
        {
            if (!Enabled) return; // 关闭时不创建UI
            var go = new GameObject("DebugLogText");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.3f);
            rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(0, 0);
            _logText = go.AddComponent<Text>();
            _logText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            _logText.fontSize = 16;
            _logText.color = new Color(0f, 1f, 0f, 0.8f);
            _logText.alignment = TextAnchor.LowerLeft;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Truncate;
            _logText.raycastTarget = false;
        }

        private void AddLine(string msg)
        {
            _lines.Add($"[{Time.frameCount}] {msg}");
            while (_lines.Count > MAX_LINES) _lines.RemoveAt(0);
            if (_logText != null) _logText.text = string.Join("\n", _lines);
        }
    }
}
