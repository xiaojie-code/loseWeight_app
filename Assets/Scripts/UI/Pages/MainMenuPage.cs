using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.UI.Pages
{
    public class MainMenuPage : MonoBehaviour
    {
        private void OnEnable()
        {
            SetText("Title", "燃脂体感训练");
            SetText("Subtitle", "选择一个体感项目");
            SetText("AIBattleBtn/Text", "拳击对战");
            SetText("CannonGameBtn/Text", "大炮射击");
            SetText("FruitNinjaBtn/Text", "水果忍者");
            SetText("OnlineMatchBtn/Text", "在线匹配");
            SetText("TrainingBtn/Text", "训练模式");
            SetText("RankingBtn/Text", "排行榜");
            SetText("DressingBtn/Text", "装扮");
            SetText("ProfileBtn/Text", "个人");
            SetText("SettingsBtn/Text", "设置");
            SetText("RecordBtn/Text", "记录");
            SetText("RegionBtn/Text", "地区");
        }

        private void SetText(string path, string value)
        {
            var child = transform.Find($"Content/{path}") ?? FindChild(transform, path);
            var text = child != null ? child.GetComponent<Text>() : null;
            if (text != null) text.text = value;
        }

        private static Transform FindChild(Transform root, string path)
        {
            if (root == null) return null;
            var direct = root.Find(path);
            if (direct != null) return direct;

            string name = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
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
