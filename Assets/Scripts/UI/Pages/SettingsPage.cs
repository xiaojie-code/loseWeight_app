using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 15. \u8bbe\u7f6e\u9875 - \u97f3\u6548\u3001\u753b\u8d28\u3001\u6444\u50cf\u5934\u3001\u9690\u79c1\u3001\u8d26\u53f7
    /// </summary>
    public class SettingsPage : MonoBehaviour
    {
        private void OnEnable()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            var bg = UIHelper.CreatePanel(transform, "Background", UIHelper.DarkBg);

            // \u6807\u9898\u680f
            var header = new GameObject("Header");
            header.transform.SetParent(transform, false);
            var headerRT = header.AddComponent<RectTransform>();
            UIHelper.SetAnchored(headerRT, new Vector2(0, 0.90f), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);
            header.AddComponent<Image>().color = UIHelper.PanelBg;

            UIHelper.CreateText(header.transform, "Title",
                "\u8bbe\u7f6e", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);

            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10); backRT.offsetMax = new Vector2(80, -10);

            // \u8bbe\u7f6e\u5217\u8868
            var scroll = UIHelper.CreateScrollView(transform, "SettingsList", Color.clear);
            var scrollRT = scroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scrollRT, new Vector2(0, 0.05f), new Vector2(1, 0.90f),
                new Vector2(10, 0), new Vector2(-10, 0));
            var content = scroll.content;

            CreateSettingSection(content, "\u97f3\u6548");
            CreateToggleRow(content, "BGM", true);
            CreateToggleRow(content, "\u97f3\u6548", true);

            CreateSettingSection(content, "\u753b\u8d28");
            CreateOptionRow(content, "\u753b\u8d28\u6863\u4f4d", "\u4e2d");
            CreateOptionRow(content, "\u5e27\u7387", "60 FPS");

            CreateSettingSection(content, "\u6444\u50cf\u5934");
            CreateOptionRow(content, "\u8bc6\u522b\u5e27\u7387", "15 FPS");
            CreateToggleRow(content, "\u663e\u793a\u5173\u952e\u70b9", true);
            CreateToggleRow(content, "\u955c\u50cf\u7ffb\u8f6c", true);

            CreateSettingSection(content, "\u5176\u4ed6");
            CreateActionRow(content, "\u9690\u79c1\u653f\u7b56");
            CreateActionRow(content, "\u7528\u6237\u534f\u8bae");
            CreateActionRow(content, "\u5173\u4e8e\u6211\u4eec");
            CreateActionRow(content, "\u6e05\u9664\u7f13\u5b58");

            // \u7248\u672c\u53f7
            var version = UIHelper.CreateText(content, "Version",
                "v1.0.0 (Build 1)", 18, UIHelper.TextGray);
            version.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
        }

        private void CreateSettingSection(Transform parent, string title)
        {
            var text = UIHelper.CreateText(parent, $"Section_{title}",
                title, 20, UIHelper.TextGray, TextAnchor.MiddleLeft, FontStyle.Bold);
            var le = text.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
        }

        private void CreateToggleRow(Transform parent, string label, bool defaultValue)
        {
            var go = new GameObject($"Toggle_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 65;
            go.AddComponent<Image>().color = UIHelper.CardBg;

            var labelText = UIHelper.CreateText(go.transform, "Label",
                label, 24, UIHelper.TextWhite, TextAnchor.MiddleLeft);
            var lRT = labelText.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(0.6f, 1);
            lRT.offsetMin = new Vector2(20, 0); lRT.offsetMax = Vector2.zero;

            var statusText = UIHelper.CreateText(go.transform, "Status",
                defaultValue ? "\u5f00" : "\u5173", 24,
                defaultValue ? Color.green : UIHelper.TextGray, TextAnchor.MiddleRight);
            var sRT = statusText.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.7f, 0); sRT.anchorMax = new Vector2(1, 1);
            sRT.offsetMin = Vector2.zero; sRT.offsetMax = new Vector2(-20, 0);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                bool newVal = statusText.text == "\u5173";
                statusText.text = newVal ? "\u5f00" : "\u5173";
                statusText.color = newVal ? Color.green : UIHelper.TextGray;
            });
        }

        private void CreateOptionRow(Transform parent, string label, string value)
        {
            var go = new GameObject($"Option_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 65;
            go.AddComponent<Image>().color = UIHelper.CardBg;

            var labelText = UIHelper.CreateText(go.transform, "Label",
                label, 24, UIHelper.TextWhite, TextAnchor.MiddleLeft);
            var lRT = labelText.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(0.5f, 1);
            lRT.offsetMin = new Vector2(20, 0); lRT.offsetMax = Vector2.zero;

            var valueText = UIHelper.CreateText(go.transform, "Value",
                value, 22, UIHelper.PrimaryColor, TextAnchor.MiddleRight);
            var vRT = valueText.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(0.5f, 0); vRT.anchorMax = new Vector2(1, 1);
            vRT.offsetMin = Vector2.zero; vRT.offsetMax = new Vector2(-20, 0);
        }

        private void CreateActionRow(Transform parent, string label)
        {
            var btn = UIHelper.CreateButton(parent, $"Action_{label}",
                label, UIHelper.CardBg, UIHelper.TextWhite, 22,
                () => Debug.Log($"[Settings] {label}"));
            btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
        }
    }
}
