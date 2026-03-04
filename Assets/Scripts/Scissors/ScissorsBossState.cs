using UnityEngine;

/// <summary>
/// Handles boss scissors rotation, spawning, and boss-only death rules.
/// </summary>
public sealed class ScissorsBossState : UnitBaseState
{
    private readonly ScissorsAgent _agent;

    private float _spawnCooldownRemaining;
    private Rigidbody2D _rigidbody;
    private RigidbodyType2D _previousBodyType;

    public ScissorsBossState(ScissorsAgent agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.SpeedMultiplier = 0f;
        _spawnCooldownRemaining = 0f;

        if (_rigidbody == null)
        {
            _rigidbody = _agent.GetComponent<Rigidbody2D>();
        }

        if (_rigidbody != null)
        {
            _previousBodyType = _rigidbody.bodyType;
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }
    }

    public override void Tick(float deltaTime)
    {
        _agent.SpeedMultiplier = 0f;

        if (_spawnCooldownRemaining > 0f)
        {
            _spawnCooldownRemaining -= deltaTime;
        }

        _agent.transform.Rotate(0f, 0f, _agent.BossSpinAngularSpeedDeg * deltaTime);

        Vector2 moveDirection = _agent.MoveDirection;
        RaycastHit2D hit = Physics2D.Raycast(_agent.transform.position, moveDirection, Mathf.Infinity);
        if (hit.collider == null || !hit.collider.CompareTag("Paper") || _spawnCooldownRemaining > 0f)
        {
            return;
        }

        SpawnNormalScissors(moveDirection);
        _spawnCooldownRemaining = Mathf.Max(0f, _agent.BossSpawnCooldown);
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Rock"))
        {
            return;
        }

        RockAgent rockAgent = other.GetComponent<RockAgent>();
        if (rockAgent != null && rockAgent.CurrentState == RockAgent.State.Boss)
        {
            _agent.Die();
        }
    }

    public void RestorePhysicsBody()
    {
        if (_rigidbody == null)
        {
            _rigidbody = _agent.GetComponent<Rigidbody2D>();
        }

        if (_rigidbody != null)
        {
            _rigidbody.bodyType = _previousBodyType;
        }
    }

    private void SpawnNormalScissors(Vector2 moveDirection)
    {
        Vector2 normalizedDirection = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.up;
        Vector3 spawnPosition = _agent.transform.position + (Vector3)(normalizedDirection * _agent.BossSpawnOffset);

        GameObject spawnedScissors = InstanceNumberManager.SpawnManaged(
            InstanceNumberManager.AgentType.Scissors,
            spawnPosition
        );

        if (spawnedScissors == null)
        {
            return;
        }

        ScissorsAgent childAgent = spawnedScissors.GetComponent<ScissorsAgent>();
        if (childAgent == null)
        {
            return;
        }

        childAgent.SwitchState(ScissorsAgent.State.Normal);
        childAgent.MoveDirection = normalizedDirection;
    }
}
