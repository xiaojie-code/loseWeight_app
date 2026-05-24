using System;

namespace LoseWeight.Data
{
    /// <summary>
    /// 玩家持久化数据
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string OpenId;
        public string Nickname;
        public string AvatarUrl;
        public Gender Gender;
        public string Province;
        public string City;
        public int TotalWins;
        public int TotalLosses;
        public int TotalKnockouts;
        public int CurrentStreak;
        public int MaxStreak;
        public int EquippedGlove;   // 0=训练, 1=烈焰, 2=雷霆
        public int EquippedOutfit;  // 0=训练服, 1=职业, 2=冠军
        public long LastRegionChangeTime;

        // 解锁状态
        public bool[] UnlockedGloves = { true, false, false };
        public bool[] UnlockedOutfits = { true, false, false };
    }

    public enum Gender
    {
        Male,
        Female
    }

    /// <summary>
    /// 排行榜条目
    /// </summary>
    [Serializable]
    public class RankEntry
    {
        public int Rank;
        public string Nickname;
        public string AvatarUrl;
        public string City;
        public int Knockouts; // 击败人数
    }

    /// <summary>
    /// 战绩记录
    /// </summary>
    [Serializable]
    public class BattleRecord
    {
        public string OpponentName;
        public string Result;     // "2:1"
        public bool IsWin;
        public long Timestamp;
        public string Mode;       // "online" / "room" / "ai"
    }
}
