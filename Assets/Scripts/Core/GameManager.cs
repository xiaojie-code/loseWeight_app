using UnityEngine;

namespace LoseWeight.Core
{
    /// <summary>
    /// 游戏全局管理器 - 单例模式，管理游戏生命周期
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            var oldState = CurrentState;
            CurrentState = newState;
            EventBus.Publish(new GameStateChangedEvent(oldState, newState));
            Debug.Log($"[GameManager] State: {oldState} -> {newState}");
        }
    }

    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        CharacterSelect,
        RegionSelect,
        Matching,
        RoomLobby,
        PreCombat,    // 对战准备（倒计时）
        Combat,       // 对战中
        RoundEnd,     // 回合结算
        MatchEnd,     // 比赛结算
        Dressing,     // 装扮
        Profile       // 个人中心
    }
}
