using UnityEngine;

namespace LoseWeight.Character
{
    /// <summary>
    /// 换装系统 - 管理角色服装和拳套切换
    /// </summary>
    public class DressupSystem : MonoBehaviour
    {
        [Header("Outfit Meshes (训练服/职业/冠军)")]
        [SerializeField] private GameObject[] _outfitMeshes;

        [Header("Glove Meshes (训练/烈焰/雷霆)")]
        [SerializeField] private GameObject[] _gloveMeshes;

        [Header("Hit Effects (对应三种拳套)")]
        [SerializeField] private ParticleSystem[] _hitEffects;

        private int _currentOutfit;
        private int _currentGlove;

        private void Start()
        {
            // 读取玩家装扮设置
            _currentOutfit = PlayerPrefs.GetInt("EquippedOutfit", 0);
            _currentGlove = PlayerPrefs.GetInt("EquippedGlove", 0);
            ApplyDressup();
        }

        /// <summary>
        /// 切换服装
        /// </summary>
        public void SetOutfit(int index)
        {
            if (index < 0 || index >= _outfitMeshes?.Length) return;
            _currentOutfit = index;
            ApplyDressup();
        }

        /// <summary>
        /// 切换拳套
        /// </summary>
        public void SetGlove(int index)
        {
            if (index < 0 || index >= _gloveMeshes?.Length) return;
            _currentGlove = index;
            ApplyDressup();
        }

        /// <summary>
        /// 播放命中特效（根据当前拳套）
        /// </summary>
        public void PlayHitEffect(Vector3 position)
        {
            if (_hitEffects == null || _currentGlove >= _hitEffects.Length) return;

            var effect = _hitEffects[_currentGlove];
            if (effect != null)
            {
                effect.transform.position = position;
                effect.Play();
            }
        }

        private void ApplyDressup()
        {
            // 切换服装显示
            if (_outfitMeshes != null)
            {
                for (int i = 0; i < _outfitMeshes.Length; i++)
                {
                    if (_outfitMeshes[i] != null)
                        _outfitMeshes[i].SetActive(i == _currentOutfit);
                }
            }

            // 切换拳套显示
            if (_gloveMeshes != null)
            {
                for (int i = 0; i < _gloveMeshes.Length; i++)
                {
                    if (_gloveMeshes[i] != null)
                        _gloveMeshes[i].SetActive(i == _currentGlove);
                }
            }
        }
    }
}
