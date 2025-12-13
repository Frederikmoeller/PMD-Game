using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Enemy : GridEntity
{
    [Header("AI Settings")]
    public float MoveDelay = 0f; // Can be 0 for simultaneous movement
    public float AttackDelay = 0.5f;
    
    [Header("Enemy Configuration")]
    public CharacterPresetSO Preset;
    public int AttackRange = 1;
    // Components
    private SpriteRenderer _spriteRenderer;
    
    // Runtime state
    private GameObject _playerTarget;
    private GridEntity _playerEntity;
    private AStarPathfinding _pathfinding;
    private float _lastPathfindingTime = 0f;
    private float _pathfindingCooldown = 0.5f; // Only recalculate path every 0.5 seconds
    private Vector2Int? _cachedNextStep = null;
    private Vector2Int _cachedPlayerPosition;
    // Track if enemy has acted this turn
    private bool _hasActedThisTurn = false;

    void Start()
    {
        Type = EntityType.Enemy;

        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize from EnemyType
        InitializeFromPreset();
        
        // Find player for AI
        _playerTarget = GameObject.FindGameObjectWithTag("Player");
        if (_playerTarget != null)
        {
            _playerEntity = _playerTarget.GetComponent<GridEntity>();
            Debug.Log($"{name} found player at {_playerEntity.GridX},{_playerEntity.GridY}");
        }
        
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterEnemy(this);
            Debug.Log($"{name} registered with TurnManager");
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
    
    public enum ActionType
    {
        None,
        Move,
        Attack,
        Wait
    }
    
    public struct ActionDecision
    {
        public ActionType Type;
        public Vector2Int Direction;
    }
    
    
    // Call this at the start of enemy turn
    public void OnTurnStart()
    {
        _hasActedThisTurn = false;
    }
    
    public IEnumerator MoveSimultaneously()
    {
        if (!IsAlive || !CanMoveThisFrame() || _playerEntity == null || !_playerEntity.IsAlive)
            yield break;
        
        // If we're currently attacking or queued to attack, don't move
        if (IsCurrentlyAttacking || IsInAttackQueue())
            yield break;
        
        // Small optional delay (can be 0)
        if (MoveDelay > 0)
            yield return new WaitForSeconds(MoveDelay);
        
        Vector2Int enemyPos = new Vector2Int(GridX, GridY);
        Vector2Int playerPos = new Vector2Int(_playerEntity.GridX, _playerEntity.GridY);
        
        // Check if we should attack instead of move
        if (IsAdjacentTo(_playerEntity) && CanAttackThisFrame())
        {
            Debug.Log($"{name}: Player is adjacent, queuing attack instead of moving");
            TurnManager.Instance?.QueueAttack(this, _playerEntity);
            yield break;
        }
        
        // Try to move toward player
        Vector2Int? moveDirection = GetMoveDirectionToPlayer(enemyPos, playerPos);
        if (moveDirection.HasValue)
        {
            Debug.Log($"{name}: Moving toward player ({moveDirection.Value})");
            
            bool moveCompleted = false;
            
            // Start the move
            TryMove(moveDirection.Value.x, moveDirection.Value.y, () => {
                moveCompleted = true;
            });
            
            // Wait for move to complete
            while (!moveCompleted && IsAlive)
            {
                yield return null;
            }
            
            Debug.Log($"{name}: Move complete");
        }
        else
        {
            Debug.Log($"{name}: No valid move direction found");
        }
    }
    
    private bool IsInAttackQueue()
    {
        // Check if this enemy is already in the attack queue
        if (TurnManager.Instance == null) return false;
        
        // We need to access the private queue - for now, we'll assume if we're adjacent and can attack,
        // we'll be queued
        return IsAdjacentTo(_playerEntity) && CanAttackThisFrame();
    }
    
    public ActionDecision DecideAction(bool allowAttacks)
    {
        if (_hasActedThisTurn || !IsAlive || !CanMoveThisFrame())
        {
            return new ActionDecision { Type = ActionType.None };
        }
        
        if (_playerEntity == null || !_playerEntity.IsAlive)
        {
            // Find player again
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _playerEntity = playerObj.GetComponent<GridEntity>();
        }
        
        if (_playerEntity == null)
        {
            return new ActionDecision { Type = ActionType.Wait };
        }
        
        Vector2Int enemyPos = new Vector2Int(GridX, GridY);
        Vector2Int playerPos = new Vector2Int(_playerEntity.GridX, _playerEntity.GridY);
        
        // Check if we can attack
        if (allowAttacks && IsAdjacentTo(_playerEntity))
        {
            Vector2Int attackDirection = playerPos - enemyPos;
            _hasActedThisTurn = true;
            return new ActionDecision 
            { 
                Type = ActionType.Attack, 
                Direction = attackDirection 
            };
        }
        
        // Try to move toward player
        Vector2Int? moveDirection = GetMoveDirectionToPlayer(enemyPos, playerPos);
        if (moveDirection.HasValue)
        {
            _hasActedThisTurn = true;
            return new ActionDecision 
            { 
                Type = ActionType.Move, 
                Direction = moveDirection.Value 
            };
        }
        
        // Can't move or attack
        return new ActionDecision { Type = ActionType.Wait };
    }
    
    // Called from attack queue (sequential)
    public IEnumerator ExecuteAttack()
    {
        if (!IsAlive || _playerEntity == null || !_playerEntity.IsAlive)
            yield break;
        
        // Check if still adjacent to player
        if (!IsAdjacentTo(_playerEntity))
        {
            Debug.Log($"{name} can no longer reach player, skipping attack");
            yield break;
        }
        
        Vector2Int enemyPos = new Vector2Int(GridX, GridY);
        Vector2Int playerPos = new Vector2Int(_playerEntity.GridX, _playerEntity.GridY);
        Vector2Int attackDirection = playerPos - enemyPos;
        
        Debug.Log($"{name} executing attack on player");
        
        // Find target in attack direction
        int targetX = GridX + attackDirection.x;
        int targetY = GridY + attackDirection.y;
        
        if (Grid == null || !Grid.InBounds(targetX, targetY))
        {
            yield break;
        }
        
        GridTile targetTile = Grid.Tiles[targetX, targetY];
        GridEntity target = targetTile.Occupant;
        
        if (target != null && target.IsAlive)
        {
            // Play attack animation
            Vector3 startPos = transform.position;
            Vector3 attackPos = Vector3.Lerp(startPos, target.transform.position, 0.3f);
            
            float duration = 0.15f;
            float elapsed = 0f;
            
            // Move toward target
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, attackPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Perform the actual attack
            Attack(target);
            
            // Return to original position
            elapsed = 0f;
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(attackPos, startPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = startPos;
        }
        
        // Optional delay after attack
        if (AttackDelay > 0)
            yield return new WaitForSeconds(AttackDelay);
    }
    
    private Vector2Int? GetMoveDirectionToPlayer(Vector2Int enemyPos, Vector2Int playerPos)
    {
        // OPTIMIZATION 1: Check if player hasn't moved since last calculation
        bool playerMoved = playerPos != _cachedPlayerPosition;
        
        // OPTIMIZATION 2: Only recalculate path after cooldown
        bool canRecalculate = Time.time - _lastPathfindingTime > _pathfindingCooldown || playerMoved;
        
        if (!canRecalculate && _cachedNextStep.HasValue)
        {
            // Use cached result
            Vector2Int direction = _cachedNextStep.Value - enemyPos;
            if (CanMoveInDirection(direction.x, direction.y))
            {
                return direction;
            }
        }
        
        // Try A* pathfinding
        if (_pathfinding != null)
        {
            Vector2Int? nextStep = _pathfinding.GetNextStep(enemyPos, playerPos);
            
            // Cache the result
            _cachedNextStep = nextStep;
            _cachedPlayerPosition = playerPos;
            _lastPathfindingTime = Time.time;
            
            if (nextStep.HasValue && nextStep.Value != playerPos)
            {
                Vector2Int direction = nextStep.Value - enemyPos;
                if (CanMoveInDirection(direction.x, direction.y))
                {
                    return direction;
                }
            }
        }
        
        // Fallback to simple direction
        return GetSimpleDirectionToPlayer(enemyPos, playerPos);
    }
    
    public bool CanAttackThisFrame()
    {
        if (!IsAlive) return false;
        if (_playerEntity == null || !_playerEntity.IsAlive) return false;
        
        // Check if adjacent to player
        return IsAdjacentTo(_playerEntity);
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
        gameObject.name = Preset.name;
        
        // Set experience value (for when killed)
        Stats.ExperienceValue = Preset.ExperienceValue;
    }
    
    void ApplyPresetStats()
    {
        // Scale stats based on current floor
        int currentFloor = GameManager.Instance?.CurrentFloor ?? 1;
        EntityStats scaledStats = EnemyScaling.ScaleForFloor(Preset.BaseStats, currentFloor);
        
        // Copy scaled stats to entity
        Stats.MaxHealth = scaledStats.MaxHealth;
        Stats.CurrentHealth = scaledStats.CurrentHealth;
        Stats.Power = scaledStats.Power;
        Stats.Focus = scaledStats.Focus;
        Stats.Resilience = scaledStats.Resilience;
        Stats.Willpower = scaledStats.Willpower;
        Stats.Fortune = scaledStats.Fortune;
        Stats.Level = scaledStats.Level;
        
        // Copy growth rates from preset
        Stats.HealthGrowth = Preset.HealthGrowth;
        Stats.PowerGrowth = Preset.PowerGrowth;
        Stats.FocusGrowth = Preset.FocusGrowth;
        Stats.ResilienceGrowth = Preset.ResilienceGrowth;
        Stats.WillpowerGrowth = Preset.WillpowerGrowth;
        Stats.FortuneGrowth = Preset.FortuneGrowth;
    }
    
    // Add property for TurnManager

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

    public bool IsAdjacentTo(GridEntity other)
    {
        if (other == null) return false;
        
        int dx = Mathf.Abs(GridX - other.GridX);
        int dy = Mathf.Abs(GridY - other.GridY);
        bool isAdjacent = dx <= AttackRange && dy <= AttackRange;
        return isAdjacent;
    }
    
    void ExecuteAI(GridEntity player)
    {
        Vector2Int enemyPos = new Vector2Int(GridX, GridY);
        Vector2Int playerPos = new Vector2Int(player.GridX, player.GridY);
        
        // Calculate distance
        int distance = Mathf.Max(
            Mathf.Abs(enemyPos.x - playerPos.x),
            Mathf.Abs(enemyPos.y - playerPos.y)
        );
        if (IsAdjacentTo(player))
        {
            Debug.Log($"{name}: Attacking player (in range)");
            AttackPlayer();
            return;
        }

        if (_pathfinding == null)
        {
            Debug.Log($"{name}: No pathfinding available");
            OnEnemyMoveComplete();
            return;
        }
        Debug.Log($"{name}: Moving toward player");
        MoveTowardPlayer(enemyPos, playerPos);
    }

    IEnumerator RangedAttackAnimation(Vector3 targetPos)
    {
        // Create projectile or line effect
        // For now, just a visual indicator
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color original = sr.color;
        sr.color = Color.yellow;
        
        yield return new WaitForSeconds(0.2f);
        
        sr.color = original;
    }
    
    void MoveTowardPlayer(Vector2Int enemyPos, Vector2Int playerPos)
    {
        Debug.Log($"{name}: Attempting to move from {enemyPos} to player at {playerPos}");
    
        Vector2Int? nextStep = _pathfinding.GetNextStep(enemyPos, playerPos);

        if (!nextStep.HasValue)
        {
            // No path found - player might be unreachable
            Debug.Log($"{name}: No path found to player!");
            Debug.Log($"{name}: Trying simple movement as fallback...");
        
            // Try simple directional movement
            TrySimpleMovement(enemyPos, playerPos);
            return;
        }

        Vector2Int direction = nextStep.Value - enemyPos;
    
        Debug.Log($"{name}: Next step from {enemyPos} to {nextStep.Value} (direction {direction})");
    
        // Special case: if next step IS the player position, we should attack instead
        if (nextStep.Value == playerPos)
        {
            Debug.Log($"{name}: Next step is player position, should attack!");
            AttackPlayer();
            return;
        }

        Debug.Log($"{name}: CanMoveInDirection({direction.x}, {direction.y}) = {CanMoveInDirection(direction.x, direction.y)}");

        // Use GridEntity's smooth movement with callback
        if (CanMoveInDirection(direction.x, direction.y))
        {
            Debug.Log($"{name}: Moving in direction {direction}");
            bool moveSuccess = TryMove(direction.x, direction.y, OnEnemyMoveComplete);
            Debug.Log($"{name}: TryMove result = {moveSuccess}");
        }
        else
        {
            Debug.Log($"{name}: Cannot move in direction {direction}");
            Debug.Log($"{name}: Trying simple movement instead...");
            TrySimpleMovement(enemyPos, playerPos);
        }
    }
    
    void TrySimpleMovement(Vector2Int enemyPos, Vector2Int playerPos)
    {
        // Simple directional movement as fallback
        int dx = 0, dy = 0;
        
        // Move toward player in the primary direction
        if (Mathf.Abs(playerPos.x - enemyPos.x) > Mathf.Abs(playerPos.y - enemyPos.y))
        {
            // Move horizontally
            dx = playerPos.x > enemyPos.x ? 1 : -1;
        }
        else
        {
            // Move vertically
            dy = playerPos.y > enemyPos.y ? 1 : -1;
        }
        
        Debug.Log($"{name}: Simple movement direction ({dx},{dy})");
        
        if (CanMoveInDirection(dx, dy))
        {
            bool success = TryMove(dx, dy, OnEnemyMoveComplete);
            Debug.Log($"{name}: Simple move success = {success}");
        }
        else
        {
            Debug.Log($"{name}: Simple move also failed");
            OnEnemyMoveComplete();
        }
    }
    
    private Vector2Int? GetSimpleDirectionToPlayer(Vector2Int enemyPos, Vector2Int playerPos)
    {
        // Simple 4-direction movement (cheaper than 8-direction)
        List<Vector2Int> possibleDirections = new List<Vector2Int>
        {
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1)   // Down
        };
        
        // Try directions in order of closeness to player
        possibleDirections.Sort((a, b) => {
            float distA = Vector2Int.Distance(enemyPos + a, playerPos);
            float distB = Vector2Int.Distance(enemyPos + b, playerPos);
            return distA.CompareTo(distB);
        });
        
        // Try each direction
        foreach (Vector2Int dir in possibleDirections)
        {
            if (CanMoveInDirection(dir.x, dir.y))
            {
                return dir;
            }
        }
        
        return null;
    }


    void AttackPlayer()
    {
        Debug.Log($"{name} attacks player");
        Attack(_playerEntity, true);
        StartCoroutine(AttackAnimation());
        OnEnemyMoveComplete();
    }

    IEnumerator AttackAnimation()
    {
        if (_spriteRenderer != null)
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.black;
            yield return new WaitForSeconds(0.2f);
            _spriteRenderer.color = original;
        }
    }
    
    // Add callback for movement completion
    void OnEnemyMoveComplete()
    {
        Debug.Log($"{name} finished moving to {GridX},{GridY}");
        // Enemy movement complete, next enemy can move
    }
    
    // Override the death method from GridEntity
    protected override void OnDeathInternal(GridEntity killer = null)
    {
        base.OnDeathInternal(killer);
        
        // Enemy-specific death logic
        TryDropLoot();
        
        // Unregister from TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }
    }
    
    void TryDropLoot()
    {
        if (Preset == null || HeldItem == null) return;

        var drop = HeldItem;
    }
    
    void OnDrawGizmos()
    {
        if (Application.isPlaying && _playerEntity != null)
        {
            // Draw line to player
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _playerTarget.transform.position);
            
            // Draw enemy position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}