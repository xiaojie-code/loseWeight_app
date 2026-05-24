using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.Data;

namespace LoseWeight.UI.Pages
{
    /// <summary>
    /// 5. \u89d2\u8272\u9009\u62e9\u9875 - \u7537/\u5973\u89d2\u8272\u9009\u62e9 + \u6027\u522b\u9009\u62e9
    /// </summary>
    public class CharacterSelectPage : MonoBehaviour
    {
        private Gender _selectedGender = Gender.Male;
        private Image _maleHighlight;
        private Image _femaleHighlight;

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
                "\u9009\u62e9\u89d2\u8272", 40, UIHelper.TextWhite, TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRT = title.GetComponent<RectTransform>();
            UIHelper.SetAnchored(titleRT, new Vector2(0, 0.88f), new Vector2(1, 0.95f),
                Vector2.zero, Vector2.zero);

            // \u7537\u6027\u89d2\u8272\u5361\u7247
            var maleCard = CreateCharacterCard("MaleCard", "\u7537\u6027\u62f3\u624b",
                new Vector2(0.05f, 0.35f), new Vector2(0.48f, 0.85f),
                new Color(0.2f, 0.4f, 0.8f, 0.3f));
            _maleHighlight = maleCard.GetComponent<Image>();
            maleCard.GetComponent<Button>().onClick.AddListener(() => SelectGender(Gender.Male));

            // \u5973\u6027\u89d2\u8272\u5361\u7247
            var femaleCard = CreateCharacterCard("FemaleCard", "\u5973\u6027\u62f3\u624b",
                new Vector2(0.52f, 0.35f), new Vector2(0.95f, 0.85f),
                new Color(0.8f, 0.2f, 0.5f, 0.3f));
            _femaleHighlight = femaleCard.GetComponent<Image>();
            femaleCard.GetComponent<Button>().onClick.AddListener(() => SelectGender(Gender.Female));

            // \u786e\u8ba4\u6309\u94ae
            var confirmBtn = UIHelper.CreateButton(transform, "ConfirmBtn",
                "\u786e\u8ba4\u9009\u62e9", UIHelper.PrimaryColor, Color.white, 32,
                () => OnConfirm());
            var confirmRT = confirmBtn.GetComponent<RectTransform>();
            UIHelper.SetAnchored(confirmRT, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.22f),
                Vector2.zero, Vector2.zero);

            // \u63d0\u793a
            var hint = UIHelper.CreateText(transform, "Hint",
                "\u9009\u62e9\u540e\u53ef\u5728\u8bbe\u7f6e\u4e2d\u4fee\u6539", 20, UIHelper.TextGray);
            var hintRT = hint.GetComponent<RectTransform>();
            UIHelper.SetAnchored(hintRT, new Vector2(0, 0.05f), new Vector2(1, 0.10f),
                Vector2.zero, Vector2.zero);

            SelectGender(Gender.Male);
        }

        private GameObject CreateCharacterCard(string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            UIHelper.SetAnchored(rt, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<Button>();

            // \u89d2\u8272\u540d
            var txt = UIHelper.CreateText(go.transform, "Label", label, 28, UIHelper.TextWhite);
            var txtRT = txt.GetComponent<RectTransform>();
            UIHelper.SetAnchored(txtRT, new Vector2(0, 0), new Vector2(1, 0.15f),
                Vector2.zero, Vector2.zero);

            // \u89d2\u8272\u7f29\u7565\u56fe\u5360\u4f4d
            var preview = UIHelper.CreateText(go.transform, "Preview", "\ud83e\udd4a", 80, Color.white);
            var previewRT = preview.GetComponent<RectTransform>();
            UIHelper.SetAnchored(previewRT, new Vector2(0, 0.3f), new Vector2(1, 0.9f),
                Vector2.zero, Vector2.zero);

            return go;
        }

        private void SelectGender(Gender gender)
        {
            _selectedGender = gender;
            if (_maleHighlight != null)
                _maleHighlight.color = gender == Gender.Male
                    ? new Color(0.2f, 0.4f, 0.8f, 0.6f) : new Color(0.2f, 0.4f, 0.8f, 0.2f);
            if (_femaleHighlight != null)
                _femaleHighlight.color = gender == Gender.Female
                    ? new Color(0.8f, 0.2f, 0.5f, 0.6f) : new Color(0.8f, 0.2f, 0.5f, 0.2f);
        }

        private void OnConfirm()
        {
            Debug.Log($"[CharacterSelect] Gender: {_selectedGender}");
            PlayerPrefs.SetInt("gender_selected", (int)_selectedGender);
            PlayerPrefs.Save();
            // \u9996\u6b21\u8fdb\u5165\u8fd8\u9700\u8981\u9009\u62e9\u5730\u533a
            GameManager.Instance.ChangeState(GameState.RegionSelect);
        }
    }
}
