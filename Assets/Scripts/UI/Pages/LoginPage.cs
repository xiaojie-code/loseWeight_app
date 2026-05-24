using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 2. \u767b\u5f55\u9875 - \u6e38\u5ba2\u3001\u5fae\u4fe1\u3001\u624b\u673a\u53f7\u5165\u53e3
    /// </summary>
    public class LoginPage : MonoBehaviour
    {
        private void OnEnable()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            var bg = UIHelper.CreatePanel(transform, "Background", UIHelper.DarkBg);

            // Logo\u533a\u57df
            var logoText = UIHelper.CreateText(transform, "Logo",
                "\u62f3\u51fb\u5bf9\u6218", 56, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var logoRT = logoText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(logoRT, new Vector2(0, 0.65f), new Vector2(1, 0.80f),
                Vector2.zero, Vector2.zero);

            var slogan = UIHelper.CreateText(transform, "Slogan",
                "\u71c3\u70e7\u5361\u8def\u91cc\uff0c\u51fb\u8d25\u5bf9\u624b", 22, UIHelper.TextGray);
            var sloganRT = slogan.GetComponent<RectTransform>();
            UIHelper.SetAnchored(sloganRT, new Vector2(0, 0.60f), new Vector2(1, 0.65f),
                Vector2.zero, Vector2.zero);

            // \u5fae\u4fe1\u767b\u5f55
            var wxBtn = UIHelper.CreateButton(transform, "WechatLogin",
                "\u5fae\u4fe1\u767b\u5f55", new Color(0.07f, 0.73f, 0.31f), Color.white, 30,
                () => OnLogin("wechat"));
            var wxRT = wxBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(wxRT, new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.50f),
                Vector2.zero, Vector2.zero);

            // \u624b\u673a\u53f7\u767b\u5f55
            var phoneBtn = UIHelper.CreateButton(transform, "PhoneLogin",
                "\u624b\u673a\u53f7\u767b\u5f55", UIHelper.PrimaryColor, Color.white, 30,
                () => OnLogin("phone"));
            var phoneRT = phoneBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(phoneRT, new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.40f),
                Vector2.zero, Vector2.zero);

            // \u6e38\u5ba2\u767b\u5f55
            var guestBtn = UIHelper.CreateButton(transform, "GuestLogin",
                "\u6e38\u5ba2\u4f53\u9a8c", UIHelper.CardBg, UIHelper.TextGray, 26,
                () => OnLogin("guest"));
            var guestRT = guestBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(guestRT, new Vector2(0.25f, 0.23f), new Vector2(0.75f, 0.30f),
                Vector2.zero, Vector2.zero);

            // \u534f\u8bae
            var agreement = UIHelper.CreateText(transform, "Agreement",
                "\u767b\u5f55\u5373\u8868\u793a\u540c\u610f\u300a\u7528\u6237\u534f\u8bae\u300b\u548c\u300a\u9690\u79c1\u653f\u7b56\u300b",
                18, UIHelper.TextGray);
            var agRT = agreement.GetComponent<RectTransform>();
            UIHelper.SetAnchored(agRT, new Vector2(0, 0.05f), new Vector2(1, 0.10f),
                Vector2.zero, Vector2.zero);
        }

        private void OnLogin(string method)
        {
            Debug.Log($"[LoginPage] Login with: {method}");
            // TODO: \u5b9e\u9645\u767b\u5f55\u903b\u8f91
            GameManager.Instance.ChangeState(GameState.CharacterSelect);
        }
    }
}
