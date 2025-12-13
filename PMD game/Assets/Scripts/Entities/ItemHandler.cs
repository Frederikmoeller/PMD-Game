using UnityEngine;

public class ItemHandler : MonoBehaviour
{
    public ItemData HeldItem;
    
    public bool PickUpItem(ItemEntity item)
    {
        if (item == null) return false;
        
        HeldItem = item.ItemData;
        Debug.Log($"Picked up {item.ItemData.ItemName}");
        
        // Remove item from world
        Destroy(item.gameObject);
        return true;
    }
    
    public void DropItem()
    {
        if (HeldItem == null) return;
        
        Debug.Log($"Dropped {HeldItem.ItemName}");
        // Spawn item in world
        // ItemManager.Instance.SpawnItem(transform.position, HeldItem);
        HeldItem = null;
    }
}
