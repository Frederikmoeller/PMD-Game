// ActionLogManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ActionLogManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform LogContainer;
    public GameObject LogEntryPrefab;
    public ScrollRect ScrollRect;
    public int MaxEntries = 10;
    
    private Queue<GameObject> _logEntries = new();
    
    public void Initialize()
    {
        ClearLog();
    }
    
    public void AddEntry(string message, Color? color = null)
    {
        if (LogEntryPrefab == null || LogContainer == null) return;
    
        // Create new log entry
        var entry = Instantiate(LogEntryPrefab, LogContainer);
        TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
    
        if (text != null)
        {
            text.text = $"> {message}";
            if (color.HasValue)
                entry.transform.GetChild(0).GetComponent<Image>().color = color.GetValueOrDefault();
        }
    
        // Add to queue
        _logEntries.Enqueue(entry);
    
        // Remove oldest if exceeds max
        if (_logEntries.Count > MaxEntries)
        {
            GameObject oldest = _logEntries.Dequeue();
            Destroy(oldest);
        }
    
        // Force layout update
        StartCoroutine(ForceLayoutUpdate());
    
        // Scroll to bottom
        if (ScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            ScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private System.Collections.IEnumerator ForceLayoutUpdate()
    {
        // Force update in next frame
        yield return null;
    
        LayoutRebuilder.ForceRebuildLayoutImmediate(LogContainer as RectTransform);
    
        // If using Content Size Fitter
        ContentSizeFitter fitter = LogContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.SetLayoutVertical();
        }
    }
    
    public void AddCombatEntry(string attacker, string action, string target, int damage = 0)
    {
        string message = $"{attacker} {action} {target}";
        if (damage > 0)
            message += $" for {damage} damage";
        else
            message += " but missed!";

        AddEntry(message, Color.red);
    }
    
    public void AddItemEntry(string playerName, string itemName)
    {
        AddEntry($"{playerName} found {itemName}", Color.green);
    }
    
    public void AddSystemEntry(string message)
    {
        AddEntry(message, Color.yellow);
    }
    
    public void ClearLog()
    {
        foreach (Transform child in LogContainer)
        {
            Destroy(child.gameObject);
        }
        _logEntries.Clear();
        
        // Rebuild layout after clearing
        if (LogContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(LogContainer as RectTransform);
        }
    }
}
