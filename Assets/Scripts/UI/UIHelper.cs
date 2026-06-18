using UnityEngine;

namespace LoseWeight.UI
{
    public static class UIHelper
    {
        public static readonly Color DarkBg = new Color(0.08f, 0.09f, 0.12f, 1f);
        public static readonly Color PanelBg = new Color(0.12f, 0.13f, 0.18f, 0.95f);
        public static readonly Color CardBg = new Color(0.18f, 0.20f, 0.28f, 0.95f);
        public static readonly Color PrimaryColor = new Color(0.95f, 0.25f, 0.18f, 1f);
        public static readonly Color SecondaryColor = new Color(1f, 0.72f, 0.18f, 1f);
        public static readonly Color TextWhite = Color.white;
        public static readonly Color TextGray = new Color(0.72f, 0.75f, 0.82f, 1f);

        public static void SetAnchored(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rt == null) return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
