using UnityEngine;

/// <summary>
/// Handles avoidance, chasing, and collision-driven evolution for elite scissors.
/// </summary>
public sealed class ScissorsEliteState : UnitBaseState
{
    private const float TurnDirectionHoldTime = 0.15f;
    private const float RetargetInterval = 0.2f;

    private readonly ScissorsAgent _agent;
    private Transform _paperTarget;
    private int _turnDirectionSign = 1;
    private float _turnDirectionHoldTimer;
    private float _retargetTimer;

    public ScissorsEliteState(ScissorsAgent agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.SpeedMultiplier = 1f;
        _paperTarget = null;
        _retargetTimer = 0f;
        _turnDirectionSign = 1;
        _turnDirectionHoldTimer = 0f;
    }

    public override void Tick(float deltaTime)
    {
        Vector2 origin = _agent.transform.position;
        Vector2 moveDirection = _agent.MoveDirection;

        _retargetTimer -= deltaTime;
        if (_retargetTimer <= 0f)
        {
            _retargetTimer = RetargetInterval;
            _paperTarget = FindNearestPaper(origin, _agent.EliteRayLength * 3f);
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            _agent.EliteCastRadius,
            moveDirection,
            _agent.EliteRayLength
        );

        if (hit.collider != null && !hit.collider.CompareTag("Paper"))
        {
            Vector2 surfaceNormal = hit.normal;
            Vector2 perpendicular = Vector2.Perpendicular(moveDirection);
            float side = Vector2.Dot(surfaceNormal, perpendicular);

            if (_turnDirectionHoldTimer <= 0f)
            {
                _turnDirectionSign = side >= 0f ? 1 : -1;
                _turnDirectionHoldTimer = TurnDirectionHoldTime;
            }

            float angleDelta = _agent.EliteAngularSpeedDeg * deltaTime * _turnDirectionSign;
            _agent.transform.Rotate(0f, 0f, angleDelta);
            _agent.SpeedMultiplier = 1f;
            _turnDirectionHoldTimer -= deltaTime;
            return;
        }

        if (_paperTarget != null)
        {
            Vector2 toTarget = (Vector2)_paperTarget.position - origin;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Vector2 desiredDirection = toTarget.normalized;
                float currentAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                float targetAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
                float angleDelta = Mathf.DeltaAngle(currentAngle, targetAngle);
                float maxTurn = _agent.EliteAngularSpeedDeg * deltaTime;

                angleDelta = Mathf.Clamp(angleDelta, -maxTurn, maxTurn);
                _agent.transform.Rotate(0f, 0f, angleDelta);
            }

            _agent.SpeedMultiplier = _agent.EliteChaseSpeedMultiplier;
            return;
        }

        _agent.SpeedMultiplier = 1f;
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Paper"))
        {
            PaperAgent paperAgent = other.GetComponent<PaperAgent>();
            if (paperAgent != null && paperAgent.CurrentState != PaperAgent.State.Boss)
            {
                _agent.RegisterPaperKill();
            }

            return;
        }

        if (other.CompareTag("Rock"))
        { 
            RockAgent rockAgent = other.GetComponent<RockAgent>();
            if (rockAgent != null && rockAgent.CurrentState != RockAgent.State.Normal)
            {
                _agent.Die();
            }
        }
    }

    private Transform FindNearestPaper(Vector2 origin, float radius)
    {
        GameObject[] paperObjects = GameObject.FindGameObjectsWithTag("Paper");

        Transform closestPaper = null;
        float searchRadiusSquared = radius * radius;
        float closestDistanceSquared = float.MaxValue;

        foreach (GameObject paperObject in paperObjects)
        {
            float distanceSquared = ((Vector2)paperObject.transform.position - origin).sqrMagnitude;
            if (distanceSquared < closestDistanceSquared && distanceSquared <= searchRadiusSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestPaper = paperObject.transform;
            }
        }

        return closestPaper;
    }
}
