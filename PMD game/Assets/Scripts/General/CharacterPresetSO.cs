// Create this as a new C# file: CharacterPresetSO.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPreset", menuName = "Stats/Character Preset")]
public class CharacterPresetSo : ScriptableObject
{
    public string CharacterName;
    public EntityStats BaseStats;
    public EnemyType Archetype; // You'll need to define this enum
    
    [Header("Growth Rates")]
    public float HealthGrowth = 1.1f;
    public float PowerGrowth = 1.08f;
    public float FocusGrowth = 1.08f;
    public float ResilienceGrowth = 1.07f;
    public float WillpowerGrowth = 1.07f;
    public float FortuneGrowth = 1.05f;
    
    [Header("Enemy Specific")]
    public int ExperienceValue = 25;
    public ItemData[] PossibleDrops;
    public int SpawnWeight = 1;
}
