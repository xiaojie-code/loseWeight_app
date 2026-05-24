using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 12. \u6392\u884c\u699c\u9875 - \u57ce\u5e02\u3001\u7701\u4efd\u3001\u5168\u56fd\u699c
    /// </summary>
    public class RankingPage : MonoBehaviour
    {
        private Transform _listContent;
        private Text _tabCityText;
        private Text _tabProvinceText;
        private Text _tabNationalText;
        private string _currentTab = "city";

        private void OnEnable()
        {
            BuildUI();
            ShowTab("city");
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
                "\u6392\u884c\u699c", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // \u8fd4\u56de\u6309\u94ae
            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0);
            backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10);
            backRT.offsetMax = new Vector2(80, -10);

            // Tab \u6309\u94ae
            var tabPanel = new GameObject("Tabs");
            tabPanel.transform.SetParent(transform, false);
            var tabRT = tabPanel.AddComponent<RectTransform>();
            UIHelper.SetAnchored(tabRT, new Vector2(0, 0.83f), new Vector2(1, 0.90f),
                new Vector2(20, 0), new Vector2(-20, 0));
            var hlg = tabPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _tabCityText = CreateTabButton(tabPanel.transform, "\u57ce\u5e02\u699c", "city");
            _tabProvinceText = CreateTabButton(tabPanel.transform, "\u7701\u4efd\u699c", "province");
            _tabNationalText = CreateTabButton(tabPanel.transform, "\u5168\u56fd\u699c", "national");

            // \u6392\u884c\u5217\u8868
            var scroll = UIHelper.CreateScrollView(transform, "RankList", Color.clear);
            var scrollRT = scroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scrollRT, new Vector2(0, 0.05f), new Vector2(1, 0.83f),
                new Vector2(10, 0), new Vector2(-10, 0));
            _listContent = scroll.content;
        }

        private Text CreateTabButton(Transform parent, string label, string tab)
        {
            var btn = UIHelper.CreateButton(parent, $"Tab_{tab}", label,
                UIHelper.CardBg, UIHelper.TextWhite, 24, () => ShowTab(tab));
            btn.gameObject.AddComponent<LayoutElement>().preferredHeight = 55;
            return btn.GetComponentInChildren<Text>();
        }

        private void ShowTab(string tab)
        {
            _currentTab = tab;
            UpdateTabHighlight();
            LoadRankData(tab);
        }

        private void UpdateTabHighlight()
        {
            if (_tabCityText != null) _tabCityText.color = _currentTab == "city" ? Color.white : UIHelper.TextGray;
            if (_tabProvinceText != null) _tabProvinceText.color = _currentTab == "province" ? Color.white : UIHelper.TextGray;
            if (_tabNationalText != null) _tabNationalText.color = _currentTab == "national" ? Color.white : UIHelper.TextGray;
        }

        private void LoadRankData(string tab)
        {
            if (_listContent == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            string scope = tab == "city" ? "\u57ce\u5e02" : tab == "province" ? "\u7701\u4efd" : "\u5168\u56fd";

            // \u6a21\u62df\u6570\u636e
            for (int i = 1; i <= 20; i++)
            {
                CreateRankItem(i, $"\u62f3\u624b{i:D3}", $"{scope}\u7b2c{i}\u540d", 100 - i * 3);
            }
        }

        private void CreateRankItem(int rank, string nickname, string region, int knockouts)
        {
            var go = new GameObject($"Rank_{rank}");
            go.transform.SetParent(_listContent, false);
            var rt = go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 80;

            var img = go.AddComponent<Image>();
            img.color = rank <= 3 ? new Color(1f, 0.8f, 0.2f, 0.1f) : UIHelper.CardBg;

            // \u6392\u540d
            var rankColor = rank <= 3 ? new Color(1f, 0.8f, 0.2f) : UIHelper.TextGray;
            var rankText = UIHelper.CreateText(go.transform, "Rank",
                $"#{rank}", 26, rankColor, TextAnchor.MiddleCenter, rank <= 3 ? FontStyle.Bold : FontStyle.Normal);
            var rkRT = rankText.GetComponent<RectTransform>();
            rkRT.anchorMin = new Vector2(0, 0); rkRT.anchorMax = new Vector2(0.12f, 1);
            rkRT.offsetMin = Vector2.zero; rkRT.offsetMax = Vector2.zero;

            // \u6635\u79f0
            var nameText = UIHelper.CreateText(go.transform, "Name",
                nickname, 24, UIHelper.TextWhite, TextAnchor.MiddleLeft);
            var nmRT = nameText.GetComponent<RectTransform>();
            nmRT.anchorMin = new Vector2(0.14f, 0.5f); nmRT.anchorMax = new Vector2(0.6f, 1);
            nmRT.offsetMin = Vector2.zero; nmRT.offsetMax = Vector2.zero;

            // \u5730\u533a
            var regionText = UIHelper.CreateText(go.transform, "Region",
                region, 18, UIHelper.TextGray, TextAnchor.MiddleLeft);
            var rgRT = regionText.GetComponent<RectTransform>();
            rgRT.anchorMin = new Vector2(0.14f, 0); rgRT.anchorMax = new Vector2(0.6f, 0.5f);
            rgRT.offsetMin = Vector2.zero; rgRT.offsetMax = Vector2.zero;

            // \u51fb\u8d25\u4eba\u6570
            var koText = UIHelper.CreateText(go.transform, "KO",
                $"\u51fb\u8d25 {knockouts}", 22, UIHelper.SecondaryColor, TextAnchor.MiddleRight);
            var koRT = koText.GetComponent<RectTransform>();
            koRT.anchorMin = new Vector2(0.65f, 0); koRT.anchorMax = new Vector2(0.98f, 1);
            koRT.offsetMin = Vector2.zero; koRT.offsetMax = Vector2.zero;
        }
    }
}
