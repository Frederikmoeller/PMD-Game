using UnityEngine;

public class PlayerPersist : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        PlayerPersist[] existingPlayers = FindObjectsByType<PlayerPersist>(FindObjectsSortMode.None);
        if (existingPlayers.Length > 1)
        {
            Debug.Log($"Destroying duplicate player: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        Debug.Log($"Player {gameObject.name} marked as persistent");
        
        // Ensure player is properly initialized
        // Get or add required components
        if (GetComponent<GridEntity>() == null)
            gameObject.AddComponent<GridEntity>();
            
        if (GetComponent<PlayerStats>() == null)
            gameObject.AddComponent<PlayerStats>();
            
        if (GetComponent<PlayerController>() == null)
            gameObject.AddComponent<PlayerController>();
            
        if (GetComponent<InteractionSystem>() == null)
            gameObject.AddComponent<InteractionSystem>();
        
        // Tag as Player if not already
        if (!gameObject.CompareTag("Player"))
            gameObject.tag = "Player";
    }
    
    // Call this when returning to town
    public void RestoreForTown()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            // Full heal
            stats.CurrentHealth = stats.MaxHealth;
            // Clear status effects
            stats.ClearStatusEffects();
            Debug.Log("Player restored for town");
        }
    }
    
    // Call this before entering dungeon
    public void PrepareForDungeon()
    {
        // Any dungeon-specific preparation
        Debug.Log("Player prepared for dungeon");
    }
}
