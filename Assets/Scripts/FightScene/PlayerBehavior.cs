using System;
using UnityEngine;

/// <summary>
/// 2D mouse-follow player movement with acceleration, drift-style course correction,
/// arrival overshoot, correction, and micro-orbit settle. Uses legacy Input; Transform-based; z = 0.
/// </summary>
public class PlayerBehavior : MonoBehaviour
{
    public enum MovementState
    {
        Chase,
        Overshoot,
        Correction,
        Settled
    }

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
    // Inspector: Arrival / Overshoot
    // -------------------------------------------------------------------------
    [Header("Arrival / Overshoot")]
    [Tooltip("Distance to mouse at which overshoot triggers (chase ends, single overshoot then correction).")]
    [SerializeField] [Min(0f)] private float arrivalDistance = 1.5f;

    [Tooltip("Overshoot distance = currentSpeed * this (world units).")]
    [SerializeField] [Min(0f)] private float overshootMultiplier = 0.4f;

    [Tooltip("Distance to correction target below which we enter Settled micro-orbit.")]
    [SerializeField] [Min(0.01f)] private float settleDistanceThreshold = 0.15f;

    // -------------------------------------------------------------------------
    // Inspector: Correction
    // -------------------------------------------------------------------------
    [Header("Correction")]
    [Tooltip("Move speed toward target = factor * distance (smooth deceleration).")]
    [SerializeField] [Min(0.01f)] private float correctionSpeedFactor = 4f;

    [Tooltip("Max speed when correcting back toward target.")]
    [SerializeField] [Min(0f)] private float correctionMaxSpeed = 6f;

    // -------------------------------------------------------------------------
    // Inspector: Micro-Orbit (Settle)
    // -------------------------------------------------------------------------
    [Header("Micro-Orbit (Settle)")]
    [Tooltip("Radius of tiny orbit around mouse (very small, subtle hover).")]
    [SerializeField] [Min(0.01f)] private float orbitIntensity = 0.2f;

    [Tooltip("Orbit angle advance in rad/s (subtle motion).")]
    [SerializeField] [Min(0f)] private float orbitAngularSpeed = 1.5f;

    // -------------------------------------------------------------------------
    // Inspector: Re-engagement
    // -------------------------------------------------------------------------
    [Header("Re-engagement")]
    [Tooltip("When mouse moves this far from settle anchor, exit micro-orbit and resume chase.")]
    [SerializeField] [Min(0f)] private float reEngageDistance = 1f;

    // -------------------------------------------------------------------------
    // Runtime state
    // -------------------------------------------------------------------------
    private MovementState _state = MovementState.Chase;
    private Vector2 _currentMoveDirection = Vector2.right;
    private float _currentSpeed;
    private float _boostTimerRemaining;
    private float _boostCooldownRemaining;
    private bool _isBoosting;

    // Overshoot state
    private Vector2 _overshootDirection;
    private float _overshootDistance;
    private float _overshootTraveled;
    private float _overshootSpeed;
    private Vector2 _markedMousePosition;

    // Correction state
    private Vector2 _correctionTarget;

    // Settled state
    private Vector2 _settleAnchorPosition;
    private float _orbitAngle;
    private Vector2 _orbitMoveDirection;

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

        switch (_state)
        {
            case MovementState.Chase:
                UpdateChase(dt);
                break;
            case MovementState.Overshoot:
                UpdateOvershoot(dt);
                break;
            case MovementState.Correction:
                UpdateCorrection(dt);
                break;
            case MovementState.Settled:
                UpdateSettle(dt);
                break;
        }
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
    /// Chase state: move toward mouse with drift and acceleration. Transitions to Overshoot when within arrivalDistance.
    /// </summary>
    public void UpdateChase(float deltaTime)
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        float distToMouse = Vector2.Distance((Vector2)transform.position, mouseWorld);

