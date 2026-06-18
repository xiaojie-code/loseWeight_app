using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.FruitNinja
{
    public class FruitNinjaHUD : MonoBehaviour
    {
        private Text _scoreText;
        private Text _comboText;
        private Text _livesText;
        private Text _timerText;
        private Text _statusText;

        public void Initialize(RectTransform parent)
        {
            _scoreText = FindText(parent, "Score");
            _comboText = FindText(parent, "Combo");
            _livesText = FindText(parent, "Lives");
            _timerText = FindText(parent, "Timer");
            _statusText = FindText(parent, "Status");
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString();
        }

        public void UpdateCombo(int combo)
        {
            if (_comboText != null) _comboText.text = combo > 1 ? $"x{combo}" : "";
        }

        public void UpdateLives(int lives)
        {
            if (_livesText == null) return;
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥";
            _livesText.text = hearts;
        }

        public void UpdateTimer(float time)
        {
            if (_timerText != null) _timerText.text = Mathf.CeilToInt(time).ToString();
        }

        public void ShowStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
        }

        public void ShowTimerMode(bool show)
        {
            if (_timerText != null) _timerText.gameObject.SetActive(show);
        }

        private static Text FindText(Transform root, string name)
        {
            var child = FindChild(root, name);
            return child != null ? child.GetComponent<Text>() : null;
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
