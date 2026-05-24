using UnityEngine;

namespace LoseWeight.Game
{
    /// <summary>
    /// \u5bf9\u6218\u8d44\u6e90\u914d\u7f6e - \u6301\u6709\u6a21\u578b\u3001\u52a8\u753b\u7b49\u5f15\u7528
    /// \u653e\u5728 Resources \u6587\u4ef6\u5939\u4e0b\uff0c\u8fd0\u884c\u65f6\u53ef\u52a0\u8f7d
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAssets", menuName = "LoseWeight/Combat Assets")]
    public class CombatAssets : ScriptableObject
    {
        [Header("\u89d2\u8272\u6a21\u578b")]
        public GameObject CharacterPrefab;

        [Header("\u52a8\u753b\u63a7\u5236\u5668")]
        public RuntimeAnimatorController CombatAnimator;
    }
}
