using UnityEngine;
using UnityEngine.Events;

public class DungeonFloorManager : MonoBehaviour
{
    [Header("References")] 
    public DungeonGenerator Generator;
    public DungeonRenderer Renderer;
    [SerializeField] private Transform _entityContainer;

    [Header("Settings")] 
    public int CurrentFloor = 1;

    [Header("Events")] 
    public UnityEvent OnFloorStart;
    public UnityEvent OnFloorEnd;

    private DungeonGrid _grid;
    private GameObject _player;
    
    [Header("Enemy Spawning")]
    public EnemySpawner EnemySpawner;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        
        if (_player == null)
        {
            Debug.LogError("No persistent Player found! Make sure Player has DontDestroyOnLoad.");
            return;
        }
        
        Debug.Log($"Found persistent player: {_player.name}");
        // First-time dungeon generation
        GenerateNewFloor();
    }

    public void GenerateNewFloor()
    {
        Debug.Log("=== GENERATING NEW FLOOR ===");
        OnFloorEnd?.Invoke();
        
        // Step 1: Clear previous floor (but keep player)
        Renderer.Clear(); 
        if (CurrentFloor > 1)
        {
            Clear();
        }

        // Step 2: Generate new layout
        _grid = Generator.Generate();
        
        if (_grid == null)
        {
            Debug.LogError("Grid generation failed!");
            return;
        }
    
        Debug.Log($"Grid generated: {_grid.Width}x{_grid.Height}");
        
        // Step 3: Render tiles & spawn objects
        Renderer.Render(_grid);

        // Step 4: Place player
        PlacePlayer();
        
        // NEW: Spawn enemies
        if (EnemySpawner != null)
        {
            EnemySpawner.SpawnEnemiesForFloor(_grid);
        }
        
        // Notify UIManager about new floor for minimap
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetupMinimap();
        }

        OnFloorStart?.Invoke();
    }

    private void PlacePlayer()
    {
        if (_grid == null)
        {
            Debug.LogError("Cannot place player: Grid is null!");
            return;
        }
        
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {
                Debug.LogError("Cannot place player: No player found!");
                return;
            }
        }

        Vector2Int start = _grid.GetSpawnPoint();
        Debug.Log($"Player spawn point: {start}");
        
        _player.transform.position = new Vector3(start.x + 0.5f, start.y + 0.5f, 0);

        PlayerStats playerEntity = _player.GetComponent<PlayerStats>();
        if (playerEntity != null)
        {
            playerEntity.GridX = start.x;
            playerEntity.GridY = start.y;
            playerEntity.SetGrid(_grid);

            if (playerEntity.Grid != null)
            {
                if (playerEntity.Grid.InBounds(playerEntity.GridX, playerEntity.GridY))
                {
                    playerEntity.Grid.Tiles[playerEntity.GridX, playerEntity.GridY].Occupant = null;
                }
            }

            if (_grid.InBounds(start.x, start.y))
            {
                _grid.Tiles[start.x, start.y].Occupant = playerEntity;
                Debug.Log($"Player occupies tile {start.x},{start.y}");
            }
        }
        else
        {
            Debug.LogWarning("Player doesn't have GridEntity component!");
        }
        
        _player.SetActive(true);
        
    }

    public void GoToNextFloor()
    {
        CurrentFloor++;
        Debug.Log("Entering Floor " + CurrentFloor);
        
        GenerateNewFloor();
        
        // Inform GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFloorChanged(CurrentFloor);
        }
    }
    
    // Call when player dies in dungeon
    public void OnPlayerDeathInDungeon()
    {
        Debug.Log("Player died in dungeon");
        
        // Could trigger game over or return to town
        // For now, just regenerate floor (like a roguelike)
        ExitDungeon(false);
    }
    
    public void ExitDungeon(bool success)
    {
        Debug.Log("Exiting dungeon");
        
        // Clear dungeon
        Renderer.Clear();
        Clear();
        
        
        // Return player to town position
        // This would be handled by your town scene manager
    }
    
    public void Clear()
    {
        if (_entityContainer == null) _entityContainer = transform;

        print(_entityContainer.childCount);
        for (int i = _entityContainer.childCount - 1; i >= 0; i--)
        {
            print(_entityContainer.GetChild(i).gameObject.name + i);
            if (Application.isPlaying && _entityContainer.childCount > 0)
            {
                print(_entityContainer.name);
                Destroy(_entityContainer.GetChild(i).gameObject);
            }
            else
            {
                //DestroyImmediate(Parent.GetChild(i).gameObject);
            }
        }
        print(_entityContainer.childCount);
    }
    
    // Helper to get player reference
    public GameObject GetPlayer()
    {
        return _player;
    }
}
