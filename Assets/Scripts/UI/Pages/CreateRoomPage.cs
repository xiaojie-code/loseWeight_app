using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 9. \u521b\u5efa\u623f\u95f4\u9875 - \u623f\u95f4\u53f7\u3001\u9080\u8bf7\u3001\u51c6\u5907\u72b6\u6001
    /// </summary>
    public class CreateRoomPage : MonoBehaviour
    {
        private Text _roomCodeText;
        private Text _playerStatusText;
        private Text _opponentStatusText;

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
                "\u623f\u95f4\u5bf9\u6218", 36, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, new Vector2(0, 0.88f), new Vector2(1, 0.95f),
                Vector2.zero, Vector2.zero);

            // \u623f\u95f4\u53f7
            _roomCodeText = UIHelper.CreateText(transform, "RoomCode",
                "\u623f\u95f4\u53f7\uff1a8888", 40, UIHelper.SecondaryColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            var codeRT = _roomCodeText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(codeRT, new Vector2(0, 0.72f), new Vector2(1, 0.82f),
                Vector2.zero, Vector2.zero);

            // \u5206\u4eab\u6309\u94ae
            var shareBtn = UIHelper.CreateButton(transform, "ShareBtn",
                "\u5206\u4eab\u7ed9\u597d\u53cb", new Color(0.07f, 0.73f, 0.31f), Color.white, 26,
                () => Debug.Log("[Room] Share"));
            var shareRT = shareBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(shareRT, new Vector2(0.3f, 0.64f), new Vector2(0.7f, 0.72f),
                Vector2.zero, Vector2.zero);

            // \u73a9\u5bb6\u72b6\u6001
            var playerCard = CreatePlayerSlot("PlayerSlot", "\u6211\uff08\u623f\u4e3b\uff09",
                new Vector2(0.05f, 0.35f), new Vector2(0.48f, 0.60f), true);
            _playerStatusText = playerCard;

            var opponentCard = CreatePlayerSlot("OpponentSlot", "\u7b49\u5f85\u52a0\u5165...",
                new Vector2(0.52f, 0.35f), new Vector2(0.95f, 0.60f), false);
            _opponentStatusText = opponentCard;

            // \u5f00\u59cb\u6309\u94ae
            var startBtn = UIHelper.CreateButton(transform, "StartBtn",
                "\u5f00\u59cb\u5bf9\u6218", UIHelper.PrimaryColor, Color.white, 32,
                () => GameManager.Instance.ChangeState(GameState.PreCombat));
            var startRT = startBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(startRT, new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.25f),
                Vector2.zero, Vector2.zero);

            // \u8fd4\u56de
            var backBtn = UIHelper.CreateButton(transform, "BackBtn",
                "\u8fd4\u56de", UIHelper.CardBg, UIHelper.TextGray, 24,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(backRT, new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.13f),
                Vector2.zero, Vector2.zero);
        }

        private Text CreatePlayerSlot(string name, string label, Vector2 aMin, Vector2 aMax, bool ready)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            UIHelper.SetAnchored(rt, aMin, aMax, Vector2.zero, Vector2.zero);
            go.AddComponent<Image>().color = UIHelper.CardBg;

            var nameText = UIHelper.CreateText(go.transform, "Name",
                label, 24, UIHelper.TextWhite);
            var nameRT = nameText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(nameRT, new Vector2(0, 0.5f), new Vector2(1, 0.85f),
                Vector2.zero, Vector2.zero);

            string status = ready ? "\u2705 \u5df2\u51c6\u5907" : "\u23f3 \u7b49\u5f85\u4e2d";
            var statusText = UIHelper.CreateText(go.transform, "Status",
                status, 20, ready ? Color.green : UIHelper.TextGray);
            var sRT = statusText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(sRT, new Vector2(0, 0.15f), new Vector2(1, 0.45f),
                Vector2.zero, Vector2.zero);

            return statusText;
        }
    }
}
