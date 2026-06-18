using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.Combat;

namespace LoseWeight.UI.Pages
{
    public class ResultPage : MonoBehaviour
    {
        private void OnEnable()
        {
            var resultText = FindText("Result");
            var scoreText = FindText("Score");
            var statsText = FindText("Stats");
            var wins = CombatManager.Instance != null ? CombatManager.Instance.RoundWins : null;
            bool isWin = wins != null && wins[0] > wins[1];
            if (resultText != null) resultText.text = isWin ? "胜利！" : "失败";
            if (scoreText != null) scoreText.text = wins != null ? $"{wins[0]} : {wins[1]}" : "0 : 0";
            if (statsText != null) statsText.text = isWin ? "恭喜你击败了对手！" : "继续加油，下次一定能赢！";
        }

        public void Retry() => GameManager.Instance.ChangeState(GameState.Combat);
        public void Home() => UIManager.Instance.OnClickBackToMenu();
        public void Share() => Debug.Log("[Result] Share to friend");

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
