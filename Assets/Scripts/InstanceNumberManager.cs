using UnityEngine;

/// <summary>
/// InstanceNumberManager
/// - Tracks total number of Rock, Paper, and Scissors instances.
/// - Uses registration instead of FindGameObjectsWithTag for performance.
/// - Provides a static method to check if the total has reached the limit.
/// </summary>
public class InstanceNumberManager : MonoBehaviour
{
    [Header("Limit Settings")]
    [SerializeField] private int maxCount = 50;

    private static InstanceNumberManager _instance;

    private int _currentCount = 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    /// <summary>
    /// Called by Rock/Paper/Scissors when enabled.
    /// </summary>
    public static void RegisterInstance()
    {
        if (_instance == null) return;
        _instance._currentCount++;
    }

    /// <summary>
    /// Called by Rock/Paper/Scissors when destroyed.
    /// </summary>
    public static void UnregisterInstance()
    {
        if (_instance == null) return;
        _instance._currentCount--;
        if (_instance._currentCount < 0)
            _instance._currentCount = 0;
    }

    /// <summary>
    /// Returns true if total instance count has reached or exceeded the limit.
    /// </summary>
    public static bool IsAtLimit()
    {
        if (_instance == null) return false;
        return _instance._currentCount >= _instance.maxCount;
    }

    public static int GetCurrentCount()
    {
        if (_instance == null) return 0;
        return _instance._currentCount;
    }
}