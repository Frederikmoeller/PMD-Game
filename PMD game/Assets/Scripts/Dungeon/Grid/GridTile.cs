using UnityEngine;

public enum TileType
{
    Floor,
    Wall,
    Effect,
    Stairs,
    Empty,
}

public class GridTile
{
    public TileType Type = TileType.Empty;
    public bool Walkable => Type is TileType.Floor or TileType.Stairs or TileType.Effect;
    public bool IsWalkable => Walkable && (Occupant == null || !Occupant.BlocksMovement);
    public bool ItemOnTile; // extend to concrete Item class
    public GridEntity Occupant = null; // player/enemy occupying tile
    public int X;
    public int Y;
    
    public EventTileEffect TileEffect { get; private set; }
    public bool IsStairs => Type == TileType.Stairs;
    
    public ItemEntity OccupyingItem { get; set; }

    public void SetTileEffect(EventTileEffect effect)
    {
        TileEffect = effect;
    }

    public void Clear()
    {
        TileEffect = null;
        Occupant = null;
    }
}
