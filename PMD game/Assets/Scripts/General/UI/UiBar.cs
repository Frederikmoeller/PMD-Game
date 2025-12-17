using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UIBar
{
    public Image fill;
    public RectTransform fillRect;
    public TextMeshProUGUI valueText;
    
    [Header("Settings")]
    public bool useAnchorBasedFill = true; // Most health bars use this
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;
    
    private float _maxWidth;
    private Vector2 _originalSize;
    private RectTransform _parentRect; // Parent container for width calculation
    
    public void Initialize()
    {
        if (fillRect == null)
        {
            Debug.LogError("UIBar: fillRect is not assigned!");
            return;
        }
        
        // Store parent for width calculation
        _parentRect = fillRect.parent as RectTransform;
        
        if (useAnchorBasedFill)
        {
            // For anchored bars (most common), we need the parent width
            if (_parentRect != null)
            {
                _maxWidth = _parentRect.rect.width;
                //Debug.Log($"Initialized anchored bar with max width: {_maxWidth}");
            }
            else
            {
                Debug.LogError("UIBar: Parent RectTransform not found for anchor-based fill!");
            }
            
            // Set initial anchor for left-aligned fill
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            // For sizeDelta-based bars
            _originalSize = fillRect.sizeDelta;
            _maxWidth = fillRect.sizeDelta.x;
            //Debug.Log($"Initialized sizeDelta bar with max width: {_maxWidth}");
        }
        
        // Ensure fill starts at 100%
        UpdateValue(100, 100);
    }
    
    public void UpdateValue(int current, int max, Color? customLowColor = null)
    {
        if (fillRect == null) return;
        
        // Clamp values
        current = Mathf.Clamp(current, 0, max);
        float percentage = max > 0 ? (float)current / max : 0f;
        
        // Update fill
        if (useAnchorBasedFill)
        {
            UpdateAnchorBasedFill(percentage);
        }
        else
        {
            UpdateSizeDeltaFill(percentage);
        }
        
        // Update text
        if (valueText != null)
        {
            valueText.text = $"{current} / {max}";
            
            // Color coding for low values
            Color lowColor = customLowColor ?? lowHealthColor;
            if (percentage < lowHealthThreshold)
            {
                fill.color = lowColor;
            }
            else
            {
                fill.color = fill.transform.parent.name == "Mana" ? Color.blue : Color.green; // Reset to default
            }
        }
        
        //Debug.Log($"Health updated: {current}/{max} ({percentage:P0}) - Width: {fillRect.sizeDelta.x}");
    }
    
    private void UpdateAnchorBasedFill(float percentage)
    {
        // For anchored bars, we adjust anchorMax.x
        fillRect.anchorMax = new Vector2(percentage, 1f);
        
        // Also set offset to 0 so it stretches from left anchor to percentage anchor
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }
    
    private void UpdateSizeDeltaFill(float percentage)
    {
        // Calculate new width
        float newWidth = _maxWidth * percentage;
        
        // Ensure it goes to exactly 0
        if (percentage <= 0.001f)
            newWidth = 0f;
        
        // Update width only, keep height the same
        fillRect.sizeDelta = new Vector2(newWidth, _originalSize.y);
    }

    private void AnimateBar(int targetCurrent, int targetMax, float duration)
    {
        // Get current percentage from bar
        float startPercentage = useAnchorBasedFill ? fillRect.anchorMax.x : (fillRect.sizeDelta.x / _maxWidth);
        float targetPercentage = (float)targetCurrent / targetMax;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentPercentage = Mathf.Lerp(startPercentage, targetPercentage, t);
            
            if (useAnchorBasedFill)
            {
                UpdateAnchorBasedFill(currentPercentage);
            }
            else
            {
                UpdateSizeDeltaFill(currentPercentage);
            }
        }
        
        // Ensure final value is exact
        UpdateValue(targetCurrent, targetMax);
    }
}