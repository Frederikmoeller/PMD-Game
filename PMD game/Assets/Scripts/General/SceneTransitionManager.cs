using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Names")]
    public string TownSceneName = "Town";
    public string DungeonSceneName = "Dungeon";
    
    [Header("Player Spawn Points")]
    public Vector3 TownSpawnPoint = new Vector3(0, 0, 0);
    public Vector3 DungeonEntrancePoint = new Vector3(5, 5, 0); // Town position for dungeon entrance

    private GameObject _player;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find player
        _player = GameObject.FindGameObjectWithTag("Player");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void EnterDungeon(string dungeonName)
    {
        Debug.Log("Entering dungeon...");
        
        // Prepare player
        PlayerPersist playerPersist = _player.GetComponent<PlayerPersist>();
        if (playerPersist != null)
        {
            playerPersist.PrepareForDungeon();
        }
        
        // Load dungeon scene
        SceneManager.LoadScene(DungeonSceneName);
    }
    
    public void ReturnToTown()
    {
        Debug.Log("Returning to town...");
        
        // Restore player
        PlayerPersist playerPersist = _player.GetComponent<PlayerPersist>();
        if (playerPersist != null)
        {
            playerPersist.RestoreForTown();
        }
        
        // Load town scene
        SceneManager.LoadScene(TownSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        
        // Ensure we have Player reference
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (_player == null)
        {
            Debug.LogError("Player not found after scene load!");
            return;
        }

        if (scene.name == TownSceneName)
        {
            PositionPlayerInTown();
        }
        else if (scene.name == DungeonSceneName)
        {
            // DungeonFloorManager will handle positioning
            // Just ensure player is active
            _player.SetActive(true);
        }
        
    }
    
    private void PositionPlayerInTown()
    {
        if (_player == null) return;
        
        // Position at town spawn point
        _player.transform.position = TownSpawnPoint;
        
        // Reset any dungeon-specific components
        GridEntity gridEntity = _player.GetComponent<GridEntity>();
        if (gridEntity != null)
        {
            // Clear grid reference for town
            gridEntity.SetGrid(null);
        }
        
        Debug.Log($"Player positioned in town at {TownSpawnPoint}");
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
