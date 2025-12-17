using UnityEngine;
using System.Collections;
using DialogueSystem;

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
    [SerializeField] protected EntityStats stats = new EntityStats();
    public EntityStats Stats => stats;
    public bool IsAlive => stats.IsAlive;
    
    // Inventory (for players/enemies that can hold items)
    public ItemData HeldItem = null;
    
    // Events
    public System.Action<int> OnHealthChanged;
    public System.Action<int> OnDamageTaken;
    public System.Action OnDeath;
    public System.Action<GridEntity> OnItemPickedUp;
    
    [Header("Movement Settings")]
    public float MoveSpeed = 5f;
    public bool UseSmoothMovement = true;
    public bool IsMoving { get; private set; }
    
    [Header("Attack System")]
    public float AttackCooldown = 0.5f;
    public float AttackAnimationDuration = 0.3f;
    public float AttackPushbackDistance = 0.3f;
    
    private bool _canAttack = true;
    private bool _isAttacking = false;
    private Coroutine _currentAttackCoroutine;
    private System.Action _onMoveComplete;
    private Vector3 _moveTarget;
    
    [Header("Player Preset")]
    public CharacterPresetSO CharacterPreset;

    public virtual void Start()
    {
        Debug.Log($"[{name}] GridEntity.Start() called");
        Debug.Log($"[{name}] CharacterPreset: {(CharacterPreset != null ? CharacterPreset.name : "NULL")}");
    
        InitializeFromPreset();
    
        Debug.Log($"[{name}] After init - stats.Name: '{stats.Name}'");
        Debug.Log($"[{name}] After init - gameObject.name: '{gameObject.name}'");
    }

    private void Update()
    {
        if (UseSmoothMovement && IsMoving)
        {
            UpdateMovement();
        }
    }
    
    // Make this VIRTUAL so child classes can override
    public virtual void InitializeFromPreset(int level = 1)
    {
        Debug.Log($"[{name}] InitializeFromPreset() called");
    
        if (CharacterPreset == null)
        {
            Debug.LogWarning($"[{name}] No CharacterPreset assigned!");
        
            // Fallback
            if (string.IsNullOrEmpty(stats.Name))
            {
                stats.Name = gameObject.name;
                Debug.Log($"[{name}] Set stats.Name from GameObject: '{stats.Name}'");
            }
            return;
        }
    
        Debug.Log($"[{name}] Setting name from preset: '{CharacterPreset.CharacterName}'");
        stats.Name = CharacterPreset.CharacterName;
        
        // Set name from preset
        stats.Name = CharacterPreset.CharacterName;
        
        // Copy base stats from preset
        stats.MaxHealth = CharacterPreset.BaseStats.MaxHealth;
        stats.CurrentHealth = CharacterPreset.BaseStats.CurrentHealth;
        stats.Power = CharacterPreset.BaseStats.Power;
        stats.Focus = CharacterPreset.BaseStats.Focus;
        stats.Resilience = CharacterPreset.BaseStats.Resilience;
        stats.Willpower = CharacterPreset.BaseStats.Willpower;
        stats.Fortune = CharacterPreset.BaseStats.Fortune;
        
        // Copy growth rates
        stats.HealthGrowth = CharacterPreset.HealthGrowth;
        stats.PowerGrowth = CharacterPreset.PowerGrowth;
        stats.FocusGrowth = CharacterPreset.FocusGrowth;
        stats.ResilienceGrowth = CharacterPreset.ResilienceGrowth;
        stats.WillpowerGrowth = CharacterPreset.WillpowerGrowth;
        stats.FortuneGrowth = CharacterPreset.FortuneGrowth;
        
        // Set GameObject name for clarity
        gameObject.name = CharacterPreset.CharacterName;
        Debug.Log($"[{name}] Updated GameObject name to: '{gameObject.name}'");
        
        
    }
    
    // ========== MOVEMENT SYSTEM ==========
    
    public void SetGrid(DungeonGrid g) => Grid = g;

    public virtual bool TryMove(int dx, int dy, System.Action onComplete = null)
    {
        if (GameManager.Instance?.IsGridBased != true) return false;
        if (Grid == null) return false;

        int targetX = GridX + dx;
        int targetY = GridY + dy;

        if (!Grid.InBounds(targetX, targetY)) return false;

        var targetTile = Grid.Tiles[targetX, targetY];
        if (!targetTile.Walkable) return false;

        // Check for BLOCKING occupants
        if (targetTile.Occupant != null && targetTile.Occupant.BlocksMovement && targetTile.Occupant != this)
        {
            return false;
        }

        // Diagonal clearance check
        if (dx != 0 && dy != 0 && !CanMoveDiagonally(dx, dy))
        {
            return false;
        }

        // Clear old position
        if (Grid.InBounds(GridX, GridY))
        {
            Grid.Tiles[GridX, GridY].Occupant = null;
        }

        // Update position
        GridX = targetX;
        GridY = targetY;

        if (BlocksMovement)
        {
            Grid.Tiles[GridX, GridY].Occupant = this;
        }
    
        // Handle movement visualization
        if (UseSmoothMovement)
        {
            _moveTarget = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
            IsMoving = true;
            _onMoveComplete = onComplete;
        }
        else
        {
            transform.position = new Vector3(GridX + 0.5f, GridY + 0.5f, 0);
            HandleTileInteractions(targetTile);
            onComplete?.Invoke();
        }
    
        return true;
    }

    private bool CanMoveDiagonally(int dx, int dy)
    {
        // Check both adjacent cardinal tiles
        if (Grid.InBounds(GridX + dx, GridY))
        {
            var horizontalTile = Grid.Tiles[GridX + dx, GridY];
            if (!horizontalTile.Walkable || 
                (horizontalTile.Occupant != null && horizontalTile.Occupant.BlocksMovement && horizontalTile.Occupant != this))
            {
                return false;
            }
        }

        if (Grid.InBounds(GridX, GridY + dy))
        {
            var verticalTile = Grid.Tiles[GridX, GridY + dy];
            if (!verticalTile.Walkable || 
                (verticalTile.Occupant != null && verticalTile.Occupant.BlocksMovement && verticalTile.Occupant != this))
            {
                return false;
            }
        }

        return true;
    }
    
    private void UpdateMovement()
    {
        if (!IsMoving) return;
        
        transform.position = Vector3.MoveTowards(transform.position, _moveTarget, MoveSpeed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, _moveTarget) < 0.01f)
        {
            transform.position = _moveTarget;
            IsMoving = false;
            
            if (Grid != null)
            {
                HandleTileInteractions(Grid.Tiles[GridX, GridY]);
            }
            
            _onMoveComplete?.Invoke();
        }
    }
    
    // ========== MOVEMENT HELPERS ==========
    
    public bool TryMoveToward(Vector2Int direction, System.Action onComplete = null)
    {
        return TryMove(direction.x, direction.y, onComplete);
    }
    
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
        if (!stats.IsAlive) return false;
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.IsGamePaused) return false;
        if (GameManager.Instance.IsInCutscene) return false;
        if (DialogueManager.Instance.IsDialogueActive) return false;
        if (!GameManager.Instance.IsGridBased) return true;
        if (TurnManager.Instance == null) return false;

        // Check status effects
        if (stats.HasStatusEffect(StatusEffectType.Sleep) || 
            stats.HasStatusEffect(StatusEffectType.Paralysis))
        {
            return false;
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
    
        // DIAGONAL MOVEMENT: Use the same check as TryMove
        if (dx != 0 && dy != 0 && !CanMoveDiagonally(dx, dy))
        {
            return false;
        }
    
        return true;
    }
    
    // ========== COMBAT SYSTEM ==========
    
    public bool CanAttackThisFrame()
    {
        return IsAlive && _canAttack && !_isAttacking;
    }
    
    public bool IsCurrentlyAttacking => _isAttacking;
    
    public void TakeDamage(int damage, GridEntity source = null, bool isPowerAttack = true)
    {
        if (!IsAlive) return;

        stats.CurrentHealth -= damage;
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
    
    // Unified attack method
    public void Attack(GridEntity target, bool isPowerAttack = true, float movePower = 1.0f)
    {
        if (!IsAlive || !target.IsAlive) return;
        
        int damage = CombatCalculator.CalculateDamage(
            stats, 
            target.stats, 
            isPowerAttack, 
            movePower
        );
        
        Debug.Log($"{name} attacks {target.name} for {damage} damage!");
        target.TakeDamage(damage, this, isPowerAttack);
        GameLogger.LogCombat(stats.Name, "attacked", target.stats.Name, damage);
    }
    
    // Animated attack for TurnManager
    public IEnumerator PerformAttack(GridEntity target, System.Action onAttackComplete = null)
    {
        if (!IsAlive || !target.IsAlive || !_canAttack || _isAttacking)
        {
            onAttackComplete?.Invoke();
            yield break;
        }
        
        _isAttacking = true;
        _canAttack = false;
        
        Debug.Log($"{name} performing attack on {target.name}");
        
        // Play attack animation
        yield return StartCoroutine(PlayAttackAnimation(target.transform.position));
        
        // Apply damage
        Attack(target);
        
        // Wait for any additional effects
        yield return new WaitForSeconds(0.1f);
        
        _isAttacking = false;
        StartCoroutine(AttackCooldownRoutine());
        onAttackComplete?.Invoke();
    }
    
    private IEnumerator PlayAttackAnimation(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 attackPos = Vector3.Lerp(startPos, targetPosition, AttackPushbackDistance);
        
        float halfDuration = AttackAnimationDuration / 2f;
        
        // Lunge forward
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(startPos, attackPos, t / halfDuration);
            yield return null;
        }
        
        // Return to position
        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(attackPos, startPos, t / halfDuration);
            yield return null;
        }
        
        transform.position = startPos;
    }
    
    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(AttackCooldown);
        _canAttack = true;
    }
    
    public void QueueAttack(GridEntity target)
    {
        if (_currentAttackCoroutine != null)
        {
            StopCoroutine(_currentAttackCoroutine);
        }
        
        _currentAttackCoroutine = StartCoroutine(PerformAttack(target, OnAttackComplete));
    }
    
    private void OnAttackComplete()
    {
        _currentAttackCoroutine = null;
        
        // If this entity is the player, notify turn manager
        if (Type == EntityType.Player)
        {
            TurnManager.Instance?.PlayerPerformedAction();
        }
    }
    
    // ========== INTERACTION & COMBAT HELPERS ==========
    
    public bool TryAttackAdjacent(GridEntity target)
    {
        if (target == null || !IsAlive || !target.IsAlive) return false;
    
        // Check if adjacent
        if (!IsAdjacentTo(target)) return false;
    
        // Queue the attack
        if (CanAttackThisFrame())
        {
            TurnManager.Instance?.QueueAttack(this, target);
            return true;
        }
        
        return false;
    }
    
    public bool IsAdjacentTo(GridEntity other)
    {
        if (other == null || Grid == null) return false;
        if (Grid != other.Grid) return false;
        
        int dx = Mathf.Abs(GridX - other.GridX);
        int dy = Mathf.Abs(GridY - other.GridY);
        
        return dx <= 1 && dy <= 1 && (dx != 0 || dy != 0);
    }
    
    // ========== DEATH & ITEM SYSTEM ==========
    
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
            // Player handles XP through PlayerStats
        }
        
        // Drop held item
        if (HeldItem != null)
        {
            // ItemManager.Instance.SpawnItem(GridX, GridY, HeldItem);
            Debug.Log($"{name} dropped {HeldItem.ItemName}");
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Handle destruction based on type
        if (Type == EntityType.Player)
        {
            gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(DeathAnimation());
        }
    }
    
    private IEnumerator DeathAnimation()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            float duration = 0.5f;
            
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                sr.color = Color.Lerp(original, Color.clear, t / duration);
                yield return null;
            }
        }
        
        Destroy(gameObject);
    }
    
    public virtual void PickUpItem(ItemEntity item)
    {
        // Remove item from world
        item.OnPickedUp(this);
        OnItemPickedUp?.Invoke(this);
    }
    
    // ========== TILE INTERACTIONS ==========
    
    private void HandleTileInteractions(GridTile tile)
    {
        if (tile.Type == TileType.Effect && tile.TileEffect != null)
        {
            OnSteppedOnEffect(tile);
            GameLogger.LogAction("Stepped on effect tile");
        }
        
        if (tile.Type == TileType.Stairs)
        {
            OnSteppedStairs();
        }
        
        if (tile.ItemOnTile)
        {
            PickUpItem(tile.OccupyingItem);
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
            
                // Clear movement state before changing floors
                var playerController = GetComponent<PlayerController>();
                playerController?.ClearMovementState();
            
                // Change floors
                floorManager.GoToNextFloor();
            }
        }
    }

    // ========== EFFECT SYSTEM ==========
    
    public void ApplyTileEffect(EventTileEffect effect)
    {
        if (effect == null) return;
    
        // Check if this entity is affected
        bool affectsEntity = (effect.AffectsPlayer && Type == EntityType.Player) || 
                            (effect.AffectsEnemies && Type == EntityType.Enemy);

        if (!affectsEntity) return;
    
        Debug.Log($"{name} activating effect: {effect.EffectName}");
    
        // Health effects
        if (effect.HealthChange != 0)
        {
            if (effect.HealthChange > 0) Heal(effect.HealthChange);
            else stats.TakeStatusDamage(-effect.HealthChange);
        }
    
        // Status effects
        if (effect.StatusEffect.Type != StatusEffectType.None && effect.StatusEffect.Duration > 0)
        {
            ApplyStatusEffect(effect.StatusEffect);
        }
    
        // Special effects
        if (effect.TeleportsToRandomFloor && Grid != null)
        {
            var randomPos = Grid.GetRandomFloorPosition();
            TryTeleport(randomPos.x, randomPos.y);
        }
    }
    
    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        stats.ApplyStatusEffect(effect.Type, effect.Duration);
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
}