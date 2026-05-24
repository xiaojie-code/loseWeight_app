using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 10. \u52a0\u5165\u623f\u95f4\u9875 - \u8f93\u5165\u623f\u95f4\u53f7/\u53e3\u4ee4\u52a0\u5165
    /// </summary>
    public class JoinRoomPage : MonoBehaviour
    {
        private InputField _roomInput;
        private Text _errorText;

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
                "\u52a0\u5165\u623f\u95f4", 36, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, new Vector2(0, 0.80f), new Vector2(1, 0.90f),
                Vector2.zero, Vector2.zero);

            // \u8f93\u5165\u6846\u80cc\u666f
            var inputBg = new GameObject("InputBg");
            inputBg.transform.SetParent(transform, false);
            var ibRT = inputBg.AddComponent<RectTransform>();
            UIHelper.SetAnchored(ibRT, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.68f),
                Vector2.zero, Vector2.zero);
            inputBg.AddComponent<Image>().color = UIHelper.CardBg;

            // InputField
            _roomInput = inputBg.AddComponent<InputField>();
            _roomInput.contentType = InputField.ContentType.IntegerNumber;

            var placeholder = UIHelper.CreateText(inputBg.transform, "Placeholder",
                "\u8bf7\u8f93\u5165\u623f\u95f4\u53f7", 28, UIHelper.TextGray, TextAnchor.MiddleCenter);
            var phRT = placeholder.GetComponent<RectTransform>();
            UIHelper.SetAnchored(phRT, Vector2.zero, Vector2.one,
                new Vector2(20, 0), new Vector2(-20, 0));
            _roomInput.placeholder = placeholder;

            var inputText = UIHelper.CreateText(inputBg.transform, "Text",
                "", 28, UIHelper.TextWhite, TextAnchor.MiddleCenter);
            var itRT = inputText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(itRT, Vector2.zero, Vector2.one,
                new Vector2(20, 0), new Vector2(-20, 0));
            _roomInput.textComponent = inputText;

            // \u9519\u8bef\u63d0\u793a
            _errorText = UIHelper.CreateText(transform, "Error",
                "", 20, new Color(1f, 0.3f, 0.3f));
            var errRT = _errorText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(errRT, new Vector2(0, 0.52f), new Vector2(1, 0.58f),
                Vector2.zero, Vector2.zero);

            // \u52a0\u5165\u6309\u94ae
            var joinBtn = UIHelper.CreateButton(transform, "JoinBtn",
                "\u52a0\u5165\u623f\u95f4", UIHelper.PrimaryColor, Color.white, 30,
                () => OnJoin());
            var joinRT = joinBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(joinRT, new Vector2(0.2f, 0.40f), new Vector2(0.8f, 0.50f),
                Vector2.zero, Vector2.zero);

            // \u8fd4\u56de
            var backBtn = UIHelper.CreateButton(transform, "BackBtn",
                "\u8fd4\u56de", UIHelper.CardBg, UIHelper.TextGray, 24,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(backRT, new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.23f),
                Vector2.zero, Vector2.zero);
        }

        private void OnJoin()
        {
            if (_roomInput == null || string.IsNullOrEmpty(_roomInput.text))
            {
                if (_errorText != null) _errorText.text = "\u8bf7\u8f93\u5165\u623f\u95f4\u53f7";
                return;
            }
            Debug.Log($"[JoinRoom] Joining room: {_roomInput.text}");
            // TODO: \u8fde\u63a5\u670d\u52a1\u5668\u52a0\u5165\u623f\u95f4
            GameManager.Instance.ChangeState(GameState.PreCombat);
        }
    }
}
