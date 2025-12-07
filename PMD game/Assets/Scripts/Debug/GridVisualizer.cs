// GridDebugger.cs - Add to any GameObject in scene
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridDebugger : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator generator;
    public DungeonRenderer DungeonRenderer;
    
    [Header("Test Tiles")]
    public TileBase testFloorTile;
    public TileBase testWallTile;
    
    [Header("Visual Debug")]
    public bool drawGizmos = true;
    public Color floorColor = Color.green;
    public Color wallColor = Color.gray;
    public Color stairColor = Color.blue;
    public Color trapColor = Color.red;
    
    private DungeonGrid _currentGrid;
    
    void Start()
    {
        Debug.Log("=== GRID DEBUGGER STARTED ===");
        RunCompleteTest();
    }
    
    [ContextMenu("Run Complete Test")]
    void RunCompleteTest()
    {
        // Step 1: Generate grid
        if (generator == null)
        {
            generator = FindFirstObjectByType<DungeonGenerator>();
            if (generator == null)
            {
                Debug.LogError("No DungeonGenerator found in scene!");
                return;
            }
        }
        
        _currentGrid = generator.Grid;

        // Step 2: Count tiles
        AnalyzeGrid();
        
        // Step 3: Basic render (bypass complex systems)
        if (DungeonRenderer != null && testFloorTile != null && testWallTile != null)
        {
            SimpleRender();
        }
        else
        {
            Debug.LogWarning("Cannot render: Missing renderer or test tiles");
        }
        
        // Step 4: Log player spawn position
        Vector2Int spawn = _currentGrid.GetSpawnPoint();
    }
    
    void AnalyzeGrid()
    {
        if (_currentGrid == null || _currentGrid.Tiles == null)
        {
            Debug.LogError("Grid or tiles array is null!");
            return;
        }
        
        int floors = 0, walls = 0, stairs = 0, traps = 0, empty = 0;
        
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                var tile = _currentGrid.Tiles[x, y];
                if (tile == null)
                {
                    Debug.LogError($"Tile at {x},{y} is NULL!");
                    empty++;
                    continue;
                }
                
                switch (tile.Type)
                {
                    case TileType.Floor: floors++; break;
                    case TileType.Wall: walls++; break;
                    case TileType.Stairs: stairs++; break;
                    case TileType.Effect: traps++; break;
                    case TileType.Empty: empty++; break;
                }
            }
        }
        
        Debug.Log($"Tile Analysis - Total: {_currentGrid.Width * _currentGrid.Height}");
        Debug.Log($"  Floors: {floors}");
        Debug.Log($"  Walls: {walls}");
        Debug.Log($"  Stairs: {stairs}");
        Debug.Log($"  Events: {traps}");
        Debug.Log($"  Empty: {empty}");
        
        // Check for issues
        if (floors == 0) Debug.LogError("NO FLOOR TILES GENERATED!");
        if (walls == 0) Debug.LogError("NO WALL TILES GENERATED!");
    }
    
    void SimpleRender()
    {
        Debug.Log("Starting simple render...");
        
        if (DungeonRenderer.FloorTilemap == null)
        {
            Debug.LogError("FloorTilemap is null!");
            return;
        }
        
        if (DungeonRenderer.WallTilemap == null)
        {
            Debug.LogError("WallTilemap is null!");
            return;
        }
        
        // Clear tilemaps
        DungeonRenderer.FloorTilemap.ClearAllTiles();
        DungeonRenderer.WallTilemap.ClearAllTiles();
        
        int tilesPlaced = 0;
        
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                var tile = _currentGrid.Tiles[x, y];
                
                if (tile.Type == TileType.Wall)
                {
                    DungeonRenderer.WallTilemap.SetTile(pos, testWallTile);
                    tilesPlaced++;
                }
                else if (tile.Type != TileType.Empty)
                {
                    DungeonRenderer.FloorTilemap.SetTile(pos, testFloorTile);
                    tilesPlaced++;
                }
            }
        }
        
        Debug.Log($"Placed {tilesPlaced} tiles");
        
        // Force tilemap update
        DungeonRenderer.FloorTilemap.RefreshAllTiles();
        DungeonRenderer.WallTilemap.RefreshAllTiles();
    }
    
    void OnDrawGizmos()
    {
        if (!drawGizmos || _currentGrid == null || _currentGrid.Tiles == null) return;
        
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                var tile = _currentGrid.Tiles[x, y];
                if (tile == null) continue;
                
                Vector3 center = new Vector3(x + 0.5f, y + 0.5f, 0);
                Vector3 size = Vector3.one * 0.9f;
                
                // Set color based on tile type
                switch (tile.Type)
                {
                    case TileType.Floor:
                        Gizmos.color = floorColor;
                        Gizmos.DrawCube(center, size);
                        break;
                    case TileType.Wall:
                        Gizmos.color = wallColor;
                        Gizmos.DrawCube(center, size);
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(center, Vector3.one);
                        break;
                    case TileType.Stairs:
                        Gizmos.color = stairColor;
                        Gizmos.DrawSphere(center, 0.4f);
                        break;
                    case TileType.Effect:
                        Gizmos.color = trapColor;
                        Gizmos.DrawWireCube(center, Vector3.one * 0.6f);
                        Gizmos.DrawLine(
                            new Vector3(x + 0.2f, y + 0.2f, 0),
                            new Vector3(x + 0.8f, y + 0.8f, 0)
                        );
                        Gizmos.DrawLine(
                            new Vector3(x + 0.8f, y + 0.2f, 0),
                            new Vector3(x + 0.2f, y + 0.8f, 0)
                        );
                        break;
                }
            }
        }
        
        // Draw grid bounds
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            new Vector3(_currentGrid.Width / 2f, _currentGrid.Height / 2f, 0),
            new Vector3(_currentGrid.Width, _currentGrid.Height, 0)
        );
    }
}