using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapSystem : MonoBehaviour
{
    [Header("Minimap Display")]
    public RawImage MinimapDisplay;
    public RectTransform MinimapContainer;
    
    [Header("Minimap Settings")]
    public int PixelsPerTile = 4;
    public Color WallColor = Color.gray;
    public Color FloorColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
    public Color UnexploredColor = Color.black;
    public Color PlayerColor = Color.green;
    public Color EnemyColor = Color.red;
    public Color StairsColor = Color.cyan;
    public Color EffectColor = Color.blue;
    public Color ItemColor = Color.yellow;
    
    [Header("Display Settings")]
    public float MaxDisplaySize = 400f;
    public bool MaintainAspectRatio = true;
    public Vector2 FixedDisplaySize = new Vector2(300, 200);
    
    [Header("Fog of War")]
    public bool UseFogOfWar = true;
    public Color ExploredFloorColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public int VisionRadius = 5;
    
    [Header("Outline Settings")]
    public bool DrawWallOutlines = true;
    public Color WallOutlineColor = Color.white;
    public int OutlineThickness = 1;

    [Header("Optimization")] 
    public bool IncrementalUpdates = true;
    public int UpdateBatchSize = 100;

    [Header("Room Discorvery")] 
    public bool AutoRevealsRooms = true;
    public Color DiscoveredRoomColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    
    private Texture2D _minimapTexture;
    private bool[,] _exploredTiles;
    private bool[,] _visibleTiles;
    private bool[,] _dirtyTiles;
    private HashSet<RectInt> _discoveredRooms = new HashSet<RectInt>();
    private DungeonGrid _currentGrid;
    private Vector2Int _lastPlayerPos;
    private Queue<Vector2Int> _updateQueue = new();
    private bool _isUpdating = false;
    private Vector2Int _currentPlayerPos;
    private bool _playerMarkerNeedsUpdate = false;

    public void Initialize()
    {
        if (MinimapDisplay == null)
        {
            Debug.LogWarning("Minimap display RawImage not set!");
            return;
        }
        
        Clear();
    }
    
    public void GenerateMinimap(DungeonGrid grid)
    {
        if (grid == null || MinimapDisplay == null) return;
        
        _currentGrid = grid;
        
        // Initialize arrays
        _exploredTiles = new bool[grid.Width, grid.Height];
        _visibleTiles = new bool[grid.Width, grid.Height];
        _dirtyTiles = new bool[grid.Width, grid.Height];
        _updateQueue.Clear();
        
        // Calculate texture size
        int texWidth = grid.Width * PixelsPerTile;
        int texHeight = grid.Height * PixelsPerTile;
        
        // Create texture
        if (_minimapTexture == null || _minimapTexture.width != texWidth || _minimapTexture.height != texHeight)
        {
            if (_minimapTexture != null)
                Object.Destroy(_minimapTexture);
            
            _minimapTexture = new Texture2D(texWidth, texHeight);
            _minimapTexture.filterMode = FilterMode.Point;
            _minimapTexture.wrapMode = TextureWrapMode.Clamp;
        }
        
        // Fill with unexplored color
        Color[] fillColors = new Color[texWidth * texHeight];
        for (int i = 0; i < fillColors.Length; i++)
            fillColors[i] = UnexploredColor;
        
        _minimapTexture.SetPixels(fillColors);
        _minimapTexture.Apply();
        
        MinimapDisplay.texture = _minimapTexture;
        
        // Size container
        if (MinimapContainer != null)
            SizeMinimapContainer(texWidth, texHeight);
        
        Debug.Log($"Minimap created: {texWidth}x{texHeight}");
    }
    
    private void CheckRoomDiscovery(Vector2Int playerPos)
    {
        if (!AutoRevealsRooms) return;
    
        //Debug.Log($"=== ROOM DISCOVERY CHECK at {playerPos} ===");
    
        // First, let's see if we can get rooms at all
        try
        {
            // Try to get rooms
            List<RectInt> rooms = _currentGrid.GetRooms();
        
            if (rooms == null)
            {
                Debug.LogError("GetRooms() returned null!");
                return;
            }
        
            //Debug.Log($"Found {rooms.Count} rooms in dungeon");
        
            // Log all rooms for debugging
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                //Debug.Log($"Room {i}: {room.xMin},{room.yMin} to {room.xMax},{room.yMax} (size: {room.width}x{room.height})");
            }
        
            // Check each room
            foreach (var room in rooms)
            {
                // Debug the exact check
                bool xInRange = playerPos.x >= room.xMin && playerPos.x < room.xMax;
                bool yInRange = playerPos.y >= room.yMin && playerPos.y < room.yMax;
                bool alreadyDiscovered = _discoveredRooms.Contains(room);
            
                /*Debug.Log($"Checking room {room.xMin},{room.yMin}-{room.xMax},{room.yMax}: " +
                          $"xInRange={xInRange} ({playerPos.x} in [{room.xMin},{room.xMax})), " +
                          $"yInRange={yInRange} ({playerPos.y} in [{room.yMin},{room.yMax})), " +
                          $"alreadyDiscovered={alreadyDiscovered}");*/
            
                if (xInRange && yInRange && !alreadyDiscovered)
                {
                    Debug.Log($"SUCCESS: Player is in undiscovered room!");
                    DiscoverRoom(room);
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CheckRoomDiscovery: {e.Message}\n{e.StackTrace}");
        }
    }

    private void DiscoverRoom(RectInt room)
    {
        Debug.Log($"Discovered room: {room.xMin},{room.yMin} to {room.xMax},{room.yMax}");

        // Add to discovered rooms
        _discoveredRooms.Add(room);

        // Mark all floor tiles in the room as explored
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                if (_currentGrid.InBounds(x, y))
                {
                    // Only explore floor tiles within the room
                    TileType tileType = _currentGrid.Tiles[x, y].Type;
                    if (tileType == TileType.Floor || tileType == TileType.Stairs || tileType == TileType.Effect)
                    {
                        if (!_exploredTiles[x, y])
                        {
                            _exploredTiles[x, y] = true;
                            MarkTileDirty(x, y);
                        }
                    }
                }
            }
        }

        // Also explore walls immediately surrounding the room
        ExploreRoomWalls(room);
    }

    private void ExploreRoomWalls(RectInt room)
    {
        // Explore walls that border the room (for outlines)
        // Check one tile outside the room in all directions

        // Top and bottom edges
        for (int x = room.xMin - 1; x <= room.xMax; x++)
        {
            // Top edge (y = room.yMax)
            if (_currentGrid.InBounds(x, room.yMax) && _currentGrid.Tiles[x, room.yMax].Type == TileType.Wall)
            {
                if (!_exploredTiles[x, room.yMax])
                {
                    _exploredTiles[x, room.yMax] = true;
                    MarkTileDirty(x, room.yMax);
                }
            }

            // Bottom edge (y = room.yMin - 1)
            if (_currentGrid.InBounds(x, room.yMin - 1) && _currentGrid.Tiles[x, room.yMin - 1].Type == TileType.Wall)
            {
                if (!_exploredTiles[x, room.yMin - 1])
                {
                    _exploredTiles[x, room.yMin - 1] = true;
                    MarkTileDirty(x, room.yMin - 1);
                }
            }
        }

        // Left and right edges
        for (int y = room.yMin - 1; y <= room.yMax; y++)
        {
            // Left edge (x = room.xMin - 1)
            if (_currentGrid.InBounds(room.xMin - 1, y) && _currentGrid.Tiles[room.xMin - 1, y].Type == TileType.Wall)
            {
                if (!_exploredTiles[room.xMin - 1, y])
                {
                    _exploredTiles[room.xMin - 1, y] = true;
                    MarkTileDirty(room.xMin - 1, y);
                }
            }

            // Right edge (x = room.xMax)
            if (_currentGrid.InBounds(room.xMax, y) && _currentGrid.Tiles[room.xMax, y].Type == TileType.Wall)
            {
                if (!_exploredTiles[room.xMax, y])
                {
                    _exploredTiles[room.xMax, y] = true;
                    MarkTileDirty(room.xMax, y);
                }
            }
        }
    }


    private void SizeMinimapContainer(int textureWidth, int textureHeight)
    {
        if (MinimapContainer == null || MinimapContainer.parent == null) return;
    
        RectTransform parentRect = MinimapContainer.parent as RectTransform;
        if (parentRect == null) return;
    
        Vector2 parentSize = parentRect.rect.size;
        if (parentSize.x <= 0 || parentSize.y <= 0)
        {
            parentSize = new Vector2(
                Mathf.Abs(parentRect.offsetMin.x) + Mathf.Abs(parentRect.offsetMax.x),
                Mathf.Abs(parentRect.offsetMin.y) + Mathf.Abs(parentRect.offsetMax.y)
            );
        }
    
        float padding = 10f;
        float maxWidth = parentSize.x - padding * 2;
        float maxHeight = parentSize.y - padding * 2;
    
        float widthRatio = maxWidth / textureWidth;
        float heightRatio = maxHeight / textureHeight;
        float scale = Mathf.Min(widthRatio, heightRatio);
    
        float displayWidth = textureWidth * scale;
        float displayHeight = textureHeight * scale;
    
        MinimapContainer.sizeDelta = new Vector2(displayWidth, displayHeight);
        MinimapContainer.anchoredPosition = Vector2.zero;
    }
    
    public void UpdatePlayerPosition(Vector2Int playerPos)
    {
        if (_currentGrid == null || _minimapTexture == null) return;
        
        // Store the new player position
        _currentPlayerPos = playerPos;
        _playerMarkerNeedsUpdate = true;
        
        // Update exploration
        UpdateExploration(playerPos);
        
        // Process updates
        if (IncrementalUpdates)
        {
            ProcessDirtyTiles();
        }
        else
        {
            RedrawMinimap();
        }
        
        // Draw player marker AFTER processing dirty tiles
        DrawPlayerMarker();
    }
    
    private void UpdateExploration(Vector2Int center)
    {
        if (!UseFogOfWar) return;
        CheckRoomDiscovery(center);
        
        HashSet<Vector2Int> changedTiles = new HashSet<Vector2Int>();
        
        // Clear current visibility
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                if (_visibleTiles[x, y])
                {
                    _visibleTiles[x, y] = false;
                    MarkTileDirty(x, y);
                }
            }
        }
        
        // Mark tiles in vision radius
        int radius = VisionRadius;
        
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                if (!_currentGrid.InBounds(x, y)) continue;
                
                float distance = Vector2Int.Distance(center, new Vector2Int(x, y));
                if (!(distance <= radius)) continue;

                if (!HasLineOfSight(center, new Vector2Int(x, y))) continue;
                
                // Check if exploration state changed
                if (_exploredTiles[x, y] && _visibleTiles[x, y]) continue;
                
                _exploredTiles[x, y] = true;
                _visibleTiles[x, y] = true;
                MarkTileDirty(x, y);
                                
                // Also mark adjacent walls for outline updates
                MarkAdjacentTilesDirty(x, y);
            }
        }
    }
    
    private void MarkTileDirty(int x, int y)
    {
        if (!_currentGrid.InBounds(x, y)) return;

        if (_dirtyTiles[x, y]) return;
        _dirtyTiles[x, y] = true;
        _updateQueue.Enqueue(new Vector2Int(x, y));
    }
    
    private void MarkAdjacentTilesDirty(int x, int y)
    {
        // Mark adjacent tiles (for wall outline updates)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int nx = x + dx;
                int ny = y + dy;
                
                if (_currentGrid.InBounds(nx, ny))
                {
                    MarkTileDirty(nx, ny);
                }
            }
        }
    }
    
    private void ProcessDirtyTiles()
    {
        if (_isUpdating || _updateQueue.Count == 0) return;
        _isUpdating = true;
        
        // Clear old player marker before redrawing tiles
        ClearPlayerMarker(_lastPlayerPos);
        
        int processed = 0;
        while (_updateQueue.Count > 0 && processed < UpdateBatchSize)
        {
            Vector2Int tilePos = _updateQueue.Dequeue();
            _dirtyTiles[tilePos.x, tilePos.y] = false;
            
            DrawSingleTile(tilePos.x, tilePos.y);
            processed++;
        }
        
        // Apply texture changes
        _minimapTexture.Apply();
        
        _isUpdating = false;
        _lastPlayerPos = _currentPlayerPos;
        
        // If there are more tiles to process, continue next frame
        if (_updateQueue.Count > 0)
        {
            ProcessDirtyTiles();
        }
    }


    private void DrawSingleTile(int gridX, int gridY)
    {
        TileType tileType = _currentGrid.Tiles[gridX, gridY].Type;
        bool isWall = tileType == TileType.Wall;
        bool isExplored = UseFogOfWar ? _exploredTiles[gridX, gridY] : true;
        bool isVisible = UseFogOfWar ? _visibleTiles[gridX, gridY] : true;
        
        Color tileColor = GetTileColor(gridX, gridY);
        
        // Draw the tile
        for (int px = 0; px < PixelsPerTile; px++)
        {
            for (int py = 0; py < PixelsPerTile; py++)
            {
                int pixelX = gridX * PixelsPerTile + px;
                int pixelY = gridY * PixelsPerTile + py;
                
                Color finalColor = tileColor;
                
                if (DrawWallOutlines && isWall && isExplored)
                {
                    if (ShouldDrawOutline(gridX, gridY, px, py))
                    {
                        finalColor = isVisible ? WallOutlineColor : WallOutlineColor * 0.5f;
                    }
                }
                
                _minimapTexture.SetPixel(pixelX, pixelY, finalColor);
            }
        }
    }
    
    private void ClearPlayerMarker(Vector2Int playerPos)
    {
        if (!_currentGrid.InBounds(playerPos.x, playerPos.y)) return;
        
        // Instead of marking the tile dirty, directly restore the original pixels
        // This is faster and more precise
        int centerX = playerPos.x * PixelsPerTile + PixelsPerTile / 2;
        int centerY = playerPos.y * PixelsPerTile + PixelsPerTile / 2;
        int markerSize = Mathf.Max(1, PixelsPerTile / 2);
        
        // Restore the original pixels that were under the marker
        RestoreOriginalPixelsUnderMarker(centerX, centerY, markerSize);
        
        // Also mark the tile as dirty so it gets properly redrawn
        MarkTileDirty(playerPos.x, playerPos.y);
    }
    
    private Dictionary<Vector2Int, Color> _savedPixels = new Dictionary<Vector2Int, Color>();
    
    private void SaveOriginalPixelsUnderMarker(int centerX, int centerY, int markerSize)
    {
        _savedPixels.Clear();
        
        // Save pixels where we'll draw the marker
        for (int i = -markerSize; i <= markerSize; i++)
        {
            // Horizontal line
            int x = centerX + i;
            int y = centerY;
            if (x >= 0 && x < _minimapTexture.width && y >= 0 && y < _minimapTexture.height)
            {
                _savedPixels[new Vector2Int(x, y)] = _minimapTexture.GetPixel(x, y);
            }
            
            // Vertical line (skip the center since we already saved it)
            if (i != 0)
            {
                x = centerX;
                y = centerY + i;
                if (x >= 0 && x < _minimapTexture.width && y >= 0 && y < _minimapTexture.height)
                {
                    _savedPixels[new Vector2Int(x, y)] = _minimapTexture.GetPixel(x, y);
                }
            }
        }
    }
    
    private void RestoreOriginalPixelsUnderMarker(int centerX, int centerY, int markerSize)
    {
        foreach (var kvp in _savedPixels)
        {
            if (kvp.Key.x >= 0 && kvp.Key.x < _minimapTexture.width && 
                kvp.Key.y >= 0 && kvp.Key.y < _minimapTexture.height)
            {
                _minimapTexture.SetPixel(kvp.Key.x, kvp.Key.y, kvp.Value);
            }
        }
        
        _savedPixels.Clear();
        _minimapTexture.Apply();
    }
    
    private bool HasLineOfSight(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        int sx = from.x < to.x ? 1 : -1;
        int sy = from.y < to.y ? 1 : -1;
        int err = dx - dy;
        
        int x = from.x;
        int y = from.y;
        
        while (true)
        {
            // Stop if we hit a wall (but mark the wall as visible)
            if (!_currentGrid.InBounds(x, y))
                return false;
            
            if (x == to.x && y == to.y)
                return true;
            
            // Check current tile
            if (_currentGrid.Tiles[x, y].Type == TileType.Wall)
            {
                // Walls block LOS, but we return true if this IS the target tile
                // (so you can see walls right next to you)
                return (x == to.x && y == to.y);
            }
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
    
    private void RedrawMinimap()
    {
        if (_currentGrid == null || _minimapTexture == null) return;
        
        // Redraw all tiles
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                DrawSingleTile(x, y);
            }
        }
        
        _minimapTexture.Apply();
    }
    
    private void DrawTile(int gridX, int gridY)
    {
        TileType tileType = _currentGrid.Tiles[gridX, gridY].Type;
        bool isWall = tileType == TileType.Wall;
        bool isExplored = !UseFogOfWar || _exploredTiles[gridX, gridY];
        bool isVisible = !UseFogOfWar || _visibleTiles[gridX, gridY];
        
        // Get base color
        Color tileColor = GetTileColor(gridX, gridY);
        
        // DEBUG: Log wall state
        if (isWall && gridX == 10 && gridY == 10)
        {
            Debug.Log($"Drawing wall at {gridX},{gridY}: explored={isExplored}, visible={isVisible}, color={tileColor}");
        }
        
        // Draw the tile
        for (int px = 0; px < PixelsPerTile; px++)
        {
            for (int py = 0; py < PixelsPerTile; py++)
            {
                int pixelX = gridX * PixelsPerTile + px;
                int pixelY = gridY * PixelsPerTile + py;
                
                Color finalColor = tileColor;
                
                // Check if this pixel should be an outline
                if (DrawWallOutlines && isWall && isExplored)
                {
                    // Only draw outlines if adjacent to explored floor
                    if (ShouldDrawOutline(gridX, gridY, px, py))
                    {
                        if (isVisible)
                        {
                            finalColor = WallOutlineColor;
                        }
                        else
                        {
                            // In fog of war (explored but not visible)
                            finalColor = WallOutlineColor * 0.5f;
                        }
                    }
                }
                
                _minimapTexture.SetPixel(pixelX, pixelY, finalColor);
            }
        }
    }
    
    private bool ShouldDrawOutline(int gridX, int gridY, int pixelX, int pixelY)
    {
        // This wall must be explored to have outlines
        if (!_exploredTiles[gridX, gridY]) return false;
        
        // Check which edge(s) this pixel is on
        bool onLeftEdge = pixelX < OutlineThickness;
        bool onRightEdge = pixelX >= PixelsPerTile - OutlineThickness;
        bool onBottomEdge = pixelY < OutlineThickness;
        bool onTopEdge = pixelY >= PixelsPerTile - OutlineThickness;
        
        // Check each direction
        if (onLeftEdge && IsExploredFloorTile(gridX - 1, gridY)) return true;
        if (onRightEdge && IsExploredFloorTile(gridX + 1, gridY)) return true;
        if (onBottomEdge && IsExploredFloorTile(gridX, gridY - 1)) return true;
        if (onTopEdge && IsExploredFloorTile(gridX, gridY + 1)) return true;
        
        // For corners
        if ((onLeftEdge && onBottomEdge) && 
            (IsExploredFloorTile(gridX - 1, gridY) || IsExploredFloorTile(gridX, gridY - 1)))
            return true;
        
        if ((onLeftEdge && onTopEdge) && 
            (IsExploredFloorTile(gridX - 1, gridY) || IsExploredFloorTile(gridX, gridY + 1)))
            return true;
        
        if ((onRightEdge && onBottomEdge) && 
            (IsExploredFloorTile(gridX + 1, gridY) || IsExploredFloorTile(gridX, gridY - 1)))
            return true;
        
        if ((onRightEdge && onTopEdge) && 
            (IsExploredFloorTile(gridX + 1, gridY) || IsExploredFloorTile(gridX, gridY + 1)))
            return true;
        
        return false;
    }
    
    private bool IsExploredFloorTile(int x, int y)
    {
        if (!_currentGrid.InBounds(x, y)) return false;
        
        // Must be explored
        if (UseFogOfWar && !_exploredTiles[x, y]) return false;
        
        // Must be a floor-type tile
        TileType type = _currentGrid.Tiles[x, y].Type;
        return type == TileType.Floor || type == TileType.Stairs || type == TileType.Effect;
    }
    
    // Keep the original IsFloorTile for other uses
    private bool IsFloorTile(int x, int y)
    {
        if (!_currentGrid.InBounds(x, y)) return false;
        
        TileType type = _currentGrid.Tiles[x, y].Type;
        return type == TileType.Floor || type == TileType.Stairs || type == TileType.Effect;
    }
    
    private Color GetTileColor(int x, int y)
    {
        if (!_currentGrid.InBounds(x, y))
            return UnexploredColor;
    
        TileType tileType = _currentGrid.Tiles[x, y].Type;
        bool isExplored = UseFogOfWar ? _exploredTiles[x, y] : true;
        bool isVisible = UseFogOfWar ? _visibleTiles[x, y] : true;
    
        if (!isExplored)
            return UnexploredColor;
    
        // Check if this tile is in a discovered room
        bool inDiscoveredRoom = IsInDiscoveredRoom(x, y);

        // Currently visible
        switch (tileType)
        {
            case TileType.Floor:
                return FloorColor;
            case TileType.Wall:
                return WallColor;
            case TileType.Stairs:
                return StairsColor;
            case TileType.Effect:
                return EffectColor;
            default:
                return Color.black;
        }
    }

    
    private void DrawPlayerMarker()
    {
        if (!_playerMarkerNeedsUpdate || !_currentGrid.InBounds(_currentPlayerPos.x, _currentPlayerPos.y)) 
            return;
        
        // Don't draw if we're currently updating tiles
        if (_isUpdating) return;
        
        int centerX = _currentPlayerPos.x * PixelsPerTile + PixelsPerTile / 2;
        int centerY = _currentPlayerPos.y * PixelsPerTile + PixelsPerTile / 2;
        int markerSize = Mathf.Max(1, PixelsPerTile / 2);
        
        // Save the original colors under where we'll draw the marker
        // This ensures we can restore them later
        SaveOriginalPixelsUnderMarker(centerX, centerY, markerSize);
        
        // Draw a plus sign
        for (int i = -markerSize; i <= markerSize; i++)
        {
            if (centerX + i >= 0 && centerX + i < _minimapTexture.width)
                _minimapTexture.SetPixel(centerX + i, centerY, PlayerColor);
            
            if (centerY + i >= 0 && centerY + i < _minimapTexture.height)
                _minimapTexture.SetPixel(centerX, centerY + i, PlayerColor);
        }
        
        _minimapTexture.Apply();
        _playerMarkerNeedsUpdate = false;
    }

    
    public void RevealRoom(RectInt room)
    {
        if (!UseFogOfWar || _currentGrid == null) return;
        
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                if (_currentGrid.InBounds(x, y))
                {
                    _exploredTiles[x, y] = true;
                    MarkTileDirty(x, y);
                }
            }
        }
        
        ProcessDirtyTiles();
    }
    
    public void RevealAll()
    {
        if (!UseFogOfWar || _currentGrid == null) return;
        
        for (int x = 0; x < _currentGrid.Width; x++)
        {
            for (int y = 0; y < _currentGrid.Height; y++)
            {
                _exploredTiles[x, y] = true;
                _visibleTiles[x, y] = true;
                MarkTileDirty(x, y);
            }
        }
        
        ProcessDirtyTiles();
    }
    
    public void Clear()
    {
        if (_minimapTexture != null)
        {
            Object.Destroy(_minimapTexture);
            _minimapTexture = null;
        }
        
        if (MinimapDisplay != null)
        {
            MinimapDisplay.texture = null;
        }
        
        _currentGrid = null;
        _exploredTiles = null;
        _visibleTiles = null;
        _dirtyTiles = null;
        _updateQueue.Clear();
    }
    
    public void ToggleVisibility(bool visible)
    {
        if (MinimapContainer != null)
        {
            MinimapContainer.gameObject.SetActive(visible);
        }
    }
    
    public void SetDisplaySize(float size)
    {
        MaxDisplaySize = size;
        if (_minimapTexture != null && MinimapContainer != null)
        {
            SizeMinimapContainer(_minimapTexture.width, _minimapTexture.height);
        }
    }
    
    public void SetZoomLevel(int newPixelsPerTile)
    {
        PixelsPerTile = Mathf.Clamp(newPixelsPerTile, 1, 8);
        if (_currentGrid != null)
        {
            GenerateMinimap(_currentGrid);
            UpdatePlayerPosition(_lastPlayerPos);
        }
    }
    private bool IsInDiscoveredRoom(int x, int y)
    {
        foreach (var room in _discoveredRooms)
        {
            // Check if the point is within the room bounds (including a 1-tile border for walls)
            if (x >= room.xMin - 1 && x < room.xMax + 1 && 
                y >= room.yMin - 1 && y < room.yMax + 1)
            {
                return true;
            }
        }
        return false;
    }
}