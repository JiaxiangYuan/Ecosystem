using UnityEngine;

/// <summary>
/// Handles jumping, merge checks, and death rules for normal rock.
/// </summary>
public sealed class RockNormalState : UnitBaseState
{
    private readonly RockAgent _agent;

    public RockNormalState(RockAgent agent)
    {
        _agent = agent;
    }

    public override void Tick(float deltaTime)
    {
        _agent.UpdateJumpCycle(deltaTime, _agent.NormalJumpForce);
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (other.CompareTag("Paper"))
        {
            _agent.Die();
            return;
        }

        if (!other.CompareTag("Rock"))
        {
            return;
        }

        RockAgent otherRock = other.GetComponent<RockAgent>();
        if (otherRock == null || otherRock.CurrentState != RockAgent.State.Normal)
        {
            return;
        }

        int myInstanceId = _agent.gameObject.GetInstanceID();
        int otherInstanceId = otherRock.gameObject.GetInstanceID();

        if (myInstanceId > otherInstanceId)
        {
            _agent.SwitchState(RockAgent.State.Elite);
            _agent.RemoveMergedRock(otherRock);
        }
    }
}
