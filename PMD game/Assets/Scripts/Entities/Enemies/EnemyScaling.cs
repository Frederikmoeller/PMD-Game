using UnityEngine;

public class EnemyScaling
{
    public static EntityStats ScaleForFloor(EntityStats baseStats, int floorNumber)
    {
        // Copy base stats
        EntityStats scaled = new EntityStats();
        
        // Floor multiplier (enemies get 15% stronger per floor)
        float floorMultiplier = 1.0f + (floorNumber - 1) * 0.15f;
        
        // Scale stats (health scales more, fortune scales less)
        scaled.MaxHealth = Mathf.RoundToInt(baseStats.MaxHealth * floorMultiplier * 1.1f);
        scaled.Power = Mathf.RoundToInt(baseStats.Power * floorMultiplier);
        scaled.Focus = Mathf.RoundToInt(baseStats.Focus * floorMultiplier);
        scaled.Resilience = Mathf.RoundToInt(baseStats.Resilience * floorMultiplier);
        scaled.Willpower = Mathf.RoundToInt(baseStats.Willpower * floorMultiplier);
        scaled.Fortune = Mathf.RoundToInt(baseStats.Fortune * floorMultiplier * 0.8f);
        
        scaled.CurrentHealth = scaled.MaxHealth;
        scaled.Level = Mathf.Max(1, floorNumber / 5); // Every 5 floors = +1 level
        
        return scaled;
    }
}
