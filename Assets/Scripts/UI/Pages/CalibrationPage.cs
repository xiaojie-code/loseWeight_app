using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class CalibrationPage : MonoBehaviour
    {
        private int _step;
        private Text _statusText;
        private Text _tipText;

        private void OnEnable()
        {
            _step = 0;
            _statusText = FindText("Status");
            _tipText = FindText("Tip");
            UpdateText();
            InvokeRepeating(nameof(NextStep), 2f, 2f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(NextStep));
        }

        private void NextStep()
        {
            _step++;
            if (_step >= 3)
            {
                CancelInvoke(nameof(NextStep));
                GameManager.Instance.ChangeState(GameState.PreCombat);
                return;
            }
            UpdateText();
        }

        private void UpdateText()
        {
            if (_statusText != null) _statusText.text = $"校准中 {_step + 1}/3";
            if (_tipText != null) _tipText.text = "请保持身体在摄像头画面内";
        }

        private Text FindText(string name) => FindChild(transform, name)?.GetComponent<Text>();
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
