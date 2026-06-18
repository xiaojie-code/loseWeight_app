using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.PoseDetection;

namespace LoseWeight.Game
{
    public class TouchCombatButtons : MonoBehaviour
    {
        private bool _bound;

        public void Build(Transform parent)
        {
            if (_bound) return;
            _bound = true;

            Bind(parent, "Btn_LeftPunch", () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.LeftStraight, Power = PunchPower.Light, Speed = 0.5f }));
            Bind(parent, "Btn_RightPunch", () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Light, Speed = 0.5f }));
            Bind(parent, "Btn_HeavyPunch", () =>
                EventBus.Publish(new PunchDetectedEvent { Type = PunchType.RightStraight, Power = PunchPower.Heavy, Speed = 0.9f }));
            Bind(parent, "Btn_Defend", () =>
                EventBus.Publish(new DefendEvent { IsActive = true }));
        }

        private static void Bind(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var button = FindChild(parent, name)?.GetComponent<Button>();
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
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
