using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject EnemyPrefab; // Assign different enemy prefabs here
    
    [Header("Spawn Settings")]
    public int MinEnemiesPerRoom = 1;
    public int MaxEnemiesPerRoom = 3;
    [Range(0f, 1f)]
    public float RoomSpawnChance = 0.7f;
    public int MinDistanceFromPlayer = 3;
    
    [Header("References")]
    public DungeonGenerator Generator;
    public DungeonTemplate DungeonTemplate;
    public Transform EnemyParent;
    
    private List<GameObject> _spawnedEnemies = new();
    
    public void SpawnEnemiesForFloor(DungeonGrid grid)
    {
        // Clear previous enemies
        ClearEnemies();
        
        if (Generator == null || grid == null || DungeonTemplate.Enemies.Count == 0)
        {
            Debug.LogWarning("Cannot spawn enemies: missing references!");
            return;
        }
        
        // Get player position (for distance check)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2Int playerPos = Vector2Int.zero;
        if (player != null)
        {
            var playerEntity = player.GetComponent<GridEntity>();
            if (playerEntity != null)
            {
                playerPos = new Vector2Int(playerEntity.GridX, playerEntity.GridY);
            }
        }
        
        // Get rooms from generator
        var rooms = grid.GetRooms(); // Use the public list
        
        int totalEnemies = 0;
        
        foreach (var room in rooms)
        {
            // Random chance to spawn enemies in this room
            if (Random.value > RoomSpawnChance) continue;
            
            // Determine how many enemies in this room
            int enemyCount = Random.Range(MinEnemiesPerRoom, MaxEnemiesPerRoom + 1);
            
            for (int i = 0; i < enemyCount; i++)
            {
                // Find a valid floor position in the room
                Vector2Int? spawnPos = FindValidSpawnPositionInRoom(room, grid, playerPos);
                
                if (spawnPos.HasValue)
                {
                    SpawnEnemyAtPosition(spawnPos.Value, grid);
                    totalEnemies++;
                }
            }
        }
        
        Debug.Log($"Spawned {totalEnemies} enemies across {rooms.Count} rooms");
    }
    
    private Vector2Int? FindValidSpawnPositionInRoom(RectInt room, DungeonGrid grid, Vector2Int playerPos)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        
        // Check all positions in the room
        for (int x = room.xMin + 1; x < room.xMax - 1; x++)
        {
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                // Skip if out of bounds
                if (!grid.InBounds(x, y)) continue;
                
                // Check if it's a walkable floor tile
                if (grid.Tiles[x, y].Type != TileType.Floor) continue;
                
                // Check if tile is already occupied
                if (grid.Tiles[x, y].Occupant != null) continue;
                
                // Check minimum distance from player
                int distFromPlayer = Mathf.Max(
                    Mathf.Abs(x - playerPos.x),
                    Mathf.Abs(y - playerPos.y)
                );
                
                if (distFromPlayer < MinDistanceFromPlayer) continue;
                
                validPositions.Add(new Vector2Int(x, y));
            }
        }
        
        if (validPositions.Count == 0) return null;
        
        // Return random valid position
        return validPositions[Random.Range(0, validPositions.Count)];
    }
    
    private void SpawnEnemyAtPosition(Vector2Int position, DungeonGrid grid)
    {
        if (DungeonTemplate.Enemies.Count == 0) return;

        CharacterPresetSO enemyPrefab = DungeonTemplate.Enemies[Random.Range(0, DungeonTemplate.Enemies.Count - 1)];
        // Create parent if needed
        if (EnemyParent == null)
        {
            GameObject parentObj = new GameObject("Enemies");
            EnemyParent = parentObj.transform;
        }
        
        // Instantiate enemy
        Vector3 worldPos = new Vector3(position.x + 0.5f, position.y + 0.5f, 0);
        GameObject enemy = Instantiate(EnemyPrefab, worldPos, Quaternion.identity, EnemyParent);

        // Set up GridEntity component
        var gridEntity = enemy.GetComponent<GridEntity>();
        if (gridEntity != null)
        {
            gridEntity.GridX = position.x;
            gridEntity.GridY = position.y;
            gridEntity.SetGrid(grid);
            gridEntity.CharacterPreset = enemyPrefab;
            
            // Mark tile as occupied
            grid.Tiles[position.x, position.y].Occupant = gridEntity;
        }
        
        // Set up Enemy component (if exists)
        var enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.InitializeFromPreset(DungeonTemplate.EnemyLevels[Random.Range(0, DungeonTemplate.EnemyLevels.Length)]);
        }

        enemy.name = gridEntity.Stats.Name;
        
        _spawnedEnemies.Add(enemy);
    }
    
    public void ClearEnemies()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null)
            {
                // Remove from grid
                var gridEntity = enemy.GetComponent<GridEntity>();
                if (gridEntity != null && gridEntity.Grid != null)
                {
                    if (gridEntity.Grid.InBounds(gridEntity.GridX, gridEntity.GridY))
                    {
                        gridEntity.Grid.Tiles[gridEntity.GridX, gridEntity.GridY].Occupant = null;
                    }
                }
                
                Destroy(enemy);
            }
        }
        
        _spawnedEnemies.Clear();
    }
}