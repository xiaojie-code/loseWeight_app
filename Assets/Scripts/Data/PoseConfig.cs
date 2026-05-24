namespace LoseWeight.Data
{
    /// <summary>
    /// 姿态检测配置 - 从原型 game.js CONFIG 迁移
    /// </summary>
    public static class PoseConfig
    {
        // 命中判定半径（归一化坐标）
        public const float HitRadius = 0.08f;

        // 出拳最低速度阈值（归一化坐标/帧）
        public const float PunchSpeedMin = 0.035f;

        // 出拳速度的身体尺度归一化阈值，减少站远/站近对轻重判断的影响。
        public const float PunchSpeedRatioMin = 0.13f;

        // 手腕相对肩膀伸展的最低增量，避免收拳/身体晃动被误判为出拳。
        public const float PunchExtensionMin = 0.014f;
        public const float PunchExtensionRatioMin = 0.06f;

        // 真机高帧率下允许快速手腕爆发触发轻拳，但仍要求不是收拳或身体侧移动作。
        public const float PunchBurstSpeedMin = 0.055f;
        public const float PunchBurstSpeedRatioMin = 0.25f;
        public const float PunchBurstPoseExtensionMin = 0.028f;
        public const float PunchBurstPoseExtensionRatioMin = 0.12f;

        // MoveNet 低帧率下手肘偶尔丢点，允许仅凭手腕-肩膀姿态识别，但手腕不能明显低于肩膀。
        public const float PunchWristAboveElbowSlack = 0.065f;
        public const float PunchNoElbowWristMaxBelowShoulder = 0.16f;

        // 身体正在侧闪/下蹲时，出拳必须有更明确的手臂伸展，避免躯干动作噪声触发拳法。
        public const float PunchDuringBodyMotionExtensionMin = 0.026f;
        public const float PunchDuringBodyMotionExtensionRatioMin = 0.12f;

        // 云端低帧率下可能采不到出拳速度峰值，手臂相对校准姿态明显伸展时也触发一次轻拳。
        public const float PunchPoseExtensionMin = 0.075f;
        public const float PunchPoseExtensionRatioMin = 0.32f;

        // 手臂回收到该阈值内后，允许同一只手再次用伸展姿势触发出拳。
        public const float PunchPoseRearmExtension = 0.035f;

        // 左右手候选分数过近时认为动作不明确，避免出错拳。
        public const float PunchAmbiguousScoreGap = 0.16f;

        // 两次可用于出拳判定的姿态帧之间最大间隔，超过后只更新姿态不触发动作。
        public const float PunchMaxFrameInterval = 1.25f;

        // 单手出拳后给另一只手一个短锁定，避免同一帧左右手噪声抢判。
        public const float PunchOppositeHandLockout = 0.32f;

        // 重拳速度阈值
        public const float HeavyPunchSpeed = 0.08f;
        public const float HeavyPunchSpeedRatio = 0.44f;

        // 重拳需要配合明显伸展，单点速度尖峰不直接判重拳。
        public const float HeavyPunchExtensionMin = 0.038f;
        public const float HeavyPunchExtensionRatioMin = 0.17f;

        // 重拳综合分阈值，兼顾速度、伸展和勾拳上抬幅度。
        public const float HeavyPunchScoreMin = 0.16f;
        public const float HeavyPunchScoreRatioMin = 0.78f;

        // 手臂相对校准姿态伸展到该幅度时，即使低帧率漏掉速度峰值，也按重拳处理。
        public const float HeavyPunchPoseExtensionMin = 0.16f;
        public const float HeavyPunchPoseExtensionRatioMin = 0.62f;
        public const float HeavyUppercutLiftRatio = 0.34f;
        public const float HeavyUppercutPoseRatio = 0.42f;

        // 对 VKSession / 服务端关键点做短窗口滤波，躯干更稳，手腕保留更多瞬时动作。
        public const float PoseBodySmoothingAlpha = 0.55f;
        public const float PoseHandSmoothingAlpha = 0.72f;
        public const float PoseConfidenceSmoothingAlpha = 0.65f;

        // 玩家回到中立姿态时缓慢更新校准基线，减少站位漂移导致的漏判。
        public const float NeutralBaselineLerp = 0.025f;
        public const float NeutralBaselineMaxOffsetRatio = 0.12f;

        // 身体尺度参考值，默认取肩宽，缺失时使用 fallback。
        public const float BodyScaleFallback = 0.22f;
        public const float BodyScaleMin = 0.12f;
        public const float BodyScaleMax = 0.42f;

        // 出拳冷却时间（秒）
        public const float PunchCooldown = 0.35f;

        // 关键点最低置信度。百度人体关键点官方推荐 0.2 作为有效点过滤基准。
        public const float MinConfidence = 0.4f;

        // 防御持续时间（秒）
        public const float DefendHoldTime = 0.3f;

        // 闪避持续时间（秒）
        public const float DodgeHoldTime = 0.04f;

        // 闪避偏移阈值（归一化）
        public const float DodgeThreshold = 0.028f;

        // 闪避后需要身体回到中心范围内，才允许再次触发同类闪避。
        public const float DodgeRearmThreshold = 0.03f;

        // 左右闪避要求头部和肩部同方向移动，避免单个关键点抖动触发。
        public const float DodgeHeadThreshold = 0.018f;

        // 单侧头部或肩部移动非常明显时，只要求另一个点有轻微同向辅助，提升真机识别率。
        public const float DodgeStrongThreshold = 0.034f;
        public const float DodgeSupportThreshold = 0.004f;

        // 躯干整体侧移阈值，用于补足百度头/肩关键点抖动时的闪避判定。
        public const float DodgeTorsoThreshold = 0.018f;

        // 闪避/下蹲的身体尺度归一化与速度阈值，用于低帧率云端姿态补偿。
        public const float DodgeRatioThreshold = 0.1f;
        public const float DodgeStrongRatioThreshold = 0.14f;
        public const float DodgeVelocityRatioThreshold = 0.08f;
        public const float DodgeRearmRatioThreshold = 0.16f;
        public const float DuckHeadRatioThreshold = 0.09f;
        public const float DuckTorsoRatioThreshold = 0.065f;
        public const float DuckVelocityRatioThreshold = 0.08f;

        // 下蹲要求垂直位移明显主导；侧向位移过大时优先按左右闪避处理。
        public const float DuckVerticalDominance = 0.65f;
        public const float DuckLateralGuardThreshold = 0.09f;
        public const float DuckLateralGuardRatio = 0.36f;

        // 触发闪避前要求连续姿态帧都满足同一方向。
        public const int DodgeRequiredStableFrames = 1;

        // 闪避冷却时间（秒）
        public const float DodgeCooldown = 0.3f;

        // 闪避触发后在战斗结算中的有效免伤时间（秒），独立于识别保持时间。
        public const float DodgeActiveDuration = 0.35f;

        // 下蹲闪避 Y 轴下降阈值（归一化）
        public const float DuckThreshold = 0.026f;

        // 下蹲时肩部允许轻微抖动，只要没有明显上移即可。
        public const float DuckShoulderDropMin = -0.03f;

        // 头部下降足够明显时允许肩部轻微抖动，避免百度肩点不稳定导致下蹲完全不触发。
        public const float DuckStrongHeadDrop = 0.032f;

        // 躯干整体下移阈值，用于补足只弯腿时头/肩下降不一致的问题。
        public const float DuckTorsoDropMin = 0.012f;

        // 上勾拳 Y 轴上升阈值（归一化）
        public const float UppercutYThreshold = 0.036f;
        public const float UppercutYRatioThreshold = 0.17f;

        // 勾拳必须从较低手位向上抬，避免直拳/闪避抖动被误归类。
        public const float UppercutStartBelowShoulderMin = 0.01f;
        public const float UppercutEndNearShoulderMax = 0.16f;

        // 勾拳要求竖直上抬明显强于水平移动，避免把直拳或收拳误识别为勾拳。
        public const float UppercutVerticalDominance = 1.35f;

        // 低帧率可能只采到勾拳最高点，用当前手腕相对肩膀的位置补充判断。
        public const float UppercutPoseAboveShoulderMin = 0.035f;
        public const float UppercutPoseAboveShoulderRatioMin = 0.14f;
        public const float UppercutPoseVerticalDominance = 0.75f;
    }
}
