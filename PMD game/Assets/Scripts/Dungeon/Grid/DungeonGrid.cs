using System.Collections.Generic;
using UnityEngine;

public class DungeonGrid
{
    public int Width { get; }
    public int Height { get; }
    public GridTile[,] Tiles;
    
    private List<RectInt> rooms;

    public DungeonGrid(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new GridTile[Width, Height];
        rooms = new List<RectInt>();
        
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tiles[x, y] = new GridTile { Type = TileType.Empty };
            }
        }
    }
    
    // Add this public method
    public List<RectInt> GetRooms()
    {
        return new List<RectInt>(rooms); // Return a copy
    }
    
    // Add this method to get room at position
    public RectInt? GetRoomAtPosition(int x, int y)
    {
        foreach (var room in rooms)
        {
            if (room.Contains(new Vector2Int(x, y)))
            {
                return room;
            }
        }
        return null;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public void AddRoom(RectInt room)
    {
        rooms.Add(room);
    }
    
    // Get spawn point (center of first room)
    public Vector2Int GetSpawnPoint()
    {
        if (rooms.Count > 0)
        {
            return Vector2Int.RoundToInt(rooms[0].center);
        }

        return new Vector2Int(Width / 2, Height / 2); // Fallback
    }

    public Vector2Int GetRandomFloorPosition()
    {
        List<Vector2Int> floorpositions = new List<Vector2Int>();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Tiles[x, y].Type == TileType.Floor && Tiles[x,y].Occupant == null)
                {
                    floorpositions.Add(new Vector2Int(x, y));
                }
            }
        }

        if (floorpositions.Count > 0)
        {
            return floorpositions[Random.Range(0, floorpositions.Count)];
        }

        return new Vector2Int(Width / 2, Height / 2);
    }
}
