using UnityEngine;
using System.Collections;


public class GridEntity : MonoBehaviour, IEffectTileHandler
{
    // Grid Properties
    public int GridX;
    public int GridY;
    public DungeonGrid Grid;
    
    // Entity Type & Behavior
    public EntityType Type;
    public bool BlocksMovement => Type == EntityType.Enemy || Type == EntityType.NPC || Type == EntityType.Player;
    
    // Stats System (UNIFIED)
    [SerializeField] private EntityStats stats = new EntityStats();
    public EntityStats Stats => stats;
    public bool IsAlive => stats.IsAlive;
    
    // Inventory (for players/enemies that can hold items)
    public ItemData HeldItem = null; // For enemies carrying items
    
    // Events (can be used by PlayerController/Enemy for UI/feedback)
    public System.Action<int> OnHealthChanged; // current health
    public System.Action<int> OnDamageTaken; // damage amount
    public System.Action OnDeath;
    public System.Action<GridEntity> OnItemPickedUp; // item picked up
    
    // Add these movement fields
    [Header("Movement Settings")]
    public float MoveSpeed = 5f;
    public bool UseSmoothMovement = true; // Can be turned off for decor items
    public bool IsMoving = false;
    public Vector3 MoveTarget;
    
    private System.Action _onMoveComplete;
    
    private void Update()
    {
        if (UseSmoothMovement && IsMoving)
        {
            UpdateMovement();
        }
    }
    
