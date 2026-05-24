using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 14. \u4e2a\u4eba\u4e2d\u5fc3\u9875 - \u8d44\u6599\u3001\u5730\u533a\u3001\u6218\u7ee9\u3001\u8d26\u53f7\u7ed1\u5b9a
    /// </summary>
    public class ProfilePage : MonoBehaviour
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
                "\u4e2a\u4eba\u4e2d\u5fc3", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);

            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10); backRT.offsetMax = new Vector2(80, -10);

            // \u5934\u50cf + \u6635\u79f0
            var avatarPanel = new GameObject("AvatarPanel");
            avatarPanel.transform.SetParent(transform, false);
            var apRT = avatarPanel.AddComponent<RectTransform>();
            UIHelper.SetAnchored(apRT, new Vector2(0, 0.72f), new Vector2(1, 0.90f),
                new Vector2(20, 0), new Vector2(-20, 0));

            var avatar = new GameObject("Avatar");
            avatar.transform.SetParent(avatarPanel.transform, false);
            var avRT = avatar.AddComponent<RectTransform>();
            avRT.anchorMin = new Vector2(0, 0.1f); avRT.anchorMax = new Vector2(0, 0.9f);
            avRT.offsetMin = new Vector2(20, 0); avRT.offsetMax = new Vector2(100, 0);
            avatar.AddComponent<Image>().color = UIHelper.PrimaryColor;

            UIHelper.CreateText(avatarPanel.transform, "Nickname",
                "\u62f3\u51fb\u8fbe\u4eba", 30, UIHelper.TextWhite, TextAnchor.MiddleLeft, FontStyle.Bold)
                .GetComponent<RectTransform>().SetParent(avatarPanel.transform, false);

            // \u4fe1\u606f\u5361\u7247
            var scroll = UIHelper.CreateScrollView(transform, "InfoList", Color.clear);
            var scrollRT = scroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scrollRT, new Vector2(0, 0.05f), new Vector2(1, 0.72f),
                new Vector2(10, 0), new Vector2(-10, 0));
            var content = scroll.content;

            CreateInfoRow(content, "\u6027\u522b", "\u7537");
            CreateInfoRow(content, "\u5730\u533a", "\u5e7f\u4e1c - \u6df1\u5733");
            CreateInfoRow(content, "\u603b\u80dc\u573a", "28");
            CreateInfoRow(content, "\u603b\u8d1f\u573a", "12");
            CreateInfoRow(content, "\u8fde\u80dc\u7eaa\u5f55", "5");
            CreateInfoRow(content, "KO\u6570", "15");
            CreateInfoRow(content, "\u6bb5\u4f4d", "\u767d\u94f6 II");

            // \u64cd\u4f5c\u6309\u94ae
            CreateActionRow(content, "\u4fee\u6539\u5730\u533a",
                () => UIManager.Instance.OnClickRegionSelect());
            CreateActionRow(content, "\u7ed1\u5b9a\u5fae\u4fe1",
                () => Debug.Log("[Profile] Bind Wechat"));
            CreateActionRow(content, "\u7ed1\u5b9a\u624b\u673a\u53f7",
                () => Debug.Log("[Profile] Bind Phone"));
            CreateActionRow(content, "\u9000\u51fa\u767b\u5f55",
                () => GameManager.Instance.ChangeState(GameState.Loading));
        }

        private void CreateInfoRow(Transform parent, string label, string value)
        {
            var go = new GameObject($"Row_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 65;
            go.AddComponent<Image>().color = UIHelper.CardBg;

            var labelText = UIHelper.CreateText(go.transform, "Label",
                label, 22, UIHelper.TextGray, TextAnchor.MiddleLeft);
            var lRT = labelText.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(0.4f, 1);
            lRT.offsetMin = new Vector2(20, 0); lRT.offsetMax = Vector2.zero;

            var valueText = UIHelper.CreateText(go.transform, "Value",
                value, 24, UIHelper.TextWhite, TextAnchor.MiddleRight);
            var vRT = valueText.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(0.4f, 0); vRT.anchorMax = new Vector2(1, 1);
            vRT.offsetMin = Vector2.zero; vRT.offsetMax = new Vector2(-20, 0);
        }

        private void CreateActionRow(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var btn = UIHelper.CreateButton(parent, $"Action_{label}",
                label, UIHelper.CardBg, UIHelper.PrimaryColor, 22, onClick);
            btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 65;
        }
    }
}
