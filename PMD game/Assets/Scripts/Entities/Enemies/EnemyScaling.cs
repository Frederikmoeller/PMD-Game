using UnityEngine;

public class EnemyScaling
{
    public static EntityStats ScaleWithLevel(EntityStats baseStats, int level)
    {
        // Copy base stats
        EntityStats scaled = new EntityStats();
        
        // Floor multiplier (enemies get 15% stronger per level)
        float levelMultiplier = level * 0.15f;
        
        // Scale stats (health scales more, fortune scales less)
        scaled.MaxHealth = Mathf.RoundToInt(baseStats.MaxHealth * levelMultiplier * 1.1f);
        scaled.Power = Mathf.RoundToInt(baseStats.Power * levelMultiplier);
        scaled.Focus = Mathf.RoundToInt(baseStats.Focus * levelMultiplier);
        scaled.Resilience = Mathf.RoundToInt(baseStats.Resilience * levelMultiplier);
        scaled.Willpower = Mathf.RoundToInt(baseStats.Willpower * levelMultiplier);
        scaled.Fortune = Mathf.RoundToInt(baseStats.Fortune * levelMultiplier * 0.8f);
        
        scaled.CurrentHealth = scaled.MaxHealth;

        return scaled;
    }
}
