using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 13. \u88c5\u626e\u9875 - \u670d\u88c5\u3001\u62f3\u5957\u3001\u7279\u6548\u9884\u89c8\u548c\u89e3\u9501
    /// </summary>
    public class DressingPage : MonoBehaviour
    {
        private string _currentTab = "outfit";

        private static readonly string[] Outfits = { "\u8bad\u7ec3\u670d", "\u804c\u4e1a\u62f3\u51fb\u670d", "\u51a0\u519b\u6218\u888d" };
        private static readonly string[] Gloves = { "\u8bad\u7ec3\u62f3\u5957", "\u70c8\u7130\u62f3\u5957", "\u96f7\u9706\u62f3\u5957" };
        private static readonly bool[] OutfitUnlocked = { true, false, false };
        private static readonly bool[] GloveUnlocked = { true, false, false };

        private Transform _itemsContent;

        private void OnEnable()
        {
            BuildUI();
            ShowItems("outfit");
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

            UIHelper.CreateText(header.transform, "Title",
                "\u88c5\u626e", 34, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold)
                .GetComponent<RectTransform>().SetParent(header.transform, false);

            var backBtn = UIHelper.CreateButton(header.transform, "BackBtn",
                "\u2190", UIHelper.CardBg, Color.white, 30,
                () => UIManager.Instance.OnClickBackToMenu());
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.offsetMin = new Vector2(10, 10); backRT.offsetMax = new Vector2(80, -10);

            // Tab
            var tabPanel = new GameObject("Tabs");
            tabPanel.transform.SetParent(transform, false);
            var tabRT = tabPanel.AddComponent<RectTransform>();
            UIHelper.SetAnchored(tabRT, new Vector2(0, 0.82f), new Vector2(1, 0.90f),
                new Vector2(20, 0), new Vector2(-20, 0));
            var hlg = tabPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            var outfitBtn = UIHelper.CreateButton(tabPanel.transform, "TabOutfit",
                "\u670d\u88c5", UIHelper.PrimaryColor, Color.white, 24,
                () => ShowItems("outfit"));
            outfitBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            var gloveBtn = UIHelper.CreateButton(tabPanel.transform, "TabGlove",
                "\u62f3\u5957", UIHelper.SecondaryColor, Color.white, 24,
                () => ShowItems("glove"));
            gloveBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            // \u7269\u54c1\u5217\u8868
            var scroll = UIHelper.CreateScrollView(transform, "ItemList", Color.clear);
            var scrollRT = scroll.GetComponent<RectTransform>();
            UIHelper.SetAnchored(scrollRT, new Vector2(0, 0.05f), new Vector2(1, 0.82f),
                new Vector2(10, 0), new Vector2(-10, 0));
            _itemsContent = scroll.content;
        }

        private void ShowItems(string tab)
        {
            _currentTab = tab;
            if (_itemsContent == null) return;
            foreach (Transform child in _itemsContent) Destroy(child.gameObject);

            var items = tab == "outfit" ? Outfits : Gloves;
            var unlocked = tab == "outfit" ? OutfitUnlocked : GloveUnlocked;

            for (int i = 0; i < items.Length; i++)
            {
                CreateItemCard(items[i], unlocked[i], i);
            }
        }

        private void CreateItemCard(string itemName, bool isUnlocked, int index)
        {
            var go = new GameObject($"Item_{itemName}");
            go.transform.SetParent(_itemsContent, false);
            var rt = go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            go.AddComponent<Image>().color = UIHelper.CardBg;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // \u56fe\u6807\u5360\u4f4d
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<RectTransform>();
            iconGo.AddComponent<LayoutElement>().preferredWidth = 80;
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = isUnlocked ? UIHelper.PrimaryColor : UIHelper.TextGray;

            // \u540d\u79f0+\u72b6\u6001
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(go.transform, false);
            infoGo.AddComponent<RectTransform>();
            infoGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var vlg = infoGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandHeight = true;

            var nameText = UIHelper.CreateText(infoGo.transform, "Name",
                itemName, 26, UIHelper.TextWhite, TextAnchor.MiddleLeft, FontStyle.Bold);
            nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            string status = isUnlocked ? "\u5df2\u89e3\u9501" : "\u672a\u89e3\u9501 - 500\u91d1\u5e01";
            var statusText = UIHelper.CreateText(infoGo.transform, "Status",
                status, 20, isUnlocked ? Color.green : UIHelper.TextGray, TextAnchor.MiddleLeft);
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;

            // \u6309\u94ae
            var btnGo = new GameObject("Btn");
            btnGo.transform.SetParent(go.transform, false);
            btnGo.AddComponent<RectTransform>();
            btnGo.AddComponent<LayoutElement>().preferredWidth = 120;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = isUnlocked ? UIHelper.PrimaryColor : UIHelper.SecondaryColor;
            var btn = btnGo.AddComponent<Button>();

            string btnLabel = isUnlocked ? "\u88c5\u5907" : "\u89e3\u9501";
            var btnText = UIHelper.CreateText(btnGo.transform, "BtnText",
                btnLabel, 22, Color.white);
            var btRT = btnText.GetComponent<RectTransform>();
            UIHelper.SetAnchored(btRT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            int idx = index;
            btn.onClick.AddListener(() => OnItemAction(idx, isUnlocked));
        }

        private void OnItemAction(int index, bool isUnlocked)
        {
            if (isUnlocked)
                Debug.Log($"[Dressing] Equip item {index} in {_currentTab}");
            else
                Debug.Log($"[Dressing] Unlock item {index} in {_currentTab}");
        }
    }
}
