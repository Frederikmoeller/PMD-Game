using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int Width = 64;
    public int Height = 40;
    public int RoomAttempts = 30;
    public int MinRoomSize = 4;
    public int MaxRoomSize = 10;
    public int Seed = 0;
    
    [HideInInspector] public DungeonGrid Grid;

    [ContextMenu("Generate")]
    public DungeonGrid Generate()
    {
        Debug.Log("=== DUNGEON GENERATION STARTED ===");
        if (Seed != 0) Random.InitState(Seed);
        Grid = new DungeonGrid(Width, Height);
        
        //Fill Walls
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Grid.Tiles[x, y].Type = TileType.Wall;
            }
        }

        List<RectInt> rooms = new List<RectInt>();

        for (int i = 0; i < RoomAttempts; i++)
        {
            int rw = Random.Range(MinRoomSize, MaxRoomSize + 1);
            int rh = Random.Range(MinRoomSize, MaxRoomSize + 1);
            int rx = Random.Range(1, Width - rw - 1);
            int ry = Random.Range(1, Height - rh - 1);

            RectInt room = new RectInt(rx, ry, rw, rh);
            bool overlaps = false;
            foreach (var r in rooms)
            {
                if (r.Overlaps(room))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps) continue;
            
            rooms.Add(room);
            Grid.AddRoom(room);
            
            // Carve room
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
                {
                    Grid.Tiles[x, y].Type = TileType.Floor;
                }
            }
        }
        
        // Connect rooms using simple corridor to center of previous room
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int a = Vector2Int.RoundToInt(rooms[i - 1].center);
            Vector2Int b = Vector2Int.RoundToInt(rooms[i].center);
            CarveCorridor(a, b);
        }

        if (rooms.Count > 0)
        {
            Vector2Int last = Vector2Int.RoundToInt(rooms[rooms.Count - 1].center);
            Grid.Tiles[last.x, last.y].Type = TileType.Stairs;
        }

        ScatterTiles(TileType.Effect, Mathf.Max(3, rooms.Count));
        
        Debug.Log($"Generated {rooms.Count} rooms");
        Debug.Log("=== DUNGEON GENERATION COMPLETE ===");


        return Grid;
    }

    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        Vector2Int cur = a;
        while (cur.x != b.x)
        {
            if (Grid.InBounds(cur.x, cur.y))
                Grid.Tiles[cur.x, cur.y].Type = TileType.Floor;
            cur.x += (b.x > cur.x) ? 1 : -1;
        }

        while (cur.y != b.y)
        {
            if (Grid.InBounds(cur.x, cur.y))
                Grid.Tiles[cur.x, cur.y].Type = TileType.Floor;
            cur.y += (b.y > cur.y) ? 1 : -1;
        }

        if (Grid.InBounds(b.x, b.y))
            Grid.Tiles[b.x, b.y].Type = TileType.Floor;
    }

    void ScatterTiles(TileType type, int count)
    {
        int tries = 0;
        int placed = 0;

        while (placed < count && tries < 1000)
        {
            tries++;
            int x = Random.Range(1, Width - 1);
            int y = Random.Range(1, Height - 1);
            if (Grid.Tiles[x, y].Type == TileType.Floor)
            {
                Grid.Tiles[x, y].Type = type;

                if (type == TileType.Effect && EffectTileManager.Instance != null)
                {
                    var randomEffect = EffectTileManager.Instance.GetRandomEffect();
                    if (randomEffect != null)
                    {
                        Grid.Tiles[x, y].SetTileEffect(randomEffect);
                    }
                }
                placed++;
            }
        }
    }
}
