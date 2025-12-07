using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private PlayerStats _playerStats;
    private GridEntity _gridEntity;
    
    // Input
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    
    // Free Movement
    public float FreeMoveSpeed = 5f;
    public float GridMoveSpeed = 5f;

    // Free movement
    private Rigidbody2D _rb;
    private Vector2 _currentVelocity;

    void Start()
    {
        _playerStats = GetComponent<PlayerStats>();
        _gridEntity = _playerStats;
        
        // Setup rigidbody
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.linearDamping = 10f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Setup input
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null) _playerInput = gameObject.AddComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        
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
        
        if (GameManager.Instance?.IsGridBased == true)
        {
            // Grid-based movement
            if (!_gridEntity.IsMoving)
            {
                HandleGridInput();
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
        else
        {
            // Try to interact with occupant (attack, etc.)
            int targetX = _gridEntity.GridX + direction.x;
            int targetY = _gridEntity.GridY + direction.y;
            
            if (_gridEntity.Grid != null && _gridEntity.Grid.InBounds(targetX, targetY))
            {
                var tile = _gridEntity.Grid.Tiles[targetX, targetY];
                if (tile.Occupant != null && tile.Occupant.BlocksMovement)
                {
                    _gridEntity.TryInteractWithOccupant(tile.Occupant, direction.x, direction.y);
                }
            }
        }
    }
    
    void OnPlayerMoveComplete()
    {
        // Player movement finished, trigger turn events
        TurnManager.Instance?.OnPlayerMoved();
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
        Vector2 targetVelocity = input * FreeMoveSpeed;
        
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