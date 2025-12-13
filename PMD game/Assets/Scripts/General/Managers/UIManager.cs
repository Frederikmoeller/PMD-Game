// UIManager.cs - Simplified (~300 lines)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DialogueSystem.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    // ===== SCENE UI (Keep this - few scenes) =====
    [Header("Scene UI")]
    public GameObject titleScreenPanel;
    public Button titleStartButton;
    public Button titleQuitButton;
    public GameObject dungeonPanel;
    public TextMeshProUGUI floorNumberText;
    public TextMeshProUGUI dungeonNameText;
    
    // ===== SUB-MANAGER REFERENCES =====
    [Header("Sub-Managers")]
    [SerializeField] private MinimapSystem minimapSystem;
    [SerializeField] private ActionLogManager actionLogManager;
    [SerializeField] private PlayerUIManager playerUIManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private DialogueUI dialogueUIManager;
    
    // Project-specific UI panels that surround dialogue
    [SerializeField] private GameObject dialoguePanel;
    
    // ===== STATE =====
    private PlayerStats _playerStats;
    
    void Awake()
    {
        Debug.Log($"=== UIManager Awake() called ===");
        Debug.Log($"Instance exists: {Instance != null}");
        Debug.Log($"This object: {gameObject.name}");
    
        if (Instance == null)
        {
            Debug.Log("Setting as Instance (first UIManager)");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Debug.Log($"Destroying duplicate UIManager. Instance is: {Instance.gameObject.name}");
            Debug.Log($"This duplicate is in scene: {gameObject.scene.name}");
            
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("This is already the Instance (re-awakening?)");
        }
    }
    
    void Start()
    {
        SetupButtonListeners();
        HideAllPanels();
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    
        // Initialize GameLogger
        if (actionLogManager != null)
        {
            GameLogger.Initialize(actionLogManager);
        }
    }
    
    void SetupButtonListeners()
    {
        if (titleStartButton != null)
            titleStartButton.onClick.AddListener(OnTitleStartClicked);
        if (titleQuitButton != null)
            titleQuitButton.onClick.AddListener(OnQuitClicked);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"=== SCENE LOADED: {scene.name} ===");
    
        // 1. Ensure our canvas is active
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
        }

        // 3. Your existing scene setup
        HideAllPanels();
    
        if (scene.name.Contains("Title"))
        {
            titleScreenPanel?.SetActive(true);
            Time.timeScale = 1f;
        }
        else if (scene.name.Contains("Dungeon"))
        {
            dungeonPanel?.SetActive(true);
            SetupDungeon();
        }
    
        // 4. Hide all popups
        //HideAllPopups();
    
        Debug.Log($"Scene setup complete for: {scene.name}");
    }

    void SetupDungeon()
    {
        _playerStats = FindFirstObjectByType<PlayerStats>();
        
        if (_playerStats != null)
        {
            playerUIManager?.Initialize(_playerStats);
            popupManager?.Initialize(_playerStats);
            
            // Keep direct listeners for coordination
            _playerStats.OnPlayerHealthChanged.AddListener(UpdateHealthUI);
            _playerStats.OnPlayerDeath.AddListener(ShowDeathScreen);
            _playerStats.OnPlayerManaChanged.AddListener(UpdateManaUI);
            _playerStats.OnPlayerLevelUp.AddListener(UpdateLevelUI);
        }
        
        UpdateDungeonInfo(GameManager.Instance.CurrentDungeon, GameManager.Instance.CurrentFloor);
        AddLogEntry("Entered dungeon", Color.white);
        SetupMinimap();
    }
    
    // ===== MINIMAP COORDINATION =====
    public void SetupMinimap()
    {
        if (minimapSystem == null) return;
        
        minimapSystem.Initialize();
        StartCoroutine(DelayedMinimapSetup());
    }
    
    IEnumerator DelayedMinimapSetup()
    {
        yield return null;
        
        var dungeonManager = FindFirstObjectByType<DungeonFloorManager>();
        if (dungeonManager?.Generator?.Grid != null)
        {
            minimapSystem.GenerateMinimap(dungeonManager.Generator.Grid);
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player?.GetComponent<PlayerStats>() is PlayerStats stats)
            {
                Vector2Int playerPos = new(stats.GridX, stats.GridY);
                minimapSystem.UpdatePlayerPosition(playerPos);
                StartCoroutine(UpdateMinimapPlayerPosition(stats));
            }
        }
    }
    
    IEnumerator UpdateMinimapPlayerPosition(PlayerStats player)
    {
        Vector2Int lastPos = new(player.GridX, player.GridY);
        
        while (true)
        {
            Vector2Int currentPos = new(player.GridX, player.GridY);
            if (currentPos != lastPos)
            {
                minimapSystem?.UpdatePlayerPosition(currentPos);
                lastPos = currentPos;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    // ===== PUBLIC API (Delegation) =====
    public void AddLogEntry(string message, Color? color = null)
    {
        actionLogManager?.AddEntry(message, color);
    }
    
    public void ToggleMinimap()
    {
        if (minimapSystem == null) return;
        
        bool isVisible = minimapSystem.minimapContainer.gameObject.activeSelf;
        minimapSystem.ToggleVisibility(!isVisible);
        AddLogEntry(isVisible ? "Minimap hidden" : "Minimap shown", Color.cyan);
    }
    
    public void UpdateHealthUI(int health) => playerUIManager?.UpdateHealth(health);
    public void UpdateManaUI(int mana) => playerUIManager?.UpdateMana(mana);
    
    public void UpdateLevelUI(int level)
    {
        playerUIManager?.UpdateLevel(level);
        AddLogEntry($"Level up! Now level {level}", Color.yellow);
    }
    
    public void UpdateDungeonInfo(string dungeonName = "Ancient Crypt", int floorNumber = 1)
    {
        if (floorNumberText != null) floorNumberText.text = $"Floor: {floorNumber}";
        if (dungeonNameText != null) dungeonNameText.text = dungeonName;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentFloor = floorNumber;
            GameManager.Instance.CurrentDungeon = dungeonName;
        }
    }
    
    public void ShowConfirmationPopup(string title, string message, 
                                     System.Action onConfirm, System.Action onCancel = null)
    {
        popupManager?.ShowConfirmation(title, message, onConfirm, onCancel);
        PauseGame(true);
    }
    
    public void ToggleInventory()
    {
        popupManager?.ToggleInventory();
        PauseGame(popupManager?.inventoryPopup?.activeSelf ?? false);
    }
    
    public void ShowDeathScreen()
    {
        popupManager?.ShowDeathScreen();
        PauseGame(true);
    }
    
    // ===== SCENE UI HELPERS =====
    void HideAllPanels()
    {
        if (titleScreenPanel != null) titleScreenPanel.SetActive(false);
        if (dungeonPanel != null) dungeonPanel.SetActive(false);
    }
    
    void OnTitleStartClicked()
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.ReturnToTown();
        else
            SceneManager.LoadScene("HubWorld");
    }
    
    void OnQuitClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    void PauseGame(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;
        GameManager.Instance.IsGamePaused = pause;
    }
    
    // Project-specific methods
    public void ShowDialogueUI()
    {
        // Show your project's UI framework around dialogue
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
    }
    
    public void HideDialogueUI()
    {
        // Hide your project's UI framework
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_playerStats != null)
        {
            _playerStats.OnPlayerHealthChanged.RemoveListener(UpdateHealthUI);
            _playerStats.OnPlayerDeath.RemoveListener(ShowDeathScreen);
            _playerStats.OnPlayerManaChanged.RemoveListener(UpdateManaUI);
            _playerStats.OnPlayerLevelUp.RemoveListener(UpdateLevelUI);
        }
        StopAllCoroutines();
    }
}