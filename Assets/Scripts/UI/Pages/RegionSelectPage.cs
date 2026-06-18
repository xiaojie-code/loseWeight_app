using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    public class RegionSelectPage : MonoBehaviour
    {
        private string _selectedProvince = "";
        private string _selectedCity = "";
        private Text _selectedText;

        private void OnEnable()
        {
            SetText("Title", "地区选择");
            SetText("Subtitle", "选择所在地区");
            SetText("ProvinceZhejiangBtn/Text", "浙江");
            SetText("ProvinceGuangdongBtn/Text", "广东");
            SetText("CityHangzhouBtn/Text", "杭州");
            SetText("CityShenzhenBtn/Text", "深圳");
            SetText("ConfirmBtn/Text", "确认");
            SetText("BackBtn/Text", "返回");
            _selectedText = FindChild(transform, "Selected")?.GetComponent<Text>();
            UpdateSelectedText();
        }

        public void SelectProvince(string province)
        {
            _selectedProvince = province;
            _selectedCity = "";
            UpdateSelectedText();
        }

        public void SelectCity(string city)
        {
            _selectedCity = city;
            UpdateSelectedText();
        }

        public void Confirm()
        {
            UIManager.Instance.OnClickBackToMenu();
        }

        private void UpdateSelectedText()
        {
            if (_selectedText == null) return;
            if (string.IsNullOrEmpty(_selectedProvince))
                _selectedText.text = "当前选择：未选择";
            else if (string.IsNullOrEmpty(_selectedCity))
                _selectedText.text = $"当前选择：{_selectedProvince}";
            else
                _selectedText.text = $"当前选择：{_selectedProvince} - {_selectedCity}";
        }

        private void SetText(string path, string value)
        {
            var child = transform.Find($"Content/{path}") ?? FindChild(transform, path);
            var text = child != null ? child.GetComponent<Text>() : null;
            if (text != null) text.text = value;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
