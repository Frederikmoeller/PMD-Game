using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public class ItemEntity : MonoBehaviour
{
    public ItemData ItemData; // ScriptableObject with item info
    public SpriteRenderer spriteRenderer;

    private GridEntity _gridEntity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gridEntity = GetComponent<GridEntity>();
        _gridEntity.Type = EntityType.Item;
        
        // Setup visuals from ItemData
        if (ItemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = ItemData.Icon;
        }
    }

    public void OnPickedUp(GridEntity picker)
    {
        Debug.Log($"{picker.name} picked up {ItemData.ItemName}");
        
        // Add to inventory, apply effects, etc.
        
        // Remove from grid and destroy
        if (_gridEntity.Grid != null)
        {
            _gridEntity.Grid.Tiles[_gridEntity.GridX, _gridEntity.GridY].Occupant = null;
        }
        Destroy(gameObject);
    }
}
