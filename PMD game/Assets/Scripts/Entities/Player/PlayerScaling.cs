using UnityEngine;

public class PlayerScaling
{
    // Experience curve (gets steeper each level)
    public int GetExpForLevel(int level)
    {
        // Quadratic growth: 100, 300, 600, 1000, 1500, etc.
        return 50 * level * (level + 1);
    }
    
    // Level up stat increases
    public void LevelUpStats(EntityStats stats)
    {
        stats.Level++;
        
        // Base increases
        stats.MaxHealth = Mathf.RoundToInt(stats.MaxHealth * stats.HealthGrowth);
        stats.Power = Mathf.RoundToInt(stats.Power * stats.PowerGrowth);
        stats.Focus = Mathf.RoundToInt(stats.Focus * stats.FocusGrowth);
        stats.Resilience = Mathf.RoundToInt(stats.Resilience * stats.ResilienceGrowth);
        stats.Willpower = Mathf.RoundToInt(stats.Willpower * stats.WillpowerGrowth);
        stats.Fortune = Mathf.RoundToInt(stats.Fortune * stats.FortuneGrowth);
        
        // Full heal on level up
        stats.CurrentHealth = stats.MaxHealth;
    }
}
