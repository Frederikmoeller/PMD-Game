using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryItemUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemCount;
    public Button useButton;
    
    private ItemData _itemData;
    
    public void Setup(ItemData item)
    {
        _itemData = item;
        
        if (itemName != null) itemName.text = item.ItemName;
        if (itemCount != null) itemCount.text = "x1"; // You'd track quantities
        
        if (useButton != null)
            useButton.clicked += OnUseClicked;
    }
    
    void OnUseClicked()
    {
        // Use the item
        Debug.Log($"Using item: {_itemData.ItemName}");
        // Item usage logic here
    }
}
