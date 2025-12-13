// Add this class for bar management

using TMPro;
using UnityEngine;

[System.Serializable]
public class UIBar
{
    public RectTransform fillRect;
    public TextMeshProUGUI valueText;

    private float _maxWidth;
    private Vector2 _originalSize;
    
    public void Initialize()
    {
        if (fillRect != null)
        {
            _originalSize = fillRect.sizeDelta;
            _maxWidth = fillRect.sizeDelta.x;
        }
    }
    
    public void UpdateValue(int current, int max, Color? lowColor = null)
    {
        if (fillRect != null)
        {
            float percentage = (float)current / max;
            
            // Calculate new width
            float newWidth = _maxWidth * percentage;
            
            // Ensure it goes to exactly 0
            if (percentage <= 0.001f)
                newWidth = 0f;
            
            // Update width
            fillRect.sizeDelta = new Vector2(newWidth, _originalSize.y);
            
            // Optional: Anchor manipulation
            // fillRect.anchorMax = new Vector2(percentage, 1f);
        }
        
        if (valueText != null)
        {
            valueText.text = $"{current} / {max}";

            // Color coding for low values
            if (lowColor.HasValue && (float)current / max < 0.3f)
                valueText.color = lowColor.Value;
        }
    }
}