        if (arrivalDistance >= 0.001f && distToMouse <= arrivalDistance)
        {
            _state = MovementState.Overshoot;
            _markedMousePosition = mouseWorld;
            _overshootDirection = _currentMoveDirection.normalized;
            if (_overshootDirection.sqrMagnitude < 0.01f)
                _overshootDirection = Vector2.right;
            _overshootSpeed = _currentSpeed;
            _overshootDistance = Mathf.Max(0f, _overshootSpeed * overshootMultiplier);
            _overshootTraveled = 0f;
            return;
        }

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
    /// Overshoot state: move in stored direction until overshoot distance is traveled, then transition to Correction.
    /// </summary>
    public void UpdateOvershoot(float deltaTime)
    {
        float remaining = _overshootDistance - _overshootTraveled;
        float step = _overshootSpeed * deltaTime;
        if (step > remaining)
            step = remaining;

        Vector2 move = _overshootDirection * step;
        _overshootTraveled += step;

        Vector3 pos = transform.position;
        pos.x += move.x;
        pos.y += move.y;
        pos.z = 0f;
        transform.position = pos;

        if (_overshootTraveled >= _overshootDistance - 0.0001f)
        {
            _state = MovementState.Correction;
            _correctionTarget = _markedMousePosition;
        }
    }

    /// <summary>
    /// Correction state: move toward marked target with distance-based speed (smooth deceleration). Transition to Settled when within threshold.
    /// </summary>
    public void UpdateCorrection(float deltaTime)
    {
        Vector2 pos2 = transform.position;
        float distToTarget = Vector2.Distance(pos2, _correctionTarget);

        if (distToTarget <= settleDistanceThreshold)
        {
            _state = MovementState.Settled;
            _settleAnchorPosition = GetMouseWorldPosition();
            _orbitAngle = 0f;
            _orbitMoveDirection = Vector2.right;
            return;
        }

        Vector2 dir = (_correctionTarget - pos2).normalized;
        float speed = Mathf.Min(correctionSpeedFactor * distToTarget, correctionMaxSpeed);
        Vector2 move = dir * speed * deltaTime;
        if (move.magnitude > distToTarget)
            move = dir * distToTarget;

        Vector3 pos = transform.position;
        pos.x += move.x;
        pos.y += move.y;
        pos.z = 0f;
        transform.position = pos;
    }

    /// <summary>
    /// Settled state: tiny micro-orbit around current mouse with momentum-style smoothing. Re-engage to Chase when mouse moves beyond reEngageDistance from anchor.
    /// </summary>
    public void UpdateSettle(float deltaTime)
    {
        Vector2 mouseWorld = GetMouseWorldPosition();
        if (Vector2.Distance(mouseWorld, _settleAnchorPosition) > reEngageDistance)
        {
            _state = MovementState.Chase;
            return;
        }

        _orbitAngle += orbitAngularSpeed * deltaTime;
        Vector2 center = mouseWorld;
        Vector2 radial = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle));
        Vector2 tangent = new Vector2(-radial.y, radial.x);
        Vector2 targetOnCircle = center + radial * orbitIntensity;
        Vector2 pos2 = transform.position;
        Vector2 toTarget = targetOnCircle - pos2;

        float blendT = Mathf.Clamp01(deltaTime / Mathf.Max(0.01f, directionSmoothing));
        Vector2 desiredDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : tangent;
        _orbitMoveDirection = Vector2.Lerp(_orbitMoveDirection, desiredDir, blendT).normalized;
        if (_orbitMoveDirection.sqrMagnitude < 0.01f)
            _orbitMoveDirection = tangent;

        float moveSpeed = orbitIntensity * orbitAngularSpeed;
        Vector2 move = _orbitMoveDirection * moveSpeed * deltaTime;
        float distToTarget = toTarget.magnitude;
        if (move.magnitude > distToTarget && distToTarget > 0.0001f)
            move = move.normalized * distToTarget;

        Vector3 pos = transform.position;
        pos.x += move.x;
        pos.y += move.y;
        pos.z = 0f;
        transform.position = pos;
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
