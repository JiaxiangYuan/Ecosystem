using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TimedEventInvoker
/// - After Start, waits for a specified time,
///   then invokes the assigned UnityEvent once.
/// </summary>
public class TimedEventInvoker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float time = 2f;

    [Header("Event")]
    [SerializeField] private UnityEvent onTimeReached;

    private void Start()
    {
        StartCoroutine(InvokeAfterDelay());
    }

    /// <summary>
    /// Waits for the given time, then invokes the event once.
    /// </summary>
    private IEnumerator InvokeAfterDelay()
    {
        if (time > 0f)
            yield return new WaitForSeconds(time);

        onTimeReached?.Invoke();
    }
}