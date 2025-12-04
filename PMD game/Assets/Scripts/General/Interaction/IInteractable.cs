using UnityEngine;

public interface IInteractable
{
    // Basic interaction
    void Interact(GameObject interactor);
    
    // Optional: Can be interacted with right now?
    bool CanInteract(GameObject interactor);
    
    // Optional: Get interaction text for UI
    string GetInteractionText();
    
    // Optional: Show/hide interaction prompt
    void ShowPrompt(bool show);
    
    // Optional: Get interaction position (for UI placement)
    Vector3 GetInteractionPosition();
}
