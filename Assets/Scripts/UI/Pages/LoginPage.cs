using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class LoginPage : MonoBehaviour
    {
        public void OnLogin(string method)
        {
            Debug.Log($"[LoginPage] Login with: {method}");
            GameManager.Instance.ChangeState(GameState.CharacterSelect);
        }
    }
}
