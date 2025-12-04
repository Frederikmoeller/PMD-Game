using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction Settings")] 
    public float InteractionRange = 2f;
    public LayerMask InteractableLayers = -1;
    public bool ShowDebugGizmos = true;

    [Header("UI Reference")] 
    public GameObject InteractionPromptPrefab;
    public Transform InteractionPromptContainer;

    [Header("Input")] 
    public InputActionReference InteractAction;

    private Camera _mainCamera;
    private List<IInteractable> _nearbyInteractables = new();
    private IInteractable _currentClosestInteractable;
    private GameObject _currentPrompt;
    
    // Events for other systems to hook into
    public Action<IInteractable> OnInteractableFound;
    public Action<IInteractable> OnInteractableLost;
    public Action<IInteractable> OnInteractionStarted;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _mainCamera = Camera.main;
        
        // Setup input
        if (InteractAction != null)
        {
            InteractAction.action.performed += OnInteractInput;
            InteractAction.action.Enable();
        }
        
        // Create prompt container if needed
        if (InteractionPromptContainer == null)
        {
            GameObject container = new GameObject("InteractionPrompts");
            container.transform.SetParent(transform);
            InteractionPromptContainer = container.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        FindNearbyInteractables();
        UpdateClosestInteractable();
        UpdateInteractionPrompt();
    }
    
        void FindNearbyInteractables()
    {
        // Clear previous list
        var oldInteractables = new List<IInteractable>(_nearbyInteractables);
        _nearbyInteractables.Clear();
        
        // Find all interactables in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, InteractionRange, InteractableLayers);
        
        foreach (Collider collider in colliders)
        {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                _nearbyInteractables.Add(interactable);
                
                // Notify if this is a new interactable
                if (!oldInteractables.Contains(interactable))
                {
                    OnInteractableFound?.Invoke(interactable);
                }
            }
        }
        
        // Notify for lost interactables
        foreach (var oldInteractable in oldInteractables)
        {
            if (!_nearbyInteractables.Contains(oldInteractable))
            {
                OnInteractableLost?.Invoke(oldInteractable);
                oldInteractable.ShowPrompt(false);
            }
        }
    }
    
    void UpdateClosestInteractable()
    {
        if (_nearbyInteractables.Count == 0)
        {
            _currentClosestInteractable = null;
            return;
        }
        
        // Find closest interactable
        IInteractable closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (var interactable in _nearbyInteractables)
        {
            // Need to get position - could be from GameObject or interface method
            Vector3 interactablePos = GetInteractablePosition(interactable);
            float distance = Vector3.Distance(transform.position, interactablePos);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }
        
        // Update current closest
        if (_currentClosestInteractable != closest)
        {
            if (_currentClosestInteractable != null)
            {
                _currentClosestInteractable.ShowPrompt(false);
            }
            
            _currentClosestInteractable = closest;
            
            if (_currentClosestInteractable != null)
            {
                _currentClosestInteractable.ShowPrompt(true);
            }
        }
    }
    
    void UpdateInteractionPrompt()
    {
        if (_currentClosestInteractable == null)
        {
            // Hide prompt
            if (_currentPrompt != null)
            {
                Destroy(_currentPrompt);
                _currentPrompt = null;
            }
            return;
        }
        
        // Create prompt if needed
        if (_currentPrompt == null && InteractionPromptPrefab != null)
        {
            _currentPrompt = Instantiate(InteractionPromptPrefab, InteractionPromptContainer);
        }
        
        // Update prompt position and text
        if (_currentPrompt != null)
        {
            Vector3 worldPos = GetInteractablePosition(_currentClosestInteractable);
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            
            // Offset above the interactable
            screenPos.y += 50f; // Adjust as needed
            
            _currentPrompt.transform.position = screenPos;
            
            // Update prompt text
            var promptText = _currentPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            if (promptText != null)
            {
                promptText.text = _currentClosestInteractable.GetInteractionText();
            }
        }
    }
    
    void OnInteractInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        if (_currentClosestInteractable != null)
        {
            InteractWithCurrent();
        }
    }
    
    public void InteractWithCurrent()
    {
        if (_currentClosestInteractable == null) return;
        
        OnInteractionStarted?.Invoke(_currentClosestInteractable);
        _currentClosestInteractable.Interact(gameObject);
    }
    
    public bool HasInteractableInRange()
    {
        return _currentClosestInteractable != null;
    }
    
    public IInteractable GetCurrentInteractable()
    {
        return _currentClosestInteractable;
    }
    
    private Vector3 GetInteractablePosition(IInteractable interactable)
    {
        // Try to get position from interface method first
        if (interactable is MonoBehaviour monoBehaviour)
        {
            return monoBehaviour.transform.position;
        }
        
        // Fallback to default
        return Vector3.zero;
    }
    
    void OnDestroy()
    {
        if (InteractAction != null)
        {
            InteractAction.action.performed -= OnInteractInput;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!ShowDebugGizmos) return;
        
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, InteractionRange);
        
        // Draw lines to nearby interactables
        Gizmos.color = Color.green;
        foreach (var interactable in _nearbyInteractables)
        {
            Vector3 pos = GetInteractablePosition(interactable);
            Gizmos.DrawLine(transform.position, pos);
        }
        
        // Highlight closest interactable
        if (_currentClosestInteractable != null)
        {
            Gizmos.color = Color.red;
            Vector3 closestPos = GetInteractablePosition(_currentClosestInteractable);
            Gizmos.DrawWireSphere(closestPos, 0.3f);
            Gizmos.DrawLine(transform.position, closestPos);
        }
    }
}
