using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystem
{
    public class InventoryItemUI : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private Button itemButton;
        
        private ItemData _itemData;
        private System.Action<ItemData> _onClickCallback;
        
        public void Initialize(ItemData itemData, int quantity, System.Action<ItemData> onClick)
        {
            _itemData = itemData;
            _onClickCallback = onClick;
            
            if (itemIcon != null && itemData.Icon != null)
                itemIcon.sprite = itemData.Icon;
            
            if (itemNameText != null)
                itemNameText.text = itemData.ItemName;
            
            if (quantityText != null)
                quantityText.text = quantity > 1 ? quantity.ToString() : "";
            
            if (itemButton != null)
                itemButton.onClick.AddListener(OnItemClicked);
        }
        
        private void OnItemClicked()
        {
            _onClickCallback?.Invoke(_itemData);
        }
    }
}