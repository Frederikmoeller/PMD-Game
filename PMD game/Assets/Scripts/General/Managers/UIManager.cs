// UIManager.cs - Simplified (~300 lines)

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DialogueSystem;
using DialogueSystem.UI;
using GameSystem;
using TMPro;
using UnityEngine.EventSystems;

namespace GameSystem
{
    public class UiManager : MonoBehaviour, IGameManagerListener
    {
        public static UiManager Instance { get; private set; }

        // ===== SUB-MANAGER REFERENCES =====
        [Header("Sub-Managers")] [SerializeField]
        private MinimapSystem _minimapSystem;

        [SerializeField] private ActionLogManager _actionLogManager;
        [SerializeField] private PlayerUIManager _playerUiManager;
        [SerializeField] private PopupManager _popupManager;
        [SerializeField] private DialogueUi _dialogueUiManager;

        // ===== SCENE UI (Keep this - few scenes) =====
        [Header("Scene UI Containers")] 
        [SerializeField] private GameObject _titleScreenContainer;
        [SerializeField] private GameObject _townUIContainer;
        [SerializeField] private GameObject _dungeonUIContainer;
        [SerializeField] private GameObject _dialogueUIContainer;
        
        [Header("Dungeon UI")]
        [SerializeField] private TextMeshProUGUI floorNumberText;
        [SerializeField] private TextMeshProUGUI dungeonNameText;
        [SerializeField] private GameObject minimapContainer;

        [Header("Buttons")] [SerializeField] private Button _titleStartButton;
        [SerializeField] private Button _titleQuitButton;
        [SerializeField] private Button _pauseResumeButton;
        [SerializeField] private Button _pauseQuitButton;

        [Header("Panels")] [SerializeField] private GameObject _pausePanel;

        // Project-specific UI panels that surround dialogue
        [SerializeField] private GameObject _dialoguePanel;

        // ===== STATE =====
        private PlayerStats _playerStats;
        private bool _isInitialized = false;
        private Coroutine _minimapUpdateCoroutine;

        // ===== PROPERTIES =====
        public bool IsPauseMenuOpen => _pausePanel != null && _pausePanel.activeSelf;

        // ===== INITIALIZATION =====
        public void Initialize()
        {
            if (_isInitialized) return;

            Debug.Log("UIManager Initializing");

            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize GameLogger
            if (_actionLogManager != null)
            {
                GameLogger.Initialize(_actionLogManager);
            }
            
            // Setup button listeners
            SetupButtonListeners();

            // Initialize sub-managers
            InitializeSubManagers();

            // Hide all UI initially
            HideAllUI();

            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoadedInternal;

            _isInitialized = true;
            Debug.Log("UIManager initialized successfully");
        }

        private void InitializeSubManagers()
        {
            // Initialize action log
            if (_actionLogManager != null)
            {
                GameLogger.Initialize(_actionLogManager);
            }

            // Initialize minimap
            _minimapSystem?.Initialize();

            // Initialize popup manager
            _popupManager?.Initialize();
        }

        private void SetupButtonListeners()
        {
            if (_titleStartButton != null)
                _titleStartButton.onClick.AddListener(OnTitleStartClicked);

            if (_titleQuitButton != null)
                _titleQuitButton.onClick.AddListener(OnQuitClicked);

            if (_pauseResumeButton != null)
                _pauseResumeButton.onClick.AddListener(OnResumeClicked);

            if (_pauseQuitButton != null)
                _pauseQuitButton.onClick.AddListener(OnQuitToTitleClicked);
        }

        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.TitleScreen:
                    ShowTitleScreen();
                    break;

                case GameState.InTown:
                    ShowTownUI();
                    break;

                case GameState.InDungeon:
                    ShowDungeonUI();
                    break;

                case GameState.InDialogue:
                    ShowDialogueUi();
                    break;

