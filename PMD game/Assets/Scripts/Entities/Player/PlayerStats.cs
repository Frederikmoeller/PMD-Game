using System.Collections.Generic;
using SaveSystem;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : GridEntity  // <-- INHERIT from GridEntity
{
    [Header("Player-Specific Stats")]
    [SaveField] public int Experience = 0;
    [SaveField] public int ExperienceToNextLevel = 100;
    [SaveField] public List<ItemData> Inventory = new();
    [SaveField] public int Money;
    [SaveField] public int MaxInventorySize = 20;

    [Header("Player Events")]
    public UnityEvent<int> OnPlayerHealthChanged; // current health
    public UnityEvent<int> OnPlayerDamageTaken; // damage amount
    public UnityEvent OnPlayerDeath;
    public UnityEvent<int> OnPlayerLevelUp; // new level
    public UnityEvent<int> OnPlayerManaChanged;

    private PlayerScaling _scaling = new PlayerScaling();

    public override void Start()
    {
        base.Start();
        // Set player type
        Type = EntityType.Player;
        
        // GridEntity.Start() already called InitializeFromPreset()
        // Calculate next level requirement
        ExperienceToNextLevel = _scaling.GetExpForLevel(Stats.Level);
        
        // Wire up events
        OnHealthChanged += (health) => OnPlayerHealthChanged?.Invoke(health);
        OnDamageTaken += (damage) => OnPlayerDamageTaken?.Invoke(damage);
        OnDeath += () => OnPlayerDeath?.Invoke();
        
        OnPlayerHealthChanged?.Invoke(Stats.CurrentHealth);
    }

    // Player-specific experience system
    public void AddExperience(int exp)
    {
        Experience += exp;
        Debug.Log($"Gained {exp} EXP. Total: {Experience}/{ExperienceToNextLevel}");
        
        CheckLevelUp();
    }
    
    void CheckLevelUp()
    {
        while (Experience >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        // Use the scaling system
        _scaling.LevelUpStats(Stats);
        
        // Update experience tracking
        Experience -= ExperienceToNextLevel;
        ExperienceToNextLevel = _scaling.GetExpForLevel(Stats.Level);
        
        Debug.Log($"Level up! Now level {Stats.Level}");
        OnPlayerLevelUp?.Invoke(Stats.Level);
        OnPlayerHealthChanged?.Invoke(Stats.CurrentHealth);
    }

    // Override GridEntity's OnDeath if you want player-specific death
    protected override void OnDeathInternal(GridEntity killer = null)
    {
        // Don't call base if you want different player death handling
        Debug.Log("Player died!");
        OnPlayerDeath?.Invoke();
        
        // Trigger game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(false);
        }
        
        // Player doesn't get destroyed - handled by GameManager
        gameObject.SetActive(false);
    }

    public override void PickUpItem(ItemEntity item)
    {
        if (Type == EntityType.Player && Inventory.Count < MaxInventorySize)
        {
            if (item.ItemData.Type != ItemType.Money)
            { 
                Inventory.Add(item.ItemData);
            }
            else
            {
                Money += Random.Range(20, 100);
            }
        }
        base.PickUpItem(item);
    }

    // Inventory management
    public bool AddItemToInventory(ItemData item)
    {
        if (Inventory.Count < MaxInventorySize)
        {
            Inventory.Add(item);
            Debug.Log($"Added {item.ItemName} to inventory");
            return true;
        }
        return false;
    }
    
    public bool RemoveItemFromInventory(ItemData item)
    {
        return Inventory.Remove(item);
    }
    
    public bool HasItem(ItemData item)
    {
        return Inventory.Contains(item);
    }
}