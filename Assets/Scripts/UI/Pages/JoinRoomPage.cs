using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class JoinRoomPage : MonoBehaviour
    {
        private InputField _roomInput;
        private Text _errorText;

        private void OnEnable()
        {
            _roomInput = GetComponentInChildren<InputField>(true);
            _errorText = FindChild(transform, "Error")?.GetComponent<Text>();
        }

        public void Join()
        {
            if (_roomInput == null || string.IsNullOrEmpty(_roomInput.text))
            {
                if (_errorText != null) _errorText.text = "请输入房间号";
                return;
            }
            GameManager.Instance.ChangeState(GameState.PreCombat);
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
