using UnityEngine;

namespace LoseWeight.FruitNinja
{
    /// <summary>
    /// 手部轨迹碰撞检测
    /// 每帧记录双手屏幕坐标，用线段-圆碰撞判定是否切中水果
    /// </summary>
    public class SliceDetector : MonoBehaviour
    {
        public Vector2 LeftHandScreen { get; private set; }
        public Vector2 RightHandScreen { get; private set; }
        public Vector2 LeftHandPrev { get; private set; }
        public Vector2 RightHandPrev { get; private set; }
        public bool LeftHandValid { get; private set; }
        public bool RightHandValid { get; private set; }

        private int _updateCount;

        /// <summary>
        /// 每帧由 FruitGameController 调用，传入手腕归一化坐标
        /// </summary>
        public void UpdateHands(Vector3 leftWrist, Vector3 rightWrist, float leftConf, float rightConf)
        {
            LeftHandValid = leftConf >= 0.3f;
            RightHandValid = rightConf >= 0.3f;

            Vector2 newLeft = LeftHandValid
                ? new Vector2((1f - leftWrist.x) * Screen.width, (1f - leftWrist.y) * Screen.height)
                : LeftHandScreen;
            Vector2 newRight = RightHandValid
                ? new Vector2((1f - rightWrist.x) * Screen.width, (1f - rightWrist.y) * Screen.height)
                : RightHandScreen;

            LeftHandPrev = LeftHandScreen;
            RightHandPrev = RightHandScreen;
            LeftHandScreen = newLeft;
            RightHandScreen = newRight;

            _updateCount++;
        }

        /// <summary>
        /// Editor 下用鼠标/触屏模拟左手
        /// </summary>
        public void UpdateFromMouse(Vector2 mouseScreenPos)
        {
            LeftHandPrev = LeftHandScreen;
            LeftHandScreen = mouseScreenPos;
            LeftHandValid = true;
            _updateCount++;
        }

        /// <summary>
        /// 判断某只手是否在这帧切中了目标
        /// </summary>
        public bool CheckSlice(Vector2 targetScreenPos, float radius, bool useLeftHand)
        {
            // 前 2 帧不判定（避免初始 (0,0) 到实际位置的巨线误切）
            if (_updateCount < 3) return false;

            Vector2 lineStart, lineEnd;
            bool valid;

            if (useLeftHand)
            {
                lineStart = LeftHandPrev;
                lineEnd = LeftHandScreen;
                valid = LeftHandValid;
            }
            else
            {
                lineStart = RightHandPrev;
                lineEnd = RightHandScreen;
                valid = RightHandValid;
            }

            if (!valid) return false;

            // 线段太短（手没动）不算切
            if (Vector2.Distance(lineStart, lineEnd) < 15f) return false;

            return LineSegmentIntersectsCircle(lineStart, lineEnd, targetScreenPos, radius);
        }

        /// <summary>
        /// 线段-圆碰撞检测
        /// </summary>
        public static bool LineSegmentIntersectsCircle(Vector2 p1, Vector2 p2, Vector2 center, float radius)
        {
            Vector2 d = p2 - p1;
            Vector2 f = p1 - center;

            float a = Vector2.Dot(d, d);
            if (a < 0.001f) return Vector2.Distance(p1, center) <= radius; // 零长线段退化为点

            float b = 2f * Vector2.Dot(f, d);
            float c = Vector2.Dot(f, f) - radius * radius;

            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0) return false;

            discriminant = Mathf.Sqrt(discriminant);
            float t1 = (-b - discriminant) / (2f * a);
            float t2 = (-b + discriminant) / (2f * a);

            if (t1 >= 0f && t1 <= 1f) return true;
            if (t2 >= 0f && t2 <= 1f) return true;

            return false;
        }

        public void Reset()
        {
            _updateCount = 0;
            LeftHandScreen = RightHandScreen = LeftHandPrev = RightHandPrev = Vector2.zero;
            LeftHandValid = RightHandValid = false;
        }
    }
}
