using UnityEngine;

/// <summary>
/// Owns the Scissors state machine, forward movement, and evolution counters.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class ScissorsAgent : MonoBehaviour
{
    public enum State
    {
        Normal,
        Elite,
        Boss
    }

    // Serialized configuration
    [Header("Movement")]
    [SerializeField] private float _movementSpeed = 3f;

    [Header("Start State")]
    [SerializeField] private State _startState = State.Normal;

    [Header("Debug")]
    [Tooltip("Changing this value at runtime forces an immediate state switch.")]
    [SerializeField] private State _debugState = State.Normal;

    [Header("Elite Steering")]
    [SerializeField] private float _eliteCastRadius = 0.4f;

    [SerializeField] private float _eliteRayLength = 4f;

    [SerializeField] private float _eliteAngularSpeedDeg = 180f;

    [SerializeField] private float _eliteChaseSpeedMultiplier = 1.2f;

    [Header("Boss")]
    [SerializeField] private float _bossSpinAngularSpeedDeg = 240f;

    [Tooltip("Spawn position = current position + movement direction * this offset.")]
    [SerializeField] private float _bossSpawnOffset = 1.25f;

    [Tooltip("Seconds between boss spawns while a valid paper target is in front.")]
    [SerializeField] private float _bossSpawnCooldown = 5f;

    [Header("Scale")]
    [SerializeField] private float _eliteScaleMultiplier = 1.5f;

    [Tooltip("Boss scale multiplier relative to the base scale.")]
    [SerializeField] private float _bossScaleMultiplier = 1.5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [SerializeField] private Sprite _normalSprite;

    [SerializeField] private Sprite _eliteSprite;

    [SerializeField] private Sprite _bossSprite;

    // Public state data
    public State CurrentState => _currentState;

    /// <summary>
    /// Scissors travel in the tail direction, which is always opposite to transform.up.
    /// </summary>
    public Vector2 MoveDirection
    {
        get => -(Vector2)transform.up;
        set
        {
            Vector2 normalizedDirection = value.sqrMagnitude > 0.0001f ? value.normalized : Vector2.up;
            transform.up = -normalizedDirection;
        }
    }

    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set => _speedMultiplier = Mathf.Max(0f, value);
    }

    public float EliteCastRadius => _eliteCastRadius;
    public float EliteRayLength => _eliteRayLength;
    public float EliteAngularSpeedDeg => _eliteAngularSpeedDeg;
    public float EliteChaseSpeedMultiplier => _eliteChaseSpeedMultiplier;
    public float BossSpinAngularSpeedDeg => _bossSpinAngularSpeedDeg;
    public float BossSpawnOffset => _bossSpawnOffset;
    public float BossSpawnCooldown => _bossSpawnCooldown;

    // Runtime state
    private Rigidbody2D _rigidbody;
    private float _speedMultiplier = 1f;
    private State _currentState;
    private State _lastDebugState;
    private Vector3 _baseScale;
    private ScissorsNormalState _normalState;
    private ScissorsEliteState _eliteState;
    private ScissorsBossState _bossState;
    private int _paperKillsWhileElite;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _baseScale = transform.localScale;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[ScissorsAgent] SpriteRenderer is not assigned.", this);
            }
        }

        _normalState = new ScissorsNormalState(this);
        _eliteState = new ScissorsEliteState(this);
        _bossState = new ScissorsBossState(this);

        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude < 0.0001f)
        {
            randomDirection = Vector2.right;
        }

        MoveDirection = randomDirection.normalized;

        SwitchState(_startState);
        _debugState = _startState;
        _lastDebugState = _startState;
    }

    private void Update()
    {
        if (_debugState != _lastDebugState)
        {
            _lastDebugState = _debugState;
            SwitchState(_debugState);
        }

        GetCurrentStateHandler().Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = MoveDirection * _movementSpeed * _speedMultiplier;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == null)
        {
            return;
        }

        GetCurrentStateHandler().OnCollisionEnter2D(collision.collider);

        if (!collision.collider.CompareTag("Paper"))
        {
            transform.Rotate(0f, 0f, 180f);
        }
    }

    // State machine helpers
    public void SwitchState(State newState)
    {
        if (_currentState == State.Boss && newState != State.Boss)
        {
            _bossState.RestorePhysicsBody();
        }

        _currentState = newState;
        _debugState = newState;
        _lastDebugState = newState;

        switch (newState)
        {
            case State.Normal:
                transform.localScale = _baseScale;
                SpeedMultiplier = 1f;
                _paperKillsWhileElite = 0;
                break;

            case State.Elite:
                transform.localScale = _baseScale * _eliteScaleMultiplier;
                SpeedMultiplier = 1f;
                _paperKillsWhileElite = 0;
                break;

            case State.Boss:
                transform.localScale = _baseScale * _bossScaleMultiplier;
                SpeedMultiplier = 0f;
                break;
        }

        ApplyStateVisuals(newState);
        GetCurrentStateHandler().Enter();
    }

    private UnitBaseState GetCurrentStateHandler()
    {
        return _currentState switch
        {
            State.Normal => _normalState,
            State.Elite => _eliteState,
            State.Boss => _bossState,
            _ => _normalState
        };
    }

    // Visual helpers
    private void ApplyStateVisuals(State state)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        switch (state)
        {
            case State.Normal:
                if (_normalSprite != null)
                {
                    _spriteRenderer.sprite = _normalSprite;
                }

                break;

            case State.Elite:
                if (_eliteSprite != null)
                {
                    _spriteRenderer.sprite = _eliteSprite;
                }

                break;

            case State.Boss:
                if (_bossSprite != null)
                {
                    _spriteRenderer.sprite = _bossSprite;
                }

                break;
        }
    }

    // Evolution helpers
    public void RegisterPaperKill()
    {
        if (_currentState == State.Normal)
        {
            SwitchState(State.Elite);
            return;
        }

        if (_currentState != State.Elite)
        {
            return;
        }

        _paperKillsWhileElite++;
        if (_paperKillsWhileElite >= 3)
        {
            SwitchState(State.Boss);
        }
    }

    public void Die()
    {
        InstanceNumberManager.Kill(gameObject);
    }

    // Editor visualization
    /// <summary>
    /// Draws the elite steering probes and the boss spawn direction.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        State previewState = Application.isPlaying ? _currentState : _debugState;
        Vector2 origin = transform.position;
        Vector2 moveDirection = -(Vector2)transform.up;

        switch (previewState)
        {
            case State.Elite:
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, origin + moveDirection * _eliteRayLength);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(origin + moveDirection * _eliteRayLength, _eliteCastRadius);

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(origin, _eliteCastRadius);
                break;

            case State.Boss:
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(origin, origin + moveDirection * 100f);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(origin + moveDirection * _bossSpawnOffset, 0.2f);
                break;
        }
    }
}
