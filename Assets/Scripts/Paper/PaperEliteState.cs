using UnityEngine;

/// <summary>
/// Handles elite paper scaling reactions, promotion timing, and collision death rules.
/// </summary>
public sealed class PaperEliteState : UnitBaseState
{
    private readonly PaperAgent _agent;
    private float _elapsedTime;

    public PaperEliteState(PaperAgent agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _elapsedTime = 0f;
    }

    public override void Tick(float deltaTime)
    {
        _elapsedTime += deltaTime;
        if (_elapsedTime >= _agent.EliteToBossDelay)
        {
            _agent.SwitchState(PaperAgent.State.Boss);
        }
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Scissors"))
        {
            ScissorsAgent scissorsAgent = other.GetComponent<ScissorsAgent>();
            if (scissorsAgent != null &&
                (scissorsAgent.CurrentState == ScissorsAgent.State.Elite ||
                 scissorsAgent.CurrentState == ScissorsAgent.State.Boss))
            {
                _agent.Die();
            }

            return;
        }

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

    /// <summary>
    /// Reacts to nearby threats while the agent is in Elite state.
    /// </summary>
    public void HandleTriggerEnter(Collider2D other)
    {
        if (other.CompareTag("Scissors"))
        {
            Vector3 targetScale = _agent.EliteBaseScale * _agent.EliteScissorsScaleFactor;
            _agent.StartScaleTransition(targetScale, _agent.EliteScaleSpeed, true);
            return;
        }

        if (!other.CompareTag("Rock"))
        {
            return;
        }

        RockAgent rockAgent = other.GetComponent<RockAgent>();
        if (rockAgent == null)
        {
            return;
        }

        if (rockAgent.CurrentState == RockAgent.State.Normal ||
            rockAgent.CurrentState == RockAgent.State.Elite)
        {
            Vector3 targetScale = _agent.EliteBaseScale * _agent.EliteRockScaleFactor;
            _agent.StartScaleTransition(targetScale, _agent.EliteScaleSpeed, false);
        }
    }
}
