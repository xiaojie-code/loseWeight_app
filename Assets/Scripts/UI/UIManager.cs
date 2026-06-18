using UnityEngine;
using UnityEngine.UI;
using LoseWeight.Core;
using LoseWeight.UI.Pages;

namespace LoseWeight.UI
{
    /// <summary>
    /// UI \u7ba1\u7406\u5668 - \u7ba1\u7406\u9875\u9762\u5207\u6362
    /// \u6587\u6863\u4e2d18\u4e2a\u529f\u80fd\u9875\u9762\u7684\u663e\u793a/\u9690\u85cf
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Pages")]
        [SerializeField] private GameObject _loadingPage;
        [SerializeField] private GameObject _loginPage;
        [SerializeField] private GameObject _mainMenuPage;
        [SerializeField] private GameObject _characterSelectPage;
        [SerializeField] private GameObject _calibrationPage;
        [SerializeField] private GameObject _combatPage;
        [SerializeField] private GameObject _matchingPage;
        [SerializeField] private GameObject _createRoomPage;
        [SerializeField] private GameObject _joinRoomPage;
        [SerializeField] private GameObject _resultPage;
        [SerializeField] private GameObject _rankingPage;
        [SerializeField] private GameObject _dressingPage;
        [SerializeField] private GameObject _profilePage;
        [SerializeField] private GameObject _settingsPage;
        [SerializeField] private GameObject _recordPage;
        [SerializeField] private GameObject _trainingPage;
        [SerializeField] private GameObject _regionSelectPage;

        private GameObject _currentPage;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializePages();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void InitializePages()
        {
            if (_initialized) return;
            _initialized = true;

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                Debug.LogError("[UIManager] Missing MCP Canvas on UIManager.");
            else
                canvas.sortingOrder = 200;

            BindPage(ref _loadingPage, "LoadingPage");
            BindPage(ref _loginPage, "LoginPage");
            BindPage(ref _mainMenuPage, "MainMenuPage");
            BindPage(ref _characterSelectPage, "CharacterSelectPage");
            BindPage(ref _calibrationPage, "CalibrationPage");
            BindPage(ref _matchingPage, "MatchingPage");
            BindPage(ref _createRoomPage, "CreateRoomPage");
            BindPage(ref _joinRoomPage, "JoinRoomPage");
            BindPage(ref _resultPage, "ResultPage");
            BindPage(ref _rankingPage, "RankingPage");
            BindPage(ref _dressingPage, "DressingPage");
            BindPage(ref _profilePage, "ProfilePage");
            BindPage(ref _settingsPage, "SettingsPage");
            BindPage(ref _recordPage, "RecordPage");
            BindPage(ref _trainingPage, "TrainingPage");
            BindPage(ref _regionSelectPage, "RegionSelectPage");

            // \u9ed8\u8ba4\u5168\u90e8\u9690\u85cf
            HideAllPages();

            // \u5982\u679c\u5f53\u524d\u5df2\u6709\u72b6\u6001\uff0c\u6839\u636e\u72b6\u6001\u663e\u793a\u5bf9\u5e94\u9875\u9762
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Loading)
                ShowPage(_loadingPage);
        }

        private void BindPage(ref GameObject page, string pageName)
        {
            if (page != null) return;
            var child = FindChild(transform, pageName);
            if (child == null)
            {
                Debug.LogError($"[UIManager] Missing MCP page node: {pageName}");
                return;
            }
            page = child.gameObject;
            page.SetActive(false);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.NewState)
            {
                case GameState.Loading:
                    ShowPage(_loadingPage);
                    break;
                case GameState.MainMenu:
                    ShowPage(_mainMenuPage);
                    break;
                case GameState.CharacterSelect:
                    ShowPage(_characterSelectPage);
                    break;
                case GameState.RegionSelect:
                    ShowPage(_regionSelectPage);
                    break;
                case GameState.Matching:
                    ShowPage(_matchingPage);
                    break;
                case GameState.RoomLobby:
                    ShowPage(_createRoomPage);
                    break;
                case GameState.PreCombat:
                    // 跳过校准页，直接进入对战
                    HideAllPages();
                    GameManager.Instance.ChangeState(GameState.Combat);
                    break;
                case GameState.Combat:
                    HideAllPages(); // \u5bf9\u6218\u65f6\u9690\u85cf UI \u9875\u9762
                    break;
                case GameState.MatchEnd:
                    ShowPage(_resultPage);
                    break;
                case GameState.FruitNinja:
                    HideAllPages();
                    break;
                case GameState.FruitNinjaEnd:
                    HideAllPages();
                    break;
                case GameState.CannonGame:
                    HideAllPages();
                    break;
                case GameState.CannonGameEnd:
                    HideAllPages();
                    break;
                case GameState.Dressing:
                    ShowPage(_dressingPage);
                    break;
                case GameState.Profile:
                    ShowPage(_profilePage);
                    break;
            }
        }

        public void ShowPage(GameObject page)
        {
            if (_currentPage != null && _currentPage != page)
                _currentPage.SetActive(false);

            if (page != null)
            {
                page.SetActive(true);
                _currentPage = page;
            }
        }

        public void HideAllPages()
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
            _currentPage = null;
        }

        // ========== \u6309\u94ae\u4e8b\u4ef6 ==========

        public void OnClickOnlineMatch()
        {
            GameManager.Instance.ChangeState(GameState.Matching);
        }

        public void OnClickCreateRoom()
        {
            GameManager.Instance.ChangeState(GameState.RoomLobby);
        }

        public void OnClickJoinRoom()
        {
            ShowPage(_joinRoomPage);
        }

        public void OnClickAIBattle()
        {
            GameManager.Instance.ChangeState(GameState.PreCombat);
        }

        public void OnClickFruitNinja()
        {
            GameManager.Instance.ChangeState(GameState.FruitNinja);
        }

        public void OnClickCannonGame()
        {
            GameManager.Instance.ChangeState(GameState.CannonGame);
        }

        public void OnClickTraining()
        {
            ShowPage(_trainingPage);
        }

        public void OnClickRanking()
        {
            ShowPage(_rankingPage);
        }

        public void OnClickDressing()
        {
            GameManager.Instance.ChangeState(GameState.Dressing);
        }

        public void OnClickProfile()
        {
            GameManager.Instance.ChangeState(GameState.Profile);
        }

        public void OnClickSettings()
        {
            ShowPage(_settingsPage);
        }

        public void OnClickRecord()
        {
            ShowPage(_recordPage);
        }

        public void OnClickRegionSelect()
        {
            ShowPage(_regionSelectPage);
        }

        public void OnClickBackToMenu()
        {
            // \u76f4\u63a5\u663e\u793a\u4e3b\u83dc\u5355\uff0c\u4e0d\u4f9d\u8d56 GameState \u53d8\u66f4
            // \uff08\u56e0\u4e3a\u8bbe\u7f6e/\u6392\u884c\u699c\u7b49\u9875\u9762\u662f\u76f4\u63a5 ShowPage \u8fdb\u5165\u7684\uff0c\u6ca1\u6539 GameState\uff09
            ShowPage(_mainMenuPage);
            // \u786e\u4fdd GameState \u4e5f\u540c\u6b65
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.MainMenu)
                GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            var direct = root.Find(name);
            if (direct != null) return direct;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