                case GameState.Paused:
                    ShowPauseMenu();
                    break;
            }
        }

        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            // Update UI based on scene
            UpdateSceneUI(sceneType);

            // Setup player reference for the new scene
            SetupPlayerReference();
        }

        public void OnPauseStateChanged(bool paused)
        {
            // Pause menu is handled by OnGameStateChanged
            // This method is for any additional pause-related UI updates
        }

        // ===== SCENE UI MANAGEMENT =====
        private void OnSceneLoadedInternal(Scene scene, LoadSceneMode mode)
        {
            // This is called by SceneManager, but we also use OnSceneChanged from GameManager
            // Keeping for backward compatibility
        }

        private void UpdateSceneUI(SceneType sceneType)
        {
            
        }

        private void SetupPlayerReference()
        {
            _playerStats = FindObjectOfType<PlayerStats>();

            if (_playerStats != null)
            {
                // Setup player UI manager
                _playerUiManager?.Initialize(_playerStats);

                // Setup popup manager
                _popupManager?.Initialize(_playerStats);

                // Subscribe to player events
                SubscribeToPlayerEvents();
            }
            else
            {
                Debug.LogWarning("No PlayerStats found in scene");
            }
        }

        private void SubscribeToPlayerEvents()
        {
            if (_playerStats == null) return;

            _playerStats.OnPlayerHealthChanged.AddListener(UpdateHealthUI);
            _playerStats.OnPlayerManaChanged.AddListener(UpdateManaUI);
            _playerStats.OnPlayerLevelUp.AddListener(UpdateLevelUI);
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerStats == null) return;

            _playerStats.OnPlayerHealthChanged.RemoveListener(UpdateHealthUI);
            _playerStats.OnPlayerManaChanged.RemoveListener(UpdateManaUI);
            _playerStats.OnPlayerLevelUp.RemoveListener(UpdateLevelUI);
        }

        // ===== UI VISIBILITY CONTROL =====
        private void HideAllUI()
        {
            if (_titleScreenContainer != null) _titleScreenContainer.SetActive(false);
            if (_townUIContainer != null) _townUIContainer.SetActive(false);
            if (_dungeonUIContainer != null) _dungeonUIContainer.SetActive(false);
            if (_dialogueUIContainer != null) _dialogueUIContainer.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        public void ShowTitleScreen()
        {
            HideAllUI();
            if (_titleScreenContainer != null) _titleScreenContainer.SetActive(true);

            // Enable cursor for menu
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void ShowTownUI()
        {
            HideAllUI();
            if (_townUIContainer != null) _townUIContainer.SetActive(true);

            // Setup player reference if not already done
            if (_playerStats == null) SetupPlayerReference();
    
            // Hide minimap in town
            if (minimapContainer != null) minimapContainer.SetActive(false);
        }

        public void ShowDungeonUI()
        {
            HideAllUI();
            if (_dungeonUIContainer != null) _dungeonUIContainer.SetActive(true);

            // Setup player reference if not already done
            if (_playerStats == null) SetupPlayerReference();

            // Show minimap container
            if (minimapContainer != null) 
            {
                minimapContainer.SetActive(true);
                Debug.Log("Minimap container activated");
            }
        }

        public void ShowDialogueUi()
        {
            // Don't hide all UI - just show dialogue overlay
            if (_dialogueUIContainer != null) _dialogueUIContainer.SetActive(true);

            // Show DialogueSystem's UI
            if (_dialogueUiManager != null && DialogueManager.Instance != null)
            {
                _dialogueUiManager.gameObject.SetActive(true);
            }
        }

        public void HideDialogueUi()
        {
            if (_dialogueUIContainer != null) _dialogueUIContainer.SetActive(false);

            // Hide DialogueSystem's UI
            if (_dialogueUiManager != null)
            {
                _dialogueUiManager.gameObject.SetActive(false);
            }
        }

        public void ShowPauseMenu()
        {
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        public void HidePauseMenu()
        {
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(false);

                // Only hide cursor in gameplay states
                if (GameManager.Instance.CurrentGameState == GameState.InDungeon)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }

        // ===== PUBLIC API =====
        // Action Log
        public void AddLogEntry(string message, Color? color = null)
        {
            _actionLogManager?.AddEntry(message, color);
        }

        // Player UI
        public void UpdateHealthUI(int health)
        {
            _playerUiManager?.UpdateHealth(health, _playerStats.Stats.MaxHealth);
        }

        public void UpdateManaUI(int mana)
        {
            _playerUiManager?.UpdateMana(mana, _playerStats.Stats.MaxMana, false);
        }

        public void UpdateLevelUI(int level)
        {
            _playerUiManager?.UpdateLevel(level);
            AddLogEntry($"Level up! Now level {level}", Color.yellow);
        }

        // Dungeon Info
        public void UpdateDungeonInfo(string dungeonName = "Unknown Dungeon", int floorNumber = 1)
        {
            if (floorNumberText != null)
                floorNumberText.text = $"Floor: {floorNumber}";

            if (dungeonNameText != null)
                dungeonNameText.text = dungeonName;
        }
        
        // ===== MINIMAP =====
        public void SetupMinimap(DungeonGrid grid)
        {
            if (_minimapSystem == null) 
            {
                Debug.LogError("MinimapSystem not assigned!");
                return;
            }
    
            if (grid == null)
            {
                Debug.LogError("Cannot setup minimap: Grid is null!");
                return;
            }

            Debug.Log($"Setting up minimap with grid: {grid.Width}x{grid.Height}");
    
            // Generate the minimap
            _minimapSystem.GenerateMinimap(grid);
    
            // Start updating player position
            StartMinimapUpdates();
        }

        private void StartMinimapUpdates()
        {
            // Stop any existing coroutine
            if (_minimapUpdateCoroutine != null)
            {
                StopCoroutine(_minimapUpdateCoroutine);
            }
    
            _minimapUpdateCoroutine = StartCoroutine(UpdateMinimapPlayerPosition());
        }

        private IEnumerator SetupMinimapCoroutine()
        {
            Debug.Log("Starting minimap setup...");
    
            // Wait for dungeon to be fully generated
            yield return new WaitForSeconds(0.1f);
    
            if (GameManager.Instance == null || GameManager.Instance.Dungeon == null)
            {
                Debug.LogError("GameManager or Dungeon not ready!");
                yield break;
            }

            var dungeonGrid = GameManager.Instance.Dungeon.CurrentGrid;
            if (dungeonGrid == null)
            {
                Debug.LogError("Dungeon grid is null!");
                yield break;
            }

            Debug.Log($"Dungeon grid ready: {dungeonGrid.Width}x{dungeonGrid.Height}");
    
            // Initialize minimap with the grid
            _minimapSystem.GenerateMinimap(dungeonGrid);
    
            // Start updating player position
            _minimapUpdateCoroutine = StartCoroutine(UpdateMinimapPlayerPosition());
        }

        private IEnumerator UpdateMinimapPlayerPosition()
        {
            if (_playerStats == null)
            {
                Debug.LogError("PlayerStats not found for minimap update!");
                yield break;
            }

            if (_minimapSystem == null)
            {
                Debug.LogError("MinimapSystem not found!");
                yield break;
            }

            Debug.Log("Starting minimap player position updates...");
    
            Vector2Int lastPos = new Vector2Int(_playerStats.GridX, _playerStats.GridY);
    
            // Send initial position
            _minimapSystem.UpdatePlayerPosition(lastPos);
    
            // Update periodically
            while (GameManager.Instance.CurrentGameState == GameState.InDungeon)
            {
                Vector2Int currentPos = new Vector2Int(_playerStats.GridX, _playerStats.GridY);
        
                if (currentPos != lastPos)
                {
                    Debug.Log($"Player moved: {lastPos} -> {currentPos}");
                    _minimapSystem.UpdatePlayerPosition(currentPos);
                    lastPos = currentPos;
                }
        
                yield return new WaitForSeconds(0.1f);
            }
    
            Debug.Log("Minimap update coroutine ended");
        }

        // Popups
        public void ShowConfirmationPopup(string title, string message,
            Action onConfirm, Action onCancel = null)
        {
            _popupManager?.ShowConfirmation(title, message, onConfirm, onCancel);
        }

        public void ShowMessage(string message, float duration = 3f)
        {
            _popupManager?.ShowMessage(message, duration);
        }

        public void ToggleInventory()
        {
            bool wasOpen = _popupManager?.ToggleInventory() ?? false;

            // Update cursor based on inventory state
            Cursor.visible = wasOpen;
            Cursor.lockState = wasOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void ShowInventory(bool show)
        {
            _popupManager?.ShowInventory(show);

            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void UpdateInventoryUI(System.Collections.Generic.List<InventorySlot> inventory)
        {
            _popupManager?.UpdateInventoryUI(inventory);
        }

        public void UpdateEquipmentUI(EquipmentSlots equipment)
        {
            _popupManager?.UpdateEquipmentUI(equipment);
        }

        public void UpdateQuestLog(System.Collections.Generic.List<ActiveQuest> quests)
        {
            _popupManager?.UpdateQuestLog(quests);
        }

        public void HideQuestLog()
        {
            _popupManager?.HideQuestLog();
        }

        // ===== BUTTON HANDLERS =====
        private void OnTitleStartClicked()
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.ReturnToTown();
            }
            else
            {
                GameManager.Instance.LoadScene("Town");
            }

            GameManager.Instance.Audio.PlayButtonClick();
        }

        private void OnQuitClicked()
        {
            GameManager.Instance.Audio.PlayButtonClick();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnResumeClicked()
        {
            GameManager.Instance.TogglePause();
            GameManager.Instance.Audio.PlayButtonClick();
        }

        private void OnQuitToTitleClicked()
        {
            GameManager.Instance.SetPauseState(false);
            GameManager.Instance.LoadScene("TitleScreen");
            GameManager.Instance.Audio.PlayButtonClick();
        }

        // ===== INPUT HANDLING =====
        private void OnEnable()
        {
            // Subscribe to input events
            if (GameManager.Instance?.Input != null)
            {
                GameManager.Instance.Input.OnInventoryInput += HandleInventoryInput;
                GameManager.Instance.Input.OnQuestLogInput += HandleQuestLogInput;
                GameManager.Instance.Input.OnPauseInput += HandlePauseInput;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from input events
            if (GameManager.Instance?.Input != null)
            {
                GameManager.Instance.Input.OnInventoryInput -= HandleInventoryInput;
                GameManager.Instance.Input.OnQuestLogInput -= HandleQuestLogInput;
                GameManager.Instance.Input.OnPauseInput -= HandlePauseInput;
            }
            
            // Stop minimap coroutine
            if (_minimapUpdateCoroutine != null)
            {
                StopCoroutine(_minimapUpdateCoroutine);
                _minimapUpdateCoroutine = null;
            }

            UnsubscribeFromPlayerEvents();
        }

        private void HandleInventoryInput()
        {
            if (GameManager.Instance.CurrentGameState == GameState.InDungeon ||
                GameManager.Instance.CurrentGameState == GameState.InTown)
            {
                ToggleInventory();
            }
        }

        private void HandleQuestLogInput()
        {
            _popupManager?.ToggleQuestLog();
        }

        private void HandlePauseInput()
        {
            // Only handle pause in gameplay states
            if (GameManager.Instance.CurrentGameState == GameState.InDungeon ||
                GameManager.Instance.CurrentGameState == GameState.InTown ||
                GameManager.Instance.CurrentGameState == GameState.Paused)
            {
                GameManager.Instance.TogglePause();
            }
        }

        // ===== CLEANUP =====
        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoadedInternal;
                Instance = null;
            }
            
            // Stop minimap coroutine
            if (_minimapUpdateCoroutine != null)
            {
                StopCoroutine(_minimapUpdateCoroutine);
                _minimapUpdateCoroutine = null;
            }

            UnsubscribeFromPlayerEvents();
            StopAllCoroutines();
        }
    }
}