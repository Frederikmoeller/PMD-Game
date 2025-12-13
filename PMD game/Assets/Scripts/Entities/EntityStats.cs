using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EntityType
{
    Player,
    Enemy,
    Item,
    NPC,
    Decoration
}

[Serializable]
public enum StatusEffectType
{
    None,
    Poison,
    Burn,
    Paralysis,
    Sleep,
    Buff_Attack,
    Buff_Defense,
    Buff_Speed,
    Debuff_Attack,
    Debuff_Defense,
    Debuff_Speed
}

[Serializable]
public class StatusEffect
{
    public StatusEffectType Type;
    public int Duration;
}

[Serializable]
public class EntityStats
{
    public string Name;
    // Primary Stats
    public int MaxHealth = 100;
    public int CurrentHealth = 100;
    public int MaxMana = 100;
    public int CurrentMana = 100;
    
    // Offensive Stats
    public int Power = 10;       // Renamed from Attack
    public int Focus = 10;       // Renamed from Special Attack
    
    // Defensive Stats  
    public int Resilience = 5;   // Renamed from Defense
    public int Willpower = 5;    // Renamed from Special Defense
    
    // Utility Stats
    public int Fortune = 5;      // Renamed from Luck
    public int Level = 1;
    
    // Status Effects
    public List<StatusEffect> CurrentStatus = new();
    
    // Experience
    public int ExperienceValue = 25;

    // Computed Properties
    public bool IsAlive => CurrentHealth > 0;
    public float HealthPercentage => (float)CurrentHealth / MaxHealth;
    
    // Growth rates (for leveling)
    public float HealthGrowth = 1.1f;
    public float PowerGrowth = 1.08f;
    public float FocusGrowth = 1.08f;
    public float ResilienceGrowth = 1.07f;
    public float WillpowerGrowth = 1.07f;
    public float FortuneGrowth = 1.05f;

    // Methods
    public void TakeDamage(int damage, bool isPowerAttack = true)
    {
        // Use the correct defense stat based on attack type
        int defense = isPowerAttack ? Resilience : Willpower;
        int actualDamage = Mathf.Max(1, damage - defense);
        CurrentHealth -= actualDamage;
        
        Debug.Log($"Took {actualDamage} damage (reduced from {damage} by {defense} defense)");
    }
    
    public void TakeStatusDamage(int damage)
    {
        // For status effects, traps, etc. that bypass defense
        CurrentHealth -= Mathf.RoundToInt(MaxHealth * damage * 0.01f);
    }
    
    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }
    
    public void ApplyStatusEffect(StatusEffectType type, int duration)
    {
        if (duration <= 0) return;
    
        // Check if already has this status
        var existingEffect = CurrentStatus.FirstOrDefault(s => s.Type == type);
    
        if (existingEffect != null)
        {
            // Refresh duration (or extend, or keep longest - your choice)
            existingEffect.Duration = Mathf.Max(existingEffect.Duration, duration);
        }
        else
        {
            // Add new status
            CurrentStatus.Add(new StatusEffect { Type = type, Duration = duration });
        }
    
        Debug.Log($"Player now has {type} for {duration} turns");
    
        // Handle immediate effects
        if (type == StatusEffectType.Paralysis)
        {
            //OnParalyzed();
        }
    }
    
    // Update all status effects (call each turn)
    public void UpdateStatusEffects()
    {
        // Process each status effect
        for (int i = CurrentStatus.Count - 1; i >= 0; i--)
        {
            var status = CurrentStatus[i];
        
            // Apply damage from damaging status effects
            switch (status.Type)
            {
                case StatusEffectType.Poison:
                    TakeStatusDamage(5);
                    Debug.Log($"takes poison damage!");
                    break;
                
                case StatusEffectType.Burn:
                    TakeStatusDamage(3);
                    Debug.Log($"takes burn damage!");
                    break;
            }
        
            // Reduce duration
            status.Duration--;
        
            // Remove if duration expired
            if (status.Duration <= 0)
            {
                CurrentStatus.RemoveAt(i);
                Debug.Log($"{status.Type} has worn off");
            
                // Handle status removal
                if (status.Type == StatusEffectType.Paralysis)
                {
                    //OnParalysisWoreOff();
                }
            }
        }
    }
    
    // Check for specific status effects
    public bool HasStatusEffect(StatusEffectType type)
    {
        return CurrentStatus.Any(s => s.Type == type);
    }
    
    // Remove a specific status effect
    public void RemoveStatusEffect(StatusEffectType type)
    {
        var effect = CurrentStatus.FirstOrDefault(s => s.Type == type);
        if (effect != null)
        {
            CurrentStatus.Remove(effect);
            Debug.Log($"{type} was removed");
        
            if (type == StatusEffectType.Paralysis)
            {
                //OnParalysisWoreOff();
            }
        }
    }
    
    // Clear all status effects
    public void ClearAllStatusEffects()
    {
        CurrentStatus.Clear();
        //OnParalysisWoreOff(); // Ensure paralysis movement restriction is lifted
    }
}