using System;
using System.Collections.Generic;

namespace LoseWeight.Core
{
    /// <summary>
    /// 轻量级事件总线 - 解耦模块间通信
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            _handlers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.ContainsKey(type))
                _handlers[type].Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type)) return;
            foreach (var handler in _handlers[type].ToArray())
            {
                ((Action<T>)handler)?.Invoke(evt);
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    // ========== 事件定义 ==========

    public struct GameStateChangedEvent
    {
        public GameState OldState;
        public GameState NewState;
        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    public struct PunchDetectedEvent
    {
        public PunchType Type;
        public PunchPower Power;
        public float Speed;
    }

    public struct DefendEvent
    {
        public bool IsActive;
    }

    public struct DodgeEvent
    {
        public DodgeDirection Direction;
    }

    public struct DamageEvent
    {
        public string TargetId;
        public int Damage;
        public int RemainingHp;
    }

    public struct AIActionEvent
    {
        public PunchType Type;
        public PunchPower Power;
    }

    public struct RoundEndEvent
    {
        public string WinnerId;
        public int[] Score; // [player1Score, player2Score]
    }

    public struct MatchEndEvent
    {
        public string WinnerId;
        public string Result; // e.g. "2:1"
    }

    // ========== 枚举 ==========

    public enum PunchType
    {
        LeftStraight,   // 左直拳
        RightStraight,  // 右直拳
        LeftUppercut,   // 左上勾拳
        RightUppercut   // 右上勾拳
    }

    public enum PunchPower
    {
        Light,  // 轻拳
        Heavy   // 重拳
    }

    public enum DodgeDirection
    {
        Left,
        Right,
        Down    // 下蹲闪避
    }
}
