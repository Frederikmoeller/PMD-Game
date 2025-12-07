using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public bool PlayersTurn = true;
    private List<Enemy> _enemies = new List<Enemy>();

    void Awake() => Instance = this;

    public void OnPlayerMoved()
    {
        PlayersTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }
    
    public void RegisterEnemy(Enemy enemy)
    {
        if (!_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }
    
    public void UnregisterEnemy(Enemy enemy)
    {
        _enemies.Remove(enemy);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        // Get all alive enemies
        List<Enemy> aliveEnemies = new List<Enemy>();
        foreach (Enemy enemy in _enemies)
        {
            var enemyEntity = enemy.GetComponent<GridEntity>();
            if (enemyEntity != null && enemyEntity.IsAlive)
            {
                aliveEnemies.Add(enemy);
            }
        }

        Debug.Log($"Enemy turn started. {aliveEnemies.Count} enemies will act.");

        // Process each enemy turn
        foreach (Enemy enemy in aliveEnemies)
        {
            if (enemy == null) continue;
            
            enemy.TakeTurn();
            
            // Wait for enemy to finish moving
            var entity = enemy.GetComponent<GridEntity>();
            if (entity != null)
            {
                while (entity.IsMoving)
                {
                    yield return null;
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Small delay between enemies
        }

        PlayersTurn = true;
        Debug.Log("Enemy turn complete. Player's turn.");
    }
}
