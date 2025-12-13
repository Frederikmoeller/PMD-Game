using System.Collections.Generic;
using SaveSystem;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : GridEntity  // <-- INHERIT from GridEntity
{
    [Header("Player-Specific Stats")]
    [SaveField] public int Experience = 0;
    [SaveField] public int ExperienceToNextLevel = 100;
    [SaveField] public List<ItemData> Inventory = new List<ItemData>();
    public int MaxInventorySize = 20;

    [Header("Player Events")]
    public UnityEvent<int> OnPlayerHealthChanged; // current health
    public UnityEvent<int> OnPlayerDamageTaken; // damage amount
    public UnityEvent OnPlayerDeath;
    public UnityEvent<int> OnPlayerLevelUp; // new level
    public UnityEvent<int> OnPlayerManaChanged;

    [Header("Player Preset")]
    public CharacterPresetSO PlayerPreset;
    
    private PlayerScaling _scaling = new PlayerScaling();

    private PlayerController _playerController;

    private void Start()
    {
        // Set player type
        Type = EntityType.Player;
        
        // Initialize from preset
        if (PlayerPreset != null)
        {
            InitializeFromPreset();
        }
        
        // Calculate next level requirement
        ExperienceToNextLevel = _scaling.GetExpForLevel(Stats.Level);
        
        // Wire up events
        OnHealthChanged += (health) => OnPlayerHealthChanged?.Invoke(health);
        OnDamageTaken += (damage) => OnPlayerDamageTaken?.Invoke(damage);
        OnDeath += () => OnPlayerDeath?.Invoke();
        
        OnPlayerHealthChanged?.Invoke(Stats.CurrentHealth);
    }
    
    void InitializeFromPreset()
    {
        // Copy base stats from preset
        Stats.MaxHealth = PlayerPreset.BaseStats.MaxHealth;
        Stats.CurrentHealth = PlayerPreset.BaseStats.CurrentHealth;
        Stats.Power = PlayerPreset.BaseStats.Power;
        Stats.Focus = PlayerPreset.BaseStats.Focus;
        Stats.Resilience = PlayerPreset.BaseStats.Resilience;
        Stats.Willpower = PlayerPreset.BaseStats.Willpower;
        Stats.Fortune = PlayerPreset.BaseStats.Fortune;
        
        // Copy growth rates
        Stats.HealthGrowth = PlayerPreset.HealthGrowth;
        Stats.PowerGrowth = PlayerPreset.PowerGrowth;
        Stats.FocusGrowth = PlayerPreset.FocusGrowth;
        Stats.ResilienceGrowth = PlayerPreset.ResilienceGrowth;
        Stats.WillpowerGrowth = PlayerPreset.WillpowerGrowth;
        Stats.FortuneGrowth = PlayerPreset.FortuneGrowth;
    }

    // Player-specific experience system
    public new void AddExperience(int exp)
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
    
    // Override item pickup for player
    protected override void OnSteppedOnItem(GridEntity item)
    {
        var itemEntity = item.GetComponent<ItemEntity>();
        if (itemEntity != null)
        {
            // Use player inventory system
            if (AddItemToInventory(itemEntity.ItemData))
            {
                itemEntity.OnPickedUp(this);
                OnItemPickedUp?.Invoke(this);
            }
        }
    }
}