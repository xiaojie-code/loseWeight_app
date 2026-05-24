using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.PoseDetection;

namespace LoseWeight.Game
{
    /// <summary>
    /// 触屏战斗按钮 - 真机上姿态检测不可用时的备用操控方式
    /// 在屏幕右下角显示出拳/防御/闪避按钮
    /// </summary>
    public class TouchCombatButtons : MonoBehaviour
    {
        private bool _built;

        public void Build(Transform parent)
        {
            if (_built) return;
            _built = true;

            // 右下角按钮区域
            var panel = new GameObject("TouchButtons");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.55f, 0f);
            rt.anchorMax = new Vector2(1f, 0.3f);
            rt.offsetMin = new Vector2(10, 10);
            rt.offsetMax = new Vector2(-10, -10);

            var grid = panel.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130, 70);
            grid.spacing = new Vector2(8, 8);
            grid.childAlignment = TextAnchor.LowerRight;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            // 出拳按钮
            MakeBtn(panel.transform, "\u5de6\u62f3", new Color(0.2f, 0.5f, 1f, 0.8f), () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftStraight, Power = PunchPower.Light, Speed = 0.5f }));
            MakeBtn(panel.transform, "\u53f3\u62f3", new Color(0.2f, 0.5f, 1f, 0.8f), () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Light, Speed = 0.5f }));
            MakeBtn(panel.transform, "\u91cd\u62f3", new Color(0.8f, 0.3f, 0.1f, 0.8f), () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Heavy, Speed = 0.9f }));
            MakeBtn(panel.transform, "\u5de6\u52fe", new Color(0.6f, 0.2f, 0.8f, 0.8f), () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftUppercut, Power = PunchPower.Heavy, Speed = 0.8f }));
            MakeBtn(panel.transform, "\u53f3\u52fe", new Color(0.6f, 0.2f, 0.8f, 0.8f), () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightUppercut, Power = PunchPower.Heavy, Speed = 0.8f }));
            MakeBtn(panel.transform, "\u9632\u5fa1", new Color(0.2f, 0.7f, 0.3f, 0.8f), () =>
                EventBus.Publish(new DefendEvent { IsActive = true }));
        }

        private void MakeBtn(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("T");
            txtGo.transform.SetParent(go.transform, false);
            var txtRT = txtGo.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }
    }
}
