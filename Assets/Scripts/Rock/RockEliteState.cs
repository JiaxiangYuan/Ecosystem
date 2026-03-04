using UnityEngine;

/// <summary>
/// Handles jumping, merge checks, and death rules for elite rock.
/// </summary>
public sealed class RockEliteState : UnitBaseState
{
    private readonly RockAgent _agent;

    public RockEliteState(RockAgent agent)
    {
        _agent = agent;
    }

    public override void Tick(float deltaTime)
    {
        _agent.UpdateJumpCycle(deltaTime, _agent.EliteJumpForce);
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Paper"))
        {
            PaperAgent paperAgent = other.GetComponent<PaperAgent>();
            if (paperAgent != null &&
                (paperAgent.CurrentState == PaperAgent.State.Elite ||
                 paperAgent.CurrentState == PaperAgent.State.Boss))
            {
                _agent.Die();
            }
        }

        if (!other.CompareTag("Rock"))
        {
            return;
        }

        RockAgent otherRock = other.GetComponent<RockAgent>();
        if (otherRock == null || otherRock.CurrentState != RockAgent.State.Elite)
        {
            return;
        }

        int myInstanceId = _agent.gameObject.GetInstanceID();
        int otherInstanceId = otherRock.gameObject.GetInstanceID();

        if (myInstanceId > otherInstanceId)
        {
            _agent.SwitchState(RockAgent.State.Boss);
            _agent.RemoveMergedRock(otherRock);
        }
    }
}
