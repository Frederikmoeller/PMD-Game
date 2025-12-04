using UnityEngine;


public enum EntityType
{
    Player,
    Enemy,      // Blocks movement
    Item,       // Doesn't block, triggers on step
    NPC,        // Maybe blocks, triggers on interact
    Decoration  // Doesn't block, no interaction
}
public class GridEntity : MonoBehaviour
{
    public int GridX;
    public int GridY;
    public DungeonGrid Grid;
    public EntityType Type = EntityType.Decoration; // Default
    public bool BlocksMovement => Type == EntityType.Enemy || Type == EntityType.NPC || Type == EntityType.Player;

    public void SetGrid(DungeonGrid g) => Grid = g;

    public virtual bool TryMove(int dx, int dy)
    {
        // Only work in grid-based mode
        if (GameManager.Instance?.IsGridBased != true) return false;
        if (Grid == null) return false;
        
        int nx = GridX + dx;
        int ny = GridY + dy;
        if (!Grid.InBounds(nx, ny)) return false;
        var target = Grid.Tiles[nx, ny];
        if (!target.Walkable) return false;
        
        // Check for occupant
        if (target.Occupant != null && target.Occupant.BlocksMovement)
        {
            return TryInteractWithOccupant(target.Occupant, dx, dy); // Attack/talk
        }

        // Move
        Grid.Tiles[GridX, GridY].Occupant = null;
        GridX = nx;
        GridY = ny;
        transform.position = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
        // Only set as occupant if we block movement
        if (BlocksMovement)
        {
            Grid.Tiles[GridX, GridY].Occupant = this;
        }
        HandleTileInteractions(target);
        return true;
    }
    
    private void HandleTileInteractions(GridTile tile)
    {
        // Effect tile interaction
        if (tile.Type == TileType.Effect && tile.TileEffect != null)
        {
            OnSteppedOnEffect(tile);
        }
    
        // Stair tile interaction  
        if (tile.Type == TileType.Stairs)
        {
            OnSteppedStairs();
        }
    
        // Item interaction (if tile has a non-blocking occupant)
        if (tile.Occupant != null && !tile.Occupant.BlocksMovement)
        {
            OnSteppedOnItem(tile.Occupant);
        }
    }

    protected virtual bool TryInteractWithOccupant(GridEntity occupant, int dx, int dy)
    {
        // By default, cannot move into occupied tile
        // Override in PlayerController for attacking enemies
        // Override in NPC for dialogue triggers
        return false;
    }


    protected virtual void OnSteppedOnEffect(GridTile tile)
    {
        Debug.Log($"{name} stepped on effect tile at {GridX},{GridY}");

        if (tile.TileEffect != null)
        {
            // Get all IEffectTileHandler components on this GameObject
            var handlers = GetComponents<IEffectTileHandler>();
            foreach (var handler in handlers)
            {
                handler.ApplyTileEffect(tile.TileEffect);
            }
            
            // Show feedback
            if (!string.IsNullOrEmpty(tile.TileEffect.ActivationMessage))
            {
                Debug.Log($"Effect: {tile.TileEffect.ActivationMessage}");
            }
            
            // Play sound if available
            if (tile.TileEffect.ActivationSound != null)
            {
                AudioSource.PlayClipAtPoint(tile.TileEffect.ActivationSound, transform.position);
            }
        }
    }

    protected virtual void OnSteppedStairs()
    {
        Debug.Log($"{name} stepped on stairs at {GridX},{GridY}");
        
        // If this is the player, go to next floor
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            var floorManager = FindFirstObjectByType<DungeonFloorManager>();
            if (floorManager != null)
            {
                floorManager.GoToNextFloor();
            }
        }
    }

    protected virtual void OnSteppedOnItem(object item)
    {
        Debug.Log($"{name} found an item at {GridX},{GridY}");
        // Pick up item logic
    }
}