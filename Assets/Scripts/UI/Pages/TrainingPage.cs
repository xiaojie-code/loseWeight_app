using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class TrainingPage : MonoBehaviour
    {
        private void OnEnable()
        {
            SetText("Title", "训练模式");
            SetText("Subtitle", "选择训练类型");
            SetText("BoxingTrainingBtn/Text", "拳击训练");
            SetText("CannonTrainingBtn/Text", "大炮射击");
            SetText("FruitTrainingBtn/Text", "水果忍者");
            SetText("BackBtn/Text", "返回");
        }

        public void StartTraining(string mode)
        {
            Debug.Log($"[TrainingPage] Start: {mode}");
            GameManager.Instance.ChangeState(GameState.PreCombat);
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
