using UnityEngine;
using LoseWeight.Core;

namespace LoseWeight.UI.Pages
{
    public class CreateRoomPage : MonoBehaviour
    {
        public void StartCombat()
        {
            GameManager.Instance.ChangeState(GameState.PreCombat);
        }
    }
}
