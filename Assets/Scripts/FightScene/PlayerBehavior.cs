using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 2D mouse-follow player movement with acceleration, drift-style course correction, and boost.
/// Uses legacy Input (Input.mousePosition, Input.GetKeyDown). Transform-based; z kept at 0.
/// </summary>
public class PlayerBehavior : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Boost feedback (subscribe from VFX/Audio components)
    // -------------------------------------------------------------------------
    public event Action OnBoostStarted;
    public event Action OnBoostEnded;

    // -------------------------------------------------------------------------
    // Inspector: Camera
    // -------------------------------------------------------------------------
    [Header("Camera")]
    [Tooltip("Optional. If unset, Camera.main is used for mouse-to-world conversion.")]
    [SerializeField] private Camera mainCamera;

    // -------------------------------------------------------------------------
    // Inspector: Base Movement
    // -------------------------------------------------------------------------
    [Header("Base Movement")]
    [Tooltip("Base movement speed toward mouse (units per second).")]
    [SerializeField] [Min(0f)] private float baseSpeed = 5f;

    [Tooltip("Maximum speed when not boosting (units per second).")]
    [SerializeField] [Min(0f)] private float terminalVelocity = 8f;

    [Tooltip("Approximate time in seconds to reach terminal velocity from rest.")]
    [SerializeField] [Min(0.01f)] private float timeToReachTerminalSeconds = 0.5f;

    // -------------------------------------------------------------------------
    // Inspector: Course Correction (drift / smooth turning)
    // -------------------------------------------------------------------------
    [Header("Course Correction")]
    [Tooltip("How aggressively to correct toward mouse (0 = heavy drift, 1 = snappy).")]
    [SerializeField] [Range(0f, 1f)] private float courseCorrectionStrength = 0.3f;

    [Tooltip("Time constant for direction smoothing; higher = slower turn-in.")]
    [SerializeField] [Min(0.01f)] private float directionSmoothing = 0.15f;

    // -------------------------------------------------------------------------
    // Inspector: Boost
    // -------------------------------------------------------------------------
    [Header("Boost")]
    [Tooltip("Speed multiplier while boost is active (effective speed = baseSpeed * this).")]
    [SerializeField] [Min(1f)] private float boostMultiplier = 1.8f;

    [Tooltip("Duration of boost in seconds.")]
    [SerializeField] [Min(0.1f)] private float boostDuration = 1.5f;

    [Tooltip("Cooldown after boost ends before it can be used again (seconds).")]
    [SerializeField] [Min(0f)] private float boostCooldown = 2f;

    [Tooltip("Key to trigger boost (legacy Input).")]
    [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;

    // -------------------------------------------------------------------------
    // Inspector: Debug / Optional
    // -------------------------------------------------------------------------
    [Header("Debug / Optional")]
    [Tooltip("Use Slerp for direction (arc) instead of Lerp (linear blend).")]
    [SerializeField] private bool useSlerpForDirection = true;

    [Tooltip("Draw debug ray in Scene view for current move direction.")]
    [SerializeField] private bool drawDebugRay;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------
    private Vector2 _currentMoveDirection = Vector2.right;
    private float _currentSpeed;
    private float _boostTimerRemaining;
    private float _boostCooldownRemaining;
    private bool _isBoosting;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogWarning($"{name}: No camera assigned and Camera.main is null. Mouse follow will fail.");
    }

    private void Start()
    {
        // Initialize direction from first desired direction so we don't snap on first frame
        Vector2 desired = GetDesiredDirection();
        if (desired.sqrMagnitude > 0.01f)
            _currentMoveDirection = desired.normalized;

        _currentSpeed = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateBoost(dt);
        UpdateMovement(dt);
    }

    /// <summary>
    /// Converts mouse screen position to 2D world position (z = 0 plane).
    /// </summary>
    public Vector2 GetMouseWorldPosition()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
            return (Vector2)transform.position;

        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(screen);
        return new Vector2(world.x, world.y);
    }

    /// <summary>
    /// Desired movement direction toward mouse. Zero vector if mouse is on player (no movement).
    /// </summary>
    public Vector2 GetDesiredDirection()
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        Vector2 toMouse = mouseWorld - (Vector2)transform.position;
        if (toMouse.sqrMagnitude < 0.0001f)
            return Vector2.zero;
        return toMouse.normalized;
    }

    /// <summary>
    /// Smooth course correction: blends current velocity direction toward desired using
    /// time-based smoothing so sharp mouse turns produce a gradual arc (drift feel).
    /// </summary>
    public Vector2 GetSmoothedDirection(float deltaTime)
    {
        Vector2 desired = GetDesiredDirection();

        if (desired.sqrMagnitude < 0.0001f)
            return _currentMoveDirection;

        float t = courseCorrectionStrength * (1f - Mathf.Exp(-deltaTime / directionSmoothing));
        Vector2 blended = useSlerpForDirection
            ? (Vector2)Vector3.Slerp(_currentMoveDirection, desired, t)
            : Vector2.Lerp(_currentMoveDirection, desired, t).normalized;

        if (blended.sqrMagnitude < 0.0001f)
            return _currentMoveDirection;

        _currentMoveDirection = blended.normalized;
        return _currentMoveDirection;
    }

    /// <summary>
    /// Current speed, accelerating toward terminal (or boost) velocity over time.
    /// </summary>
    public float GetCurrentSpeed(float deltaTime)
    {
        float targetSpeed = _isBoosting
            ? baseSpeed * boostMultiplier
            : terminalVelocity;

        float timeToTarget = _isBoosting ? timeToReachTerminalSeconds * 0.5f : timeToReachTerminalSeconds;
        float t = Mathf.Clamp01(deltaTime / Mathf.Max(0.001f, timeToTarget));
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, t);
        return _currentSpeed;
    }

    /// <summary>
    /// Applies movement: smoothed direction and accelerated speed, then updates position (z = 0).
    /// </summary>
    public void UpdateMovement(float deltaTime)
    {
        Vector2 dir = GetSmoothedDirection(deltaTime);
        float speed = GetCurrentSpeed(deltaTime);

        Vector2 move = dir * speed * deltaTime;
        Vector3 pos = transform.position;
        pos.x += move.x;
        pos.y += move.y;
        pos.z = 0f;
        transform.position = pos;

        if (drawDebugRay)
            Debug.DrawRay(transform.position, (Vector3)dir * 2f, Color.green, 0.5f);
    }

    /// <summary>
    /// Handles boost input (GetKeyDown), duration timer, and cooldown. Fires OnBoostStarted / OnBoostEnded.
    /// </summary>
    public void UpdateBoost(float deltaTime)
    {
        if (_isBoosting)
        {
            _boostTimerRemaining -= deltaTime;
            if (_boostTimerRemaining <= 0f)
            {
                _isBoosting = false;
                _boostCooldownRemaining = boostCooldown;
                OnBoostEnded?.Invoke();
            }
            return;
        }

        if (_boostCooldownRemaining > 0f)
        {
            _boostCooldownRemaining -= deltaTime;
            return;
        }

        if (Input.GetKeyDown(boostKey))
        {
            _isBoosting = true;
            _boostTimerRemaining = boostDuration;
            OnBoostStarted?.Invoke();
        }
    }
}
