using UnityEngine;

public class ItemEntity : MonoBehaviour
{
    public ItemData ItemData; // ScriptableObject with item info
    public SpriteRenderer SpriteRenderer;
    public Vector2Int GridPosition;

    private DungeonGrid _grid;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Initialize(ItemData itemData, DungeonGrid grid, Vector2Int position)
    {
        ItemData = itemData;
        _grid = grid;
        GridPosition = position;
        
        // Update grid tile
        if (_grid.InBounds(position.x, position.y))
        {
            _grid.Tiles[position.x, position.y].OccupyingItem = this;
        }
        
        // Set visuals
        if (SpriteRenderer != null && ItemData != null)
        {
            SpriteRenderer.sprite = ItemData.Icon;
        }
        
        // Position in world
        transform.position = new Vector3(position.x + 0.5f, position.y + 0.5f, 0);
    }

    public void OnPickedUp(GridEntity picker)
    {
        Debug.Log($"{picker.name} picked up {ItemData.ItemName}");
        
        // Remove from grid
        if (_grid != null && _grid.InBounds(GridPosition.x, GridPosition.y))
        {
            _grid.Tiles[GridPosition.x, GridPosition.y].OccupyingItem = null;
        }

        // Destroy the world object
        Destroy(gameObject);
    }
    
    // Simple check if something can pick this up
    public bool CanBePickedUpBy(GridEntity entity)
    {
        return entity != null && entity.Type is EntityType.Player or EntityType.Enemy;
    }
}
