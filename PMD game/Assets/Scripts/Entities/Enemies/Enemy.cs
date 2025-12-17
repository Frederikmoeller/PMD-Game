using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Enemy : GridEntity
{
    [Header("AI Settings")]
    public float MoveDelay = 0f;
    public float AttackDelay = 0.5f;

    // Runtime state
    private GridEntity _playerEntity;
    private AStarPathfinding _pathfinding;
    private float _lastPathfindingTime = 0f;
    private float _pathfindingCooldown = 0.5f;
    private Vector2Int? _cachedNextStep = null;
    private Vector2Int _cachedPlayerPosition;
    private bool _hasActedThisTurn = false;
    private bool _hasMoved;

    public override void Start()
    {
        base.Start();
        Type = EntityType.Enemy;
        
        // GridEntity.Start() already calls InitializeFromPreset()
        // But we might need enemy-specific initialization
        
        FindPlayer();
        
        // Register with TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterEnemy(this);
        }
    }
    
    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }
    }
    
    // ========== TURN SYSTEM METHODS ==========
    
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
        
        if (MoveDelay > 0)
            yield return new WaitForSeconds(MoveDelay);
        
        Vector2Int enemyPos = new Vector2Int(GridX, GridY);
        Vector2Int playerPos = new Vector2Int(_playerEntity.GridX, _playerEntity.GridY);
        
        // Check if we should attack instead of move
        if (IsAdjacentTo(_playerEntity) && CanAttackThisFrame())
        {
            Debug.Log($"{name}: Player is adjacent, queuing attack");
            TurnManager.Instance?.QueueAttack(this, _playerEntity);
            yield break;
        }
        
        // Try to move toward player
        Vector2Int? moveDirection = GetMoveDirectionToPlayer(enemyPos, playerPos);
        if (moveDirection.HasValue)
        {
            bool moveCompleted = false;
            _hasMoved = false;
            TryMove(moveDirection.Value.x, moveDirection.Value.y, () => {
                moveCompleted = true;
                _hasMoved = true;
            });
            
            while (!moveCompleted && IsAlive)
            {
                yield return null;
            }
        }
    }
    
    // ========== COMBAT METHODS ==========
    
    // This is the ONLY attack method needed - used by TurnManager
    public IEnumerator ExecuteAttack()
    {
        if (!IsAlive || _playerEntity == null || !_playerEntity.IsAlive)
            yield break;
        
        // Use the inherited PerformAttack method from GridEntity
        yield return StartCoroutine(PerformAttack(_playerEntity));
        
        if (AttackDelay > 0)
            yield return new WaitForSeconds(AttackDelay);
    }
    
    // Helper method for TurnManager
    public new bool CanAttackThisFrame()
    {
        if (!IsAlive) return false;
        if (_playerEntity == null || !_playerEntity.IsAlive || !_hasMoved) return false;
        
        return IsAdjacentTo(_playerEntity);
    }
    
    private bool IsInAttackQueue()
    {
        if (TurnManager.Instance == null) return false;
        return IsAdjacentTo(_playerEntity) && CanAttackThisFrame();
    }
    
    // ========== AI MOVEMENT LOGIC ==========
    
    private Vector2Int? GetMoveDirectionToPlayer(Vector2Int enemyPos, Vector2Int playerPos)
    {
        // Optimization: Check if player hasn't moved since last calculation
        bool playerMoved = playerPos != _cachedPlayerPosition;
        bool canRecalculate = Time.time - _lastPathfindingTime > _pathfindingCooldown || playerMoved;
        
        if (!canRecalculate && _cachedNextStep.HasValue)
        {
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
    
    private Vector2Int? GetSimpleDirectionToPlayer(Vector2Int enemyPos, Vector2Int playerPos)
    {
        // Simple 4-direction movement
        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1)   // Down
        };
        
        // Sort by distance to player
        directions.Sort((a, b) => {
            float distA = Vector2Int.Distance(enemyPos + a, playerPos);
            float distB = Vector2Int.Distance(enemyPos + b, playerPos);
            return distA.CompareTo(distB);
        });
        
        // Try each direction
        foreach (Vector2Int dir in directions)
        {
            if (CanMoveInDirection(dir.x, dir.y))
            {
                return dir;
            }
        }
        
        return null;
    }
    
    // ========== INITIALIZATION ==========
    
    private void FindPlayer()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerEntity = playerObj.GetComponent<GridEntity>();
        }
    }

    public override void PickUpItem(ItemEntity item)
    {
        if (Type == EntityType.Enemy && HeldItem == null)
        {
            HeldItem = item.ItemData;
            Debug.Log($"{name} picked up {item.ItemData.ItemName}");
        }
        base.PickUpItem(item);
    }
    
    // OPTIONAL: Override if enemies need special initialization
    public override void InitializeFromPreset(int level = 1)
    {
        // Call base first to get standard initialization
        base.InitializeFromPreset(level);
        
        if (CharacterPreset == null) return;

        // Enemy-specific scaling
        stats.Level = level;
        EntityStats scaledStats = EnemyScaling.ScaleWithLevel(CharacterPreset.BaseStats, CharacterPreset.BaseStats.Level);
        
        // Apply scaled stats
        stats.MaxHealth = scaledStats.MaxHealth;
        stats.CurrentHealth = scaledStats.CurrentHealth;
        stats.Power = scaledStats.Power;
        stats.Focus = scaledStats.Focus;
        stats.Resilience = scaledStats.Resilience;
        stats.Willpower = scaledStats.Willpower;
        stats.Fortune = scaledStats.Fortune;

        // Set experience value
        Stats.ExperienceValue = CharacterPreset.ExperienceValue;
    }
    
    // ========== OVERRIDES ==========
    
    protected override void OnDeathInternal(GridEntity killer = null)
    {
        base.OnDeathInternal(killer);
        
        // Drop loot if holding item
        if (HeldItem != null)
        {
            // ItemManager.Instance.SpawnItem(GridX, GridY, HeldItem);
        }
    }
    
    // ========== EDITOR DEBUGGING ==========
    
    void OnDrawGizmos()
    {
        if (Application.isPlaying && _playerEntity != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _playerEntity.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}