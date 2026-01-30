using System;
using UnityEngine;

/// <summary>
/// 2D top-down player movement that chases the mouse cursor with
/// smooth, car-like drifting, brief overshoot momentum, and a subtle
/// micro-orbit settle behavior. Includes a temporary boost with cooldown
/// and a small internal state machine.
///
/// Assumptions:
/// - Used in a 2D top-down scene where X/Y are the movement axes and Z = 0.
/// - Object has a Rigidbody2D set to Kinematic or Dynamic (gravity generally off).
/// - Uses legacy Input for mouse and boost key.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerChaseController2D : MonoBehaviour
{
    // ---------------------------------------------------------------------
    // Movement state
    // ---------------------------------------------------------------------

    private enum MovementState
    {
        Chase,
        Overshoot,
        Settle
    }

    // ---------------------------------------------------------------------
    // Inspector: Camera
    // ---------------------------------------------------------------------

    [Header("Camera")]
    [Tooltip("Optional. If unset, Camera.main is used for mouse-to-world conversion.")]
    [SerializeField] private Camera mainCamera;

    // ---------------------------------------------------------------------
    // Inspector: Base Movement
    // ---------------------------------------------------------------------

    [Header("Base Movement")]
    [Tooltip("Maximum chase speed when not boosting (units per second).")]
    [SerializeField] [Min(0f)] private float maxSpeed = 8f;

    [Tooltip("Acceleration toward desired velocity (units per second^2).")]
    [SerializeField] [Min(0.01f)] private float acceleration = 30f;

    [Tooltip("How aggressively to steer current velocity toward target direction (0 = heavy drift, 1 = snappy).")]
    [SerializeField] [Range(0f, 1f)] private float steeringResponsiveness = 0.3f;

    [Tooltip("Time constant for steering smoothing; higher = slower turning response.")]
    [SerializeField] [Min(0.01f)] private float steeringSmoothing = 0.15f;

    // ---------------------------------------------------------------------
    // Inspector: Distances
    // ---------------------------------------------------------------------

    [Header("Distances")]
    [Tooltip("Distance to mouse at which we consider the player to have \"arrived\" and enter Overshoot.")]
    [SerializeField] [Min(0f)] private float arrivalDistance = 1.0f;

    [Tooltip("When in Settle state, if distance to mouse exceeds this, re-enter Chase.")]
    [SerializeField] [Min(0f)] private float reEngageDistance = 2.0f;

    [Tooltip("Minimum orbit radius around mouse in Settle (very subtle).")]
    [SerializeField] [Min(0.01f)] private float orbitRadiusMin = 0.05f;

    [Tooltip("Maximum orbit radius around mouse in Settle (still subtle).")]
    [SerializeField] [Min(0.01f)] private float orbitRadiusMax = 0.15f;

    // ---------------------------------------------------------------------
    // Inspector: Overshoot
    // ---------------------------------------------------------------------

    [Header("Overshoot")]
    [Tooltip("How long (seconds) the player continues momentum past the mouse before settling.")]
    [SerializeField] [Min(0.01f)] private float overshootDuration = 0.25f;

    [Tooltip("Damping applied each second to velocity during overshoot (0 = no damping, 1 = instant stop).")]
    [SerializeField] [Range(0f, 1f)] private float overshootDamping = 0.4f;

    // ---------------------------------------------------------------------
    // Inspector: Settle / Micro-Orbit
    // ---------------------------------------------------------------------

    [Header("Settle / Micro-Orbit")]
    [Tooltip("Angular speed of micro-orbit around mouse in radians per second.")]
    [SerializeField] [Min(0f)] private float orbitAngularSpeed = 1.5f;

    [Tooltip("How quickly the player moves toward the desired orbit position (1 = fairly snappy, lower = more floaty).")]
    [SerializeField] [Range(0.01f, 1f)] private float settleLerpFactor = 0.2f;

    // ---------------------------------------------------------------------
    // Inspector: Boost
    // ---------------------------------------------------------------------

    [Header("Boost")]
    [Tooltip("Speed multiplier while boost is active (applied to maxSpeed and acceleration).")]
    [SerializeField] [Min(1f)] private float boostMultiplier = 1.8f;

    [Tooltip("Duration of boost in seconds.")]
    [SerializeField] [Min(0.1f)] private float boostDuration = 1.0f;

    [Tooltip("Cooldown after boost ends before it can be used again (seconds).")]
    [SerializeField] [Min(0f)] private float boostCooldown = 2.0f;

    [Tooltip("Key to trigger boost (legacy Input).")]
    [SerializeField] private KeyCode boostKey = KeyCode.LeftShift;

    // ---------------------------------------------------------------------
    // Inspector: Debug
    // ---------------------------------------------------------------------

    [Header("Debug")]
    [Tooltip("Draw debug ray for current velocity direction in Scene view.")]
    [SerializeField] private bool drawDebugRay = false;

    // ---------------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------------

    private Rigidbody2D _rb;
    private MovementState _state = MovementState.Chase;

    // Movement
    private Vector2 _currentVelocity;
    private Vector2 _lastTargetDirection = Vector2.right;

    // Overshoot
    private float _overshootTimer;

    // Settle (orbit)
    private float _orbitAngle;
    private float _orbitRadius;

    // Boost
    private bool _isBoosting;
    private float _boostTimer;
    private float _boostCooldownTimer;

    // ---------------------------------------------------------------------
    // MonoBehaviour lifecycle
    // ---------------------------------------------------------------------

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogWarning($"{name}: No camera assigned and Camera.main is null. Mouse follow will fail.");
    }

    private void Start()
    {
        _currentVelocity = Vector2.zero;
        _lastTargetDirection = Vector2.right;
        EnterChase();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateBoost(dt);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        switch (_state)
        {
            case MovementState.Chase:
                UpdateChase(dt);
                break;
            case MovementState.Overshoot:
                UpdateOvershoot(dt);
                break;
            case MovementState.Settle:
                UpdateSettle(dt);
                break;
            default:
                UpdateChase(dt);
                break;
        }

        if (drawDebugRay)
            Debug.DrawRay(transform.position, _currentVelocity.normalized * 2f, Color.cyan, 0.1f);
    }

    // ---------------------------------------------------------------------
    // State helpers
    // ---------------------------------------------------------------------

    private void EnterChase()
    {
        _state = MovementState.Chase;
    }

    private void EnterOvershoot()
    {
        _state = MovementState.Overshoot;
        _overshootTimer = overshootDuration;
    }

    private void EnterSettle()
    {
        _state = MovementState.Settle;

        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 toMouse = mousePos - _rb.position;
        float distance = toMouse.magnitude;

        // Set a subtle orbit radius based on current distance, clamped to configured range.
        _orbitRadius = Mathf.Clamp(distance * 0.25f, orbitRadiusMin, orbitRadiusMax);

        // Initialize orbit angle so we start from current direction to mouse.
        _orbitAngle = Mathf.Atan2(toMouse.y, toMouse.x);
    }

    // ---------------------------------------------------------------------
    // Per-state updates
    // ---------------------------------------------------------------------

    private void UpdateChase(float dt)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 toMouse = mousePos - _rb.position;
        float distance = toMouse.magnitude;

        if (arrivalDistance > 0f && distance <= arrivalDistance)
        {
            EnterOvershoot();
            return;
        }

        Vector2 desiredDir = distance > 0.0001f ? toMouse.normalized : _lastTargetDirection;
        Vector2 smoothedDir = GetSteeredDirection(desiredDir, dt);
        _lastTargetDirection = smoothedDir;

        float effectiveMaxSpeed = GetEffectiveMaxSpeed();
        float effectiveAccel = GetEffectiveAcceleration();

        Vector2 desiredVelocity = smoothedDir * effectiveMaxSpeed;
        _currentVelocity = Vector2.MoveTowards(_currentVelocity, desiredVelocity, effectiveAccel * dt);

        _rb.MovePosition(_rb.position + _currentVelocity * dt);
    }

    private void UpdateOvershoot(float dt)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 toMouse = mousePos - _rb.position;
        float distance = toMouse.magnitude;

        // Still steer gently back toward the mouse using the same steering model.
        Vector2 desiredDir = distance > 0.0001f ? toMouse.normalized : _lastTargetDirection;
        Vector2 steeredDir = GetSteeredDirection(desiredDir, dt);
        _lastTargetDirection = steeredDir;

        float effectiveMaxSpeed = GetEffectiveMaxSpeed();
        float effectiveAccel = GetEffectiveAcceleration();
        Vector2 desiredVelocity = steeredDir * effectiveMaxSpeed;

        // Blend between existing momentum and steering back toward target.
        _currentVelocity = Vector2.MoveTowards(_currentVelocity, desiredVelocity, effectiveAccel * dt);

        // Apply damping so overshoot distance stays brief and controlled.
        float dampingFactor = Mathf.Clamp01(overshootDamping * dt);
        _currentVelocity *= (1f - dampingFactor);

        _rb.MovePosition(_rb.position + _currentVelocity * dt);

        _overshootTimer -= dt;
        if (_overshootTimer <= 0.0f || distance <= arrivalDistance * 0.5f)
        {
            EnterSettle();
        }
    }

    private void UpdateSettle(float dt)
    {
        Vector2 mousePos = GetMouseWorldPosition();
        Vector2 toMouse = mousePos - _rb.position;
        float distance = toMouse.magnitude;

        // If mouse moved far enough away, re-engage chase.
        if (reEngageDistance > 0f && distance >= reEngageDistance)
        {
            EnterChase();
            return;
        }

        // Advance a very small orbit around the mouse.
        _orbitAngle += orbitAngularSpeed * dt;

        Vector2 orbitOffset = new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * _orbitRadius;
        Vector2 desiredPosition = mousePos + orbitOffset;

        // Smoothly interpolate toward the orbit position to keep it subtle and organic.
        Vector2 newPos = Vector2.Lerp(_rb.position, desiredPosition, settleLerpFactor);
        _currentVelocity = (newPos - _rb.position) / dt;
        _rb.MovePosition(newPos);
    }

    // ---------------------------------------------------------------------
    // Steering and boost helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Blends current movement direction toward desired using time-based smoothing
    /// so rapid mouse direction changes produce a drifting arc.
    /// </summary>
    private Vector2 GetSteeredDirection(Vector2 desiredDir, float dt)
    {
        if (desiredDir.sqrMagnitude < 0.0001f)
            return _lastTargetDirection.sqrMagnitude > 0.0001f ? _lastTargetDirection : Vector2.right;

        float t = steeringResponsiveness * (1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, steeringSmoothing)));
        Vector2 currentDir = _currentVelocity.sqrMagnitude > 0.0001f ? _currentVelocity.normalized : _lastTargetDirection;
        Vector2 blended = Vector2.Lerp(currentDir, desiredDir, t);

        if (blended.sqrMagnitude < 0.0001f)
            return currentDir;

        return blended.normalized;
    }

    private float GetEffectiveMaxSpeed()
    {
        return _isBoosting ? maxSpeed * boostMultiplier : maxSpeed;
    }

    private float GetEffectiveAcceleration()
    {
        return _isBoosting ? acceleration * boostMultiplier : acceleration;
    }

    private void UpdateBoost(float dt)
    {
        if (_isBoosting)
        {
            _boostTimer -= dt;
            if (_boostTimer <= 0f)
            {
                _isBoosting = false;
                _boostCooldownTimer = boostCooldown;
            }
            return;
        }

        if (_boostCooldownTimer > 0f)
        {
            _boostCooldownTimer -= dt;
            return;
        }

        if (Input.GetKeyDown(boostKey))
        {
            _isBoosting = true;
            _boostTimer = boostDuration;
        }
    }

    // ---------------------------------------------------------------------
    // Mouse helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Converts mouse screen position to 2D world position (z = 0 plane).
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
            return _rb.position;

        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(screen);
        return new Vector2(world.x, world.y);
    }

    // ---------------------------------------------------------------------
    // Inspector validation
    // ---------------------------------------------------------------------

    private void OnValidate()
    {
        if (reEngageDistance > 0f && arrivalDistance > 0f && reEngageDistance <= arrivalDistance)
        {
            Debug.LogWarning($"{nameof(PlayerChaseController2D)} on {name}: Re-engage distance should be larger than arrival distance for re-engagement to work cleanly.", this);
        }

        if (orbitRadiusMax < orbitRadiusMin)
        {
            orbitRadiusMax = orbitRadiusMin;
            Debug.LogWarning($"{nameof(PlayerChaseController2D)} on {name}: orbitRadiusMax was smaller than orbitRadiusMin; clamped to match.", this);
        }

        float maxRelevantRadius = Mathf.Max(arrivalDistance, reEngageDistance);
        if (maxRelevantRadius > 0f && orbitRadiusMax > maxRelevantRadius)
        {
            Debug.LogWarning($"{nameof(PlayerChaseController2D)} on {name}: Orbit radius is larger than key distances; micro-orbit may feel too large.", this);
        }
    }
}

