using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 1. \u542f\u52a8\u9875 - \u8d44\u6e90\u68c0\u67e5\u3001\u7248\u672c\u68c0\u67e5\u3001\u521d\u59cb\u5316 SDK
    /// </summary>
    public class LoadingPage : MonoBehaviour
    {
        private Text _progressText;
        private Image _progressBar;
        private float _progress;
        private bool _loading;

        private void OnEnable()
        {
            BuildUI();
            StartLoading();
        }

        private void Update()
        {
            if (!_loading) return;
            _progress += Time.deltaTime * 0.5f;
            if (_progressBar != null)
                _progressBar.fillAmount = Mathf.Clamp01(_progress);
            if (_progressText != null)
                _progressText.text = $"\u52a0\u8f7d\u4e2d... {Mathf.FloorToInt(_progress * 100)}%";

            if (_progress >= 1f)
            {
                _loading = false;
                OnLoadComplete();
            }
        }

        private void BuildUI()
        {
            if (transform.childCount > 0) return;

            var bg = UIHelper.CreatePanel(transform, "Background", UIHelper.DarkBg);

            // Logo
            var logo = UIHelper.CreateText(transform, "Logo",
                "\u62f3\u51fb\u5bf9\u6218", 60, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var logoRT = logo.GetComponent<RectTransform>();
            UIHelper.SetAnchored(logoRT, new Vector2(0, 0.55f), new Vector2(1, 0.75f),
                Vector2.zero, Vector2.zero);

            var sub = UIHelper.CreateText(transform, "Sub",
                "\u71c3\u70e7\u5361\u8def\u91cc\uff0c\u51fb\u8d25\u5bf9\u624b", 22, UIHelper.TextGray);
            var subRT = sub.GetComponent<RectTransform>();
            UIHelper.SetAnchored(subRT, new Vector2(0, 0.48f), new Vector2(1, 0.55f),
                Vector2.zero, Vector2.zero);

            // \u8fdb\u5ea6\u6761\u80cc\u666f
            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(transform, false);
            var barBgRT = barBg.AddComponent<RectTransform>();
            UIHelper.SetAnchored(barBgRT, new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.31f),
                Vector2.zero, Vector2.zero);
            barBg.AddComponent<Image>().color = UIHelper.CardBg;

            // \u8fdb\u5ea6\u6761\u586b\u5145
            var barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRT = barFill.AddComponent<RectTransform>();
            UIHelper.SetAnchored(barFillRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _progressBar = barFill.AddComponent<Image>();
            _progressBar.color = UIHelper.PrimaryColor;
            _progressBar.type = Image.Type.Filled;
            _progressBar.fillMethod = Image.FillMethod.Horizontal;
            _progressBar.fillAmount = 0;

            // \u8fdb\u5ea6\u6587\u5b57
            _progressText = UIHelper.CreateText(transform, "Progress",
                "\u52a0\u8f7d\u4e2d... 0%", 20, UIHelper.TextGray);
            var ptRT = _progressText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(ptRT, new Vector2(0, 0.22f), new Vector2(1, 0.28f),
                Vector2.zero, Vector2.zero);
        }

        private void StartLoading()
        {
            _progress = 0;
            _loading = true;
        }

        private void OnLoadComplete()
        {
            // \u68c0\u67e5\u662f\u5426\u9996\u6b21\u8fdb\u5165\uff08\u672a\u9009\u62e9\u6027\u522b/\u5730\u533a\uff09
            bool isFirstTime = !PlayerPrefs.HasKey("gender_selected");
            if (isFirstTime)
            {
                GameManager.Instance.ChangeState(GameState.CharacterSelect);
            }
            else
            {
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }
    }
}
