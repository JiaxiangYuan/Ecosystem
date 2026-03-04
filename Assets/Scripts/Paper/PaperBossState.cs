using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the boss paper exposure loop and its boss-only combat rules.
/// </summary>
public sealed class PaperBossState : UnitBaseState
{
    private readonly PaperAgent _agent;
    private Coroutine _bossLoopRoutine;

    public PaperBossState(PaperAgent agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        if (_bossLoopRoutine != null)
        {
            _agent.StopCoroutine(_bossLoopRoutine);
            _bossLoopRoutine = null;
        }

        _agent.SetBodyColliderEnabled(false);
        _agent.SetBossVisualState(false);
        _bossLoopRoutine = _agent.StartCoroutine(BossLoopRoutine());
    }

    public override void Tick(float deltaTime)
    {
    }

    public override void OnCollisionEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Scissors"))
        {
            return;
        }

        ScissorsAgent scissorsAgent = other.GetComponent<ScissorsAgent>();
        if (scissorsAgent != null &&
            (scissorsAgent.CurrentState == ScissorsAgent.State.Elite ||
             scissorsAgent.CurrentState == ScissorsAgent.State.Boss))
        {
            _agent.Die();
        }
    }

    private IEnumerator BossLoopRoutine()
    {
        float interval = Mathf.Max(0.0001f, _agent.BossColliderInterval);
        float exposureDuration = Mathf.Max(0.0001f, _agent.BossColliderOnDuration);

        while (_agent != null && _agent.CurrentState == PaperAgent.State.Boss)
        {
            yield return new WaitForSeconds(interval);

            if (_agent == null || _agent.CurrentState != PaperAgent.State.Boss)
            {
                _bossLoopRoutine = null;
                yield break;
            }

            _agent.SetBodyColliderEnabled(true);
            _agent.SetBossVisualState(true);

            yield return new WaitForSeconds(exposureDuration);

            if (_agent == null || _agent.CurrentState != PaperAgent.State.Boss)
            {
                _bossLoopRoutine = null;
                yield break;
            }

            _agent.SetBodyColliderEnabled(false);
            _agent.SetBossVisualState(false);
            _agent.SpawnNormalPaperWithImpulse(_agent.BossSpawnImpulse);
        }

        _bossLoopRoutine = null;
    }
}
