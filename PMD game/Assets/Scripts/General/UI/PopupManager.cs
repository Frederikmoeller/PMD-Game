using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystem
{
    public class PopupManager : MonoBehaviour
    {
        [Header("Popup References")]
        [SerializeField] private GameObject confirmationPopup;
        [SerializeField] private GameObject messagePopup;
        [SerializeField] private GameObject inventoryPopup;
        [SerializeField] private GameObject questLogPopup;
        
        [Header("Confirmation Popup")]
        [SerializeField] private TextMeshProUGUI confirmationTitle;
        [SerializeField] private TextMeshProUGUI confirmationMessage;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Message Popup")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float messageDuration = 3f;
        
        [Header("Inventory UI")]
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventoryItemPrefab;
        [SerializeField] private TextMeshProUGUI inventoryCapacityText;
        
        [Header("Equipment UI")]
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Image armorIcon;
        [SerializeField] private Image helmetIcon;
        [SerializeField] private Image bootsIcon;
        [SerializeField] private Image accessory1Icon;
        [SerializeField] private Image accessory2Icon;
        
        [Header("Quest Log UI")]
        [SerializeField] private Transform questLogContent;
        [SerializeField] private GameObject questEntryPrefab;
        
        // State
        private PlayerStats _playerStats;
        private Queue<string> _messageQueue = new Queue<string>();
        private bool _isShowingMessage = false;
        
        public void Initialize()
        {
            // Initialize popups as hidden
            if (confirmationPopup != null) confirmationPopup.SetActive(false);
            if (messagePopup != null) messagePopup.SetActive(false);
            if (inventoryPopup != null) inventoryPopup.SetActive(false);
            if (questLogPopup != null) questLogPopup.SetActive(false);
        }
        
        public void Initialize(PlayerStats playerStats)
        {
            _playerStats = playerStats;
            Initialize();
        }
        
        // ===== CONFIRMATION POPUP =====
        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
            if (confirmationPopup == null) return;
            
            confirmationPopup.SetActive(true);
            
            if (confirmationTitle != null) confirmationTitle.text = title;
            if (confirmationMessage != null) confirmationMessage.text = message;
            
            // Clear previous listeners
            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            
            // Add new listeners
            confirmButton.onClick.AddListener(() =>
            {
                confirmationPopup.SetActive(false);
                onConfirm?.Invoke();
            });
            
            cancelButton.onClick.AddListener(() =>
            {
                confirmationPopup.SetActive(false);
                onCancel?.Invoke();
            });
        }
        
        // ===== MESSAGE POPUP =====
        public void ShowMessage(string message, float duration = 3f)
        {
            _messageQueue.Enqueue(message);
            
            if (!_isShowingMessage)
            {
                StartCoroutine(ShowMessageCoroutine(duration));
            }
        }
        
        private System.Collections.IEnumerator ShowMessageCoroutine(float duration)
        {
            _isShowingMessage = true;
            
            while (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();
                
                if (messagePopup != null && messageText != null)
                {
                    messagePopup.SetActive(true);
                    messageText.text = message;
                    
                    yield return new WaitForSeconds(duration);
                    
                    messagePopup.SetActive(false);
                }
                
                yield return null;
            }
            
            _isShowingMessage = false;
        }
        
        // ===== INVENTORY =====
        public void ShowInventory(bool show)
        {
            if (inventoryPopup != null)
            {
                inventoryPopup.SetActive(show);
                
                if (show)
                {
                    RefreshInventoryUI();
                }
            }
        }
        
        public bool ToggleInventory()
        {
            if (inventoryPopup == null) return false;
            
            bool isOpen = !inventoryPopup.activeSelf;
            inventoryPopup.SetActive(isOpen);
            
            if (isOpen)
            {
                RefreshInventoryUI();
            }
            
            return isOpen;
        }
        
        public void UpdateInventoryUI(List<InventorySlot> inventory)
        {
            if (!inventoryPopup.activeSelf) return;
            RefreshInventoryUI(inventory);
        }
        
        private void RefreshInventoryUI(List<InventorySlot> inventory = null)
        {
            if (inventoryContent == null || inventoryItemPrefab == null) return;
            
            // Clear existing items
            foreach (Transform child in inventoryContent)
            {
                Destroy(child.gameObject);
            }
            
            // Get inventory from GameManager if not provided
            inventory ??= GameManager.Instance.Inventory?.Inventory;
            
            if (inventory == null) return;
            
            // Create inventory items
            foreach (var slot in inventory)
            {
                if (slot.ItemData == null) continue;
                
                GameObject itemObj = Instantiate(inventoryItemPrefab, inventoryContent);
                InventoryItemUI itemUI = itemObj.GetComponent<InventoryItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Initialize(slot.ItemData, slot.Quantity, OnItemClicked);
                }
            }
            
            // Update capacity text
            if (inventoryCapacityText != null)
            {
                int current = inventory.Count;
                int max = GameManager.Instance.Inventory?.MaxInventorySize ?? 20;
                inventoryCapacityText.text = $"{current}/{max}";
            }
        }
        
        private void OnItemClicked(ItemData itemData)
        {
            // Handle item click (use, equip, etc.)
            ShowConfirmation($"Use {itemData.ItemName}", 
                $"What would you like to do with {itemData.ItemName}?",
                () => GameManager.Instance.Inventory?.UseItem(itemData),
                () => GameManager.Instance.Inventory?.EquipItem(itemData));
        }
        
        // ===== EQUIPMENT =====
        public void UpdateEquipmentUI(EquipmentSlots equipment)
        {
            if (equipment == null) return;
            
            UpdateEquipmentIcon(weaponIcon, equipment.Weapon);
            UpdateEquipmentIcon(armorIcon, equipment.Armor);
            UpdateEquipmentIcon(helmetIcon, equipment.Helmet);
            UpdateEquipmentIcon(bootsIcon, equipment.Boots);
            UpdateEquipmentIcon(accessory1Icon, equipment.Accessory1);
            UpdateEquipmentIcon(accessory2Icon, equipment.Accessory2);
        }
        
        private void UpdateEquipmentIcon(Image icon, ItemData itemData)
        {
            if (icon == null) return;
            
            if (itemData != null && itemData.Icon != null)
            {
                icon.sprite = itemData.Icon;
                icon.color = Color.white;
            }
            else
            {
                icon.sprite = null;
                icon.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }
        
        // ===== QUEST LOG =====
        public void ToggleQuestLog()
        {
            if (questLogPopup == null) return;
            
            bool isOpen = !questLogPopup.activeSelf;
            questLogPopup.SetActive(isOpen);
            
            if (isOpen)
            {
                RefreshQuestLog();
            }
        }
        
        public void HideQuestLog()
        {
            if (questLogPopup != null) questLogPopup.SetActive(false);
        }
        
        public void UpdateQuestLog(List<ActiveQuest> quests)
        {
            if (!questLogPopup.activeSelf) return;
            RefreshQuestLog(quests);
        }
        
        private void RefreshQuestLog(List<ActiveQuest> quests = null)
        {
            if (questLogContent == null || questEntryPrefab == null) return;
            
            // Clear existing entries
            foreach (Transform child in questLogContent)
            {
                Destroy(child.gameObject);
            }
            
            // Get quests from GameManager if not provided
            quests ??= GameManager.Instance.Quest?.ActiveQuests;
            
            if (quests == null || quests.Count == 0) return;
            
            // Create quest entries
            foreach (var quest in quests)
            {
                GameObject questObj = Instantiate(questEntryPrefab, questLogContent);
                QuestEntryUI questUI = questObj.GetComponent<QuestEntryUI>();
                
                if (questUI != null)
                {
                    questUI.Initialize(quest);
                }
            }
        }
        
        // ===== CLEANUP =====
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}