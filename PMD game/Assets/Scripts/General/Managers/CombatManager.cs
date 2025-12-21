using System;
using System.Collections;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;

namespace GameSystem
{
    public class CombatManager : MonoBehaviour, IGameManagerListener
    {
        [Header("Combat Settings")] [SerializeField]
        private float _turnDelay = 0.3f;

        [SerializeField] private float _attackAnimationDuration = 0.5f;

        // State
        private List<Enemy> _activeEnemies = new();
        private PlayerStats _playerStats;
        private Queue<CombatAction> _actionQueue = new();

        public event Action<CombatAction> OnCombatAction;

        public int ActiveEnemyCount => _activeEnemies.Count;

        public void Initialize()
        {
            Debug.Log("CombatManager Initializing");

            // Find player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerStats = player.GetComponent<PlayerStats>();
            }

            // Register with TurnManager if it exists
            var turnManager = TurnManager.Instance;
            if (turnManager != null)
            {
                // Subscribe to turn events
                // This depends on your TurnManager implementation
            }

            Debug.Log("CombatManager initialized successfully");
        }

        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.InDungeon:
                case GameState.InTown:
                    break;
            }
        }

        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {

        }

        public void OnPauseStateChanged(bool paused)
        {
            // Pause combat animations, timers, etc.
            if (paused)
            {
                // Stop any combat coroutines
                StopAllCoroutines();
            }
        }

        // ===== ENEMY MANAGEMENT =====
        public void RegisterEnemy(Enemy enemy)
        {
            if (_activeEnemies.Contains(enemy)) return;
            _activeEnemies.Add(enemy);
            Debug.Log($"Enemy registered: {enemy.name}. Total: {_activeEnemies.Count}");
        }

        public void UnregisterEnemy(Enemy enemy)
        {
            _activeEnemies.Remove(enemy);
        }

        // ===== ACTION PROCESSING =====
        public void QueuePlayerAction(CombatAction action)
        {
            _actionQueue.Enqueue(action);
            ProcessNextAction();
        }

        public void QueueEnemyAction(Enemy enemy, CombatAction action)
        {
            _actionQueue.Enqueue(action);

            // If player just acted, process enemy response
            if (_actionQueue.Count == 1) // Only this action in queue
            {
                ProcessNextAction();
            }
        }

        private void ProcessNextAction()
        {
            if (_actionQueue.Count == 0) return;

            var action = _actionQueue.Dequeue();
            OnCombatAction?.Invoke(action);

            // Execute action
            StartCoroutine(ExecuteCombatAction(action));
        }

        private IEnumerator ExecuteCombatAction(CombatAction action)
        {
            // Freeze input during action
            GameManager.Instance.Input.SetCombatLock(true);

            // Play animation
            yield return StartCoroutine(PlayActionAnimation(action));

            // Apply effects
            ApplyActionEffects(action);

            // Check for deaths
            CheckForDeaths();

            // Small delay
            yield return new WaitForSeconds(_turnDelay);

            // Unfreeze input
            GameManager.Instance.Input.SetCombatLock(false);

            // Process next action if any
            if (_actionQueue.Count > 0)
            {
                ProcessNextAction();
            }
        }

        private IEnumerator PlayActionAnimation(CombatAction action)
        {
            // Move attacker toward target
            Vector3 attackerPos = action.Attacker.transform.position;
            Vector3 targetPos = action.Target.transform.position;
            Vector3 direction = (targetPos - attackerPos).normalized;

            // Store original position
            var originalPos = attackerPos;

            // Move forward
            float elapsed = 0f;
            while (elapsed < _attackAnimationDuration / 2)
            {
                elapsed += Time.deltaTime;
                action.Attacker.transform.position = Vector3.Lerp(
                    originalPos,
                    originalPos + direction * 0.3f,
                    elapsed / (_attackAnimationDuration / 2)
                );
                yield return null;
            }

            // Return to original position
            elapsed = 0f;
            while (elapsed < _attackAnimationDuration / 2)
            {
                elapsed += Time.deltaTime;
                action.Attacker.transform.position = Vector3.Lerp(
                    originalPos + direction * 0.3f,
                    originalPos,
                    elapsed / (_attackAnimationDuration / 2)
                );
                yield return null;
            }

            // Ensure exact position
            action.Attacker.transform.position = originalPos;
        }

        private void ApplyActionEffects(CombatAction action)
        {
            // Apply damage
            if (action.Damage > 0)
            {
                var targetStats = action.Target.GetComponent<GridEntity>();
                if (targetStats != null)
                {
                    targetStats.TakeDamage(action.Damage);

                    // Log to action log
                    GameManager.Instance.Ui.AddLogEntry(
                        $"{action.Attacker.name} hits {action.Target.name} for {action.Damage} damage!",
                        action.IsPlayerAction ? Color.cyan : Color.red
                    );
                }
            }

            // Apply status effects
            if (action.StatusEffects != null)
            {
                foreach (var effect in action.StatusEffects)
                {
                    // Apply effect to target
                    // This would depend on your status effect system
                }
            }

            // Apply healing
            if (action.Healing > 0)
            {
                var targetStats = action.Target.GetComponent<GridEntity>();
                if (targetStats != null)
                {
                    targetStats.Heal(action.Healing);

                    GameManager.Instance.Ui.AddLogEntry(
                        $"{action.Attacker.name} heals {action.Target.name} for {action.Healing} HP!",
                        Color.green
                    );
                }
            }
        }

        private void CheckForDeaths()
        {
            // Check player
            if (_playerStats != null && !_playerStats.IsAlive)
            {
                PlayerDeath();
                return;
            }

            // Check enemies
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] != null && !_activeEnemies[i].IsAlive)
                {
                    var enemy = _activeEnemies[i];
                    UnregisterEnemy(enemy);

                    // Award XP
                    if (_playerStats != null)
                    {
                        _playerStats.AddExperience(enemy.Stats.ExperienceValue);
                    }

                    // Destroy enemy
                    Destroy(enemy.gameObject);
                }
            }
        }

        private void PlayerDeath()
        {
            Debug.Log("Player died in combat");

            // Handle player death
            GameManager.Instance.Dungeon.ExitDungeon();
        }

        public bool IsEnemyAdjacentToPlayer(Enemy enemy)
        {
            if (_playerStats == null || enemy == null) return false;

            var playerPos = new Vector2Int(_playerStats.GridX, _playerStats.GridY);
            var enemyPos = new Vector2Int(enemy.GridX, enemy.GridY);

            return Mathf.Abs(playerPos.x - enemyPos.x) <= 1 &&
                   Mathf.Abs(playerPos.y - enemyPos.y) <= 1;
        }

        public void Reset()
        {
            _activeEnemies.Clear();
            _actionQueue.Clear();
        }
    }

    // Combat action data structure
    [Serializable]
    public struct CombatAction
    {
        public GameObject Attacker;
        public GameObject Target;
        public int Damage;
        public int Healing;
        public string[] StatusEffects;
        public bool IsPlayerAction;
        public string ActionName;
    }
}
