using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GridEntity))]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Configuration")]
    public CharacterPresetSO Preset;
    // Components
    private GridEntity _entity;
    private SpriteRenderer _spriteRenderer;
    
    // Runtime state
    private GameObject _playerTarget;

    void Start()
    {
        _entity = GetComponent<GridEntity>();
        _entity.Type = EntityType.Enemy;
        
        // Initialize from EnemyType
        InitializeFromPreset();
        
        // Find player for AI
        _playerTarget = GameObject.FindGameObjectWithTag("Player");
        
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterEnemy(this);
        }
    }
    
    void OnDestroy()
    {
        // Unregister from TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }
    }

    
    void InitializeFromPreset()
    {
        if (Preset == null)
        {
            Debug.LogWarning($"Enemy {name} has no Preset assigned!");
            return;
        }
        
        // Apply preset stats, scaled for current floor
        ApplyPresetStats();
        
        // Set name
        gameObject.name = Preset.CharacterName;
        
        // Set experience value (for when killed)
        _entity.Stats.ExperienceValue = Preset.ExperienceValue;
    }
    
    void ApplyPresetStats()
    {
        // Scale stats based on current floor
        int currentFloor = GameManager.Instance?.CurrentFloor ?? 1;
        EntityStats scaledStats = EnemyScaling.ScaleForFloor(Preset.BaseStats, currentFloor);
        
        // Copy scaled stats to entity
        _entity.Stats.MaxHealth = scaledStats.MaxHealth;
        _entity.Stats.CurrentHealth = scaledStats.CurrentHealth;
        _entity.Stats.Power = scaledStats.Power;
        _entity.Stats.Focus = scaledStats.Focus;
        _entity.Stats.Resilience = scaledStats.Resilience;
        _entity.Stats.Willpower = scaledStats.Willpower;
        _entity.Stats.Fortune = scaledStats.Fortune;
        _entity.Stats.Level = scaledStats.Level;
        
        // Copy growth rates from preset
        _entity.Stats.HealthGrowth = Preset.HealthGrowth;
        _entity.Stats.PowerGrowth = Preset.PowerGrowth;
        _entity.Stats.FocusGrowth = Preset.FocusGrowth;
        _entity.Stats.ResilienceGrowth = Preset.ResilienceGrowth;
        _entity.Stats.WillpowerGrowth = Preset.WillpowerGrowth;
        _entity.Stats.FortuneGrowth = Preset.FortuneGrowth;
    }
    
    // Add property for TurnManager
    public bool IsAlive => _entity != null && _entity.IsAlive;
    
    void SetupResistances()
    {
        /*if (EnemyType.ImmuneToPoison)
            _entity.Stats.Resistances.Add(new StatusEffectResistance { EffectType = StatusEffectType.Poison, Resistance = 1f });
        
        if (EnemyType.ImmuneToParalysis)
            _entity.Stats.Resistances.Add(new StatusEffectResistance { EffectType = StatusEffectType.Paralysis, Resistance = 1f });
        
        if (EnemyType.ImmuneToBurn)
            _entity.Stats.Resistances.Add(new StatusEffectResistance { EffectType = StatusEffectType.Burn, Resistance = 1f });
        
        if (EnemyType.ImmuneToSleep)
            _entity.Stats.Resistances.Add(new StatusEffectResistance { EffectType = StatusEffectType.Sleep, Resistance = 1f });*/
    }

    public void TakeTurn()
    {
        if (!_entity.IsAlive) return;

        // Update status effects
        _entity.UpdateStatusEffects();
        
        // Skip turn if paralyzed/asleep
        if (!_entity.CanMoveThisFrame())
        {
            return;
        }
        
        // Find and attack player
        if (_playerTarget == null)
        {
            _playerTarget = GameObject.FindGameObjectWithTag("Player");
            if (_playerTarget == null) return;
        }
        
        GridEntity playerEntity = _playerTarget.GetComponent<GridEntity>();
        if (playerEntity == null || !playerEntity.IsAlive) return;
        
        // UNIFIED AI: Chase and attack player
        ExecuteAI(playerEntity);
    }
    
    bool IsAdjacentTo(GridEntity other)
    {
        return Mathf.Abs(_entity.GridX - other.GridX) <= 1 &&
               Mathf.Abs(_entity.GridY - other.GridY) <= 1;
    }
    
    void ExecuteAI(GridEntity player)
    {
        // Calculate distance
        int distance = Mathf.Max(
            Mathf.Abs(_entity.GridX - player.GridX),
            Mathf.Abs(_entity.GridY - player.GridY)
        );
        
        MoveTowardPlayer(player);
    }

    System.Collections.IEnumerator RangedAttackAnimation(Vector3 targetPos)
    {
        // Create projectile or line effect
        // For now, just a visual indicator
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color original = sr.color;
        sr.color = Color.yellow;
        
        yield return new WaitForSeconds(0.2f);
        
        sr.color = original;
    }
    
    void MoveTowardPlayer(GridEntity player)
    {
        Vector2Int direction = GetDirectionToward(player);
        
        // Use GridEntity's smooth movement with callback
        if (_entity.CanMoveInDirection(direction.x, direction.y))
        {
            _entity.TryMove(direction.x, direction.y, OnEnemyMoveComplete);
        }
        else
        {
            // Can't move, still signal completion
            OnEnemyMoveComplete();
        }
        
        //TODO: Use Navmesh or own A* algorithm
        // If primary direction blocked, try orthogonal directions
        /*Vector2Int[] alternatives = GetAlternativeDirections(direction);
        
        foreach (Vector2Int alt in alternatives)
        {
            if (_entity.CanMoveInDirection(alt.x, alt.y))
            {
                _entity.TryMove(alt.x, alt.y);
                return;
            }
        }
        
        // Can't move toward player, maybe random move?
        //TryRandomMove(); */
    }
    
    // Add callback for movement completion
    void OnEnemyMoveComplete()
    {
        Debug.Log($"{name} finished moving to {_entity.GridX},{_entity.GridY}");
        // Enemy movement complete, next enemy can move
    }
    
    Vector2Int GetDirectionToward(GridEntity target)
    {
        int dx = target.GridX - _entity.GridX;
        int dy = target.GridY - _entity.GridY;
        
        // Simple: move in primary direction
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
            return new Vector2Int(dx > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, dy > 0 ? 1 : -1);
    }
    
    // Called when enemy dies (optional override)
    void OnDeath()
    {
        TryDropLoot();
    }
    
    void TryDropLoot()
    {
        if (Preset == null || _entity.HeldItem == null) return;

        var drop = _entity.HeldItem;
    }
}