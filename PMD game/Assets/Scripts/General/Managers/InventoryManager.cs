using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystem
{
    public class InventoryManager : MonoBehaviour, IGameManagerListener
    {
        [Header("Inventory Settings")] 
        public int MaxInventorySize = 20;
        
        // State
        private List<InventorySlot> _inventory = new();
        private EquipmentSlots _equipment = new();

        public event Action<ItemData> OnItemAdded;
        public event Action<ItemData> OnItemRemoved;
        public event Action<EquipmentSlot, ItemData> OnItemEquipped;
        public event Action<EquipmentSlot> OnItemUnequipped;

        public List<InventorySlot> Inventory => _inventory;
        public EquipmentSlots Equipment => _equipment;
        
        public void Initialize()
        {
            Debug.Log("InventoryManager Initializing");
            
            // Load saved inventory if any
            LoadDefaultInventory();
            
            Debug.Log("InventoryManager initialized successfully");
        }
        
        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            // Inventory doesn't care about most game states
        }
        
        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            // Reset inventory UI when changing scenes
            GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
        }
        
        public void OnPauseStateChanged(bool paused)
        {
            // Pause should just give the pause menu
        }
        
                // ===== INVENTORY OPERATIONS =====
        public bool AddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return false;
            
            // Check if we have space
            if (_inventory.Count >= MaxInventorySize)
            {
                GameManager.Instance.Ui.AddLogEntry("Inventory is full!");
                return false;
            }

            // Check for existing stack
            foreach (var slot in _inventory)
            {
                if (slot.ItemData == itemData && slot.Quantity < itemData.MaxStack)
                {
                    int canAdd = itemData.MaxStack - slot.Quantity;
                    int addAmount = Mathf.Min(quantity, canAdd);
                    
                    slot.Quantity += addAmount;

                    OnItemAdded?.Invoke(itemData);
                    GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
                    
                    Debug.Log($"Added {addAmount} {itemData.ItemName} to existing stack");
                    
                    // If we still have more to add, recurse
                    if (quantity > addAmount)
                    {
                        return AddItem(itemData, quantity - addAmount);
                    }
                    
                    return true;
                }
            }
            
            // Create new slot
            var newSlot = new InventorySlot
            {
                ItemData = itemData,
                Quantity = quantity
            };
            
            _inventory.Add(newSlot);

            OnItemAdded?.Invoke(itemData);
            GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
            
            Debug.Log($"Added {quantity} {itemData.ItemName} to inventory");
            return true;
        }
        
        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return false;
            
            for (int i = _inventory.Count - 1; i >= 0; i--)
            {
                var slot = _inventory[i];
                
                if (slot.ItemData == itemData)
                {
                    if (slot.Quantity > quantity)
                    {
                        // Reduce quantity
                        slot.Quantity -= quantity;

                        OnItemRemoved?.Invoke(itemData);
                        GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
                        
                        Debug.Log($"Removed {quantity} {itemData.ItemName} from inventory");
                        return true;
                    }
                    else if (slot.Quantity == quantity)
                    {
                        // Remove entire slot
                        _inventory.RemoveAt(i);

                        OnItemRemoved?.Invoke(itemData);
                        GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
                        
                        Debug.Log($"Removed all {itemData.ItemName} from inventory");
                        return true;
                    }
                    else
                    {
                        // Not enough in this stack
                        quantity -= slot.Quantity;

                        _inventory.RemoveAt(i);
                        OnItemRemoved?.Invoke(itemData);
                        
                        // Continue with remaining quantity
                    }
                }
            }
            
            Debug.LogWarning($"Not enough {itemData.ItemName} in inventory");
            return false;
        }
        
        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            int total = 0;
            
            foreach (var slot in _inventory)
            {
                if (slot.ItemData == itemData)
                {
                    total += slot.Quantity;
                    if (total >= quantity) return true;
                }
            }
            
            return false;
        }
        
        public int GetItemCount(ItemData itemData)
        {
            int total = 0;
            
            foreach (var slot in _inventory)
            {
                if (slot.ItemData == itemData)
                {
                    total += slot.Quantity;
                }
            }
            
            return total;
        }
        
        // ===== EQUIPMENT OPERATIONS =====
        public bool EquipItem(ItemData itemData)
        {
            if (itemData == null || itemData.EquipmentSlot == EquipmentSlot.None)
            {
                Debug.LogWarning("Item is not equippable");
                return false;
            }
            
            // Check if we have the item
            if (!HasItem(itemData, 1))
            {
                Debug.LogWarning("Don't have item to equip");
                return false;
            }
            
            // Unequip current item in slot
            var currentEquipped = _equipment.GetItem(itemData.EquipmentSlot);
            if (currentEquipped != null)
            {
                UnequipItem(itemData.EquipmentSlot);
            }
            
            // Remove from inventory
            RemoveItem(itemData, 1);
            
            // Equip new item
            _equipment.SetItem(itemData.EquipmentSlot, itemData);
            
            // Apply equipment stats to player
            ApplyEquipmentStats(itemData, true);
            
            OnItemEquipped?.Invoke(itemData.EquipmentSlot, itemData);
            GameManager.Instance.Ui.UpdateEquipmentUI(_equipment);
            
            Debug.Log($"Equipped {itemData.ItemName} in {itemData.EquipmentSlot} slot");
            return true;
        }
        
        public bool UnequipItem(EquipmentSlot slot)
        {
            var itemData = _equipment.GetItem(slot);
            if (itemData == null) return false;
            
            // Try to add back to inventory
            if (AddItem(itemData, 1))
            {
                // Remove equipment stats
                ApplyEquipmentStats(itemData, false);
                
                // Clear slot
                _equipment.SetItem(slot, null);
                
                OnItemUnequipped?.Invoke(slot);
                GameManager.Instance.Ui.UpdateEquipmentUI(_equipment);
                
                Debug.Log($"Unequipped {itemData.ItemName} from {slot}");
                return true;
            }
            
            Debug.LogWarning("Inventory full, cannot unequip");
            return false;
        }
        
        private void ApplyEquipmentStats(ItemData itemData, bool equip)
        {
            var playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
            if (playerStats == null) return;
            
            int modifier = equip ? 1 : -1;
            
            // Apply stats
            playerStats.Stats.MaxHealth += itemData.ItemStats.HealthBonus * modifier;
            playerStats.Stats.MaxMana += itemData.ItemStats.ManaBonus * modifier;
            playerStats.Stats.Power += itemData.ItemStats.AttackBonus * modifier;
            playerStats.Stats.Resilience += itemData.ItemStats.DefenseBonus * modifier;
        }
        
        // ===== ITEM USAGE =====
        public bool UseItem(ItemData itemData, GameObject target = null)
        {
            if (itemData == null || !HasItem(itemData, 1)) return false;
            
            // Determine target
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Player");
            }
            
            // Apply item effects
            bool success = ApplyItemEffects(itemData, target);
            
            if (success)
            {
                // Consume item if consumable
                if (itemData.Type == ItemType.Consumable)
                {
                    RemoveItem(itemData);
                }
                
                Debug.Log($"Used {itemData.ItemName} on {target.name}");
                return true;
            }
            
            return false;
        }
        
        private bool ApplyItemEffects(ItemData itemData, GameObject target)
        {
            if (itemData == null || target == null) return false;
            
            var targetStats = target.GetComponent<GridEntity>();
            if (targetStats == null) return false;
            
            // This should work so it applies it to the target for now just invoke
            itemData.ItemEffect.Invoke();

            return true;
        }
        
        // ===== SAVE/LOAD =====
        private void LoadDefaultInventory()
        {
            // Add starting items
            // This could be loaded from a save file or from game settings
            // For now, start with empty inventory
            _inventory.Clear();
            _equipment.Clear();
        }
        
        public InventorySaveData GetSaveData()
        {
            return new InventorySaveData
            {
                InventorySlots = _inventory.ToArray(),
                EquipmentSlots = _equipment.GetAllItems(),
            };
        }
        
        public void LoadSaveData(InventorySaveData data)
        {
            if (data.InventorySlots != null)
            {
                _inventory = new List<InventorySlot>(data.InventorySlots);
            }
            
            if (data.EquipmentSlots != null)
            {
                _equipment.LoadFromArray(data.EquipmentSlots);
            }

            // Update UI
            GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
            GameManager.Instance.Ui.UpdateEquipmentUI(_equipment);
        }
        
        public void Clear()
        {
            _inventory.Clear();
            _equipment.Clear();

            GameManager.Instance.Ui.UpdateInventoryUI(_inventory);
            GameManager.Instance.Ui.UpdateEquipmentUI(_equipment);
        }
    }
    
    }
    // Inventory data structures
    [Serializable]
    public class InventorySlot
    {
        public ItemData ItemData;
        public int Quantity = 1;
    }
    
    [Serializable]
    public class InventorySaveData
    {
        public InventorySlot[] InventorySlots;
        public ItemData[] EquipmentSlots;
        public int CurrentWeight;
    }
    
     [Serializable]
    public class EquipmentSlots
    {
        public ItemData Weapon;
        public ItemData Armor;
        public ItemData Helmet;
        public ItemData Boots;
        public ItemData Accessory1;
        public ItemData Accessory2;
        
        public ItemData GetItem(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => Weapon,
                EquipmentSlot.Armor => Armor,
                EquipmentSlot.Helmet => Helmet,
                EquipmentSlot.Boots => Boots,
                EquipmentSlot.Accessory1 => Accessory1,
                EquipmentSlot.Accessory2 => Accessory2,
                _ => null
            };
        }
        
        public void SetItem(EquipmentSlot slot, ItemData item)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon: Weapon = item; break;
                case EquipmentSlot.Armor: Armor = item; break;
                case EquipmentSlot.Helmet: Helmet = item; break;
                case EquipmentSlot.Boots: Boots = item; break;
                case EquipmentSlot.Accessory1: Accessory1 = item; break;
                case EquipmentSlot.Accessory2: Accessory2 = item; break;
            }
        }
        
        public ItemData[] GetAllItems()
        {
            return new ItemData[]
            {
                Weapon, Armor, Helmet, Boots, Accessory1, Accessory2
            };
        }
        
        public void LoadFromArray(ItemData[] items)
        {
            if (items == null || items.Length < 6) return;
            
            Weapon = items[0];
            Armor = items[1];
            Helmet = items[2];
            Boots = items[3];
            Accessory1 = items[4];
            Accessory2 = items[5];
        }
        
        public void Clear()
        {
            Weapon = null;
            Armor = null;
            Helmet = null;
            Boots = null;
            Accessory1 = null;
            Accessory2 = null;
        }
    }
    
    public enum EquipmentSlot
    {
        None,
        Weapon,
        Armor,
        Helmet,
        Boots,
        Accessory1,
        Accessory2
    }
