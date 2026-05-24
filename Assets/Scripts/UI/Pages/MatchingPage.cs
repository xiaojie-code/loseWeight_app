using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 8. \u5339\u914d\u7b49\u5f85\u9875 - \u5728\u7ebf\u5339\u914d\u3001\u53d6\u6d88\u3001\u9884\u8ba1\u7b49\u5f85
    /// </summary>
    public class MatchingPage : MonoBehaviour
    {
        private Text _statusText;
        private Text _timerText;
        private float _waitTime;
        private bool _isMatching;

        private void OnEnable()
        {
            BuildUI();
            StartMatching();
        }

        private void OnDisable()
        {
            _isMatching = false;
        }

        private void Update()
        {
            if (_isMatching)
            {
                _waitTime += Time.deltaTime;
                if (_timerText != null)
                    _timerText.text = $"{Mathf.FloorToInt(_waitTime)}s";

                // \u6a21\u62df 3\u79d2\u540e\u5339\u914d\u6210\u529f
                if (_waitTime > 3f)
                {
                    _isMatching = false;
                    OnMatchFound();
                }
            }
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            var bg = UIHelper.CreatePanel(transform, "Background", UIHelper.DarkBg);

            // \u5339\u914d\u52a8\u753b\u5360\u4f4d
            var animText = UIHelper.CreateText(transform, "Anim",
                "\u2694\ufe0f", 100, Color.white);
            var animRT = animText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(animRT, new Vector2(0.3f, 0.55f), new Vector2(0.7f, 0.75f),
                Vector2.zero, Vector2.zero);

            // \u72b6\u6001\u6587\u5b57
            _statusText = UIHelper.CreateText(transform, "Status",
                "\u6b63\u5728\u5339\u914d\u5bf9\u624b...", 30, UIHelper.TextWhite);
            var statusRT = _statusText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(statusRT, new Vector2(0, 0.45f), new Vector2(1, 0.55f),
                Vector2.zero, Vector2.zero);

            // \u8ba1\u65f6
            _timerText = UIHelper.CreateText(transform, "Timer",
                "0s", 24, UIHelper.TextGray);
            var timerRT = _timerText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(timerRT, new Vector2(0, 0.38f), new Vector2(1, 0.45f),
                Vector2.zero, Vector2.zero);

            // \u53d6\u6d88\u6309\u94ae
            var cancelBtn = UIHelper.CreateButton(transform, "CancelBtn",
                "\u53d6\u6d88\u5339\u914d", UIHelper.CardBg, UIHelper.TextGray, 28,
                () => OnCancel());
            var cancelRT = cancelBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(cancelRT, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.25f),
                Vector2.zero, Vector2.zero);
        }

        private void StartMatching()
        {
            _waitTime = 0;
            _isMatching = true;
            if (_statusText != null) _statusText.text = "\u6b63\u5728\u5339\u914d\u5bf9\u624b...";
        }

        private void OnMatchFound()
        {
            if (_statusText != null) _statusText.text = "\u5339\u914d\u6210\u529f\uff01";
            Invoke(nameof(EnterCombat), 1f);
        }

        private void EnterCombat()
        {
            GameManager.Instance.ChangeState(GameState.PreCombat);
        }

        private void OnCancel()
        {
            _isMatching = false;
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }
    }
}
