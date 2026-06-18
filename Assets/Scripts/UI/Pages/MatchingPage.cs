using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class MatchingPage : MonoBehaviour
    {
        private Text _statusText;
        private Text _timerText;
        private float _timer;
        private bool _matching;

        private void OnEnable()
        {
            _statusText = FindText("Status");
            _timerText = FindText("Timer");
            _timer = 0f;
            _matching = true;
        }

        private void OnDisable()
        {
            _matching = false;
        }

        private void Update()
        {
            if (!_matching) return;
            _timer += Time.deltaTime;
            if (_statusText != null) _statusText.text = "正在匹配...";
            if (_timerText != null) _timerText.text = $"{Mathf.FloorToInt(_timer)}s";
            if (_timer >= 3f) GameManager.Instance.ChangeState(GameState.PreCombat);
        }

        public void Cancel()
        {
            UIManager.Instance.OnClickBackToMenu();
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
