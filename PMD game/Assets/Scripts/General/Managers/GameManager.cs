using System;
using System.Collections.Generic;
using DialogueSystem;
using DialogueSystem.Data;
using SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSystem
{
    public enum GameState
    {
        Initializing,
        TitleScreen,
        Loading,
        InTown,
        InDungeon,
        InDialogue,
        InCutscene,
        Paused,
    }

    public enum SceneType
    {
        Title,
        Town,
        Dungeon,
    }
    
    
    [Serializable]
    public struct SceneConfig
    {
        public string SceneName;
        public SceneType Type;
        public bool UseGridMovement;
        public bool IsPersistentScene;
    }

    [Serializable]
    public class GameSessionData
    {
        public GameObject Player;
        public string CurrentDungeon;
        public int CurrentFloor;
        public int StoryProgress;
        public bool[] StoryFlags;
        public long PlayTimeSeconds;
        public DateTime LastSaveTime;
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Scene Configuration")]
        [SerializeField] private SceneConfig[] _sceneConfigurations;

        [Header("Manager References")]
        [SerializeField] private UiManager _uiManager;
        [SerializeField] private DialogueManager _dialogueManager;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AudioManager _audioManager;
        [SerializeField] private InputManager _inputManager;

        // ===== PRIVATE COMPONENTS =====
        private DungeonManager _dungeonManager;
        private CombatManager _combatManager;
        private InventoryManager _inventoryManager;
        private QuestManager _questManager;

        // ===== PUBLIC PROPERTIES (Read-Only) =====
        public GameState CurrentGameState { get; private set; }
        public SceneType CurrentSceneType { get; private set; }
        public SceneConfig CurrentSceneConfig { get; private set; }
        public bool IsGamePaused { get; private set; }
        public bool IsInCutscene { get; private set; }
        
        // Session Data (source of truth)
        public GameSessionData SessionData { get; private set; }

        // ===== MANAGER ACCESSORS =====
        // Public access for other scripts
        public UiManager Ui => _uiManager;
        public DialogueManager Dialogue => _dialogueManager;
        public SaveManager Save => _saveManager;
        public AudioManager Audio => _audioManager;
        
        public InputManager Input => _inputManager;

        // Lazy-loaded managers
        public DungeonManager Dungeon => _dungeonManager ??= FindManager<DungeonManager>();
        public CombatManager Combat => _combatManager ??= FindManager<CombatManager>();
        public InventoryManager Inventory => _inventoryManager ??= FindManager<InventoryManager>();
        public QuestManager Quest => _questManager ??= FindManager<QuestManager>();
        
        // ===== EVENT SYSTEM =====
        public event Action<GameState> OnGameStateChanged;
        public event Action<SceneType> OnSceneTypeChanged;
        public event Action<bool> OnPauseStateChanged;
        public event Action<GameSessionData> OnSessionDataUpdated;

        // ===== INITIALIZATION =====
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            Debug.Log("=== GameManager Initializing ===");
            
            // Set initial state
            CurrentGameState = GameState.Initializing;
            IsGamePaused = false;
            IsInCutscene = false;
            
            // Initialize session data
            SessionData = new GameSessionData
            {
                Player = GameObject.FindGameObjectWithTag("Player"),
                StoryFlags = new bool[50],
                PlayTimeSeconds = 0,
                LastSaveTime = DateTime.Now
            };
            
            
            // Subscribe to events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Setup initial scene config
            UpdateSceneConfig(SceneManager.GetActiveScene().name);
            
            Debug.Log("GameManager initialized successfully");
        }
        
        private void Start()
        {
            // Initialize all managers
            InitializeAllManagers();
            // Start game in proper state
            ChangeGameState(DetermineInitialGameState());
            
        }
        
        private void InitializeAllManagers()
        {
            Debug.Log("Initializing all managers...");
            
            // Initialize core managers (in dependency order)
            _inputManager?.Initialize();
            _audioManager?.Initialize();
            _dungeonManager?.Initialize();
            _combatManager?.Initialize();
            
            // Initialize UI and dialogue last (they depend on others)
            _uiManager?.Initialize();

            // Lazy-loaded managers will initialize when first accessed
            Debug.Log("All managers initialized");
        }
        
        // ===== SCENE MANAGEMENT =====
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"GameManager: Scene loaded - {scene.name}");
            
            // Update scene configuration
            UpdateSceneConfig(scene.name);
            
            // Update game state based on scene type
            UpdateGameStateForScene();
            
            // Notify managers about scene change
            NotifyManagersOfSceneChange();
        }
        
        private void UpdateSceneConfig(string sceneName)
        {
            // Find scene config
            foreach (var config in _sceneConfigurations)
            {
                if (config.SceneName == sceneName)
                {
                    CurrentSceneConfig = config;
                    CurrentSceneType = config.Type;
                    OnSceneTypeChanged?.Invoke(config.Type);
                    return;
                }
            }
            
            // Default configuration for unknown scenes
            CurrentSceneConfig = new SceneConfig
            {
                SceneName = sceneName,
                Type = DetermineSceneType(sceneName),
                UseGridMovement = sceneName.Contains("Dungeon"),
                IsPersistentScene = false
            };
            CurrentSceneType = CurrentSceneConfig.Type;
            OnSceneTypeChanged?.Invoke(CurrentSceneType);
        }
        
        private SceneType DetermineSceneType(string sceneName)
        {
            if (sceneName.Contains("Title") || sceneName.Contains("Menu"))
                return SceneType.Title;
            if (sceneName.Contains("Town") || sceneName.Contains("Town"))
                return SceneType.Town;
            if (sceneName.Contains("Dungeon") || sceneName.Contains("Dungeon"))
                return SceneType.Dungeon;

            return SceneType.Town; // Default
        }
        
        private void UpdateGameStateForScene()
        {
            switch (CurrentSceneType)
            {
                case SceneType.Title:
                    ChangeGameState(GameState.TitleScreen);
                    break;
                case SceneType.Town:
                    ChangeGameState(GameState.InTown);
                    break;
                case SceneType.Dungeon:
                    ChangeGameState(GameState.InDungeon);
                    break;
            }
        }
        
        private void NotifyManagersOfSceneChange()
        {
            // Notify UI Manager
            Ui?.OnSceneChanged(CurrentSceneType, CurrentSceneConfig);
            
            // Notify Audio Manager
            Audio?.OnSceneChanged(CurrentSceneType, CurrentSceneConfig);
            
            // Initialize dungeon manager if in dungeon
            if (CurrentSceneType == SceneType.Dungeon)
            {
                
                Dungeon?.Initialize();
            }
            
            // Initialize combat manager if needed
            if (CurrentSceneType == SceneType.Dungeon)
            {
                Combat?.Initialize();
            }
        }

        // ===== GAME STATE MANAGEMENT =====
        public void ChangeGameState(GameState newState)
        {
            if (CurrentGameState == newState) return;
            
            var previousState = CurrentGameState;
            CurrentGameState = newState;
            
            Debug.Log($"GameState changed: {previousState} -> {newState}");
            
            // Handle state transitions
            HandleStateTransition(previousState, newState);
            
            // Notify listeners
            OnGameStateChanged?.Invoke(newState);
            
            // Notify managers
            NotifyManagersOfStateChange(newState);
        }
        
        private void HandleStateTransition(GameState from, GameState to)
        {
            // Handle pause state
            if (to == GameState.Paused)
            {
                SetPauseState(true);
            }
            else if (from == GameState.Paused && to != GameState.Paused)
            {
                SetPauseState(false);
            }
            
            // Handle cutscene state
            if (to == GameState.InDialogue || to == GameState.InCutscene)
            {
                IsInCutscene = true;
            }
            else if (from == GameState.InDialogue || from == GameState.InCutscene)
            {
                IsInCutscene = false;
            }
        }
        
        private void NotifyManagersOfStateChange(GameState newState)
        {
            Ui?.OnGameStateChanged(newState);
            Combat?.OnGameStateChanged(newState);
            Dungeon?.OnGameStateChanged(newState);
            Audio?.OnGameStateChanged(newState);
        }
        
        private GameState DetermineInitialGameState()
        {
            return CurrentSceneType switch
            {
                SceneType.Title => GameState.TitleScreen,
                SceneType.Town => GameState.InTown,
                SceneType.Dungeon => GameState.InDungeon,
                _ => GameState.InTown
            };
        }
        
        // ===== SESSION DATA MANAGEMENT =====
        public void UpdateSessionData(Action<GameSessionData> updateAction)
        {
            var data = SessionData;
            updateAction?.Invoke(data);
            SessionData = data;
            
            OnSessionDataUpdated?.Invoke(SessionData);
        }
        
        public void SetCurrentDungeon(string dungeonName, int floor = 1)
        {
            UpdateSessionData(data =>
            {
                data.CurrentDungeon = dungeonName;
                data.CurrentFloor = floor;
            });
            
            // Notify UI
            Ui?.UpdateDungeonInfo(dungeonName, floor);
            
            // Notify dungeon manager
            Dungeon?.OnDungeonChanged(dungeonName, floor);
        }
        
        public void SetStoryProgress(int progress)
        {
            UpdateSessionData(data =>
            {
                data.StoryProgress = progress;
            });
            
            // Check for story events
            CheckStoryEvents();
        }
        
        public void SetStoryFlag(int flagIndex, bool value)
        {
            if (flagIndex >= 0 && flagIndex < SessionData.StoryFlags.Length)
            {
                UpdateSessionData(data =>
                {
                    data.StoryFlags[flagIndex] = value;
                });
                
                CheckStoryEvents();
            }
        }
        
        private void CheckStoryEvents()
        {
            // Example: Check for specific story triggers
            if (SessionData.StoryProgress >= 5 && !SessionData.StoryFlags[0])
            {
                // Trigger first major story event
                Dialogue?.StartDialogue();
                SetStoryFlag(0, true);
            }
        }
        
        // ===== PUBLIC API METHODS =====
        public void StartNewGame()
        {
            Debug.Log("Starting new game...");
            
            ChangeGameState(GameState.Loading);
            
            // Reset session data
            SessionData = new GameSessionData
            {
                CurrentDungeon = "",
                CurrentFloor = 1,
                StoryProgress = 0,
                StoryFlags = new bool[50],
                PlayTimeSeconds = 0,
                LastSaveTime = DateTime.Now
            };
            
            // Clear temporary data from managers
            Inventory?.Clear();
            Quest?.Reset();
            Dungeon?.Reset();
            Combat?.Reset();
            
            // Load starting scene
            LoadScene("Town_Hub");
        }
        
        public void LoadGame(string saveSlot)
        {
            Debug.Log($"Loading game from slot: {saveSlot}");
            
            ChangeGameState(GameState.Loading);
            
            // Load through save manager
            Save?.LoadSlot(saveSlot);
        }
        
        public void SaveGame(string saveSlot)
        {
            Debug.Log($"Saving game to slot: {saveSlot}");
            
            // Update session data
            UpdateSessionData(data =>
            {
                data.LastSaveTime = DateTime.Now;
            });
            
            // Save through save manager
            Save?.SaveSlot(saveSlot);
        }
        
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Cannot load scene: scene name is empty");
                return;
            }
            
            Debug.Log($"Loading scene: {sceneName}");
            ChangeGameState(GameState.Loading);
            
            SceneManager.LoadScene(sceneName);
        }
        
        public void SetPauseState(bool paused)
        {
            if (IsGamePaused == paused) return;
            
            IsGamePaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            
            OnPauseStateChanged?.Invoke(paused);
            
            // Notify managers
            Ui?.OnPauseStateChanged(paused);
            Input?.OnPauseStateChanged(paused);
            Audio?.OnPauseStateChanged(paused);
        }
        
        public void TogglePause()
        {
            if (CurrentGameState == GameState.Paused)
            {
                ChangeGameState(PreviousNonPausedState());
            }
            else
            {
                ChangeGameState(GameState.Paused);
            }
        }
        
        public void StartDialogue(DialogueAsset dialogueKey)
        {
            if (CurrentGameState == GameState.InDialogue) return;
            
            ChangeGameState(GameState.InDialogue);
            Dialogue?.StartDialogue(dialogueKey);
        }
        
        public void EndDialogue()
        {
            if (CurrentGameState != GameState.InDialogue) return;
            
            // Return to previous state
            ChangeGameState(PreviousNonPausedState());
        }

        // ===== HELPER METHODS =====
        private T FindManager<T>() where T : MonoBehaviour
        {
            // Try to find in children first
            var manager = GetComponentInChildren<T>(true);
            if (manager != null) return manager;
            
            // Try to find in scene
            manager = FindObjectOfType<T>(true);
            if (manager != null)
            {
                // Parent it to GameManager for organization
                manager.transform.SetParent(transform);
                return manager;
            }
            
            // Create a new one if needed
            Debug.LogWarning($"Manager of type {typeof(T).Name} not found, creating default...");
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(transform);
            return go.AddComponent<T>();
        }
        
        private GameState PreviousNonPausedState()
        {
            // Determine which state to return to after pause/dialogue
            if (CurrentSceneType == SceneType.Dungeon)
                return GameState.InDungeon;
            if (CurrentSceneType == SceneType.Town)
                return GameState.InTown;
                
            return GameState.InTown;
        }

        // ===== CLEANUP =====
        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }
        
        // ===== DEBUG HELPERS =====
        [ContextMenu("Print Current State")]
        private void PrintCurrentState()
        {
            Debug.Log($"=== GAME MANAGER STATE ===");
            Debug.Log($"Game State: {CurrentGameState}");
            Debug.Log($"Scene Type: {CurrentSceneType}");
            Debug.Log($"Scene: {CurrentSceneConfig.SceneName}");
            Debug.Log($"Paused: {IsGamePaused}");
            Debug.Log($"In Cutscene: {IsInCutscene}");
            Debug.Log($"Dungeon: {SessionData.CurrentDungeon} Floor: {SessionData.CurrentFloor}");
            Debug.Log($"Story Progress: {SessionData.StoryProgress}");
            Debug.Log($"Play Time: {SessionData.PlayTimeSeconds}s");
            Debug.Log($"==========================");
        }
        
        [ContextMenu("Reset Game")]
        private void ResetGame()
        {
            StartNewGame();
        }
        
    }
    // ===== INTERFACE FOR MANAGERS =====
    public interface IGameManagerListener
    {
        void OnGameStateChanged(GameState newState);
        void OnSceneChanged(SceneType sceneType, SceneConfig config);
        void OnPauseStateChanged(bool paused);
    }
}