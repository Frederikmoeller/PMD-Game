using System.Collections;
using DialogueSystem;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private PlayerStats _playerStats;
    private GridEntity _gridEntity;
    
    [Header("Sprint Settings")]
    public float SprintMultiplier = 2f; // 2x speed when sprinting
    public float FreeSprintSpeed = 10f; // Absolute speed for free movement sprint

    // Sprint state
    private bool _isSprinting = false;
    private InputAction _sprintAction;

    // Input
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    
    // Free Movement
    public float FreeMoveSpeed = 5f;
    public float GridMoveSpeed = 5f;

    // Free movement
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;
    
    private Vector2 _lastMoveDirection = Vector2.right;
    private Vector2 _lastRawInput = Vector2.zero;
    private float _directionChangeThreshold = 0.1f;
    private bool _isAttacking = false;

    // Add to PlayerController.cs
    [Header("Interaction")]
    public float InteractionRange = 1.5f;
    private InputAction _interactAction;

    void Start()
    {
        _playerStats = GetComponent<PlayerStats>();
        _gridEntity = _playerStats;
        _playerInput = GetComponent<PlayerInput>();
        
        // Setup interact input
        if (_playerInput != null)
        {
            _interactAction = _playerInput.actions["Interact"];
            if (_interactAction == null)
            {
                Debug.LogWarning("No 'Interact' action found in Input System!");
            }
        }
        
        // Setup sprint input
        if (_playerInput != null)
        {
            _sprintAction = _playerInput.actions["Sprint"];
            if (_sprintAction == null)
            {
                Debug.LogWarning("No 'Sprint' action found in Input System!");
            }
        }
        
        // Setup input
        if (_playerInput == null) _playerInput = gameObject.AddComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        
        // Setup rigidbody
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.linearDamping = 10f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        
        // Subscribe to events
        if (_gridEntity != null)
        {
            _gridEntity.OnDeath += OnPlayerDeath;
            _gridEntity.OnHealthChanged += OnHealthChanged;
            _gridEntity.MoveSpeed = GridMoveSpeed; // Set movement speed
        }
    }
    
    void Update()
    {
        if (!_playerStats.CanMoveThisFrame()) return;
        
        // Update direction based on movement input
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        UpdateDirection(moveInput);
        
        // Update sprint state
        UpdateSprintState();
        
        if (GameManager.Instance?.IsGridBased == true)
        {
            // Grid-based movement
            if (!_gridEntity.IsMoving)
            {
                HandleGridInput();
                
                // Handle interact/attack input
                if (ShouldInteract())
                {
                    TryInteractOrAttack();
                }
            }
        }
        else
        {
            // Free movement - handled in FixedUpdate
            // No grid input needed
        }
    }
    
    void FixedUpdate()
    {
        // Only handle free movement physics in FixedUpdate
        if (GameManager.Instance?.IsGridBased == true) return;
        if (!_playerStats.CanMoveThisFrame()) return;
        
        
        HandleFreeMovementFixed();
    }
    
    void UpdateSprintState()
    {
        bool sprintPressed = _sprintAction != null && _sprintAction.ReadValue<float>() > 0.5f;

        if (sprintPressed && !_isSprinting)
        {
            StartSprinting();
        }
        else if (!sprintPressed && _isSprinting)
        {
            StopSprinting();
        }
    }
    
    void StartSprinting()
    {
        _isSprinting = true;
        
        // Apply sprint speed
        if (GameManager.Instance?.IsGridBased == true)
        {
            // Grid mode: increase movement speed
            _gridEntity.MoveSpeed = GridMoveSpeed * SprintMultiplier;
        }
        else
        {
            // Free mode: use sprint speed
            // (Handled in HandleFreeMovementFixed)
        }
        
        // Visual/audio feedback
        //Debug.Log("Sprinting!");
        // AudioManager.Instance.PlaySFX("SprintStart");
        // _animator.SetBool("IsSprinting", true);
    }
    
    void StopSprinting()
    {
        _isSprinting = false;
        
        // Reset to normal speed
        if (GameManager.Instance?.IsGridBased == true)
        {
            _gridEntity.MoveSpeed = GridMoveSpeed;
        }
        
        //Debug.Log("Stopped sprinting");
        // AudioManager.Instance.PlaySFX("SprintEnd");
        // _animator.SetBool("IsSprinting", false);
    }
    
    // Update method to track direction
    void UpdateDirection(Vector2 input)
    {
        if (input.magnitude > _directionChangeThreshold)
        {
            _lastRawInput = input;
            _lastMoveDirection = GetGridDirectionVector(input);
        
            // Optional: Visual feedback (rotate sprite, show arrow, etc.)
            UpdateVisualDirection(_lastMoveDirection);
        }
    }
    
    Vector2 GetGridDirectionVector(Vector2 input)
    {
        // Convert analog input to 8-direction vector
        Vector2 normalized = input.normalized;
        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
    
        // Snap to 8 directions (N, NE, E, SE, S, SW, W, NW)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
    
        return new Vector2(
            Mathf.Cos(snappedAngle * Mathf.Deg2Rad),
            Mathf.Sin(snappedAngle * Mathf.Deg2Rad)
        ).normalized;
    }

    void UpdateVisualDirection(Vector2 direction)
    {
        // Optional: Update character sprite facing
        // TODO: Make animator for characters (should be the same for all entities)
    }
    
    bool ShouldInteract()
    {
        // Check for interact input (multiple options)
        return _interactAction != null && _interactAction.WasPressedThisFrame();
    }
    
    void TryInteractOrAttack()
    { 
        Vector2Int direction = Vector2Int.RoundToInt(_lastMoveDirection);
        
        if (GameManager.Instance?.IsGridBased == true)
        {
            // Grid-based interaction
            TryGridInteraction(direction);
        }
        else
        {
            // Free movement interaction
            TryFreeInteraction(_lastMoveDirection);
        }
    }
    
    // Grid-based interaction (dungeon)
    void TryGridInteraction(Vector2Int direction)
    {
        if (_gridEntity == null || _gridEntity.Grid == null) return;
        
        int targetX = _gridEntity.GridX + direction.x;
        int targetY = _gridEntity.GridY + direction.y;
        
        // Check bounds
        if (!_gridEntity.Grid.InBounds(targetX, targetY)) return;
        
        var targetTile = _gridEntity.Grid.Tiles[targetX, targetY];
        
        // Check for occupant
        if (targetTile.Occupant != null)
        {
            HandleOccupantInteraction(targetTile.Occupant, direction);
            return;
        }
        
        // Empty space attack
        Debug.Log("Attack into empty space");
        StartCoroutine(PerformEmptyAttack(direction));
        GameLogger.LogAction($"{_playerStats.PlayerPreset.CharacterName} swung at nothing");
        
        // Player attacked, enemies can counter-attack
        TurnManager.Instance?.PlayerPerformedAction();
    }

    void HandleOccupantInteraction(GridEntity occupant, Vector2Int direction)
    {
        if (occupant == null) return;

        switch (occupant.Type)
        {
            case EntityType.Enemy:
                // Use the unified attack system
                if (_gridEntity.CanAttackThisFrame())
                {
                    // Queue the player's attack
                    TurnManager.Instance?.QueueAttack(_gridEntity, occupant);
                    
                    // Notify turn manager that player performed an action (attack)
                    TurnManager.Instance?.PlayerPerformedAction(wasAttack: true);
                }
                break;
            
            case EntityType.NPC:
                TalkToNPC(occupant);
                break;

            default:
                Debug.Log($"Looking at {occupant.Type}: {occupant.name}");
                break;
        }
    }
    
    IEnumerator PerformEmptyAttack(Vector2Int direction)
    {
        if (!_gridEntity.CanAttackThisFrame()) yield break;
        
        // Play empty attack animation
        _isAttacking = true;
        
        Vector3 originalPos = transform.position;
        Vector3 attackOffset = new Vector3(direction.x, direction.y, 0) * 0.3f;
        float duration = 0.1f;
        float elapsed = 0f;
    
        // Lunge forward
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos, originalPos + attackOffset, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        // Return
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos + attackOffset, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        transform.position = originalPos;
        _isAttacking = false;
        
        // Player performed an action (attack into empty space)
        TurnManager.Instance?.PlayerPerformedAction(wasAttack: true);
    }
    

    // Callback when player completes any action
    // Callback when player completes any action


    void TalkToNPC(GridEntity npc)
    {
        // Get dialogue from NPC and start it
        var npcDialogue = npc.GetComponent<NPCController>();
        if (npcDialogue != null && npcDialogue.dialogueData != null)
        {
            // Use your existing DialogueManager
            DialogueManager.Instance.StartDialogue(npcDialogue.dialogueData);
        }
        else
        {
            Debug.Log($"{npc.name} has nothing to say.");
        }
    }
    
    // Free movement interaction (hub world)
    void TryFreeInteraction(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
        {
            // If no direction, use default (usually forward/right)
            direction = _lastMoveDirection.magnitude > 0 ? _lastMoveDirection : Vector2.right;
        }

        // Raycast to find interactable in direction
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            InteractionRange,
            LayerMask.GetMask("Interactable", "Enemy", "Item")
        );

        if (hit.collider != null)
        {
            HandleFreeInteraction(hit.collider.gameObject, direction);
            return;
        }
    }

    void HandleFreeInteraction(GameObject target, Vector2 direction)
    {
        var entity = target.GetComponent<GridEntity>();
        if (entity != null)
        {
            TalkToNPC(entity);
        }

        // Check for other interactable components
        var interactable = target.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(gameObject);
            return;
        }

        // Generic object
        Debug.Log($"Looking at {target.name}");
    }
    
    void PlayAttackAnimation(Vector2 direction)
    {
        // Simple visual feedback
        StartCoroutine(AttackAnimationRoutine(direction));
    
        // Play sound
        // AudioManager.Instance.PlaySFX("Swing");
    }
    
    IEnumerator AttackAnimationRoutine(Vector2 direction)
    {
        _isAttacking = true;
    
        // Store original position
        Vector3 originalPos = transform.position;
    
        // Lunge forward slightly
        Vector3 attackOffset = (Vector3)direction * 0.3f;
        float duration = 0.1f;
        float elapsed = 0f;
    
        // Lunge forward
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos, originalPos + attackOffset, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        // Return
        elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(originalPos + attackOffset, originalPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        transform.position = originalPos;
        _isAttacking = false;
    }

    void HandleGridInput()
    {

        Vector2 input = _moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.5f) return;
        
        Vector2Int direction = GetGridDirection(input);
        if (direction == Vector2Int.zero) return;
        
        // Use GridEntity's movement system
        if (_gridEntity.CanMoveInDirection(direction.x, direction.y))
        {
            _gridEntity.TryMove(direction.x, direction.y, OnPlayerMoveComplete);
        }
    }
    
    void OnPlayerMoveComplete()
    {
        // Player movement finished
        Debug.Log("Player move complete");
        
        // Notify enemies to move simultaneously
        TurnManager.Instance?.PlayerPerformedAction(wasAttack: false);
        
        _gridEntity.UpdateStatusEffects();
    }

    public void ClearMovementState()
    {
        _gridEntity.IsMoving = false;
        _gridEntity.MoveTarget = transform.position; // Set target to current position
        _currentVelocity = Vector2.zero;
    
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }
    
        Debug.Log($"Movement state cleared. Now at position: {transform.position}");
    }

    void HandleFreeMovementFixed()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
    
        // Calculate target speed (normal or sprint)
        float currentMoveSpeed = _isSprinting ? FreeSprintSpeed : FreeMoveSpeed;
        Vector2 targetVelocity = input * currentMoveSpeed;
    
        // Apply movement with smoothing
        _rb.linearVelocity = Vector2.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, 0.1f);
    }
    
    Vector2Int GetGridDirection(Vector2 input)
    {
        if (input.magnitude < 0.5f) return Vector2Int.zero;
        
        Vector2 normalized = input.normalized;
        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        
        // Snap to 8 directions
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        Vector2 direction = new Vector2(
            Mathf.Cos(snappedAngle * Mathf.Deg2Rad),
            Mathf.Sin(snappedAngle * Mathf.Deg2Rad)
        ).normalized;
        
        return new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y));
    }
    
    

    void OnHealthChanged(int health)
    {
        Debug.Log($"Player health changed: {health}");
    }
    
    void OnPlayerDeath()
    {
        Debug.Log("Player died");
        GameManager.Instance?.GameOver(false);
    }
    
    public void SetCanMove(bool canMove)
    {
        if (_playerInput != null)
            _playerInput.enabled = canMove;
        
        if (!canMove && _rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _currentVelocity = Vector2.zero;
        }
    }
}