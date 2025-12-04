using System.Collections.Generic;
using SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneMovementConfig
{
    public string SceneName;
    public bool UseGridMovement;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int CurrentFloor = 1;
    public bool IsGamePaused = false;
    public bool IsInCutscene = false;
    public bool IsGridBased { get; private set; } = false;
    
    [Header("Scene Movement Configuration")]
    public SceneMovementConfig[] sceneMovementConfigs;
    
    [Header("Dungeons")]
    public string CurrentDungeon = "";
    [SaveField] public List<string> AvailableDungeons;
    public List<string> AllDungeons;

    [Header("Story Progression")]
    [SaveField] public int StoryProgress = 0; // Could be enum for different story beats
    [SaveField] public bool[] StoryFlags; // For tracking story events
    
    [Header("Game Stats")]
    public int TotalEnemiesDefeated = 0;
    public int TotalItemsCollected = 0;
    public int TotalFloorsCleared = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Initialize()
    {
        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Initialize story flags
        InitializeStoryFlags();
        
        // Set initial movement mode based on current scene
        UpdateMovementModeForScene(SceneManager.GetActiveScene().name);
        
        Debug.Log("GameManager initialized");
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameManager: Scene loaded - {scene.name}");
        
        // Update movement mode
        UpdateMovementModeForScene(scene.name);
        
        // Update dungeon tracking
        UpdateDungeonState(scene.name);
    }
    
    void UpdateMovementModeForScene(string sceneName)
    {
        bool shouldBeGridBased = DetermineMovementMode(sceneName);
        
        // Only update if changed
        if (IsGridBased == shouldBeGridBased) return;
        IsGridBased = shouldBeGridBased;
        Debug.Log($"Movement mode: {(IsGridBased ? "Grid-Based" : "Free Movement")}");
    }
    
    bool DetermineMovementMode(string sceneName)
    {
        // First, check custom configs from Inspector
        if (sceneMovementConfigs != null)
        {
            foreach (var config in sceneMovementConfigs)
            {
                if (config.SceneName == sceneName)
                {
                    return config.UseGridMovement;
                }
            }
        }
        
        // SIMPLE RULE: Only dungeons use grid movement
        // Check if scene name contains dungeon indicators
        bool isDungeonScene = sceneName.Contains("Dungeon") || 
                              sceneName.Contains("dungeon") || 
                              sceneName.Contains("Floor") ||
                              sceneName.ToLower().Contains("dng");
        
        return isDungeonScene;
    }
    
    void UpdateDungeonState(string sceneName)
    {
        // Reset dungeon state if not in a dungeon
        if (!IsGridBased)
        {
            CurrentDungeon = "";
        }
        // Otherwise, extract dungeon name from scene name
        else if (sceneName.Contains("_"))
        {
            // Example: "Dungeon_Forest_Floor1" -> "Forest"
            string[] parts = sceneName.Split('_');
            if (parts.Length > 1)
            {
                CurrentDungeon = parts[1];
                Debug.Log($"Entered dungeon: {CurrentDungeon}");
            }
        }
    }
    
    // ===== PUBLIC HELPERS =====
    
    public bool IsInDungeon()
    {
        return IsGridBased && !string.IsNullOrEmpty(CurrentDungeon);
    }
    
    public bool IsInTown()
    {
        return !IsGridBased;
    }

    private void InitializeStoryFlags()
    {
        // Initialize story flags based on save data or default
        StoryFlags = new bool[20]; // Adjust size as needed
    }
    
    public void OnFloorChanged(int floorNumber)
    {
        CurrentFloor = floorNumber;
        TotalFloorsCleared++;
        
        Debug.Log($"Floor {floorNumber} reached");
        // Trigger story events based on floor
        CheckFloorStoryEvents(floorNumber);
    }

    private void CheckFloorStoryEvents(int floor)
    {
        // Example: Trigger story events at specific floors
        if (floor == 5)
        {
            TriggerStoryEvent(1); // First boss or story beat
        }
        else if (floor == 10)
        {
            TriggerStoryEvent(2); // Major story event
        }
    }
    
    public void TriggerStoryEvent(int eventId)
    {
        if (eventId < StoryFlags.Length)
        {
            StoryFlags[eventId] = true;
            Debug.Log($"Story event {eventId} triggered");
            
            // Could trigger dialogue or cutscene here
            // DialogueManager.Instance?.StartDialogue(eventId);
        }
    }
    
    public void TogglePause(bool pause)
    {
        IsGamePaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        // Show/hide pause menu
    }
    
    public void StartCutscene()
    {
        IsInCutscene = true;

        // Pause turn-based systems if needed
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.playersTurn = false;
        }
    }
    
    public void EndCutscene()
    {
        IsInCutscene = false;

        // Resume turn-based systems
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.playersTurn = true;
        }
    }
    
    public void GameOver(bool win = false)
    {
        Debug.Log(win ? "Victory!" : "Game Over");
        // Show game over screen, return to menu, etc.
        // SaveManager.Instance?.SaveGame();
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
