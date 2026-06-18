using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class LoadingPage : MonoBehaviour
    {
        private Text _progressText;
        private Image _progressBar;
        private float _progress;
        private bool _loading;

        private void OnEnable()
        {
            _progressText = FindText("Progress");
            _progressBar = FindImage("BarFill");
            _progress = 0f;
            _loading = true;
        }

        private void Update()
        {
            if (!_loading) return;
            _progress += Time.deltaTime * 0.5f;
            if (_progressBar != null) _progressBar.fillAmount = Mathf.Clamp01(_progress);
            if (_progressText != null) _progressText.text = $"加载中... {Mathf.FloorToInt(_progress * 100)}%";
            if (_progress >= 1f)
            {
                _loading = false;
                GameManager.Instance.ChangeState(PlayerPrefs.HasKey("gender_selected") ? GameState.MainMenu : GameState.CharacterSelect);
            }
        }

        private Text FindText(string name) => FindChild(name)?.GetComponent<Text>();
        private Image FindImage(string name) => FindChild(name)?.GetComponent<Image>();

        private Transform FindChild(string name)
        {
            return FindChild(transform, name);
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
