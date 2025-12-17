using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryItemUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemValue;
    public TextMeshProUGUI ItemDescription;
    public TextMeshProUGUI itemType;
    public Button useButton;
    
    private ItemData _itemData;
    
    public void Setup(ItemData item)
    {
        _itemData = item;

        if (itemIcon != null) itemIcon.sprite = item.Icon;
        if (ItemName != null) ItemName.text = item.ItemName;
        if (ItemDescription != null) ItemDescription.text = item.ItemDescription;
        if (itemType != null) itemType.text = item.Type.ToString();
        if (ItemValue != null) ItemValue.text = item.ItemValue.ToString();

        if (useButton != null)
            useButton.clicked += OnUseClicked;
    }
    
    void OnUseClicked()
    {
        // Use the item
        Debug.Log($"Using item: {_itemData.ItemName}");
        _itemData.ItemEffect.Invoke();
    }
}