    void UpdateMovement()
    {
        if (!IsMoving) return;
        
        transform.position = Vector3.MoveTowards(transform.position, MoveTarget, MoveSpeed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, MoveTarget) < 0.01f)
        {
            transform.position = MoveTarget;
            IsMoving = false;
            
            // Handle tile interactions after movement completes
            if (Grid != null)
            {
                var tile = Grid.Tiles[GridX, GridY];
                HandleTileInteractions(tile);
            }
            
            // Trigger completion callback
            _onMoveComplete?.Invoke();
        }
    }
    
    // Movement
    public void SetGrid(DungeonGrid g) => Grid = g;

    public virtual bool TryMove(int dx, int dy, System.Action onComplete = null)
    {
        Debug.Log($"GridEntity.TryMove({dx},{dy}) called by {name}");

        // Only work in grid-based mode
        if (GameManager.Instance?.IsGridBased != true) return false;
        if (Grid == null) return false;

        int targetX = GridX + dx;
        int targetY = GridY + dy;

        Debug.Log($"Moving from {GridX},{GridY} to {targetX},{targetY}");

        if (!Grid.InBounds(targetX, targetY)) return false;

        var targetTile = Grid.Tiles[targetX, targetY];
        if (!targetTile.Walkable) return false;

        // Check for BLOCKING occupants only
        if (targetTile.Occupant != null && targetTile.Occupant.BlocksMovement && targetTile.Occupant != this)
        {
            Debug.Log($"Tile occupied by {targetTile.Occupant.name}");
            return TryInteractWithOccupant(targetTile.Occupant, dx, dy);
        }

        // DIAGONAL: Additional corner cutting check
        if (dx != 0 && dy != 0)
        {
            if (!CheckDiagonalClearance(dx, dy)) return false;
        }

        // CRITICAL: Clear old grid position BEFORE updating GridX/GridY
        if (Grid.InBounds(GridX, GridY))
        {
            Grid.Tiles[GridX, GridY].Occupant = null;
        }

        // Update grid coordinates
        GridX = targetX;
        GridY = targetY;

        // Set as occupant if we block movement
        if (BlocksMovement)
        {
            Grid.Tiles[GridX, GridY].Occupant = this;
        }
    
        // Handle movement visualization
        if (UseSmoothMovement)
        {
            MoveTarget = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
            IsMoving = true;
            _onMoveComplete = onComplete;
        }
        else
        {
            // Teleport instantly
            transform.position = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
        
            // Handle tile interactions immediately
            HandleTileInteractions(targetTile);
            onComplete?.Invoke();
        }
    
        Debug.Log($"Movement successful. New position: {GridX},{GridY}");
        return true;
    }

    private bool CheckDiagonalClearance(int dx, int dy)
    {
        // Check horizontal adjacent tile
        if (Grid.InBounds(GridX + dx, GridY))
        {
            var horizontalTile = Grid.Tiles[GridX + dx, GridY];
            if (!horizontalTile.Walkable ||
                (horizontalTile.Occupant != null && horizontalTile.Occupant.BlocksMovement &&
                 horizontalTile.Occupant != this))
            {
                Debug.Log($"Diagonal blocked horizontally at {GridX + dx},{GridY}");
                return false;
            }
        }

        // Check vertical adjacent tile
        if (Grid.InBounds(GridX, GridY + dy))
        {
            var verticalTile = Grid.Tiles[GridX, GridY + dy];
            if (!verticalTile.Walkable ||
                (verticalTile.Occupant != null && verticalTile.Occupant.BlocksMovement &&
                 verticalTile.Occupant != this))
            {
                Debug.Log($"Diagonal blocked vertically at {GridX},{GridY + dy}");
                return false;
            }
        }

        return true;
    }
    
    // Helper method for enemy AI
    public bool TryMoveToward(Vector2Int direction, System.Action onComplete = null)
    {
        return TryMove(direction.x, direction.y, onComplete);
    }
    
    // Stop any ongoing movement
    public void StopMovement()
    {
        IsMoving = false;
        if (Grid != null)
        {
            transform.position = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
        }
    }

    public bool CanMoveThisFrame()
    {
        if (!Stats.IsAlive) return false;
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.IsGamePaused) return false;
        if (GameManager.Instance.IsInCutscene) return false;
        if (!GameManager.Instance.IsGridBased) return true;
        if (TurnManager.Instance == null) return false;
        if (!TurnManager.Instance.PlayersTurn) return false;
    
        // Check status effects
        if (Stats.CurrentStatus != null)
        {
            // Assuming Stats.CurrentStatus is List<StatusEffect> or similar
            foreach (var status in Stats.CurrentStatus)
            {
                if (status.StatusEffectType == StatusEffectType.Sleep || 
                    status.StatusEffectType == StatusEffectType.Paralysis)
                {
                    return false;
                }
            }
        }
    
        return true;
    }
    
    public bool CanMoveInDirection(int dx, int dy)
    {
        if (Grid == null) return false;

        int targetX = GridX + dx;
        int targetY = GridY + dy;
    
        // Check bounds
        if (!Grid.InBounds(targetX, targetY)) return false;
    
        // Check if target tile is walkable
        var targetTile = Grid.Tiles[targetX, targetY];
        if (!targetTile.Walkable) return false;
    
        // Check for blocking occupants
        if (targetTile.Occupant != null && targetTile.Occupant.BlocksMovement && targetTile.Occupant != this)
        {
            return false;
        }
    
        // DIAGONAL MOVEMENT: Prevent corner cutting
        if (dx != 0 && dy != 0) // Moving diagonally
        {
            // Need both adjacent cardinal tiles to be walkable
            bool horizontalClear = Grid.InBounds(GridX + dx, GridY);
            bool verticalClear = Grid.InBounds(GridX, GridY + dy);
        
            if (horizontalClear)
            {
                var horizontalTile = Grid.Tiles[GridX + dx, GridY];
                if (!horizontalTile.Walkable) return false;
                if (horizontalTile.Occupant != null && horizontalTile.Occupant.BlocksMovement && horizontalTile.Occupant != this)
                    return false;
            }
        
            if (verticalClear)
            {
                var verticalTile = Grid.Tiles[GridX, GridY + dy];
                if (!verticalTile.Walkable) return false;
                if (verticalTile.Occupant != null && verticalTile.Occupant.BlocksMovement && verticalTile.Occupant != this)
                    return false;
            }
        }
    
        return true;
    }
    
    // COMBAT SYSTEM (in GridEntity!)
    public void TakeDamage(int damage, GridEntity source = null, bool isPowerAttack = true)
    {
        if (!IsAlive) return;
        
        // Use the new damage calculation
        stats.TakeDamage(damage, isPowerAttack);
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(stats.CurrentHealth);
        
        Debug.Log($"{name} took {damage} damage. Health: {stats.CurrentHealth}/{stats.MaxHealth}");
        
        if (!IsAlive)
        {
            OnDeathInternal(source);
        }
    }
    
    public void Heal(int amount)
    {
        stats.Heal(amount);
        OnHealthChanged?.Invoke(stats.CurrentHealth);
        Debug.Log($"{name} healed {amount}. Health: {stats.CurrentHealth}/{stats.MaxHealth}");
    }
    
    public void Attack(GridEntity target, bool isPowerAttack = true, float movePower = 1.0f)
    {
        if (!IsAlive || !target.IsAlive) return;
        
        // Use the new combat calculator
        int damage = CombatCalculator.CalculateDamage(
            stats, 
            target.stats, 
            isPowerAttack, 
            movePower
        );
        
        Debug.Log($"{name} attacks {target.name} for {damage} damage!");
        
        // Pass the attack type information
        target.TakeDamage(damage, this, isPowerAttack);
        
        // Trigger combat animation
        StartCoroutine(AttackAnimation(target.transform.position));
    }

    private IEnumerator AttackAnimation(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        Vector3 attackPos = Vector3.Lerp(startPos, targetPos, 0.3f);
        
        float duration = 0.15f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, attackPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(attackPos, startPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = startPos;
    }
    
    protected virtual void OnDeathInternal(GridEntity killer = null)
    {
        Debug.Log($"{name} died!");
        
        // Clear grid position
        if (Grid != null)
        {
            Grid.Tiles[GridX, GridY].Occupant = null;
        }
        
        // Give XP to killer if applicable
        if (killer != null && killer.Type == EntityType.Player && Type == EntityType.Enemy)
        {
            killer.AddExperience(stats.ExperienceValue);
        }
        
        // Drop held item
        if (HeldItem != null)
        {
            DropItem(HeldItem);
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Handle destruction based on type
        if (Type == EntityType.Player)
        {
            // Player doesn't get destroyed - handled by GameManager
            gameObject.SetActive(false);
        }
        else
        {
            // Enemies/NPCs get destroyed after animation
            StartCoroutine(DeathAnimation());
        }
    }
    
    private IEnumerator DeathAnimation()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Color original = sr.color;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                sr.color = Color.Lerp(original, Color.clear, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        Destroy(gameObject);
    }
    
    // EXPERIENCE SYSTEM (in GridEntity!)
    public void AddExperience(int amount)
    {
        // Only players care about experience for now
        if (Type == EntityType.Player)
        {
            // This would be handled by PlayerController extending this
            // But we keep the method here for consistency
        }
    }
    
    // ITEM SYSTEM (in GridEntity!)
    public bool PickUpItem(ItemEntity item)
    {
        if (item == null) return false;
        
        // Check if we can hold items
        if (Type == EntityType.Player || Type == EntityType.Enemy)
        { 
            if (Type == EntityType.Enemy)
            {
                // Enemy holds the item
                HeldItem = item.ItemData;
                Debug.Log($"{name} picked up {item.ItemData.ItemName}");
            }
            
            // Remove item from world
            item.OnPickedUp(this);
            OnItemPickedUp?.Invoke(this);
            
            return true;
        }
        
        return false;
    }
    
    private void DropItem(ItemData item)
    {
        // Spawn item at current position
        Debug.Log($"{name} dropped {item.ItemName}");
        // ItemManager.Instance.SpawnItem(GridX, GridY, item);
    }
    
    // TILE INTERACTIONS (in GridEntity!)
    private void HandleTileInteractions(GridTile tile)
    {
        if (tile.Type == TileType.Effect && tile.TileEffect != null)
        {
            OnSteppedOnEffect(tile);
        }
        
        if (tile.Type == TileType.Stairs)
        {
            OnSteppedStairs();
        }
        
        if (tile.Occupant != null && !tile.Occupant.BlocksMovement)
        {
            OnSteppedOnItem(tile.Occupant);
        }
    }
    
    protected virtual void OnSteppedOnEffect(GridTile tile)
    {
        var handlers = GetComponents<IEffectTileHandler>();
        foreach (var handler in handlers)
        {
            handler.ApplyTileEffect(tile.TileEffect);
        }
    }
    
    protected virtual void OnSteppedStairs()
    {
        if (Type == EntityType.Player)
        {
            var floorManager = FindFirstObjectByType<DungeonFloorManager>();
            if (floorManager != null)
            {
                Debug.Log($"Player stepping on stairs at {GridX},{GridY}");
            
                // CRITICAL: Clear movement state BEFORE changing floors
                var playerController = GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Clear movement
                    playerController.ClearMovementState();
                }
            
                // Now change floors
                floorManager.GoToNextFloor();
            }
        }
    }
    
    protected virtual void OnSteppedOnItem(GridEntity item)
    {
        var itemEntity = item.GetComponent<ItemEntity>();
        if (itemEntity != null)
        {
            PickUpItem(itemEntity);
        }
    }

    // Update TryInteractWithOccupant to use new Attack method
    public virtual bool TryInteractWithOccupant(GridEntity occupant, int dx, int dy)
    {
        if ((Type == EntityType.Player && occupant.Type == EntityType.Enemy) ||
            (Type == EntityType.Enemy && occupant.Type == EntityType.Player))
        {
            Attack(occupant);
            return true;
        }
        
        return false;
    }
    
    // IEffectTileHandler implementation
    public void ApplyTileEffect(EventTileEffect effect)
    {
        if (effect == null) return;
    
        // Check if this entity is affected by this effect
        bool affectsEntity = effect.AffectsPlayer && Type == EntityType.Player || effect.AffectsEnemies && Type == EntityType.Enemy;

        if (!affectsEntity) return;
    
        Debug.Log($"{name} activating effect: {effect.EffectName}");
    
        // Health effects
        if (effect.HealthChange != 0)
        {
            if (effect.HealthChange > 0) Heal(effect.HealthChange);
            else TakeDamage(-effect.HealthChange);
        }
    
        // Status effects - using new structure
        if (effect.StatusEffect.StatusEffectType != StatusEffectType.None && effect.StatusEffect.Duration > 0)
        {
            ApplyStatusEffect(effect.StatusEffect);
        }
    
        // Stat modifiers
        if (effect.Duration > 0 && (effect.AttackBonus != 0 || effect.DefenseBonus != 0 || effect.SpeedBonus != 0))
        {
            Debug.Log($"Temporary stats: +{effect.AttackBonus} ATK, +{effect.DefenseBonus} DEF, +{effect.SpeedBonus} SPD");
            // TODO: Implement temporary buffs
        }
    
        // Special effects
        if (effect.TeleportsToRandomFloor && Grid != null)
        {
            var randomPos = Grid.GetRandomFloorPosition();
            TryTeleport(randomPos.x, randomPos.y);
        }
    
        // Visual feedback
        if (!string.IsNullOrEmpty(effect.ActivationMessage))
        {
            Debug.Log(effect.ActivationMessage);
        }
    }
    
    // In GridEntity.cs
    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        // Default: apply effect normally
        Stats.ApplyStatusEffect(effect.StatusEffectType, effect.Duration);
    }
    
    public bool TryTeleport(int targetX, int targetY)
    {
        if (Grid == null || !Grid.InBounds(targetX, targetY)) return false;
        
        var targetTile = Grid.Tiles[targetX, targetY];
        if (!targetTile.Walkable) return false;
        
        Grid.Tiles[GridX, GridY].Occupant = null;
        GridX = targetX;
        GridY = targetY;
        transform.position = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
        
        if (BlocksMovement)
        {
            Grid.Tiles[GridX, GridY].Occupant = this;
        }
        
        return true;
    }
    
    // Update status effects (called each turn)
    public void UpdateStatusEffects()
    {
        stats.UpdateStatusEffects();
    }
    
    protected virtual void OnParalyzed()
    {
        // Base behavior: entity is paralyzed
        // Can be overridden by specific entities
    }
    
    protected virtual void OnStatusEffectCleared()
    {
        // Base behavior: status effect wears off
    }
}