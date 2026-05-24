using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 16. \u6218\u7ee9\u9875 - \u5386\u53f2\u5bf9\u5c40\u3001\u80dc\u7387\u3001\u52a8\u4f5c\u7edf\u8ba1
    /// </summary>
    public class RecordPage : MonoBehaviour
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
                "\u6218\u7ee9", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);

            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10); backRT.offsetMax = new Vector2(80, -10);

            // \u7edf\u8ba1\u6982\u89c8
            var statsPanel = new GameObject("StatsPanel");
            statsPanel.transform.SetParent(transform, false);
            var spRT = statsPanel.AddComponent<RectTransform>();
            UIHelper.SetAnchored(spRT, new Vector2(0, 0.72f), new Vector2(1, 0.90f),
                new Vector2(15, 0), new Vector2(-15, 0));
            statsPanel.AddComponent<Image>().color = UIHelper.CardBg;

            var hlg = statsPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateStatBlock(statsPanel.transform, "\u603b\u573a\u6b21", "40");
            CreateStatBlock(statsPanel.transform, "\u80dc\u7387", "70%");
            CreateStatBlock(statsPanel.transform, "\u8fde\u80dc", "5");
            CreateStatBlock(statsPanel.transform, "KO", "15");

            // \u5386\u53f2\u5bf9\u5c40\u5217\u8868
            var scroll = UIHelper.CreateScrollView(transform, "RecordList", Color.clear);
            var scrollRT = scroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scrollRT, new Vector2(0, 0.05f), new Vector2(1, 0.70f),
                new Vector2(10, 0), new Vector2(-10, 0));
            var content = scroll.content;

            // \u6a21\u62df\u5386\u53f2\u6570\u636e
            CreateRecordItem(content, "\u62f3\u624b001", "2:1", true, "\u5728\u7ebf\u5339\u914d", "5\u5206\u949f\u524d");
            CreateRecordItem(content, "\u62f3\u624b015", "0:2", false, "\u623f\u95f4\u5bf9\u6218", "12\u5206\u949f\u524d");
            CreateRecordItem(content, "AI \u5bf9\u624b", "2:0", true, "AI\u5bf9\u6218", "30\u5206\u949f\u524d");
            CreateRecordItem(content, "\u62f3\u624b088", "2:1", true, "\u5728\u7ebf\u5339\u914d", "1\u5c0f\u65f6\u524d");
            CreateRecordItem(content, "\u62f3\u624b042", "1:2", false, "\u5728\u7ebf\u5339\u914d", "2\u5c0f\u65f6\u524d");
            CreateRecordItem(content, "AI \u5bf9\u624b", "2:0", true, "AI\u5bf9\u6218", "\u6628\u5929");
        }

        private void CreateStatBlock(Transform parent, string label, string value)
        {
            var go = new GameObject($"Stat_{label}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandHeight = true;

            var valText = UIHelper.CreateText(go.transform, "Value",
                value, 32, UIHelper.PrimaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            valText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            var lblText = UIHelper.CreateText(go.transform, "Label",
                label, 18, UIHelper.TextGray);
            lblText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        }

        private void CreateRecordItem(Transform parent, string opponent, string score, bool isWin, string mode, string time)
        {
            var go = new GameObject($"Record_{opponent}");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 80;
            go.AddComponent<Image>().color = UIHelper.CardBg;

            // \u80dc\u8d1f\u6807\u8bb0
            var flagText = UIHelper.CreateText(go.transform, "Flag",
                isWin ? "\u80dc" : "\u8d1f", 24,
                isWin ? Color.green : new Color(1f, 0.3f, 0.3f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var fRT = flagText.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(0.1f, 1);
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;

            // \u5bf9\u624b + \u6bd4\u5206
            var nameText = UIHelper.CreateText(go.transform, "Name",
                $"{opponent}  {score}", 22, UIHelper.TextWhite, TextAnchor.MiddleLeft);
            var nRT = nameText.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.12f, 0.4f); nRT.anchorMax = new Vector2(0.7f, 1);
            nRT.offsetMin = Vector2.zero; nRT.offsetMax = Vector2.zero;

            // \u6a21\u5f0f
            var modeText = UIHelper.CreateText(go.transform, "Mode",
                mode, 18, UIHelper.TextGray, TextAnchor.MiddleLeft);
            var mRT = modeText.GetComponent<RectTransform>();
            mRT.anchorMin = new Vector2(0.12f, 0); mRT.anchorMax = new Vector2(0.7f, 0.4f);
            mRT.offsetMin = Vector2.zero; mRT.offsetMax = Vector2.zero;

            // \u65f6\u95f4
            var timeText = UIHelper.CreateText(go.transform, "Time",
                time, 18, UIHelper.TextGray, TextAnchor.MiddleRight);
            var tRT = timeText.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.7f, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = new Vector2(-15, 0);
        }
    }
}
