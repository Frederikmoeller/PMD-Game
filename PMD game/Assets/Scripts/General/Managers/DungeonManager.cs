using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace GameSystem
{
    public class DungeonManager : MonoBehaviour, IGameManagerListener
    {
        [Header("Dependencies")] 
        [SerializeField] private DungeonGenerator _generator;
        [SerializeField] private DungeonRenderer _renderer;
        [SerializeField] private Transform _entityContainer;
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private EnemySpawner _enemySpawner;

        [Header("Events")] 
        public UnityEvent<DungeonTemplate> OnDungeonStarted;
        public UnityEvent<int> OnFloorChanged;
        public UnityEvent OnDungeonCleared;
        
        // State
        [SerializeField] private DungeonTemplate _currentTemplate;
        private DungeonGrid _currentGrid;

        public DungeonTemplate CurrentTemplate => _currentTemplate;
        public DungeonGrid CurrentGrid => _currentGrid;
        public int CurrentFloor { get; private set; } = 1;
        public bool IsInDungeon { get; private set; }

        // Dungeon-Specific state
        public void Initialize()
        {
            Debug.Log("DungeonManager Initializing");

            if (GameManager.Instance.SessionData.Player == null)
            {
                Debug.LogWarning("No player found. DungeonManager will work in editor mode only.");
            }

            // Find dependencies if not set
            if (_generator == null) _generator = FindObjectOfType<DungeonGenerator>();
            if (_renderer == null) _renderer = FindObjectOfType<DungeonRenderer>();
            if (_entityContainer == null) _entityContainer = transform;
            Debug.Log("DungeonManager initialized successfully");
        }
        
        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.InDungeon:
                    EnterDungeonMode();
                    break;
                case GameState.InTown:
                case GameState.TitleScreen:
                    ExitDungeonMode();
                    break;
            }
        }
        
        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            if (GameManager.Instance.CurrentSceneType == SceneType.Dungeon)
            {
                IsInDungeon = true;
                SetupDungeonScene();
            }
            else
            {
                IsInDungeon = false;
            }
        }
        
        public void OnPauseStateChanged(bool paused)
        {
            // Pause enemy AI, animations, etc.
            if (_enemySpawner != null)
            {
                //_enemySpawner.SetPaused(paused);
            }
        }
        
        // ===== PUBLIC API =====
        public void StartDungeon(DungeonTemplate template)
        {
            if (template == null)
            {
                Debug.LogError("Cannot start dungeon: template is null!");
                return;
            }
            
            _currentTemplate = template;
            CurrentFloor = 1;
            IsInDungeon = true;
            
            // Update GameManager session data
            GameManager.Instance.SetCurrentDungeon(template.Name, CurrentFloor);
            
            // Generate first floor
            GenerateFloor(CurrentFloor);
            
            // Fire event
            OnDungeonStarted?.Invoke(template);
            
            Debug.Log($"Started dungeon: {template.Name}");
        }
        
        public void GenerateFloor(int floorNumber)
        {
            if (_generator == null || GetComponent<DungeonRenderer>() == null)
            {
                Debug.LogError("Dungeon dependencies missing!");
                return;
            }
            
            // Clear previous floor
            ClearCurrentFloor();
            
            // Configure generator based on floor
            ConfigureGeneratorForFloor(floorNumber);
            
            // Generate grid
            _currentGrid = _generator.Generate();
            
            // Render
            GetComponent<DungeonRenderer>().Render(_currentGrid);

            // Place player
            PlacePlayer();
            
            // Spawn entities
            SpawnFloorEntities(floorNumber);

            // Update minimap
            GameManager.Instance.Ui.SetupMinimap(_currentGrid);
            
            // Fire event
            OnFloorChanged?.Invoke(floorNumber);
            
            Debug.Log($"Generated floor {floorNumber} for {_currentTemplate?.Name}");
        }
        
        public void GoToNextFloor()
        {
            CurrentFloor++;
            GenerateFloor(CurrentFloor);
            
            // Update GameManager
            GameManager.Instance.SetCurrentDungeon(_currentTemplate.Name, CurrentFloor);
        }
        
        public void ExitDungeon(bool success = false)
        {
            if (success)
            {
                Debug.Log($"Dungeon cleared successfully! Floors completed: {CurrentFloor}");
                OnDungeonCleared?.Invoke();
                
                // Award rewards
                AwardDungeonRewards();
            }
            
            // Clear dungeon
            ClearCurrentFloor();
            
            // Return to town
            GameManager.Instance.LoadScene("Town");
            
            // Reset state
            IsInDungeon = false;
            _currentTemplate = null;
            _currentGrid = null;
        }
        
        public void OnDungeonChanged(string dungeonName, int floor)
        {
            // Called when GameManager's session data is updated
            CurrentFloor = floor;
            
            // If we have a template matching this name, use it
            if (_currentTemplate != null && _currentTemplate.Name != dungeonName)
            {
                // Dungeon changed externally (e.g., from save)
                // We'd need to load the appropriate template
            }
        }
        
        // ===== DUNGEON OPERATIONS =====
        private void EnterDungeonMode()
        {
            Debug.Log("Entering dungeon mode");
            IsInDungeon = true;
            
            StartDungeon(_currentTemplate);

            // Activate dungeon-specific systems
            //if (_enemySpawner != null) _enemySpawner.SetActive(true);
            
            // Update UI
            GameManager.Instance.Ui.ShowDungeonUI();
        }
        
        private void ExitDungeonMode()
        {
            Debug.Log("Exiting dungeon mode");
            IsInDungeon = false;
            
            // Deactivate dungeon systems
            //if (_enemySpawner != null) _enemySpawner.SetActive(false);
            
            // Clear current dungeon
            ClearCurrentFloor();
        }
        
        private void SetupDungeonScene()
        {
            Debug.Log("Setting up dungeon scene");
            
            // Ensure player exists
            if (GameManager.Instance.SessionData.Player == null)
            {
                GameManager.Instance.SessionData.Player = GameObject.FindGameObjectWithTag("Player");
            }
            
            // Generate floor if we have a template
            if (_currentTemplate != null)
            {
                GenerateFloor(CurrentFloor);
            }
        }
        
        private void ConfigureGeneratorForFloor(int floor)
        {
            // Scale difficulty based on floor
            if (_generator != null)
            {
                // Example: Increase room count and enemy spawns every 5 floors
                _generator.RoomAttempts = 25 + (floor / 5) * 5;
                _generator.MinRoomSize = Mathf.Max(3, 5 - (floor / 10));
                _generator.MaxRoomSize = 8 + (floor / 7);
                
                // Set seed for reproducibility
                //_generator.Seed = (GameManager.Instance.SessionData.PlayTimeSeconds + floor).GetHashCode();
            }
        }
        
        private void SpawnFloorEntities(int floor)
        {
            if (_currentTemplate == null || _currentGrid == null) return;
            
            // Spawn items
            if (_currentTemplate.Items != null && _currentTemplate.Items.Count > 0)
            {
                SpawnItems(CalculateItemCount(floor));
            }
            
            // Spawn enemies
            if (_enemySpawner != null)
            {
                _enemySpawner.SpawnEnemiesForFloor(_currentGrid);
            }
            
            // Spawn special tiles
            SpawnEffectTiles(CalculateEffectTileCount(floor));
        }
        
        private void SpawnItems(int count)
        {
            if (_itemPrefab == null) return;
            
            int placed = 0;
            int attempts = 0;
            
            while (placed < count && attempts < 1000)
            {
                attempts++;
                int x = Random.Range(1, _currentGrid.Width - 1);
                int y = Random.Range(1, _currentGrid.Height - 1);
                
                if (_currentGrid.Tiles[x, y].Walkable && 
                    _currentGrid.Tiles[x, y].Occupant == null &&
                    _currentGrid.Tiles[x, y].Type == TileType.Floor)
                {
                    // Get random item from template
                    var itemData = _currentTemplate.Items[
                        UnityEngine.Random.Range(0, _currentTemplate.Items.Count)];
                    
                    // Spawn item
                    var itemObj = Instantiate(_itemPrefab, _entityContainer);
                    itemObj.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);
                    
                    // Initialize item entity
                    var itemEntity = itemObj.GetComponent<ItemEntity>();
                    if (itemEntity != null)
                    {
                        itemEntity.Initialize(itemData, _currentGrid, new Vector2Int(x, y));
                    }
                    
                    placed++;
                }
            }
            
            Debug.Log($"Spawned {placed} items");
        }
        
        private void SpawnEffectTiles(int count)
        {
            if (EffectTileManager.Instance == null || _currentGrid == null) return;
            
            int placed = 0;
            int attempts = 0;
            
            while (placed < count && attempts < 1000)
            {
                attempts++;
                int x = UnityEngine.Random.Range(1, _currentGrid.Width - 1);
                int y = UnityEngine.Random.Range(1, _currentGrid.Height - 1);
                
                if (_currentGrid.Tiles[x, y].Walkable && 
                    _currentGrid.Tiles[x, y].Occupant == null &&
                    _currentGrid.Tiles[x, y].Type == TileType.Floor)
                {
                    // Get random effect
                    var effect = EffectTileManager.Instance.GetRandomEffect();
                    if (effect != null)
                    {
                        _currentGrid.Tiles[x, y].SetTileEffect(effect);
                        _currentGrid.Tiles[x, y].Type = TileType.Effect;
                        placed++;
                    }
                }
            }
            
            Debug.Log($"Spawned {placed} effect tiles");
        }
        
        private void PlacePlayer()
        {
            Debug.LogWarning("placing player begun");
            Debug.LogWarning(GameManager.Instance.SessionData.Player.name);
            if (GameManager.Instance.SessionData.Player == null || _currentGrid == null) return;
            Debug.LogWarning("No Null. next get spawn point");
            // Get spawn point from grid
            var spawnPoint = _currentGrid.GetSpawnPoint();
            Debug.LogWarning(spawnPoint.x + ", " + spawnPoint.y);
            
            // Position player
            GameManager.Instance.SessionData.Player.transform.position = new Vector3(
                spawnPoint.x + 0.5f, 
                spawnPoint.y + 0.5f, 
                0
            );
            
            // Update player stats
            var playerStats = GameManager.Instance.SessionData.Player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.GridX = spawnPoint.x;
                playerStats.GridY = spawnPoint.y;
                playerStats.SetGrid(_currentGrid);
                
                // Update grid occupancy
                if (_currentGrid.InBounds(spawnPoint.x, spawnPoint.y))
                {
                    _currentGrid.Tiles[spawnPoint.x, spawnPoint.y].Occupant = playerStats;
                }
            }
            
            Debug.Log($"Player placed at {spawnPoint}");
        }
        
        private void ClearCurrentFloor()
        {
            if (_entityContainer == null) return;
            
            // Destroy all children
            for (int i = _entityContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(_entityContainer.GetChild(i).gameObject);
                }
            }
            
            // Clear grid
            if (_currentGrid != null)
            {
                // Clear tile occupants
                for (int x = 0; x < _currentGrid.Width; x++)
                {
                    for (int y = 0; y < _currentGrid.Height; y++)
                    {
                        _currentGrid.Tiles[x, y].Occupant = null;
                        _currentGrid.Tiles[x, y].ItemOnTile = false;
                    }
                }
            }
            
            Debug.Log("Cleared current floor");
        }
        
        private int CalculateItemCount(int floor)
        {
            // Scale item count with floor
            return Mathf.Max(10, 20 + (floor / 3));
        }
        
        private int CalculateEffectTileCount(int floor)
        {
            // Scale effect tiles with floor
            return Mathf.Max(3, 5 + (floor / 5));
        }
        
        private void AwardDungeonRewards()
        {
            if (_currentTemplate == null) return;
            
            // Award XP
            var playerStats = GameManager.Instance.SessionData.Player?.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                int xpReward = CurrentFloor * 100;
                playerStats.AddExperience(xpReward);
                Debug.Log($"Awarded {xpReward} XP for clearing dungeon");
            }
            
            // Award items
            var inventory = GameManager.Instance.Inventory;
            if (inventory != null && _currentTemplate.Items.Count > 0)
            {
                // Award 1-3 random items from the dungeon
                int itemCount = UnityEngine.Random.Range(1, 4);
                for (int i = 0; i < itemCount; i++)
                {
                    var randomItem = _currentTemplate.Items[
                        UnityEngine.Random.Range(0, _currentTemplate.Items.Count)];
                    inventory.AddItem(randomItem);
                }
            }
        }
        
        // ===== HELPER METHODS =====
        public Vector2Int GetPlayerGridPosition()
        {
            if (GameManager.Instance.SessionData.Player == null) return Vector2Int.zero;
            
            var playerStats = GameManager.Instance.SessionData.Player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                return new Vector2Int(playerStats.GridX, playerStats.GridY);
            }
            
            return Vector2Int.zero;
        }
        
        public bool IsTileWalkable(Vector2Int position)
        {
            if (_currentGrid == null) return false;
            return _currentGrid.Tiles[position.x, position.y].IsWalkable;
        }
        
        public void Reset()
        {
            CurrentFloor = 1;
            _currentTemplate = null;
            _currentGrid = null;
            ClearCurrentFloor();
        }
        
        // ===== EDITOR HELPERS =====
        [ContextMenu("Test Generate Floor")]
        private void TestGenerateFloor()
        {
            if (Application.isPlaying)
            {
                GenerateFloor(1);
            }
        }
    }
}

