using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the Rock state machine, jump timing, merge flow, and impact visuals.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class RockAgent : MonoBehaviour
{
    public enum State
    {
        Normal,
        Elite,
        Boss
    }

    // Serialized configuration
    [Header("Start State")]
    [SerializeField] private State _startState = State.Normal;

    [Header("Debug")]
    [SerializeField] private State _debugState = State.Normal;

    [Header("Jump")]
    [SerializeField] private float _jumpInterval = 1f;

    [SerializeField] private float _normalJumpForce = 6f;

    [SerializeField] private float _eliteJumpForce = 9f;

    [SerializeField] private float _bossJumpForce = 11f;

    [Header("Scale")]
    [SerializeField] private float _eliteScaleMultiplier = 1.5f;

    [SerializeField] private float _bossScaleMultiplier = 1.5f;

    [Header("Boss Camera Shake")]
    [SerializeField] private float _bossShakeMagnitude = 0.3f;

    [SerializeField] private float _bossShakeDuration = 0.25f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [SerializeField] private Sprite _normalSprite;

    [SerializeField] private Sprite _eliteSprite;

    [SerializeField] private Sprite _bossSprite;

    // Public state data
    public State CurrentState => _currentState;
    public float NormalJumpForce => _normalJumpForce;
    public float EliteJumpForce => _eliteJumpForce;
    public float BossJumpForce => _bossJumpForce;
    public float JumpInterval => _jumpInterval;
    public float BossShakeMagnitude => _bossShakeMagnitude;
    public float BossShakeDuration => _bossShakeDuration;

    // Runtime state
    private Rigidbody2D _rigidbody;
    private float _jumpTimer;
    private State _currentState;
    private State _lastDebugState;
    private Vector3 _baseScale;
    private RockNormalState _normalState;
    private RockEliteState _eliteState;
    private RockBossState _bossState;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[RockAgent] SpriteRenderer is not assigned.", this);
            }
        }

        _baseScale = transform.localScale;

        _normalState = new RockNormalState(this);
        _eliteState = new RockEliteState(this);
        _bossState = new RockBossState(this);

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
        GetCurrentStateHandler().OnCollisionEnter2D(collision.collider);
    }

    // State machine helpers
    public void SwitchState(State newState)
    {
        _currentState = newState;
        _debugState = newState;
        _lastDebugState = newState;

        switch (newState)
        {
            case State.Normal:
                transform.localScale = _baseScale;
                break;

            case State.Elite:
                transform.localScale = _baseScale * _eliteScaleMultiplier;
                break;

            case State.Boss:
                transform.localScale = _baseScale * _bossScaleMultiplier;
                break;
        }

        _jumpTimer = 0f;
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

    // Movement helpers
    public void UpdateJumpCycle(float deltaTime, float jumpForce)
    {
        if (_jumpInterval <= 0f)
        {
            return;
        }

        _jumpTimer += deltaTime;
        if (_jumpTimer < _jumpInterval)
        {
            return;
        }

        _jumpTimer -= _jumpInterval;
        Jump(jumpForce);
    }

    private void Jump(float jumpForce)
    {
        float angleDegrees = Random.Range(-45f, 45f);
        Vector2 jumpDirection = Quaternion.Euler(0f, 0f, angleDegrees) * Vector2.up;
        _rigidbody.AddForce(jumpDirection.normalized * jumpForce, ForceMode2D.Impulse);
    }

    // Merge and death helpers
    public void RemoveMergedRock(RockAgent otherRock)
    {
        if (otherRock == null)
        {
            return;
        }

        InstanceNumberManager.Kill(otherRock.gameObject);
    }

    public void Die()
    {
        InstanceNumberManager.Kill(gameObject);
    }

    // Camera feedback
    public void ShakeCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            cameraTransform.position = GetCameraRestPosition(cameraTransform.position.z);
        }

        _shakeRoutine = StartCoroutine(CameraShakeRoutine(cameraTransform));
    }

    private IEnumerator CameraShakeRoutine(Transform cameraTransform)
    {
        Vector3 restPosition = GetCameraRestPosition(cameraTransform.position.z);

        float elapsedTime = 0f;
        while (elapsedTime < _bossShakeDuration)
        {
            elapsedTime += Time.deltaTime;

            Vector2 offset = Random.insideUnitCircle * _bossShakeMagnitude;
            cameraTransform.position = restPosition + new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        cameraTransform.position = restPosition;
        _shakeRoutine = null;
    }

    private static Vector3 GetCameraRestPosition(float cameraZ)
    {
        return new Vector3(0f, 0f, cameraZ);
    }
}
