namespace LoseWeight.Data
{
    /// <summary>
    /// 对战规则配置
    /// </summary>
    public static class CombatConfig
    {
        // 回合设置
        public const int MaxRounds = 3;           // BO3
        public const int WinRoundsNeeded = 2;     // 先赢2回合
        public const float RoundDuration = 60f;   // 每回合60秒
        public const int MaxHp = 100;             // 每回合HP

        // 伤害值
        public const int LightStraightDamage = 5;
        public const int HeavyStraightDamage = 10;
        public const int LightUppercutDamage = 8;
        public const int HeavyUppercutDamage = 15;

        // 防御减伤
        public const float DefendDamageReduction = 0.5f; // 50%减伤

        // 匹配超时
        public const float MatchTimeout = 5f;     // 5秒匹配超时

        // 房间超时
        public const float RoomTimeout = 300f;    // 5分钟无人加入解散

        // 对战准备倒计时
        public const float PreCombatCountdown = 3f;

        // 断线重连宽容窗口
        public const float ReconnectWindow = 5f;
    }
}
