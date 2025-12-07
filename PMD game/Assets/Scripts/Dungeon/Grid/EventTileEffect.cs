using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EventTileEffect", menuName = "Scriptable Objects/EventTileEffect")]
public class EventTileEffect : ScriptableObject
{
    [Header("Basic Info")] 
    public string EffectName;
    [TextArea] public string Description;
    
    [Header("Visual")]
    public Color TileColor = Color.cyan;
    public Sprite EffectSprite;
    public GameObject VisualEffectPrefab;

    [Header("Effects on Step")] 
    public bool AffectsPlayer = true;
    public bool AffectsEnemies = true;
    
    [Header("Health Effects")]
    public int HealthChange = 0; // Positive for healing, negative for damage
    
    [Header("Status Effects")]
    public StatusEffect StatusEffect;

    [Header("Stat Modifiers (Temporary)")]
    public int AttackBonus = 0;
    public int DefenseBonus = 0;
    public int SpeedBonus = 0;
    public float Duration = 0f; // 0 = permanent for this floor, >0 = temporary in turns
    
    [Header("Special Effects")]
    public bool TeleportsToRandomFloor = false;
    public bool SpawnsItem = false;
    public GameObject ItemPrefab;
    public bool RevealsHiddenTiles = false;
    public int RevealRadius = 3;
    
    [Header("Audio/Visual Feedback")]
    public AudioClip ActivationSound;
    public string ActivationMessage = "";
}

[Serializable]
public class StatusEffect
{
    public StatusEffectType StatusEffectType;
    public int Duration;
}

public enum StatusEffectType
{
    None,
    Poison,
    Paralysis,
    Burn,
    Sleep,
    Confusion,
    SpeedBoost,
    DefenseBoost,
    AttackBoost,
    Slow,
    Weakening,
    DefenseDown,
    StatusClear,
}
