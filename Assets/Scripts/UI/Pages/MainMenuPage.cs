using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 4. \u9996\u9875 - \u6a21\u5f0f\u5165\u53e3\u3001\u89d2\u8272\u5c55\u793a\u3001\u6d3b\u52a8\u5165\u53e3
    /// </summary>
    public class MainMenuPage : MonoBehaviour
    {
        private void OnEnable()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            var bg = UIHelper.CreatePanel(transform, "Background", UIHelper.DarkBg);

            // \u6807\u9898
            var title = UIHelper.CreateText(transform, "Title",
                "\u62f3\u51fb\u5bf9\u6218", 48, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, new Vector2(0, 0.85f), new Vector2(1, 0.95f),
                Vector2.zero, Vector2.zero);

            // \u526f\u6807\u9898
            var sub = UIHelper.CreateText(transform, "Subtitle",
                "\u4f53\u611f\u62f3\u51fb\u5065\u8eab\u6e38\u620f", 24, UIHelper.TextGray);
            var subRT = sub.GetComponent<RectTransform>();
            UIHelper.SetAnchored(subRT, new Vector2(0, 0.80f), new Vector2(1, 0.85f),
                Vector2.zero, Vector2.zero);

            // \u6309\u94ae\u533a\u57df
            float btnY = 0.65f;
            float btnH = 0.08f;
            float gap = 0.02f;

            CreateMenuButton("\u5728\u7ebf\u5339\u914d", btnY, UIHelper.PrimaryColor,
                () => UIManager.Instance.OnClickOnlineMatch());
            btnY -= btnH + gap;

            CreateMenuButton("\u521b\u5efa\u623f\u95f4", btnY, UIHelper.PrimaryColor,
                () => UIManager.Instance.OnClickCreateRoom());
            btnY -= btnH + gap;

            CreateMenuButton("AI \u5bf9\u6218", btnY, UIHelper.SecondaryColor,
                () => UIManager.Instance.OnClickAIBattle());
            btnY -= btnH + gap;

            CreateMenuButton("\u6392\u884c\u699c", btnY, UIHelper.CardBg,
                () => UIManager.Instance.OnClickRanking());
            btnY -= btnH + gap;

            CreateMenuButton("\u88c5\u626e", btnY, UIHelper.CardBg,
                () => UIManager.Instance.OnClickDressing());
            btnY -= btnH + gap;

            CreateMenuButton("\u8bad\u7ec3\u6a21\u5f0f", btnY, UIHelper.CardBg,
                () => UIManager.Instance.OnClickAIBattle());
            btnY -= btnH + gap;

            // \u5e95\u90e8\u5c0f\u6309\u94ae
            var bottomPanel = new GameObject("BottomPanel");
            bottomPanel.transform.SetParent(transform, false);
            var bpRT = bottomPanel.AddComponent<RectTransform>();
            UIHelper.SetAnchored(bpRT, new Vector2(0, 0.02f), new Vector2(1, 0.10f),
                new Vector2(40, 0), new Vector2(-40, 0));
            var hlg = bottomPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateSmallButton(bottomPanel.transform, "\u4e2a\u4eba\u4e2d\u5fc3",
                () => UIManager.Instance.OnClickProfile());
            CreateSmallButton(bottomPanel.transform, "\u8bbe\u7f6e",
                () => UIManager.Instance.OnClickSettings());
            CreateSmallButton(bottomPanel.transform, "\u6218\u7ee9",
                () => UIManager.Instance.OnClickRecord());
        }

        private void CreateMenuButton(string label, float yNorm, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var btn = UIHelper.CreateButton(transform, $"Btn_{label}", label, color, Color.white, 30, onClick);
            var rt = btn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(rt, new Vector2(0.15f, yNorm), new Vector2(0.85f, yNorm + 0.08f),
                Vector2.zero, Vector2.zero);
        }

        private void CreateSmallButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var btn = UIHelper.CreateButton(parent, $"Btn_{label}", label, UIHelper.CardBg, UIHelper.TextGray, 22, onClick);
            var le = btn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
        }
    }
}
