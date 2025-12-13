using UnityEngine;
using DialogueSystem.Data;

[CreateAssetMenu(fileName = "DialogueEventChannel", menuName = "Events/DialogueEventChannel")]
public class DialogueEventChannel : ScriptableObject
{
    // Core dialogue events
    public event System.Action<DialogueAsset> OnStartDialogue;
    public event System.Action OnDialogueStarted;
    public event System.Action OnDialogueEnded;
    public event System.Action<DialogueLine> OnDialogueLineDisplayed;
    public event System.Action<DialogueChoice[]> OnChoicesPresented;
    public event System.Action<int> OnChoiceSelected;
    
    // Raise methods with null checks and logging
    public void RaiseStartDialogue(DialogueAsset asset)
    {
        if (asset == null)
        {
            Debug.LogWarning("[DialogueEvent] Attempted to raise StartDialogue with null asset");
            return;
        }
        
        Debug.Log($"[DialogueEvent] StartDialogue: {asset.name}");
        OnStartDialogue?.Invoke(asset);
    }
    
    public void RaiseDialogueStarted()
    {
        Debug.Log("[DialogueEvent] DialogueStarted");
        OnDialogueStarted?.Invoke();
    }
    
    public void RaiseDialogueEnded()
    {
        Debug.Log("[DialogueEvent] DialogueEnded");
        OnDialogueEnded?.Invoke();
    }
    
    public void RaiseDialogueLineDisplayed(DialogueLine line)
    {
        OnDialogueLineDisplayed?.Invoke(line);
    }
    
    public void RaiseChoicesPresented(DialogueChoice[] choices)
    {
        Debug.Log($"[DialogueEvent] ChoicesPresented: {choices?.Length} choices");
        OnChoicesPresented?.Invoke(choices);
    }
    
    public void RaiseChoiceSelected(int choiceIndex)
    {
        Debug.Log($"[DialogueEvent] ChoiceSelected: {choiceIndex}");
        OnChoiceSelected?.Invoke(choiceIndex);
    }
    
    // Optional: Helper to clear all listeners (useful for tests)
    public void ClearAllListeners()
    {
        OnStartDialogue = null;
        OnDialogueStarted = null;
        OnDialogueEnded = null;
        OnDialogueLineDisplayed = null;
        OnChoicesPresented = null;
        OnChoiceSelected = null;
    }
}
