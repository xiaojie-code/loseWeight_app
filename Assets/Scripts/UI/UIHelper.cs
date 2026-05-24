using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace LoseWeight.UI
{
    /// <summary>
    /// UI \u5de5\u5177\u7c7b - \u7528\u4e8e\u4ee3\u7801\u52a8\u6001\u521b\u5efa UI \u5143\u7d20
    /// </summary>
    public static class UIHelper
    {
        private static Font _defaultFont;
        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                    _defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
                return _defaultFont;
            }
        }

        public static readonly Color PrimaryColor = new Color(0.2f, 0.6f, 1f);
        public static readonly Color SecondaryColor = new Color(1f, 0.5f, 0.2f);
        public static readonly Color DarkBg = new Color(0.08f, 0.08f, 0.12f);
        public static readonly Color PanelBg = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        public static readonly Color CardBg = new Color(0.18f, 0.18f, 0.25f);
        public static readonly Color TextWhite = Color.white;
        public static readonly Color TextGray = new Color(0.6f, 0.6f, 0.65f);

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static Text CreateText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            Color bgColor, Color textColor, int fontSize = 28, UnityAction onClick = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // \u6309\u94ae\u6587\u5b57
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;

            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static ScrollRect CreateScrollView(Transform parent, string name, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            scroll.content = cRt;

            return scroll;
        }

        public static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static void SetSize(RectTransform rt, float width, float height)
        {
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
