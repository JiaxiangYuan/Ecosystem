using UnityEngine;

/// <summary>
/// Handles timed promotion and collision death rules for normal paper.
/// </summary>
public sealed class PaperNormalState : UnitBaseState
{
    private readonly PaperAgent _agent;
    private float _elapsedTime;

    public PaperNormalState(PaperAgent agent)
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
        if (_elapsedTime >= _agent.NormalToEliteDelay)
        {
            _agent.SwitchState(PaperAgent.State.Elite);
        }
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Scissors"))
        {
            _agent.Die();
            return;
        }

        if (!other.CompareTag("Rock"))
        {
            return;
        }

        RockAgent rockAgent = other.GetComponent<RockAgent>();
        if (rockAgent != null && rockAgent.CurrentState != RockAgent.State.Normal)
        {
            _agent.Die();
        }
    }
}
