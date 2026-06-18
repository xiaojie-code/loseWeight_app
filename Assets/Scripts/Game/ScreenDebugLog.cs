using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.Game
{
    public class ScreenDebugLog : MonoBehaviour
    {
        public static bool Enabled = false;
        private static ScreenDebugLog _instance;
        private readonly List<string> _lines = new List<string>();
        private Text _logText;

        private const int MaxLines = 12;

        public static void Log(string msg)
        {
            if (!Enabled) return;
            if (_instance != null) _instance.AddLine(msg);
            Debug.Log(msg);
        }

        private void Awake()
        {
            _instance = this;
        }

        public void Initialize(Transform parent)
        {
            _logText = FindChild(parent, "DebugLogText")?.GetComponent<Text>();
            if (_logText != null)
            {
                _logText.gameObject.SetActive(Enabled);
                _logText.text = "";
            }
            _lines.Clear();
        }

        private void AddLine(string msg)
        {
            _lines.Add($"[{Time.frameCount}] {msg}");
            while (_lines.Count > MaxLines) _lines.RemoveAt(0);
            if (_logText != null) _logText.text = string.Join("\n", _lines);
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
