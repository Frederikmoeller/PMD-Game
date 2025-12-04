using UnityEngine;

[RequireComponent(typeof(GridEntity))]
public class Enemy : MonoBehaviour, IEffectTileHandler
{
    private GridEntity _entity;
    
    // Add enemy stats if needed
    private int _currentHealth = 50;
    private int _maxHealth = 50;
    
    private StatusEffectType CurrentStatus = StatusEffectType.None;
    private int StatusDuration = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _entity = GetComponent<GridEntity>();
    }

    public void TakeTurn()
    {
        // Update status effects at start of turn
        UpdateStatusEffects();
        
        // Skip turn if paralyzed
        if (CurrentStatus == StatusEffectType.Paralysis)
        {
            Debug.Log($"{name} is paralyzed and cannot move!");
            StatusDuration--;
            if (StatusDuration <= 0)
            {
                CurrentStatus = StatusEffectType.None;
            }
            return;
        }
        
        // Skip turn if asleep
        if (CurrentStatus == StatusEffectType.Sleep)
        {
            Debug.Log($"{name} is asleep!");
            StatusDuration--;
            if (StatusDuration <= 0)
            {
                CurrentStatus = StatusEffectType.None;
            }
            return;
        }
        
        Debug.Log($"{name} taking turn");
        
        // Simple AI: try to move randomly
        int dx = Random.Range(-1, 2);
        int dy = Random.Range(-1, 2);
        
        if (dx != 0 || dy != 0 && _entity != null)
        {
            _entity.TryMove(dx, dy);
        }
    }

    protected void OnSteppedTrap()
    {
        
    }

    public void ApplyTileEffect(EventTileEffect effect)
    {
        if (effect == null || !effect.AffectsEnemies) return;
        
        Debug.Log($"{name} activating effect: {effect.EffectName}");
        
        // Health effects
        if (effect.HealthChange < 0) // Negative = damage
        {
            int damage = Mathf.RoundToInt(_maxHealth * (Mathf.Abs(effect.HealthChange * 0.01f)));
            TakeDamage(-damage);
        }
        else if (effect.HealthChange > 0) // Positive = healing
        {
            int healing = Mathf.RoundToInt(effect.HealthChange * 0.01f);
            Heal(healing);
        }
        
        // Status effects could affect enemy AI here
        if (effect.StatusEffect != StatusEffectType.None)
        {
            Debug.Log($"{name} is now {effect.StatusEffect} for {effect.StatusDuration} turns");
        }
        
        // Stat modifiers (temporary)
        if (effect.Duration > 0)
        {
            Debug.Log($"{name} gets temporary stats: +{effect.AttackBonus} ATK, +{effect.DefenseBonus} DEF, +{effect.SpeedBonus} SPD");
            // TODO: Implement temporary buffs for enemies
        }
        
        // Teleport effect
        if (effect.TeleportsToRandomFloor && _entity != null && _entity.Grid != null)
        {
            var randomPos = _entity.Grid.GetRandomFloorPosition();
            transform.position = new Vector3(randomPos.x + 0.5f, randomPos.y + 0.5f, 0);
            
            _entity.Grid.Tiles[_entity.GridX, _entity.GridY].Occupant = null;
            _entity.GridX = randomPos.x;
            _entity.GridY = randomPos.y;
            _entity.Grid.Tiles[_entity.GridX, _entity.GridY].Occupant = _entity;
        }
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        Debug.Log($"{name} took {damage} damage. Health: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        Debug.Log($"{name} healed {amount}. Health: {_currentHealth}/{_maxHealth}");
    }
    
    public void ApplyStatusEffect(StatusEffectType effect, int duration)
    {
        CurrentStatus = effect;
        StatusDuration = duration;
        Debug.Log($"{name} is now {effect} for {duration} turns");
    }
    
    private void UpdateStatusEffects()
    {
        if (StatusDuration <= 0) return;
        StatusDuration--;
            
        // Apply poison damage
        if (CurrentStatus == StatusEffectType.Poison)
        {
            TakeDamage(5);
            Debug.Log($"{name} takes poison damage!");
        }
            
        // Apply burn damage
        if (CurrentStatus == StatusEffectType.Burn)
        {
            TakeDamage(3);
            Debug.Log($"{name} takes burn damage!");
        }
            
        if (StatusDuration <= 0)
        {
            CurrentStatus = StatusEffectType.None;
            Debug.Log($"{name}'s status effect has worn off");
        }
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
        if (_entity != null && _entity.Grid != null)
        {
            _entity.Grid.Tiles[_entity.GridX, _entity.GridY].Occupant = null;
        }
        Destroy(gameObject);
    }
}
