using DialogueSystem;
using DialogueSystem.Data;
using UnityEngine;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue Settings")]
    public DialogueAsset DialogueAsset;
    public bool CanInteractMultipleTimes = false;
    public bool FaceInteractor = true;
    
    [Header("Interaction Prompt")]
    public string InteractionText = "Talk";
    public GameObject InteractionPrompt;
    public Vector3 PromptOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Conditions")]
    public bool RequiresItem;
    public string RequiredItemId;
    public bool ConsumesItem;
    
    private bool _hasInteracted = false;
    private SpriteRenderer _spriteRenderer;
    
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Hide prompt initially
        if (InteractionPrompt != null)
        {
            InteractionPrompt.SetActive(false);
        }
    }
    
    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        
        Debug.Log($"DialogueInteractable: {name} interacted with by {interactor.name}");
        
        // Face the interactor if configured
        if (FaceInteractor && _spriteRenderer != null)
        {
            Vector3 direction = interactor.transform.position - transform.position;
            _spriteRenderer.flipX = direction.x > 0;
        }
        
        // Start dialogue
        if (DialogueAsset != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(DialogueAsset);
        }
        else
        {
            Debug.LogWarning($"DialogueInteractable: No dialogue asset or DialogueManager for {name}");
        }
        
        // Mark as interacted if needed
        if (!CanInteractMultipleTimes)
        {
            _hasInteracted = true;
            ShowPrompt(false);
        }
        
        // Handle item consumption
        if (RequiresItem && ConsumesItem)
        {
            // Remove item from player inventory
            // InventorySystem.Instance.RemoveItem(requiredItemId);
        }
    }
    
    public bool CanInteract(GameObject interactor)
    {
        // Check if already interacted (for one-time interactions)
        if (!CanInteractMultipleTimes && _hasInteracted) return false;
        
        // Check item requirement
        if (RequiresItem)
        {
            // Check if player has the required item
            // return InventorySystem.Instance.HasItem(requiredItemId);
            return true; // Placeholder
        }
        
        return true;
    }
    
    public string GetInteractionText()
    {
        return InteractionText;
    }
    
    public void ShowPrompt(bool show)
    {
        if (InteractionPrompt != null)
        {
            InteractionPrompt.SetActive(show && CanInteract(null));
        }
    }
    
    public Vector3 GetInteractionPosition()
    {
        return transform.position + PromptOffset;
    }
    
    // Optional: Reset interaction state
    public void ResetInteraction()
    {
        _hasInteracted = false;
    }
    
    // Optional: Change dialogue asset at runtime
    public void SetDialogueAsset(DialogueAsset newAsset)
    {
        DialogueAsset = newAsset;
        ResetInteraction();
    }
}