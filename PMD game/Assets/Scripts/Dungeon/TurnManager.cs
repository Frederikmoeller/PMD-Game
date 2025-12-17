using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    
    private List<Enemy> _enemies = new List<Enemy>();
    private Queue<AttackData> _attackQueue = new Queue<AttackData>();
    private bool _isProcessingAttacks = false;
    
    private PlayerController _playerController;
    private GridEntity _playerEntity;
    private bool _playerIsFrozen = false;
    
    void Awake() => Instance = this;
    
    void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (_playerController != null)
        {
            _playerEntity = _playerController.GetComponent<GridEntity>();
        }
    }
    
    void Update()
    {
        if (!_isProcessingAttacks && _attackQueue.Count > 0)
        {
            StartCoroutine(ProcessAttackQueue());
        }
    }
    
    // NEW: Unified method for ANY player action
    public void PlayerPerformedAction(bool wasAttack = false)
    {
        Debug.Log($"Player performed action: {(wasAttack ? "Attack" : "Move")}");
        
        // Freeze player during enemy responses
        FreezePlayer(true);
        
        // Player turn over - check for enemy attacks
        CheckForEnemyAttacks();
        
        
        // If no attacks queued, unfreeze player immediately
        if (_attackQueue.Count == 0)
        {
            Debug.Log("No enemy responses, player can continue");
            FreezePlayer(false);
        }
    }
    
    private void CheckForEnemyAttacks()
    {
        Debug.Log("Checking for enemy attacks after player move");
        
        foreach (Enemy enemy in _enemies)
        {
            if (enemy != null && enemy.IsAlive && enemy.CanAttackThisFrame())
            {
                if (enemy.IsAdjacentTo(_playerEntity))
                {
                    QueueAttack(enemy, _playerEntity);
                    Debug.Log($"{enemy.name} will attack player (adjacent)");
                }
            }
        }
    }

    public void MoveAllEnemies()
    {
        Debug.Log("Moving all enemies");
        
        foreach (Enemy enemy in _enemies)
        {
            if (enemy != null && enemy.IsAlive && enemy.CanMoveThisFrame() && !enemy.IsCurrentlyAttacking)
            {
                // Don't move if they're already attacking
                if (!IsEnemyInAttackQueue(enemy))
                {
                    StartCoroutine(enemy.MoveSimultaneously());
                }
            }
        }
    }
    
    private bool IsEnemyInAttackQueue(Enemy enemy)
    {
        foreach (AttackData attack in _attackQueue)
        {
            if (attack.Attacker == enemy)
                return true;
        }
        return false;
    }
    
    // Queue any attack
    public void QueueAttack(GridEntity attacker, GridEntity target)
    {
        if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
            return;
        
        _attackQueue.Enqueue(new AttackData
        {
            Attacker = attacker,
            Target = target
        });
        
        Debug.Log($"Attack queued: {attacker.name} -> {target.name}");
    }
    
    private IEnumerator ProcessAttackQueue()
    {
        _isProcessingAttacks = true;
        
        Debug.Log($"=== PROCESSING ATTACK QUEUE: {_attackQueue.Count} ATTACKS ===");
        
        while (_attackQueue.Count > 0)
        {
            AttackData attack = _attackQueue.Dequeue();
            
            if (attack.Attacker != null && attack.Attacker.IsAlive && 
                attack.Target != null && attack.Target.IsAlive)
            {
                Debug.Log($"Executing: {attack.Attacker.name} -> {attack.Target.name}");
                yield return attack.Attacker.StartCoroutine(
                    attack.Attacker.PerformAttack(attack.Target)
                );
                
                // Small delay between attacks
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        Debug.Log("=== ALL ATTACKS COMPLETE ===");
        _isProcessingAttacks = false;
        
        // Update status effects after all attacks
        UpdateAllStatusEffects();
        
        // Unfreeze player after all attacks are done
        FreezePlayer(false);
    }
    
    private void FreezePlayer(bool freeze)
    {
        _playerIsFrozen = freeze;
        
        if (_playerController != null)
        {
            _playerController.SetCanMove(!freeze);
            Debug.Log(freeze ? "Player frozen" : "Player unfrozen");
        }
    }
    
    public bool IsPlayerFrozen => _playerIsFrozen;
    public bool IsProcessingAttacks => _isProcessingAttacks;
    
    public void RegisterEnemy(Enemy enemy)
    {
        if (!_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }
    
    public void UnregisterEnemy(Enemy enemy)
    {
        _enemies.Remove(enemy);
        // Clean up attack queue
        var newQueue = new Queue<AttackData>();
        while (_attackQueue.Count > 0)
        {
            AttackData attack = _attackQueue.Dequeue();
            if (attack.Attacker != enemy && attack.Target != enemy)
                newQueue.Enqueue(attack);
        }
        _attackQueue = newQueue;
    }
    
    private void UpdateAllStatusEffects()
    {
        if (_playerEntity != null)
        {
            _playerEntity.UpdateStatusEffects();
        }
        
        foreach (Enemy enemy in _enemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                enemy.UpdateStatusEffects();
            }
        }
    }
    
    private struct AttackData
    {
        public GridEntity Attacker;
        public GridEntity Target;
    }
}