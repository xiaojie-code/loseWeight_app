using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 17. \u8bad\u7ec3\u9875 - \u4eba\u673a/\u6253\u9776/\u52a8\u4f5c\u6821\u51c6\u8bad\u7ec3
    /// </summary>
    public class TrainingPage : MonoBehaviour
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
            var header = new GameObject("Header");
            header.transform.SetParent(transform, false);
            var headerRT = header.AddComponent<RectTransform>();
            UIHelper.SetAnchored(headerRT, new Vector2(0, 0.90f), new Vector2(1, 1),
                Vector2.zero, Vector2.zero);
            header.AddComponent<Image>().color = UIHelper.PanelBg;

            UIHelper.CreateText(header.transform, "Title",
                "\u8bad\u7ec3\u6a21\u5f0f", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);

            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10); backRT.offsetMax = new Vector2(80, -10);

            // \u8bad\u7ec3\u6a21\u5f0f\u5361\u7247
            CreateTrainingCard("\u52a8\u4f5c\u6821\u51c6",
                "\u6821\u51c6\u4f60\u7684\u51fa\u62f3\u3001\u9632\u5fa1\u3001\u95ea\u907f\u52a8\u4f5c",
                UIHelper.PrimaryColor,
                new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.85f),
                () => StartTraining("calibration"));

            CreateTrainingCard("\u6253\u9776\u8bad\u7ec3",
                "\u6309\u63d0\u793a\u65b9\u5411\u51fa\u62f3\uff0c\u8bad\u7ec3\u53cd\u5e94\u901f\u5ea6",
                UIHelper.SecondaryColor,
                new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.60f),
                () => StartTraining("target"));

            CreateTrainingCard("AI \u5bf9\u6218\u8bad\u7ec3",
                "\u4e0e AI \u5bf9\u624b\u8fdb\u884c\u65e0\u6392\u540d\u5bf9\u6218",
                UIHelper.CardBg,
                new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.36f),
                () => StartTraining("ai"));
        }

        private void CreateTrainingCard(string title, string desc, Color color,
            Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Card_{title}");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            UIHelper.SetAnchored(rt, aMin, aMax, Vector2.zero, Vector2.zero);
            go.AddComponent<Image>().color = color;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var titleText = UIHelper.CreateText(go.transform, "Title",
                title, 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            var tRT = titleText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(tRT, new Vector2(0, 0.5f), new Vector2(0.9f, 0.9f),
                new Vector2(25, 0), Vector2.zero);

            var descText = UIHelper.CreateText(go.transform, "Desc",
                desc, 20, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            var dRT = descText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(dRT, new Vector2(0, 0.1f), new Vector2(0.9f, 0.5f),
                new Vector2(25, 0), Vector2.zero);
        }

        private void StartTraining(string mode)
        {
            Debug.Log($"[Training] Start mode: {mode}");
            GameManager.Instance.ChangeState(GameState.PreCombat);
        }
    }
}
