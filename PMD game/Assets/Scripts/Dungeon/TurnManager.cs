using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public bool playersTurn = true;

    void Awake() => Instance = this;

    public void OnPlayerMoved()
    {
        playersTurn = false;
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        // Get all enemies and have them take their turn
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            enemy.TakeTurn(); 
            yield return new WaitForSeconds(0.1f);
            Debug.Log($"Enemy turn started. {enemies.Length} enemies will act.");
        }
        playersTurn = true;
    }
}
