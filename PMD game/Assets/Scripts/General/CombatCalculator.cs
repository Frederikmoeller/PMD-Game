using UnityEngine;

public class CombatCalculator
{
    // Core damage formula
    public static int CalculateDamage(EntityStats attacker, EntityStats defender, 
        bool isPowerAttack = true, float movePower = 1.0f)
    {
        // Base formula: (Attack Stat - Defense Stat) * Multipliers
        int attackStat = isPowerAttack ? attacker.Power : attacker.Focus;
        int defenseStat = isPowerAttack ? defender.Resilience : defender.Willpower;
        
        // Ensure minimum damage
        int baseDamage = Mathf.Max(1, attackStat - defenseStat);
        
        // Apply move power (0.5x for weak moves, 2.0x for strong)
        float damage = baseDamage * movePower;
        
        // Fortune adds variance (luck factor)
        float fortuneMultiplier = 1.0f + (attacker.Fortune - defender.Fortune) * 0.01f;
        damage *= fortuneMultiplier;
        
        // Random variance (85% to 115%)
        damage *= Random.Range(0.85f, 1.15f);
        
        // Critical hit chance based on fortune
        if (Random.value < attacker.Fortune * 0.01f)
        {
            damage *= 1.5f;
            Debug.Log("Critical hit!");
        }
        
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }
    
    // Heal calculation
    public static int CalculateHeal(EntityStats healer, float healPower = 1.0f)
    {
        // Healing based on Focus stat (mental/willpower)
        float baseHeal = healer.Focus * healPower;
        
        // Fortune bonus
        float fortuneBonus = 1.0f + (healer.Fortune * 0.005f);
        
        return Mathf.RoundToInt(baseHeal * fortuneBonus);
    }
}
