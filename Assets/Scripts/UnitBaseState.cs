using UnityEngine;

/// <summary>
/// Defines the lifecycle hooks shared by every agent state.
/// </summary>
public abstract class UnitBaseState
{
    protected InstanceNumberManager InstanceManager => InstanceNumberManager.Instance;

    public virtual void Enter() { }

    public virtual void Tick(float deltaTime) { }

    public virtual void OnCollisionEnter2D(Collider2D other) { }
}
