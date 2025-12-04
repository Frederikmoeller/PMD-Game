using DialogueSystem;
using DialogueSystem.Data;
using UnityEngine;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue Settings")]
    public DialogueAsset dialogueAsset;
    public bool canInteractMultipleTimes = false;
    public bool faceInteractor = true;
    
    [Header("Interaction Prompt")]
    public string interactionText = "Talk";
    public GameObject interactionPrompt;
    public Vector3 promptOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Conditions")]
    public bool requiresItem;
    public string requiredItemId;
    public bool consumesItem;
    
    private bool _hasInteracted = false;
    private SpriteRenderer _spriteRenderer;
    
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Hide prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        
        Debug.Log($"DialogueInteractable: {name} interacted with by {interactor.name}");
        
        // Face the interactor if configured
        if (faceInteractor && _spriteRenderer != null)
        {
            Vector3 direction = interactor.transform.position - transform.position;
            _spriteRenderer.flipX = direction.x > 0;
        }
        
        // Start dialogue
        if (dialogueAsset != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueAsset);
        }
        else
        {
            Debug.LogWarning($"DialogueInteractable: No dialogue asset or DialogueManager for {name}");
        }
        
        // Mark as interacted if needed
        if (!canInteractMultipleTimes)
        {
            _hasInteracted = true;
            ShowPrompt(false);
        }
        
        // Handle item consumption
        if (requiresItem && consumesItem)
        {
            // Remove item from player inventory
            // InventorySystem.Instance.RemoveItem(requiredItemId);
        }
    }
    
    public bool CanInteract(GameObject interactor)
    {
        // Check if already interacted (for one-time interactions)
        if (!canInteractMultipleTimes && _hasInteracted) return false;
        
        // Check item requirement
        if (requiresItem)
        {
            // Check if player has the required item
            // return InventorySystem.Instance.HasItem(requiredItemId);
            return true; // Placeholder
        }
        
        return true;
    }
    
    public string GetInteractionText()
    {
        return interactionText;
    }
    
    public void ShowPrompt(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show && CanInteract(null));
        }
    }
    
    public Vector3 GetInteractionPosition()
    {
        return transform.position + promptOffset;
    }
    
    // Optional: Reset interaction state
    public void ResetInteraction()
    {
        _hasInteracted = false;
    }
    
    // Optional: Change dialogue asset at runtime
    public void SetDialogueAsset(DialogueAsset newAsset)
    {
        dialogueAsset = newAsset;
        ResetInteraction();
    }
}