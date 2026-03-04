using UnityEngine;

/// <summary>
/// Handles boss rock jumping, boss-only death checks, and wall impact feedback.
/// </summary>
public sealed class RockBossState : UnitBaseState
{
    private readonly RockAgent _agent;

    public RockBossState(RockAgent agent)
    {
        _agent = agent;
    }

    public override void Tick(float deltaTime)
    {
        _agent.UpdateJumpCycle(deltaTime, _agent.BossJumpForce);
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Paper"))
        {
            PaperAgent paperAgent = other.GetComponent<PaperAgent>();
            if (paperAgent != null && paperAgent.CurrentState == PaperAgent.State.Boss)
            {
                _agent.Die();
            }
        }

        if (other.CompareTag("Wall"))
        {
            _agent.ShakeCamera();
        }
    }
}
