// PlayerUIManager.cs
using UnityEngine;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Player Info")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerLevelText;
    
    [Header("Bars")]
    public UIBar healthBar;
    public UIBar manaBar;
    
    private PlayerStats _playerStats;
    
    public void Initialize(PlayerStats playerStats)
    {
        _playerStats = playerStats;
        
        if (playerStats != null)
        {
            // Setup listeners if PlayerStats has events
            if (playerStats.OnPlayerHealthChanged != null)
                playerStats.OnPlayerHealthChanged.AddListener(UpdateHealth);
            
            if (playerStats.OnPlayerManaChanged != null)
                playerStats.OnPlayerManaChanged.AddListener(UpdateMana);
            
            if (playerStats.OnPlayerLevelUp != null)
                playerStats.OnPlayerLevelUp.AddListener(UpdateLevel);
            
            // Initial update
            UpdatePlayerName(playerStats.PlayerPreset?.CharacterName ?? "Player");
            UpdateLevel(playerStats.Stats.Level);
            UpdateHealth(playerStats.Stats.CurrentHealth);
            UpdateMana(playerStats.Stats.CurrentMana);
            
            // Initialize bars
            if (healthBar != null) healthBar.Initialize();
            if (manaBar != null) manaBar.Initialize();
        }
    }
    
    public void UpdatePlayerName(string name)
    {
        if (playerNameText != null)
            playerNameText.text = name;
    }
    
    public void UpdateLevel(int level)
    {
        if (playerLevelText != null)
            playerLevelText.text = $"Lvl {level}";
    }
    
    public void UpdateHealth(int currentHealth)
    {
        if (healthBar != null && _playerStats != null)
            healthBar.UpdateValue(currentHealth, _playerStats.Stats.MaxHealth, Color.red);
    }
    
    public void UpdateMana(int currentMana)
    {
        if (manaBar != null && _playerStats != null)
            manaBar.UpdateValue(currentMana, _playerStats.Stats.MaxMana);
    }
    
    private void OnDestroy()
    {
        // Clean up listeners
        if (_playerStats != null)
        {
            if (_playerStats.OnPlayerHealthChanged != null)
                _playerStats.OnPlayerHealthChanged.RemoveListener(UpdateHealth);
            
            if (_playerStats.OnPlayerManaChanged != null)
                _playerStats.OnPlayerManaChanged.RemoveListener(UpdateMana);
            
            if (_playerStats.OnPlayerLevelUp != null)
                _playerStats.OnPlayerLevelUp.RemoveListener(UpdateLevel);
        }
    }
}