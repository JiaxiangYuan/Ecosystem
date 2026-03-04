using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the Paper state machine, visuals, collider exposure, and scale transitions.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PaperAgent : MonoBehaviour
{
    public enum State
    {
        Normal,
        Elite,
        Boss
    }

    // Serialized configuration
    [Header("Colliders")]
    [Tooltip("Physical collider used for collisions and boss vulnerability windows.")]
    [SerializeField] private Collider2D _bodyCollider;

    [Tooltip("Trigger collider used for sensing while the body collider may be disabled.")]
    [SerializeField] private Collider2D _triggerCollider;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [SerializeField] private Sprite _normalSprite;

    [SerializeField] private Sprite _eliteSprite;

    [SerializeField] private Sprite _bossSprite;

    [Tooltip("Transparency applied while the agent is in Elite state.")]
    [Range(0f, 1f)]
    [SerializeField] private float _eliteAlpha = 0.8f;

    [Header("Start State")]
    [SerializeField] private State _startState = State.Normal;

    [Header("Debug")]
    [Tooltip("Changing this value at runtime forces an immediate state switch.")]
    [SerializeField] private State _debugState = State.Normal;

    [Header("Normal -> Elite")]
    [SerializeField] private float _normalToEliteDelay = 10f;

    [Header("Elite")]
    [Tooltip("Elite scale = normal scale * this multiplier.")]
    [SerializeField] private float _eliteBaseScaleMultiplier = 2f;

    [Tooltip("Interpolation speed used by elite scale reactions.")]
    [SerializeField] private float _eliteScaleSpeed = 6f;

    [Tooltip("Scissors trigger target = elite scale * this factor.")]
    [SerializeField] private float _eliteScissorsScaleFactor = 0.25f;

    [Tooltip("Rock trigger target = elite scale * this factor.")]
    [SerializeField] private float _eliteRockScaleFactor = 2f;

    [Header("Elite -> Boss")]
    [SerializeField] private float _eliteToBossDelay = 20f;

    [Header("Boss")]
    [Tooltip("Boss scale = elite scale * this multiplier.")]
    [SerializeField] private float _bossBaseScaleMultiplier = 2f;

    [Tooltip("Interval between boss vulnerability windows.")]
    [SerializeField] private float _bossColliderInterval = 5f;

    [Tooltip("How long the body collider stays enabled during a vulnerability window.")]
    [SerializeField] private float _bossColliderOnDuration = 2f;

    [Tooltip("Impulse magnitude applied to papers spawned by the boss.")]
    [SerializeField] private float _bossSpawnImpulse = 8f;

    // Public state data
    public State CurrentState => _currentState;
    public float NormalToEliteDelay => _normalToEliteDelay;
    public float EliteScaleSpeed => _eliteScaleSpeed;
    public float EliteScissorsScaleFactor => _eliteScissorsScaleFactor;
    public float EliteRockScaleFactor => _eliteRockScaleFactor;
    public float EliteToBossDelay => _eliteToBossDelay;
    public float BossBaseScaleMultiplier => _bossBaseScaleMultiplier;
    public float BossColliderInterval => _bossColliderInterval;
    public float BossColliderOnDuration => _bossColliderOnDuration;
    public float BossSpawnImpulse => _bossSpawnImpulse;

    public Vector3 NormalBaseScale => _normalBaseScale;
    public Vector3 EliteBaseScale => _normalBaseScale * _eliteBaseScaleMultiplier;
    public Vector3 BossBaseScale => _normalBaseScale * BossBaseScaleMultiplier;

    // Runtime state
    private State _currentState;
    private State _lastDebugState;
    private UnitBaseState _normalState;
    private PaperEliteState _eliteState;
    private PaperBossState _bossState;
    private Vector3 _normalBaseScale;
    private Coroutine _scaleRoutine;
    private bool _isScaleUninterruptible;

    private void Awake()
    {
        if (_bodyCollider == null)
        {
            Debug.LogError("[PaperAgent] Body collider is not assigned.", this);
        }

        if (_triggerCollider == null)
        {
            Debug.LogError("[PaperAgent] Trigger collider is not assigned.", this);
        }
        else if (!_triggerCollider.isTrigger)
        {
            Debug.LogError("[PaperAgent] Trigger collider must have Is Trigger enabled.", this);
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[PaperAgent] SpriteRenderer is not assigned.", this);
            }
        }

        _normalBaseScale = transform.localScale;

        _normalState = new PaperNormalState(this);
        _eliteState = new PaperEliteState(this);
        _bossState = new PaperBossState(this);

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        if (_bodyCollider != null && !_bodyCollider.enabled)
        {
            return;
        }

        GetCurrentStateHandler().OnCollisionEnter2D(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || _currentState != State.Elite)
        {
            return;
        }

        _eliteState.HandleTriggerEnter(other);
    }

    // State machine helpers
    public void SwitchState(State newState)
    {
        _currentState = newState;
        _debugState = newState;
        _lastDebugState = newState;

        StopScaleTransition();

        switch (newState)
        {
            case State.Normal:
                transform.localScale = _normalBaseScale;
                SetBodyColliderEnabled(true);
                SetTriggerColliderEnabled(true);
                break;

            case State.Elite:
                transform.localScale = EliteBaseScale;
                SetBodyColliderEnabled(true);
                SetTriggerColliderEnabled(true);
                break;

            case State.Boss:
                transform.localScale = BossBaseScale;
                SetBodyColliderEnabled(false);
                SetTriggerColliderEnabled(true);
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

                SetAlpha(1f);
                break;

            case State.Elite:
                if (_eliteSprite != null)
                {
                    _spriteRenderer.sprite = _eliteSprite;
                }

                SetAlpha(_eliteAlpha);
                break;

            case State.Boss:
                if (_bossSprite != null)
                {
                    _spriteRenderer.sprite = _bossSprite;
                }

                SetAlpha(1f);
                break;
        }
    }

    public void SetAlpha(float alpha)
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        Color color = _spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        _spriteRenderer.color = color;
    }

    public void SetBossVisualState(bool isColliderEnabled)
    {
        SetAlpha(isColliderEnabled ? 0.8f : 0.2f);
    }

    // Collider helpers
    public void SetBodyColliderEnabled(bool isEnabled)
    {
        if (_bodyCollider != null)
        {
            _bodyCollider.enabled = isEnabled;
        }
    }

    public void SetTriggerColliderEnabled(bool isEnabled)
    {
        if (_triggerCollider != null)
        {
            _triggerCollider.enabled = isEnabled;
        }
    }

    // Scale helpers
    /// <summary>
    /// Starts a scale transition and optionally blocks later requests until it finishes.
    /// </summary>
    public void StartScaleTransition(Vector3 targetScale, float interpolationSpeed, bool isUninterruptible)
    {
        if (_scaleRoutine != null && _isScaleUninterruptible)
        {
            return;
        }

        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
        }

        _isScaleUninterruptible = isUninterruptible;
        _scaleRoutine = StartCoroutine(ScaleToRoutine(targetScale, interpolationSpeed));
    }

    private IEnumerator ScaleToRoutine(Vector3 targetScale, float interpolationSpeed)
    {
        interpolationSpeed = Mathf.Max(0.0001f, interpolationSpeed);
        const float threshold = 0.00001f;

        while ((transform.localScale - targetScale).sqrMagnitude > threshold)
        {
            float deltaTime = Time.deltaTime;
            float blendFactor = 1f - Mathf.Exp(-interpolationSpeed * deltaTime);

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blendFactor);
            yield return null;
        }

        transform.localScale = targetScale;
        _scaleRoutine = null;
        _isScaleUninterruptible = false;
    }

    private void StopScaleTransition()
    {
        if (_scaleRoutine != null)
        {
            StopCoroutine(_scaleRoutine);
            _scaleRoutine = null;
        }

        _isScaleUninterruptible = false;
    }

    // Spawn helpers
    public void SpawnNormalPaperWithImpulse(float impulseMagnitude)
    {
        GameObject spawnedPaper = InstanceNumberManager.SpawnManaged(
            InstanceNumberManager.AgentType.Paper,
            transform.position
        );

        if (spawnedPaper == null)
        {
            return;
        }

        PaperAgent spawnedAgent = spawnedPaper.GetComponent<PaperAgent>();
        if (spawnedAgent != null)
        {
            spawnedAgent.SwitchState(State.Normal);
        }

        Rigidbody2D spawnedBody = spawnedPaper.GetComponent<Rigidbody2D>();
        if (spawnedBody == null)
        {
            return;
        }

        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }

        spawnedBody.AddForce(direction.normalized * impulseMagnitude, ForceMode2D.Impulse);
    }

    public void Die()
    {
        InstanceNumberManager.Kill(gameObject);
    }
}
