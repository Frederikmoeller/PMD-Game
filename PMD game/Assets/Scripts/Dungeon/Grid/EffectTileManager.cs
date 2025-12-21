using System.Collections.Generic;
using UnityEngine;

public class EffectTileManager : MonoBehaviour
{
    public static EffectTileManager Instance { get; private set; }
    
    [SerializeField] private List<EventTileEffect> _allPossibleEffects = new List<EventTileEffect>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public EventTileEffect GetRandomEffect()
    {
        if (_allPossibleEffects == null || _allPossibleEffects.Count == 0)
        {
            Debug.LogWarning("No effect tile effects assigned to EffectTileManager!");
            
            // Create a default effect as fallback
            return CreateDefaultEffect();
        }
        
        var effect = _allPossibleEffects[Random.Range(0, _allPossibleEffects.Count)];
        //Debug.Log($"Selected random effect: {effect.EffectName}");
        return effect;
    }
    
    private EventTileEffect CreateDefaultEffect()
    {
        // Create a default healing effect
        EventTileEffect defaultEffect = ScriptableObject.CreateInstance<EventTileEffect>();
        defaultEffect.EffectName = "Healing Spring";
        defaultEffect.Description = "Restores health";
        defaultEffect.TileColor = Color.green;
        defaultEffect.AffectsPlayer = true;
        defaultEffect.AffectsEnemies = false;
        defaultEffect.HealthChange = 10;
        defaultEffect.ActivationMessage = "You feel refreshed! +10 HP";
        
        return defaultEffect;
    }
    
    // Editor helper
    void OnValidate()
    {
        if (_allPossibleEffects.Count == 0)
        {
            Debug.LogWarning("Add some EventTileEffect assets to EffectTileManager!");
        }
    }
}