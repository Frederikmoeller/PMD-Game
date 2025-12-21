using System.Collections;
using DialogueSystem;
using GameSystem;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // References
    private PlayerStats _player;
    private Rigidbody2D _rb;
    private PlayerInput _playerInput;
    
    [Header("Movement Settings")]
    public float FreeMoveSpeed = 5f;
    public float GridMoveSpeed = 5f;
    public float SprintMultiplier = 2f;
    public float FreeSprintSpeed = 10f;
    
    [Header("Interaction")]
    public float InteractionRange = 1.5f;
    
    // Input Actions
    private InputAction _moveAction;
    private InputAction _interactAction;
    private InputAction _sprintAction;
    
    // State
    private bool _isSprinting = false;
    private bool _isAttacking = false;
    private Vector2 _lastMoveDirection = Vector2.right;
    private Vector2 _currentVelocity;
    private float _directionChangeThreshold = 0.1f;
    
    void Start()
    {
        // Get the PlayerStats component (which IS-A GridEntity)
        _player = GetComponent<PlayerStats>();
        _rb = GetComponent<Rigidbody2D>();
        _playerInput = GetComponent<PlayerInput>();
        
        // Setup input actions
        if (_playerInput == null)
            _playerInput = gameObject.AddComponent<PlayerInput>();
        
        _moveAction = _playerInput.actions["Move"];
        _interactAction = _playerInput.actions["Interact"];
        _sprintAction = _playerInput.actions["Sprint"];
        
        // Setup physics for free movement
        if (_rb != null)
        {
            _rb.gravityScale = 0;
            _rb.linearDamping = 10f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Set initial movement speed for grid mode
        _player.MoveSpeed = GridMoveSpeed;
        
        // Subscribe to player events
        _player.OnDeath += OnPlayerDeath;
        _player.OnHealthChanged += OnHealthChanged;
    }
    
    private void OnEnable()
    {
        if (GameManager.Instance?.Input != null)
        {
            GameManager.Instance.Input.OnMoveInput += HandleMoveInput;
            GameManager.Instance.Input.OnInteractInput += HandleInteractInput;
            GameManager.Instance.Input.OnAttackInput += HandleSprintInput;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance?.Input != null)
        {
            GameManager.Instance.Input.OnMoveInput -= HandleMoveInput;
            GameManager.Instance.Input.OnInteractInput -= HandleInteractInput;
            GameManager.Instance.Input.OnAttackInput -= HandleSprintInput;
        }
    }
    
    private void HandleMoveInput(Vector2Int direction)
    {
        if (GameManager.Instance.IsGamePaused) return;
        if (GameManager.Instance.Dialogue.IsDialogueActive) return;
    }

    private void HandleInteractInput()
    {
        if (GameManager.Instance.IsGamePaused) return;
        if (GameManager.Instance.Dialogue.IsDialogueActive) return;
    }

    private void HandleSprintInput()
    {
        if (GameManager.Instance.IsGamePaused) return;
        if (GameManager.Instance.Dialogue.IsDialogueActive) return;
    }
    
    void Update()
    {
        if (!CanAct()) return;
        
        // Update facing direction
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        UpdateDirection(moveInput);
        
        // Update sprint state
        UpdateSprintState();
        
        if (GameManager.Instance?.Dungeon.IsInDungeon == true)
        {
            // Grid-based movement (dungeon)
            if (!_player.IsMoving)
            {
                HandleGridInput();
                
                // Handle interact/attack
                if (ShouldInteract())
                {
                    TryInteractOrAttack();
                }
            }
        }
        // Free movement is handled in FixedUpdate
    }
    
    void FixedUpdate()
    {
        // Skip free movement if in grid mode
        if (GameManager.Instance?.Dungeon.IsInDungeon == true) return;
        if (!CanAct()) return;
        
        HandleFreeMovement();
    }
    
    // ========== CORE METHODS ==========
    
    private bool CanAct()
    {
        if (_player == null) return false;
        return _player.CanMoveThisFrame();
    }
    
    // ========== GRID MOVEMENT ==========
    
    private void HandleGridInput()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.5f) return;
        
        Vector2Int direction = GetGridDirection(input);
        if (direction == Vector2Int.zero) return;
        
        // Use PlayerStats' inherited GridEntity movement
        if (_player.CanMoveInDirection(direction.x, direction.y))
        {
            _player.TryMove(direction.x, direction.y, OnPlayerMoveComplete);
            
            // Notify turn manager (player moved, enemies can respond)
            TurnManager.Instance?.MoveAllEnemies();
        }
    }
    
    private void OnPlayerMoveComplete()
    {
        // Update status effects
        _player.UpdateStatusEffects();
        TurnManager.Instance?.PlayerPerformedAction();
    }
    
    // ========== FREE MOVEMENT ==========
    
    private void HandleFreeMovement()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        
        // Calculate speed (with sprint)
        float currentSpeed = _isSprinting ? FreeSprintSpeed : FreeMoveSpeed;
        Vector2 targetVelocity = input * currentSpeed;
        
        // Apply smooth movement
        _rb.linearVelocity = Vector2.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, 0.1f);
    }
    
    // ========== SPRINT SYSTEM ==========
    
    private void UpdateSprintState()
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
    
    private void StartSprinting()
    {
        _isSprinting = true;
        
        if (GameManager.Instance?.Dungeon.IsInDungeon == true)
        {
            _player.MoveSpeed = GridMoveSpeed * SprintMultiplier;
        }
        // Free movement speed is handled in HandleFreeMovement()
    }
    
    private void StopSprinting()
    {
        _isSprinting = false;
        
        if (GameManager.Instance?.Dungeon.IsInDungeon == true)
        {
            _player.MoveSpeed = GridMoveSpeed;
        }
    }
    
    // ========== INTERACTION & COMBAT ==========
    
    private bool ShouldInteract()
    {
        return _interactAction != null && _interactAction.WasPressedThisFrame();
    }
    
    private void TryInteractOrAttack()
    {
        Vector2Int direction = Vector2Int.RoundToInt(_lastMoveDirection);
        
        if (GameManager.Instance?.Dungeon.IsInDungeon == true)
        {
            TryGridInteraction(direction);
        }
        else
        {
            TryFreeInteraction(_lastMoveDirection);
        }
    }
    
    private void TryGridInteraction(Vector2Int direction)
    {
        if (_player == null || _player.Grid == null) return;
        
        int targetX = _player.GridX + direction.x;
        int targetY = _player.GridY + direction.y;
        
        if (!_player.Grid.InBounds(targetX, targetY)) return;
        
        var targetTile = _player.Grid.Tiles[targetX, targetY];
        
        // Check for occupant
        if (targetTile.Occupant != null)
        {
            HandleOccupantInteraction(targetTile.Occupant);
            return;
        }
        
        // Empty space attack
        PerformEmptyAttack(direction);
    }
    
    private void HandleOccupantInteraction(GridEntity occupant)
    {
        if (occupant == null) return;
        
        switch (occupant.Type)
        {
            case EntityType.Enemy:
                // Attack using PlayerStats' inherited Attack method
                if (_player.CanAttackThisFrame())
                {
                    TurnManager.Instance?.QueueAttack(_player, occupant);
                    TurnManager.Instance?.PlayerPerformedAction(wasAttack: true);
                }
                break;
                
            case EntityType.Npc:
                TalkToNpc(occupant);
                break;
                
            default:
                Debug.Log($"Looking at {occupant.Type}: {occupant.name}");
                break;
        }
    }
    
    private void PerformEmptyAttack(Vector2Int direction)
    {
        if (!_player.CanAttackThisFrame()) return;
        
        // Play animation
        StartCoroutine(EmptyAttackAnimation(direction));
        
        // Notify turn system
        GameLogger.LogAction($"{_player.CharacterPreset.CharacterName} swung at nothing");
        TurnManager.Instance?.PlayerPerformedAction(wasAttack: true);
    }
    
    private IEnumerator EmptyAttackAnimation(Vector2Int direction)
    {
        _isAttacking = true;
        
        Vector3 originalPos = transform.position;
        Vector3 attackOffset = new Vector3(direction.x, direction.y, 0) * 0.3f;
        float duration = 0.1f;
        
        // Lunge forward
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(originalPos, originalPos + attackOffset, t / duration);
            yield return null;
        }
        
        // Return
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(originalPos + attackOffset, originalPos, t / duration);
            yield return null;
        }
        
        transform.position = originalPos;
        _isAttacking = false;
    }
    
    // ========== FREE MOVEMENT INTERACTION ==========
    
    private void TryFreeInteraction(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
        {
            direction = _lastMoveDirection.magnitude > 0 ? _lastMoveDirection : Vector2.right;
        }
        
        // Raycast for interactables
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            InteractionRange,
            LayerMask.GetMask("Interactable", "NPC")
        );
        
        if (hit.collider != null)
        {
            HandleFreeInteraction(hit.collider.gameObject);
        }
    }
    
    private void HandleFreeInteraction(GameObject target)
    {
        // Check for NPC
        var npc = target.GetComponent<GridEntity>();
        if (npc != null && npc.Type == EntityType.Npc)
        {
            TalkToNpc(npc);
            return;
        }
        
        // Check for other interactables
        var interactable = target.GetComponent<IInteractable>();
        interactable?.Interact(gameObject);
    }
    
    private void TalkToNpc(GridEntity npc)
    {
        var npcDialogue = npc.GetComponent<NpcController>();
        if (npcDialogue != null && npcDialogue.DialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(npcDialogue.DialogueData);
        }
        else
        {
            Debug.Log($"{npc.name} has nothing to say.");
        }
    }
    
    // ========== DIRECTION HANDLING ==========
    
    private void UpdateDirection(Vector2 input)
    {
        if (input.magnitude > _directionChangeThreshold)
        {
            _lastMoveDirection = GetGridDirectionVector(input);
            UpdateVisualDirection(_lastMoveDirection);
        }
    }
    
    private Vector2 GetGridDirectionVector(Vector2 input)
    {
        Vector2 normalized = input.normalized;
        float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        return new Vector2(
            Mathf.Cos(snappedAngle * Mathf.Deg2Rad),
            Mathf.Sin(snappedAngle * Mathf.Deg2Rad)
        ).normalized;
    }
    
    private Vector2Int GetGridDirection(Vector2 input)
    {
        if (input.magnitude < 0.5f) return Vector2Int.zero;
        
        Vector2 direction = GetGridDirectionVector(input);
        return new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y));
    }
    
    private void UpdateVisualDirection(Vector2 direction)
    {
        // TODO: Update sprite facing
        // Example: 
        // if (direction.x != 0)
        //     transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
    }
    
    // ========== EVENT HANDLERS ==========
    
    private void OnHealthChanged(int health)
    {
        Debug.Log($"Player health: {health}");
        // Update UI here
    }
    
    private void OnPlayerDeath()
    {
        Debug.Log("Player died");
    }
    
    // ========== PUBLIC METHODS ==========
    
    public void ClearMovementState()
    {
        if (_player != null)
        {
            _player.StopMovement();
        }
        
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _currentVelocity = Vector2.zero;
        }
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