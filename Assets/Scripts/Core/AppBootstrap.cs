using UnityEngine;

namespace LoseWeight.Core
{
    /// <summary>
    /// Ӧ���������� - ��ʼ�����й�����
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Server Config")]
        [SerializeField] private string _serverUrl = "wss://your-server.com/ws";

        [Header("Debug")]
        [SerializeField] private bool _skipLogin = false;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        private void Start()
        {
            InitializeApp();
        }

        private void InitializeApp()
        {
            Debug.Log("[Bootstrap] Initializing...");

            // 0. UIManager must be authored in the scene by MCP.
            if (LoseWeight.UI.UIManager.Instance == null)
            {
                Debug.LogError("[Bootstrap] Missing MCP UIManager scene node.");
            }

            // 1. \u767b\u5f55\uff08App \u7248\uff1a\u5fae\u4fe1 OpenSDK / \u6e38\u5ba2\u767b\u5f55\uff09
            if (!_skipLogin)
            {
                // TODO: \u5b9e\u9645\u767b\u5f55 SDK
            }

            // 2. \u8fde\u63a5\u670d\u52a1\u5668
            // TODO: \u542f\u7528 WebSocket \u7f51\u7edc\u6a21\u5757

            // 3. \u8fdb\u5165\u52a0\u8f7d\u72b6\u6001
            GameManager.Instance?.ChangeState(GameState.Loading);

            Debug.Log("[Bootstrap] Initialization complete");
        }
    }
}
