using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 6. \u59ff\u6001\u6821\u51c6\u9875 - \u6444\u50cf\u5934\u9884\u89c8\u3001\u7ad9\u4f4d\u5f15\u5bfc\u3001\u5149\u7ebf\u63d0\u793a
    /// </summary>
    public class CalibrationPage : MonoBehaviour
    {
        private Text _statusText;
        private Text _tipText;
        private int _calibrationStep;

        private static readonly string[] Tips = {
            "\u8bf7\u7ad9\u5728\u6444\u50cf\u5934\u524d 1.5 \u7c73\u5904",
            "\u786e\u4fdd\u4e0a\u534a\u8eab\u5728\u753b\u9762\u5185",
            "\u8bf7\u4fdd\u6301\u81ea\u7136\u7ad9\u7acb\u59ff\u52bf",
            "\u6821\u51c6\u5b8c\u6210\uff01\u5373\u5c06\u5f00\u59cb..."
        };

        private void OnEnable()
        {
            BuildUI();
            _calibrationStep = 0;
            InvokeRepeating(nameof(NextStep), 2f, 2f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(NextStep));
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            // \u534a\u900f\u660e\u80cc\u666f\uff08\u6444\u50cf\u5934\u5728\u4e0b\u9762\uff09
            var bg = UIHelper.CreatePanel(transform, "Background", new Color(0, 0, 0, 0.6f));

            // \u4eba\u7269\u8f6e\u5ed3\u63d0\u793a\u6846
            var outline = new GameObject("Outline");
            outline.transform.SetParent(transform, false);
            var olRT = outline.AddComponent<RectTransform>();
            UIHelper.SetAnchored(olRT, new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.85f),
                Vector2.zero, Vector2.zero);
            var olImg = outline.AddComponent<Image>();
            olImg.color = Color.clear;
            var olOutline = outline.AddComponent<Outline>();
            olOutline.effectColor = new Color(0.2f, 1f, 0.5f, 0.8f);
            olOutline.effectDistance = new Vector2(3, 3);

            // \u72b6\u6001\u6587\u5b57
            _statusText = UIHelper.CreateText(transform, "Status",
                "\u6821\u51c6\u4e2d...", 32, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var sRT = _statusText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(sRT, new Vector2(0, 0.88f), new Vector2(1, 0.95f),
                Vector2.zero, Vector2.zero);

            // \u63d0\u793a\u6587\u5b57
            _tipText = UIHelper.CreateText(transform, "Tip",
                Tips[0], 24, new Color(0.2f, 1f, 0.5f), TextAnchor.MiddleCenter);
            var tRT = _tipText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(tRT, new Vector2(0, 0.10f), new Vector2(1, 0.20f),
                Vector2.zero, Vector2.zero);

            // \u8df3\u8fc7\u6309\u94ae
            var skipBtn = UIHelper.CreateButton(transform, "SkipBtn",
                "\u8df3\u8fc7", UIHelper.CardBg, UIHelper.TextGray, 22,
                () => OnComplete());
            var skipRT = skipBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(skipRT, new Vector2(0.7f, 0.02f), new Vector2(0.95f, 0.08f),
                Vector2.zero, Vector2.zero);
        }

        private void NextStep()
        {
            _calibrationStep++;
            if (_calibrationStep >= Tips.Length)
            {
                CancelInvoke(nameof(NextStep));
                OnComplete();
                return;
            }
            if (_tipText != null) _tipText.text = Tips[_calibrationStep];
            if (_statusText != null && _calibrationStep == Tips.Length - 1)
                _statusText.text = "\u6821\u51c6\u5b8c\u6210\uff01";
        }

        private void OnComplete()
        {
            CancelInvoke(nameof(NextStep));
            GameManager.Instance.ChangeState(GameState.Combat);
        }
    }
}
