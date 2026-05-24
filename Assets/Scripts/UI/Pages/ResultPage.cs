using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.Combat;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 11. \u7ed3\u7b97\u9875 - \u5bf9\u5c40\u7ed3\u675f\u5f39\u6846\uff0c\u80dc\u8d1f\u3001\u5956\u52b1\u3001\u518d\u73a9\u4e00\u5c40/\u8fd4\u56de\u9996\u9875/\u5206\u4eab\u597d\u53cb
    /// </summary>
    public class ResultPage : MonoBehaviour
    {
        private Text _resultText;
        private Text _scoreText;
        private Text _statsText;
        private bool _built;

        private void OnEnable()
        {
            BuildUI();
            RefreshData();
        }

        private void RefreshData()
        {
            if (CombatManager.Instance == null) return;
            var player = CombatManager.Instance.GetPlayerData();
            var opponent = CombatManager.Instance.GetOpponentData();
            var wins = CombatManager.Instance.RoundWins;

            bool isWin = wins != null && wins[0] > wins[1];
            string score = wins != null ? $"{wins[0]} : {wins[1]}" : "0 : 0";

            if (_resultText != null)
            {
                _resultText.text = isWin ? "\u80dc\u5229\uff01" : "\u5931\u8d25";
                _resultText.color = isWin ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f);
            }
            if (_scoreText != null)
                _scoreText.text = score;
            if (_statsText != null)
                _statsText.text = isWin
                    ? "\u606d\u559c\u4f60\u51fb\u8d25\u4e86\u5bf9\u624b\uff01"
                    : "\u7ee7\u7eed\u52a0\u6cb9\uff0c\u4e0b\u6b21\u4e00\u5b9a\u80fd\u8d62\uff01";
        }

        private void BuildUI()
        {
            if (_built) return;
            _built = true;

            // \u534a\u900f\u660e\u80cc\u666f\u906e\u7f69
            var overlay = UIHelper.CreatePanel(transform, "Overlay", new Color(0, 0, 0, 0.7f));

            // \u5f39\u6846\u5361\u7247
            var card = new GameObject("Card");
            card.transform.SetParent(transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            UIHelper.SetAnchored(cardRT, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.75f),
                Vector2.zero, Vector2.zero);
            card.AddComponent<Image>().color = UIHelper.PanelBg;

            // \u80dc\u8d1f\u6807\u9898
            _resultText = UIHelper.CreateText(card.transform, "Result",
                "\u80dc\u5229\uff01", 52, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var resultRT = _resultText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(resultRT, new Vector2(0, 0.72f), new Vector2(1, 0.92f),
                Vector2.zero, Vector2.zero);

            // \u6bd4\u5206
            _scoreText = UIHelper.CreateText(card.transform, "Score",
                "2 : 1", 40, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var scoreRT = _scoreText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scoreRT, new Vector2(0, 0.55f), new Vector2(1, 0.72f),
                Vector2.zero, Vector2.zero);

            // \u63cf\u8ff0
            _statsText = UIHelper.CreateText(card.transform, "Stats",
                "", 22, UIHelper.TextGray, TextAnchor.MiddleCenter);
            var statsRT = _statsText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(statsRT, new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.55f),
                Vector2.zero, Vector2.zero);

            // \u6309\u94ae\u533a\u57df
            // \u518d\u73a9\u4e00\u5c40
            var retryBtn = UIHelper.CreateButton(card.transform, "RetryBtn",
                "\u518d\u73a9\u4e00\u5c40", UIHelper.PrimaryColor, Color.white, 28,
                () => OnRetry());
            var retryRT = retryBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(retryRT, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.37f),
                Vector2.zero, Vector2.zero);

            // \u8fd4\u56de\u9996\u9875
            var homeBtn = UIHelper.CreateButton(card.transform, "HomeBtn",
                "\u8fd4\u56de\u9996\u9875", UIHelper.CardBg, UIHelper.TextWhite, 26,
                () => OnHome());
            var homeRT = homeBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(homeRT, new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.20f),
                Vector2.zero, Vector2.zero);

            // \u5206\u4eab\u597d\u53cb
            var shareBtn = UIHelper.CreateButton(card.transform, "ShareBtn",
                "\u5206\u4eab\u597d\u53cb", new Color(0.07f, 0.73f, 0.31f), Color.white, 26,
                () => OnShare());
            var shareRT = shareBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(shareRT, new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.20f),
                Vector2.zero, Vector2.zero);
        }

        private void OnRetry()
        {
            // \u76f4\u63a5\u8fdb\u5165\u5bf9\u6218
            GameManager.Instance.ChangeState(GameState.Combat);
        }

        private void OnHome()
        {
            UIManager.Instance.OnClickBackToMenu();
        }

        private void OnShare()
        {
            Debug.Log("[Result] Share to friend");
            // TODO: \u8c03\u7528\u5fae\u4fe1\u5206\u4eab SDK
        }
    }
}
