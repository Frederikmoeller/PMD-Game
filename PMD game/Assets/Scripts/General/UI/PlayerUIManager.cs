using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystem
{
    public class PlayerUIManager : MonoBehaviour
    {
        [Header("UI Bars")]
        [SerializeField] private UiBar _healthBar;
        [SerializeField] private UiBar _manaBar;

        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI experienceText;
        
        [Header("Colors")]
        [SerializeField] private Color highHealthColor = Color.green;
        [SerializeField] private Color mediumHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color manaColor = Color.blue;
        [SerializeField] private Color experienceColor = Color.magenta;

        // State
        private PlayerStats _playerStats;
        private int _currentLevel = 1;
        private int _currentXP = 0;
        private int _xpToNextLevel = 100;

        // Animation coroutines
        private Coroutine _healthAnimationCoroutine;
        private Coroutine _manaAnimationCoroutine;
        private Coroutine _xpAnimationCoroutine;

        private void Start()
        {
            // Initialize UI bars
            if (_healthBar != null) _healthBar.Initialize();
            if (_manaBar != null) _manaBar.Initialize();
        }

        public void Initialize(PlayerStats playerStats)
        {
            _playerStats = playerStats;

            if (_playerStats != null)
            {
                // Update all UI elements with initial values
                UpdateHealth(_playerStats.Stats.CurrentHealth, _playerStats.Stats.MaxHealth, false);
                UpdateMana(_playerStats.Stats.CurrentMana, _playerStats.Stats.MaxMana, false);
                UpdateLevelUI(_playerStats.Stats.Level);
            }

            // Configure health color thresholds
            if (_healthBar != null)
            {
                _healthBar.LowHealthColor = lowHealthColor;
                _healthBar.LowHealthThreshold = 0.3f;
            }

            // Set mana color
            if (_manaBar != null && _manaBar.Fill != null)
            {
                _manaBar.Fill.color = manaColor;
            }
        }

        // ===== HEALTH =====
        public void UpdateHealth(int currentHealth, int maxHealth, bool animate = true)
        {
            if (_healthBar == null) return;

            // Update the health bar
            _healthBar.UpdateValue(currentHealth, maxHealth, lowHealthColor);

            // Update text separately (in case you want custom formatting)
            if (_healthBar.ValueText != null)
            {
                _healthBar.ValueText.text = $"{currentHealth} / {maxHealth}";
            }

            // Update color based on percentage
            UpdateHealthAnimated(currentHealth, maxHealth);
        }

        private void UpdateHealthColor(int currentHealth, int maxHealth)
        {
            if (_healthBar == null || _healthBar.Fill == null) return;

            float percentage = (float)currentHealth / maxHealth;

            if (percentage > 0.6f)
                _healthBar.Fill.color = highHealthColor;
            else if (percentage > 0.3f)
                _healthBar.Fill.color = mediumHealthColor;
            else
                _healthBar.Fill.color = lowHealthColor;
        }

        // ===== MANA =====
        public void UpdateMana(int currentMana, int maxMana, bool animate = true)
        {
            if (_manaBar == null) return;

            // Update the mana bar
            _manaBar.UpdateValue(currentMana, maxMana, manaColor);

            // Update text separately
            if (_manaBar.ValueText != null)
            {
                _manaBar.ValueText.text = $"{currentMana} / {maxMana}";
            }

            // Ensure mana color stays consistent
            if (_manaBar.Fill != null)
            {
                _manaBar.Fill.color = manaColor;
            }
        }

        // ===== LEVEL =====
        public void UpdateLevel(int level)
        {
            UpdateLevelUI(level);
        }

        private void UpdateLevelUI(int level)
        {
            _currentLevel = level;

            if (levelText != null)
                levelText.text = $"Level {level}";

            // Recalculate XP needed for next level when level changes
            _xpToNextLevel = CalculateXPForNextLevel(level);
        }

        private int CalculateXPForNextLevel(int currentLevel)
        {
            // Example formula: base XP * level multiplier
            // You can replace this with your actual leveling formula
            return 100 + (currentLevel * 50);
        }

        // ===== ANIMATED UPDATES =====
        public void UpdateHealthAnimated(int targetHealth, int maxHealth, float duration = 0.5f)
        {
            if (_healthBar == null) return;

            // Start animation coroutine
            if (_healthAnimationCoroutine != null)
                StopCoroutine(_healthAnimationCoroutine);

            _healthAnimationCoroutine = StartCoroutine(AnimateBarValue(
                _healthBar, 
                _playerStats.Stats.CurrentHealth, 
                targetHealth, 
                maxHealth, 
                duration,
                () => _healthAnimationCoroutine = null
            ));
        }

        public void UpdateManaAnimated(int targetMana, int maxMana, float duration = 0.5f)
        {
            if (_manaBar == null) return;

            if (_manaAnimationCoroutine != null)
                StopCoroutine(_manaAnimationCoroutine);

            _manaAnimationCoroutine = StartCoroutine(AnimateBarValue(
                _manaBar,
                _playerStats.Stats.CurrentMana,
                targetMana,
                maxMana,
                duration,
                () => _manaAnimationCoroutine = null
            ));
        }

        private System.Collections.IEnumerator AnimateBarValue(UiBar bar, int startValue, int endValue, int maxValue, float duration, System.Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                // Calculate interpolated value
                int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, EaseInOut(t)));
                
                // Update bar with interpolated value
                bar.UpdateValue(currentValue, maxValue);
                
                yield return null;
            }

            // Ensure final value
            bar.UpdateValue(endValue, maxValue);
            
            onComplete?.Invoke();
        }

        private float EaseInOut(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        // ===== VISIBILITY CONTROL =====
        public void ShowHealthBar(bool show)
        {
            if (_healthBar != null && _healthBar.Fill != null)
                _healthBar.Fill.transform.parent.gameObject.SetActive(show);
        }

        public void ShowManaBar(bool show)
        {
            if (_manaBar != null && _manaBar.Fill != null)
                _manaBar.Fill.transform.parent.gameObject.SetActive(show);
        }

        // ===== CLEANUP =====
        private void OnDestroy()
        {
            // Stop any running coroutines
            if (_healthAnimationCoroutine != null)
                StopCoroutine(_healthAnimationCoroutine);

            if (_manaAnimationCoroutine != null)
                StopCoroutine(_manaAnimationCoroutine);

            if (_xpAnimationCoroutine != null)
                StopCoroutine(_xpAnimationCoroutine);
        }
    }
}