using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace LoseWeight.Core
{
    /// <summary>
    /// 场景加载器 - 异步加载场景
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public float LoadProgress { get; private set; }

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

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// 加载对战场景
        /// </summary>
        public void LoadCombatScene()
        {
            LoadScene("CombatScene");
        }

        /// <summary>
        /// 返回主菜单场景
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene("MainScene");
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            LoadProgress = 0;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Scene not found: {sceneName}");
                yield break;
            }

            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                LoadProgress = Mathf.Clamp01(operation.progress / 0.9f);

                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            LoadProgress = 1f;
        }
    }
}
