using UnityEngine;

/// <summary>
/// Handles collision-driven evolution and death rules for normal scissors.
/// </summary>
public sealed class ScissorsNormalState : UnitBaseState
{
    private readonly ScissorsAgent _agent;

    public ScissorsNormalState(ScissorsAgent agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _agent.SpeedMultiplier = 1f;
    }

    public override void Tick(float deltaTime)
    {
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Paper"))
        {
            _agent.RegisterPaperKill();
            return;
        }

        if (other.CompareTag("Rock"))
        {
            _agent.Die();
        }
    }
}
