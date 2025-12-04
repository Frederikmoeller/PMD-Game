using SaveSystem;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Vital Stats")]
    [SaveField] public int MaxHealth = 100;
    [SaveField] public int CurrentHealth = 100;
    [SaveField] public int Attack = 10;
    [SaveField] public int Defense = 5;
    [SaveField] public int Speed = 5;

    [Header("Experience/Level")]
    [SaveField] public int Level = 1;
    [SaveField] public int Experience = 0;
    [SaveField] public int ExperienceToNextLevel = 100;

    [Header("Status Effects")]
    public bool IsPoisoned = false;
    public bool IsParalyzed = false;
    public int StatusEffectDuration = 0;

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged; // current health
    public UnityEvent<int> OnDamageTaken; // damage amount
    public UnityEvent OnPlayerDeath;
    public UnityEvent<int> OnLevelUp; // new level

    private PlayerController _playerController;

    void Start()
    {
        _playerController = GetComponent<PlayerController>();
        CurrentHealth = MaxHealth;
        OnHealthChanged?.Invoke(CurrentHealth);
    }

    public void TakeDamage(int damage)
    {
        // Apply defense
        int actualDamage = Mathf.Max(1, damage - Defense);
        CurrentHealth -= actualDamage;
        
        Debug.Log($"Player took {actualDamage} damage. Health: {CurrentHealth}/{MaxHealth}");
        
        OnDamageTaken?.Invoke(actualDamage);
        OnHealthChanged?.Invoke(CurrentHealth);
        
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth);
        Debug.Log($"Player healed {amount}. Health: {CurrentHealth}/{MaxHealth}");
    }

    public void AddExperience(int exp)
    {
        Experience += exp;
        Debug.Log($"Gained {exp} EXP. Total: {Experience}/{ExperienceToNextLevel}");
        
        while (Experience >= ExperienceToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        Experience -= ExperienceToNextLevel;
        ExperienceToNextLevel = Mathf.RoundToInt(ExperienceToNextLevel * 1.5f);
        
        // Increase stats
        MaxHealth += 10;
        Attack += 2;
        Defense += 1;
        Speed += 1;
        
        // Heal on level up
        CurrentHealth = MaxHealth;
        
        Debug.Log($"Level up! Now level {Level}");
        OnLevelUp?.Invoke(Level);
        OnHealthChanged?.Invoke(CurrentHealth);
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnPlayerDeath?.Invoke();
        
        // Trigger game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver(false);
        }
    }

    public void ApplyStatusEffect(StatusEffectType effect, int duration)
    {
        switch (effect)
        {
            case StatusEffectType.Poison:
                IsPoisoned = true;
                break;
            case StatusEffectType.Paralysis:
                IsParalyzed = true;
                _playerController?.SetCanMove(false);
                break;
        }
        StatusEffectDuration = duration;
    }

    public void UpdateStatusEffects()
    {
        if (StatusEffectDuration > 0)
        {
            StatusEffectDuration--;
            
            if (IsPoisoned)
            {
                TakeDamage(5); // Poison damage per turn
            }
            
            if (StatusEffectDuration <= 0)
            {
                ClearStatusEffects();
            }
        }
    }

    public void ClearStatusEffects()
    {
        IsPoisoned = false;
        IsParalyzed = false;
        _playerController?.SetCanMove(true);
        StatusEffectDuration = 0;
    }
}