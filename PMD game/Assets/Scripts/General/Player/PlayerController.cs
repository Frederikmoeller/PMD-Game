using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour, IEffectTileHandler
{
    private GridEntity _entity;
    private PlayerStats _stats;
    private bool _canMove = true;
    
    // Input System
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _interactAction;
    private InputAction _waitAction;
    private InputAction _menuAction;

    [Header("Grid Movement Settings")] 
    public float GridMoveSpeed = 5f;
    public float InitialMoveDelay = 0.15f; // Delay before first move
    public float ContinuousMoveDelay = 0.08f; // Delay between continuous moves
    public float InputBufferTime = 0.1f; // Time to buffer input changes
    public bool allowDiagonalMovement = true;
    public float diagonalMoveMultiplier = 1.4f; // √2 ≈ 1.414
    
    // Movement state
    private Vector2 _moveInput;
    private Vector2 _bufferedMoveInput;
    private Vector2Int _currentMoveDirection = Vector2Int.zero;
    private bool _isMovingBetweenTiles = false;
    private Vector3 _startMovePosition;
    private Vector3 _targetMovePosition;
    private float _moveTimer = 0f;
    private float _moveCooldown = 0f;
    private bool _isFirstMoveInput = true;
    private float _inputBufferTimer = 0f;
    
    // Free movement components
    private Rigidbody2D _rb;
    public float freeMoveSpeed = 5f;
    private Vector2 _currentVelocity;

    void Start()
    {
        _entity = GetComponent<GridEntity>();
        _stats = GetComponent<PlayerStats>();
        _rb = GetComponent<Rigidbody2D>();
        
        // Setup rigidbody for free movement
        if (_rb != null)
        {
            _rb.gravityScale = 0;
            _rb.linearDamping = 10f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Subscribe to death event
        if (_stats != null)
        {
            _stats.OnPlayerDeath.AddListener(OnPlayerDeath);
        }
        
        SetupInput();
    }

    void SetupInput()
    {
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            _playerInput = gameObject.AddComponent<PlayerInput>();
        }
        
        // Get actions
        _moveAction = _playerInput.actions["Move"];
        _interactAction = _playerInput.actions["Interact"];
        //_waitAction = _playerInput.actions["Wait"];
        //_menuAction = _playerInput.actions["OpenMenu"];
        
        // Subscribe to action events
        _interactAction.performed += OnInteract;
        //_waitAction.performed += OnWait;
        //_menuAction.performed += OnOpenMenu;
    }

    void Update()
    {
        if (!_canMove || !CanMoveThisFrame()) return;

        // Get raw input
        _moveInput = _moveAction.ReadValue<Vector2>();

        // Buffer input changes
        UpdateInputBuffer();
        
        if (GameManager.Instance?.IsGridBased == true)
        {
            HandleGridMovement();
        }
        else
        {
            HandleFreeMovement();
        }
    }

    void UpdateInputBuffer()
    {
        // Buffer input changes for smooth direction switching
        if (_moveInput.magnitude > 0.5f)
        {
            _bufferedMoveInput = _moveInput;
            _inputBufferTimer = InputBufferTime;
        }
        else if (_inputBufferTimer > 0)
        {
            _inputBufferTimer -= Time.deltaTime;
            if (_inputBufferTimer <= 0)
            {
                _bufferedMoveInput = Vector2.zero;
            }
        }
    }
    
    void FixedUpdate()
    {
        // Physics-based movement for free mode
        if (!CanMoveThisFrame() || GameManager.Instance?.IsGridBased == true) return;
        
        Vector2 targetVelocity = _moveInput * freeMoveSpeed;
        _rb.linearVelocity = Vector2.SmoothDamp(_rb.linearVelocity, targetVelocity, ref _currentVelocity, 0.1f);
    }
    
    bool CanMoveThisFrame()
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.playersTurn) return false;
        if (GameManager.Instance == null) return false;
        if (GameManager.Instance.IsInCutscene) return false;
        if (GameManager.Instance.IsGamePaused) return false;
        return true;
    }

    void HandleGridMovement()
    {
        if (_isMovingBetweenTiles)
        {
            // Currently moving between tiles - update interpolation
            UpdateGridMovement();
        }
        else
        {
            // Not moving - check for new movement input
            if (_moveCooldown > 0)
            {
                _moveCooldown -= Time.deltaTime;
                return;
            }
            
            // Get movement direction from buffered input
            Vector2Int desiredDirection = GetGridDirectionFromInput(_bufferedMoveInput);
            
            if (desiredDirection != Vector2Int.zero)
            {
                // Try to start moving
                StartGridMove(desiredDirection);
            }
            else
            {
                // No input, reset first move flag
                _isFirstMoveInput = true;
            }
        }
    }
    
    Vector2Int GetGridDirectionFromInput(Vector2 input)
    {
        if (input.magnitude < 0.5f) return Vector2Int.zero;
        
        // Normalize input for consistent diagonal detection
        Vector2 normalizedInput = input.normalized;
        
        // 8-way movement with diagonal support
        float angle = Mathf.Atan2(normalizedInput.y, normalizedInput.x) * Mathf.Rad2Deg;
        
        // Snap to 8 directions (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        // Convert angle to direction vector
        Vector2 direction = new Vector2(
            Mathf.Cos(snappedAngle * Mathf.Deg2Rad),
            Mathf.Sin(snappedAngle * Mathf.Deg2Rad)
        ).normalized;
        
        // Convert to grid integers
        int dx = Mathf.RoundToInt(direction.x);
        int dy = Mathf.RoundToInt(direction.y);
        
        // If diagonal movement is disabled, prioritize cardinal directions
        if (allowDiagonalMovement || dx == 0 || dy == 0) return new Vector2Int(dx, dy);
        // Choose the stronger component
        return Mathf.Abs(input.x) > Mathf.Abs(input.y) ? new Vector2Int(dx, 0) : new Vector2Int(0, dy);
    }
    
    void StartGridMove(Vector2Int direction)
    {
        // Check if we can move in this direction
        if (!CanMoveInDirection(direction)) return;
        
        // Set up movement interpolation
        _currentMoveDirection = direction;
        _startMovePosition = transform.position;
        _targetMovePosition = _startMovePosition + new Vector3(direction.x, direction.y, 0);
        _moveTimer = 0f;
        _isMovingBetweenTiles = true;
        
        // Adjust speed for diagonal movement (√2 times longer distance)
        float speedMultiplier = (direction.x != 0 && direction.y != 0) ? diagonalMoveMultiplier : 1f;
        
        // Update grid entity position immediately (for collision/occupancy)
        if (_entity != null)
        {
            // Reserve the target tile
            _entity.GridX += direction.x;
            _entity.GridY += direction.y;
        }
        
        // Set cooldown for next move
        _moveCooldown = _isFirstMoveInput ? InitialMoveDelay : ContinuousMoveDelay;
        _isFirstMoveInput = false;
        
        // Adjust movement speed for diagonals
        if (direction.x != 0 && direction.y != 0)
        {
            // Diagonals take √2 times longer, so adjust the timer
            _moveTimer = -0.01f; // Small offset to compensate
        }
    }
    
    void UpdateGridMovement()
    {
        // Calculate effective speed (slower for diagonals)
        float effectiveSpeed = GridMoveSpeed;
        if (_currentMoveDirection.x != 0 && _currentMoveDirection.y != 0)
        {
            effectiveSpeed = GridMoveSpeed / diagonalMoveMultiplier;
        }
        
        // Update interpolation timer
        _moveTimer += Time.deltaTime * effectiveSpeed;
        
        // Pure linear interpolation - no easing
        float t = Mathf.Clamp01(_moveTimer);
        
        // Move player linearly between tiles
        transform.position = Vector3.Lerp(_startMovePosition, _targetMovePosition, t);
        
        // Check if movement is complete
        if (t >= 1f)
        {
            // Snap to exact grid position
            transform.position = _targetMovePosition;
            _isMovingBetweenTiles = false;
            
            // Trigger turn-based events
            OnGridMoveComplete();
            
            // Check for immediate follow-up movement
            CheckImmediateFollowUp();
        }
    }
    
    void CheckImmediateFollowUp()
    {
        // Get current input direction
        Vector2Int currentInputDir = GetGridDirectionFromInput(_bufferedMoveInput);
        
        // If still holding in the same direction, move immediately
        if (currentInputDir == _currentMoveDirection && _moveCooldown <= 0)
        {
            StartGridMove(currentInputDir);
        }
    }
    
    void OnGridMoveComplete()
    {
        // Notify turn system
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerMoved();
        }
        
        // Update status effects
        _stats?.UpdateStatusEffects();
        
        // Check tile interactions (traps, stairs, etc.)
        CheckTileInteractions();
    }
    
    bool CanMoveInDirection(Vector2Int direction)
    {
        if (_entity == null || _entity.Grid == null) return false;
        
        int targetX = _entity.GridX + direction.x;
        int targetY = _entity.GridY + direction.y;
        
        // Check bounds
        if (!_entity.Grid.InBounds(targetX, targetY)) return false;
        
        // Check if tile is walkable
        var targetTile = _entity.Grid.Tiles[targetX, targetY];
        if (!targetTile.Walkable) return false;
        
        // For diagonal movement, also check the cardinal directions
        if (direction.x != 0 && direction.y != 0)
        {
            // Check if we're "cutting corners" through walls
            bool horizontalClear = _entity.Grid.InBounds(_entity.GridX + direction.x, _entity.GridY);
            bool verticalClear = _entity.Grid.InBounds(_entity.GridX, _entity.GridY + direction.y);
            
            if (horizontalClear)
            {
                var horizontalTile = _entity.Grid.Tiles[_entity.GridX + direction.x, _entity.GridY];
                if (!horizontalTile.Walkable || (horizontalTile.Occupant != null && horizontalTile.Occupant != _entity))
                    return false;
            }
            
            if (verticalClear)
            {
                var verticalTile = _entity.Grid.Tiles[_entity.GridX, _entity.GridY + direction.y];
                if (!verticalTile.Walkable || (verticalTile.Occupant != null && verticalTile.Occupant != _entity))
                    return false;
            }
        }
        
        // Check for occupants on the target tile
        if (targetTile.Occupant != null && targetTile.Occupant != _entity)
        {
            // Try to interact (attack, talk, etc.)
            // For now, just block movement
            return false;
        }
        
        return true;
    }
    
    bool TryInteractWithOccupant(GridEntity occupant, Vector2Int direction)
    {
        // Try to interact (attack, talk, etc.)
        // For now, just block movement
        return false;
    }
    
    void CheckTileInteractions()
    {
        if (_entity == null || _entity.Grid == null) return;
        
        var currentTile = _entity.Grid.Tiles[_entity.GridX, _entity.GridY];
        
        // Check for traps
        if (currentTile.Type == TileType.Effect)
        {
            //_entity.OnSteppedTrap();
        }
        
        // Check for stairs
        if (currentTile.Type == TileType.Stairs)
        {
            //_entity.OnSteppedStairs();
        }
    }

    void HandleFreeMovement()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || !CanMoveThisFrame()) return;
        
        TryInteract();
    }

    void OnWait(InputAction.CallbackContext context)
    {
        if (!context.performed || !CanMoveThisFrame()) return;
        
        SkipTurn();
    }

    void OnOpenMenu(InputAction.CallbackContext context)
    {
        if (!context.performed || GameManager.Instance?.IsInCutscene == true) return;
        
        OpenInventory();
    }

    private void TryMove(int dx, int dy)
    {
        if (_entity.TryMove(dx, dy))
        {
            TurnManager.Instance.OnPlayerMoved();
            _stats?.UpdateStatusEffects();
        }
    }

    private void SkipTurn()
    {
        // Only skip turn in grid-based mode
        if (GameManager.Instance?.IsGridBased == true)
        {
            Debug.Log("Player waits...");
            TurnManager.Instance.OnPlayerMoved();
            _stats?.UpdateStatusEffects();
        }
    }

    private void TryInteract()
    {
        Debug.Log("Attempting to interact...");
        // Your interaction system will handle this
    }

    private void OpenInventory()
    {
        Debug.Log("Opening inventory...");

        if (GameManager.Instance == null) return;
        bool newPauseState = !GameManager.Instance.IsGamePaused;
        GameManager.Instance.TogglePause(newPauseState);
            
        // Switch action maps
        if (_playerInput != null)
        {
            _playerInput.SwitchCurrentActionMap(newPauseState ? "UI" : "PlayerMovement");
        }
    }

    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;
        
        if (_playerInput != null)
        {
            _playerInput.enabled = canMove;
        }
        
        // Stop movement when disabled
        if (!canMove)
        {
            // Reset all movement state
            _isMovingBetweenTiles = false;
            _isFirstMoveInput = true;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _currentVelocity = Vector2.zero;
            }
        }
    }

    public void ApplyTileEffect(EventTileEffect effect)
    {
        if (effect == null) return;

        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats == null) return;
        
        Debug.Log($"Player activating effect: {effect.EffectName}");
        
        // Health effects
        if (effect.HealthChange != 0)
        {
            if (effect.HealthChange > 0)
            {
                int healing = Mathf.RoundToInt(effect.HealthChange * 0.01f);
                stats.Heal(effect.HealthChange);
            }
            else
            {
                int damage = Mathf.RoundToInt(stats.MaxHealth * (Mathf.Abs(effect.HealthChange * 0.01f)));
                stats.TakeDamage(-effect.HealthChange); // Convert negative to positive damage
            }
        }
        
        // Status effects
        if (effect.StatusEffect != StatusEffectType.None && effect.StatusDuration > 0)
        {
            stats.ApplyStatusEffect(effect.StatusEffect, effect.StatusDuration);
        }
        
        // Stat modifiers (temporary)
        if (effect.Duration > 0 && (effect.AttackBonus != 0 || effect.DefenseBonus != 0 || effect.SpeedBonus != 0))
        {
            Debug.Log($"Temporary stats: +{effect.AttackBonus} ATK, +{effect.DefenseBonus} DEF, +{effect.SpeedBonus} SPD for {effect.Duration} turns");
            // TODO: Implement temporary buff system in PlayerStats
        }
        
        // Special effects
        if (effect.SpawnsItem && effect.ItemPrefab != null)
        {
            Instantiate(effect.ItemPrefab, transform.position, Quaternion.identity);
        }
        
        if (effect.TeleportsToRandomFloor && _entity != null && _entity.Grid != null)
        {
            // Teleport to random floor tile
            var randomPos = _entity.Grid.GetRandomFloorPosition();
            transform.position = new Vector3(randomPos.x + 0.5f, randomPos.y + 0.5f, 0);
            
            // Update GridEntity position
            _entity.Grid.Tiles[_entity.GridX, _entity.GridY].Occupant = null;
            _entity.GridX = randomPos.x;
            _entity.GridY = randomPos.y;
            _entity.Grid.Tiles[_entity.GridX, _entity.GridY].Occupant = _entity;
        }
        
        // Visual feedback
        if (!string.IsNullOrEmpty(effect.ActivationMessage))
        {
            Debug.Log(effect.ActivationMessage);
            // You could show this in a UI message
        }
    }

    private void OnPlayerDeath()
    {
        SetCanMove(false);
        Debug.Log("Player controller disabled due to death");
    }

    void OnDestroy()
    {
        if (_interactAction != null) _interactAction.performed -= OnInteract;
        if (_waitAction != null) _waitAction.performed -= OnWait;
        if (_menuAction != null) _menuAction.performed -= OnOpenMenu;
    }
}