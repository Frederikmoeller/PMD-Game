using UnityEngine;

[CreateAssetMenu(fileName = "EnemyType", menuName = "Scriptable Objects/EnemyType")]
public class EnemyType : ScriptableObject
{
    [Header("Basic Info")]
    public string DisplayName;
    public string Description;
    public Sprite Sprite;
    public Color Color = Color.white;
    
    [Header("Base Stats")]
    public int BaseHealth = 50;
    public int BaseAttack = 8;
    public int BaseDefense = 3;
    public int BaseSpeed = 4;
    public int BaseExperience = 25;
    
    [Header("Resistances")]
    public bool ImmuneToPoison = false;
    public bool ImmuneToParalysis = false;
    public bool ImmuneToBurn = false;
    public bool ImmuneToSleep = false;
    
    [Header("Combat")]
    public int AttackRange = 1; // 1 = melee, 2+ = ranged
    public bool HasRangedAttack = false;
    public int RangedAttackDamage = 5;
    public int RangedAttackRange = 3;
    
    [Header("Special Abilities")]
    public bool CanFly = false;
    public bool IsAquatic = false;
    public bool Regenerates = false;
    public bool IsBoss = false;
    public int RegenerationAmount = 5;
    
    [Header("Loot")]
    public ItemData[] PossibleDrops;
    [Range(0f, 1f)] public float DropChance = 0.3f;
    
    [Header("AI Behavior")]
    public int AggroRange = 5;
    public bool FleesAtLowHealth = false;
    public float FleeHealthPercent = 0.3f;
}
