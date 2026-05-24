using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// \u5730\u533a\u9009\u62e9\u9875 - \u7701\u5e02\u4e8c\u7ea7\u8054\u52a8\u9009\u62e9
    /// </summary>
    public class RegionSelectPage : MonoBehaviour
    {
        private string _selectedProvince = "";
        private string _selectedCity = "";
        private Text _selectedText;
        private Transform _provinceContent;
        private Transform _cityContent;

        // \u7b80\u5316\u7684\u7701\u5e02\u6570\u636e
        private static readonly string[][] RegionData = {
            new[] { "\u5317\u4eac", "\u5317\u4eac\u5e02" },
            new[] { "\u4e0a\u6d77", "\u4e0a\u6d77\u5e02" },
            new[] { "\u5e7f\u4e1c", "\u5e7f\u5dde", "\u6df1\u5733", "\u4e1c\u839e", "\u4f5b\u5c71", "\u73e0\u6d77" },
            new[] { "\u6d59\u6c5f", "\u676d\u5dde", "\u5b81\u6ce2", "\u6e29\u5dde", "\u5609\u5174" },
            new[] { "\u6c5f\u82cf", "\u5357\u4eac", "\u82cf\u5dde", "\u65e0\u9521", "\u5e38\u5dde" },
            new[] { "\u56db\u5ddd", "\u6210\u90fd", "\u7ef5\u9633", "\u5fb7\u9633" },
            new[] { "\u6e56\u5317", "\u6b66\u6c49", "\u5b9c\u660c", "\u8944\u9633" },
            new[] { "\u6e56\u5357", "\u957f\u6c99", "\u682a\u6d32", "\u5cb3\u9633" },
            new[] { "\u798f\u5efa", "\u798f\u5dde", "\u53a6\u95e8", "\u6cc9\u5dde" },
            new[] { "\u5c71\u4e1c", "\u6d4e\u5357", "\u9752\u5c9b", "\u70df\u53f0" },
            new[] { "\u6cb3\u5357", "\u90d1\u5dde", "\u6d1b\u9633", "\u5f00\u5c01" },
            new[] { "\u91cd\u5e86", "\u91cd\u5e86\u5e02" },
            new[] { "\u5929\u6d25", "\u5929\u6d25\u5e02" },
        };

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

            var title = UIHelper.CreateText(header.transform, "Title",
                "\u9009\u62e9\u5730\u533a", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // \u5df2\u9009\u663e\u793a
            _selectedText = UIHelper.CreateText(transform, "Selected",
                "\u5f53\u524d\u9009\u62e9\uff1a\u672a\u9009\u62e9", 24, UIHelper.TextGray);
            var selRT = _selectedText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(selRT, new Vector2(0, 0.84f), new Vector2(1, 0.90f),
                new Vector2(20, 0), Vector2.zero);

            // \u7701\u4efd\u5217\u8868\uff08\u5de6\u4fa7\uff09
            var provinceScroll = UIHelper.CreateScrollView(transform, "ProvinceList", UIHelper.CardBg);
            var psRT = provinceScroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(psRT, new Vector2(0, 0.12f), new Vector2(0.4f, 0.84f),
                new Vector2(10, 0), new Vector2(-5, 0));
            _provinceContent = provinceScroll.content;

            // \u57ce\u5e02\u5217\u8868\uff08\u53f3\u4fa7\uff09
            var cityScroll = UIHelper.CreateScrollView(transform, "CityList", UIHelper.CardBg);
            var csRT = cityScroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(csRT, new Vector2(0.4f, 0.12f), new Vector2(1, 0.84f),
                new Vector2(5, 0), new Vector2(-10, 0));
            _cityContent = cityScroll.content;

            // \u786e\u8ba4\u6309\u94ae
            var confirmBtn = UIHelper.CreateButton(transform, "ConfirmBtn",
                "\u786e\u8ba4", UIHelper.PrimaryColor, Color.white, 28, () => OnConfirm());
            var cbRT = confirmBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(cbRT, new Vector2(0.3f, 0.03f), new Vector2(0.7f, 0.10f),
                Vector2.zero, Vector2.zero);

            PopulateProvinces();
        }

        private void PopulateProvinces()
        {
            foreach (var region in RegionData)
            {
                string province = region[0];
                var btn = UIHelper.CreateButton(_provinceContent, $"P_{province}", province,
                    UIHelper.CardBg, UIHelper.TextWhite, 24, null);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 60;
                string p = province;
                btn.onClick.AddListener(() => OnProvinceSelected(p));
            }
        }

        private void OnProvinceSelected(string province)
        {
            _selectedProvince = province;
            _selectedCity = "";

            // \u6e05\u7a7a\u57ce\u5e02\u5217\u8868
            foreach (Transform child in _cityContent) Destroy(child.gameObject);

            // \u586b\u5145\u57ce\u5e02
            foreach (var region in RegionData)
            {
                if (region[0] != province) continue;
                for (int i = 1; i < region.Length; i++)
                {
                    string city = region[i];
                    var btn = UIHelper.CreateButton(_cityContent, $"C_{city}", city,
                        UIHelper.CardBg, UIHelper.TextWhite, 24, null);
                    var le = btn.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 60;
                    string c = city;
                    btn.onClick.AddListener(() => OnCitySelected(c));
                }
                break;
            }

            UpdateSelectedText();
        }

        private void OnCitySelected(string city)
        {
            _selectedCity = city;
            UpdateSelectedText();
        }

        private void UpdateSelectedText()
        {
            if (_selectedText == null) return;
            if (string.IsNullOrEmpty(_selectedProvince))
                _selectedText.text = "\u5f53\u524d\u9009\u62e9\uff1a\u672a\u9009\u62e9";
            else if (string.IsNullOrEmpty(_selectedCity))
                _selectedText.text = $"\u5f53\u524d\u9009\u62e9\uff1a{_selectedProvince}";
            else
                _selectedText.text = $"\u5f53\u524d\u9009\u62e9\uff1a{_selectedProvince} - {_selectedCity}";
        }

        private void OnConfirm()
        {
            Debug.Log($"[RegionSelect] {_selectedProvince} / {_selectedCity}");
            UIManager.Instance.OnClickBackToMenu();
        }
    }
}
