using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LoseWeight.CannonGame
{
    public class CannonHUD : MonoBehaviour
    {
        private RectTransform _parent;
        private Text _scoreText;
        private Text _livesText;
        private Text _timerText;
        private Text _statusText;
        private Text _comboText;
        private Text _hitText;
        private Text _waveText;
        private Text _stageText;
        private Text _levelText;

        public void Initialize(RectTransform parent)
        {
            _parent = parent;
            _scoreText = FindText("Score");
            _comboText = FindText("Combo");
            _waveText = FindText("Wave");
            _timerText = FindText("Timer");
            _livesText = FindText("Lives");
            _stageText = FindText("Stage");
            _levelText = FindText("Level");
            _statusText = FindText("Status");
            _hitText = FindText("Hit");
        }

        public void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = $"\u5206\u6570 {score}";
        }

        public void UpdateLives(int lives)
        {
            if (_livesText == null) return;
            _livesText.text = new string('\u2665', Mathf.Max(0, lives));
        }

        public void UpdateTimer(float time)
        {
            if (_timerText != null) _timerText.text = $"{Mathf.CeilToInt(Mathf.Max(0, time))}s";
        }

        public void UpdateCombo(int combo)
        {
            if (_comboText == null) return;
            _comboText.text = combo > 1 ? $"\u8fde\u51fb x{combo}" : "\u8fde\u51fb x0";
            if (combo > 1) StartCoroutine(Pulse(_comboText.transform as RectTransform));
        }

        public void UpdateWave(int wave, int stage)
        {
            if (_waveText != null) _waveText.text = $"< \u5f53\u524d\u6ce2\u6b21\uff1a{wave} >";
            if (_stageText != null) _stageText.text = $"< \u5173\u5361\uff1a{stage}-{wave} >";
            if (_levelText != null) _levelText.text = $"< \u5927\u70ae\u7b49\u7ea7\uff1aLV.{Mathf.Clamp(stage + wave - 1, 1, 9)} >";
        }

        public void ShowStatus(string text)
        {
            if (_statusText != null) _statusText.text = text;
        }

        public void ShowHitFeedback(bool hit, int points)
        {
            if (_hitText == null) return;
            _hitText.text = hit ? (points > 0 ? $"+{points}" : "\u547d\u4e2d") : "\u672a\u547d\u4e2d";
            _hitText.color = hit ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
            StopCoroutine(nameof(FadeHit));
            StartCoroutine(nameof(FadeHit));
        }

        private Text FindText(string name)
        {
            if (_parent == null) return null;
            var child = FindChild(_parent, name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private IEnumerator FadeHit()
        {
            Color start = _hitText.color;
            float elapsed = 0f;
            const float duration = 0.55f;
            while (elapsed < duration && _hitText != null)
            {
                elapsed += Time.deltaTime;
                Color c = start;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                _hitText.color = c;
                yield return null;
            }
            if (_hitText != null) _hitText.text = "";
        }

        private IEnumerator Pulse(RectTransform rt)
        {
            if (rt == null) yield break;
            rt.localScale = Vector3.one * 1.12f;
            yield return new WaitForSeconds(0.08f);
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            var direct = root.Find(name);
            if (direct != null) return direct;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
