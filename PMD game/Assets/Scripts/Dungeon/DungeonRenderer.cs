using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public struct MaskTilePair
{
    public int Mask;
    public TileBase Tile;
}
public class DungeonRenderer : MonoBehaviour
{
    public DungeonGenerator Generator;
    public Tilemap FloorTilemap;
    public Tilemap WallTilemap;
    public TileBase DefaultFloor;
    public TileBase DefaultWall;
    [Tooltip("Map mask -> tile. Mask uses 8-bit layout: UL U UR L R DL D DR")]
    public MaskTilePair[] MaskTiles;
    private Dictionary<int, TileBase> _maskLookup;

    private void Awake() => BuildLookup();

    private void BuildLookup()
    {
        _maskLookup = new Dictionary<int, TileBase>();
        if (MaskTiles == null) return;
        foreach (var mtp in MaskTiles)
        {
            if (_maskLookup.ContainsKey(mtp.Mask) && mtp.Tile != null)
            {
                _maskLookup.Add(mtp.Mask, mtp.Tile);
            }
        }
    }

    [ContextMenu("Render")]
    public void Render()
    {
        if (Generator == null || Generator.Grid == null) 
        { 
            Debug.LogError("No generator/grid"); 
            return; 
        }
        Render(Generator.Grid);
    }

    public void Render(DungeonGrid grid)
    {
        Debug.Log($"Rendering grid: {grid.Width}x{grid.Height}");
    
        if (FloorTilemap == null) Debug.LogError("FloorTilemap is null!");
        if (WallTilemap == null) Debug.LogError("WallTilemap is null!");
        
        BuildLookup();
        FloorTilemap?.ClearAllTiles();
        WallTilemap?.ClearAllTiles();

        int floorTiles = 0;
        int wallTiles = 0;
    
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                var t = grid.Tiles[x, y];
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (t.Type is TileType.Floor or TileType.Stairs or TileType.Effect)
                {
                    FloorTilemap?.SetTile(pos, DefaultFloor);
    
                    // Debug effect tiles
                    if (t.Type == TileType.Effect)
                    {
                        Debug.Log($"Effect tile walkable: {Generator.Grid.Tiles[x,y].Type}");
                    }
    
                    floorTiles++;
                }
                else if (t.Type == TileType.Wall)
                {
                    int mask = GetMask(grid, x, y);
                    if (_maskLookup != null && _maskLookup.TryGetValue(mask, out var tile))
                    {
                        WallTilemap?.SetTile(pos, tile);
                    }
                    else
                    {
                        WallTilemap?.SetTile(pos, DefaultWall);
                    }
                    wallTiles++;
                }
            }
        }
        
    
        Debug.Log($"Rendered {floorTiles} floor tiles and {wallTiles} wall tiles");
    }
    
    public void Clear()
    {
        FloorTilemap.ClearAllTiles();
        WallTilemap.ClearAllTiles();
    }

    private int GetMask(DungeonGrid grid, int x, int y)
    {
        int mask = 0;
        bool ul = IsWalkable(grid, x - 1, y + 1);
        bool u  = IsWalkable(grid, x,     y + 1);
        bool ur = IsWalkable(grid, x + 1, y + 1);
        bool l  = IsWalkable(grid, x - 1, y);
        bool r  = IsWalkable(grid, x + 1, y);
        bool dl = IsWalkable(grid, x - 1, y - 1);
        bool d  = IsWalkable(grid, x,     y - 1);
        bool dr = IsWalkable(grid, x + 1, y - 1);

        if (ul) mask |= 1;
        if (u)  mask |= 2;
        if (ur) mask |= 4;
        if (l)  mask |= 8;
        if (r)  mask |= 16;
        if (dl) mask |= 32;
        if (d)  mask |= 64;
        if (dr) mask |= 128;

        return mask;
    }

    bool IsWalkable(DungeonGrid grid, int x, int y)
    {
        if (!grid.InBounds(x, y)) return false;
        return grid.Tiles[x, y].Walkable;
    }
}
