using UnityEngine;
using System;
using GameSystem;

public abstract class Entity : MonoBehaviour
{
    public enum EntityType { Player, Enemy, Npc, Item, Decoration }
    
    [Header("Entity Info")]
    public EntityType Type;
    public string DisplayName;
    
    [Header("Core Stats")]
    public EntityStats Stats = new EntityStats();
    public bool IsAlive => Stats.IsAlive;
    
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public bool CanMove = true;
    
    
    public event Action<int> OnHealthChanged;
    public event Action<int> OnDamageTaken;
    public event Action OnDeath;
    public event Action<ItemData> OnItemPickedUp;
    
    // Core methods
    public virtual void TakeDamage(int damage, Entity source = null)
    {
        if (!IsAlive) return;
        
        Stats.CurrentHealth -= damage;
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(Stats.CurrentHealth);
        
        if (!IsAlive)
        {
            Die(source);
        }
    }
    
    public virtual void Heal(int amount)
    {
        Stats.Heal(amount);
        OnHealthChanged?.Invoke(Stats.CurrentHealth);
    }
    
    protected virtual void Die(Entity killer = null)
    {
        OnDeath?.Invoke();
        Debug.Log($"{DisplayName} died!");
    }
    
    public virtual bool CanAct()
    {
        if (!IsAlive) return false;
        if (GameManager.Instance?.IsGamePaused == true) return false;
        if (GameManager.Instance?.IsInCutscene == true) return false;
        
        // Check status effects
        if (Stats.HasStatusEffect(StatusEffectType.Sleep) || 
            Stats.HasStatusEffect(StatusEffectType.Paralysis))
            return false;
            
        return true;
    }
}
