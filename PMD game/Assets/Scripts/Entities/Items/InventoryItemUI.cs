using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryItemUi : MonoBehaviour
{
    public Image ItemIcon;
    public TextMeshProUGUI ItemName;
    public TextMeshProUGUI ItemValue;
    public TextMeshProUGUI ItemDescription;
    public TextMeshProUGUI ItemType;
    public Button UseButton;
    
    private ItemData _itemData;
    
    public void Setup(ItemData item)
    {
        _itemData = item;

        if (ItemIcon != null) ItemIcon.sprite = item.Icon;
        if (ItemName != null) ItemName.text = item.ItemName;
        if (ItemDescription != null) ItemDescription.text = item.ItemDescription;
        if (ItemType != null) ItemType.text = item.Type.ToString();
        if (ItemValue != null) ItemValue.text = item.ItemValue.ToString();

        if (UseButton != null)
            UseButton.clicked += OnUseClicked;
    }
    
    void OnUseClicked()
    {
        // Use the item
        Debug.Log($"Using item: {_itemData.ItemName}");
        _itemData.ItemEffect.Invoke();
    }
}
