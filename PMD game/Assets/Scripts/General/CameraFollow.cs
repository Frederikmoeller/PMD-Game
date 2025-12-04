using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow Settings")] 
    public Transform DefaultTarget;
    public float FollowSpeed = 5f;
    public float SnapThreshold = 0.1f;
    
    [Header("Bounds (Optional)")]
    public bool UseBoundaryCollider = false;
    public Collider2D BoundaryCollider; // BoxCollider2D or PolygonCollider2D
    public float BoundaryPadding = 0.5f; // Keep camera inside by this much

    private Camera _camera;
    private Transform _currentTarget;
    private Vector3 _velocity = Vector3.zero;

    // For animation/cutscene control
    private bool _isControlledByCutscene = false;
    private Vector3? _cutsceneTargetPosition = null;
    private float? _cutsceneTargetSize = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }        
    }
    
    void Initialize()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("No camera found!");
                return;
            }
        }

        // Try to find player if not assigned
        if (DefaultTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                DefaultTarget = player.transform;
            }
        }
        
        // Start following default target
        if (DefaultTarget != null)
        {
            SetTarget(DefaultTarget);
        }
    }
    
    void LateUpdate()
    {
        if (_isControlledByCutscene)
        {
            UpdateCutsceneControl();
        }
        else
        {
            UpdateNormalFollow();
        }
    }
    
    void UpdateNormalFollow()
    {
        if (_currentTarget == null) return;
        
        Vector3 desiredPosition = GetDesiredPosition();
        
        // Snap when close enough (good for grid-based)
        transform.position = desiredPosition;
        _velocity = Vector3.zero;
        
    }
    
    void UpdateCutsceneControl()
    {
        if (_cutsceneTargetPosition.HasValue)
        {
            // Smooth move to cutscene position
            transform.position = Vector3.SmoothDamp(
                transform.position,
                _cutsceneTargetPosition.Value,
                ref _velocity,
                0.5f
            );
        }
    }

    Vector3 GetDesiredPosition()
    {
        if (_currentTarget == null) return transform.position;
        
        Vector3 targetPos = _currentTarget.position;
        targetPos.z = transform.position.z; // Keep camera Z
        
        // Apply boundary constraints if enabled
        if (UseBoundaryCollider && BoundaryCollider != null)
        {
            targetPos = ConstrainToBoundary(targetPos);
        }
        
        return targetPos;
    }
    
    Vector3 ConstrainToBoundary(Vector3 desiredPosition)
    {
        // Calculate camera viewport bounds
        float cameraHeight = _camera.orthographicSize;
        float cameraWidth = cameraHeight * _camera.aspect;
        
        // Get collider bounds (in world space)
        Bounds colliderBounds = BoundaryCollider.bounds;
        
        // Add padding
        colliderBounds.Expand(-BoundaryPadding * 2);
        
        // Constrain camera position to keep view inside bounds
        float minX = colliderBounds.min.x + cameraWidth;
        float maxX = colliderBounds.max.x - cameraWidth;
        float minY = colliderBounds.min.y + cameraHeight;
        float maxY = colliderBounds.max.y - cameraHeight;
        
        // Handle cases where level is smaller than camera view
        if (maxX < minX) minX = maxX = (minX + maxX) * 0.5f;
        if (maxY < minY) minY = maxY = (minY + maxY) * 0.5f;
        
        // Apply constraints
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        
        return desiredPosition;
    }

    #region Public API
    /// <summary>
    /// Follow the player character
    /// </summary>
    public void FollowPlayer()
    {
        if (DefaultTarget == null) return;
        SetTarget(DefaultTarget);
        Debug.Log("Camera following player");
    }
    
    /// <summary>
    /// Follow a specific target
    /// </summary>
    public void FollowTarget(Transform target)
    {
        SetTarget(target);
    }
    
    /// <summary>
    /// Let cutscene system take control of camera
    /// </summary>
    public void StartCutsceneControl(Vector3? position = null, float? size = null)
    {
        _isControlledByCutscene = true;
        _cutsceneTargetPosition = position;
        _cutsceneTargetSize = size;
        
        // Stop following any target
        _currentTarget = null;
        _velocity = Vector3.zero;
        
        Debug.Log("Camera under cutscene control");
    }
    
    /// <summary>
    /// Return camera to normal follow mode
    /// </summary>
    public void EndCutsceneControl()
    {
        _isControlledByCutscene = false;
        _cutsceneTargetPosition = null;
        _cutsceneTargetSize = null;
        
        // Return to following player
        FollowPlayer();
        
        Debug.Log("Camera returned to normal control");
    }

    /// <summary>
    /// Set exact camera position (for cutscene timeline)
    /// </summary>
    public void SetCutscenePosition(Vector3 position)
    {
        _cutsceneTargetPosition = position;
        position.z = transform.position.z; // Ensure correct Z
    }

    /// <summary>
    /// Shake camera for impact
    /// </summary>
    public void ShakeCamera(float intensity = 0.5f, float duration = 0.3f)
    {
        StartCoroutine(ShakeRoutine(intensity, duration));
    }
    #endregion
    

    #region Helper Methods
    private System.Collections.IEnumerator ShakeRoutine(float intensity, float duration)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = originalPos.x + Random.Range(-intensity, intensity);
            float y = originalPos.y + Random.Range(-intensity, intensity);
            transform.position = new Vector3(x, y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    private void SetTarget(Transform target)
    {
        _currentTarget = target;
        _isControlledByCutscene = false; // Exit cutscene mode
        _cutsceneTargetPosition = null;
        _cutsceneTargetSize = null;
    }
    
    public void OnPlayerSpawned(GameObject player)
    {
        DefaultTarget = player.transform;
        FollowPlayer();
    }
    
    public void OnCompanionTurnStarted(GameObject companion)
    {
        FollowTarget(companion.transform);
    }
    
    public void OnPlayerTurnStarted()
    {
        FollowPlayer();
    }
    
    // Getters for current state
    public bool IsControlledByCutscene => _isControlledByCutscene;
    public Transform CurrentTarget => _currentTarget;
    
    #endregion
}
