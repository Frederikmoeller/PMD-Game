using System.Collections.Generic;
using UnityEngine;

public interface IInventoryHandler
{
    bool AddItem(ItemData item);
    bool RemoveItem(ItemData item);
    bool HasItem(ItemData item);
    int GetItemCount(ItemData item);
    List<ItemData> GetInventory();
}
