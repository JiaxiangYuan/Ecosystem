using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Maintains the target population of Rock, Paper, and Scissors agents.
/// </summary>
public class InstanceNumberManager : MonoBehaviour
{
    public enum AgentType
    {
        Rock = 0,
        Paper = 1,
        Scissors = 2
    }

    /// <summary>
    /// Reports unexpected destroys so tracked counts stay consistent.
    /// </summary>
    private sealed class ManagedInstance : MonoBehaviour
    {
        private int _instanceId;
        private bool _isInitialized;

        public void Initialize(int instanceId)
        {
            _instanceId = instanceId;
            _isInitialized = true;
        }

        private void OnDestroy()
        {
            if (!_isInitialized)
            {
                return;
            }

            InstanceNumberManager manager = Instance;
            if (manager == null || manager._isShuttingDown)
            {
                return;
            }

            manager.HandleInstanceDestroyed(_instanceId);
        }
    }

    // Serialized configuration
    [Header("Prefabs")]
    [SerializeField] private GameObject _rockPrefab;

    [SerializeField] private GameObject _paperPrefab;

    [SerializeField] private GameObject _scissorsPrefab;

    [Header("Population")]
    [SerializeField] private int _maxCount = 50;

    [Header("Spawn Area")]
    [Tooltip("Spawn inside a centered rectangle within the camera view.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float _spawnAreaScale = 0.8f;

    [Header("Spawn Spacing")]
    [Tooltip("Require this minimum world-space distance from every tracked living agent.")]
    [Min(0.01f)]
    [SerializeField] private float _minSpawnDistance = 3f;

    [Tooltip("Stop trying to place a new spawn after this many random samples.")]
    [Range(1, 500)]
    [SerializeField] private int _maxSpawnPositionTries = 80;

    // Singleton
    private static InstanceNumberManager _instance;

    public static InstanceNumberManager Instance => _instance;

    // Runtime state
    private bool _isShuttingDown;
    private bool _isRefillQueued;
    private int _currentCount;

    private readonly Dictionary<int, AgentType> _trackedTypeByInstanceId = new Dictionary<int, AgentType>(128);
    private readonly Dictionary<int, Transform> _trackedTransformByInstanceId = new Dictionary<int, Transform>(128);
    private readonly int[] _countsByType = new int[3];
    private readonly GameObject[] _prefabsByType = new GameObject[3];

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (_instance._isShuttingDown)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            _instance = this;
        }

        _isShuttingDown = false;
        RefreshPrefabCache();
    }

    private void OnEnable()
    {
        _isShuttingDown = false;
    }

    private void Start()
    {
        FillToLimit();
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDisable()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        _isShuttingDown = true;
        _isRefillQueued = false;

        if (_instance == this)
        {
            _instance = null;
        }

        _trackedTypeByInstanceId.Clear();
        _trackedTransformByInstanceId.Clear();
        _currentCount = 0;

        for (int typeIndex = 0; typeIndex < _countsByType.Length; typeIndex++)
        {
            _countsByType[typeIndex] = 0;
            _prefabsByType[typeIndex] = null;
        }
    }

    // Public API
    /// <summary>
    /// Returns whether the tracked population has reached the configured limit.
    /// </summary>
    public static bool IsAtLimit()
    {
        return _instance != null && _instance._currentCount >= _instance._maxCount;
    }

    /// <summary>
    /// Returns the current tracked population across all types.
    /// </summary>
    public static int GetCurrentCount()
    {
        return _instance == null ? 0 : _instance._currentCount;
    }

    /// <summary>
    /// Returns the current tracked population for a single agent type.
    /// </summary>
    public static int GetTypeCount(AgentType type)
    {
        return _instance == null ? 0 : _instance._countsByType[(int)type];
    }

    public static GameObject GetRockPrefab()
    {
        return _instance == null ? null : _instance._rockPrefab;
    }

    public static GameObject GetPaperPrefab()
    {
        return _instance == null ? null : _instance._paperPrefab;
    }

    public static GameObject GetScissorsPrefab()
    {
        return _instance == null ? null : _instance._scissorsPrefab;
    }

    /// <summary>
    /// Spawns a specific agent type through the manager so tracking stays consistent.
    /// </summary>
    public static GameObject SpawnManaged(AgentType type, Vector3 position)
    {
        if (_instance == null)
        {
            return null;
        }

        return _instance.SpawnManagedInstance(type, position);
    }

    /// <summary>
    /// Destroys a managed instance through the population manager so counts and refill stay in sync.
    /// </summary>
    public static void Kill(GameObject instance)
    {
        if (_instance == null || instance == null)
        {
            return;
        }

        if (_instance._isShuttingDown)
        {
            Destroy(instance);
            return;
        }

        int instanceId = instance.GetInstanceID();
        _instance.UnregisterInstanceById(instanceId);
        Destroy(instance);
        _instance.FillToLimitInternal();
    }

    /// <summary>
    /// Spawns agents until the configured population limit is reached.
    /// </summary>
    public static void FillToLimit()
    {
        if (_instance == null)
        {
            return;
        }

        _instance.FillToLimitInternal();
    }

    // Internal lifecycle
    private void HandleInstanceDestroyed(int instanceId)
    {
        if (_isShuttingDown)
        {
            return;
        }

        if (UnregisterInstanceById(instanceId))
        {
            QueueRefill();
        }
    }

    private void QueueRefill()
    {
        if (_isShuttingDown || _isRefillQueued)
        {
            return;
        }

        _isRefillQueued = true;
        StartCoroutine(RefillNextFrame());
    }

    private IEnumerator RefillNextFrame()
    {
        yield return null;

        _isRefillQueued = false;

        if (_isShuttingDown)
        {
            yield break;
        }

        FillToLimitInternal();
    }

    // Spawn and tracking helpers
    private void FillToLimitInternal()
    {
        if (_isShuttingDown || !RefreshPrefabCache())
        {
            return;
        }

        while (_currentCount < _maxCount)
        {
            if (!TrySpawnLeastRepresentedType())
            {
                break;
            }
        }
    }

    private bool TrySpawnLeastRepresentedType()
    {
        if (_isShuttingDown || _currentCount >= _maxCount)
        {
            return false;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[InstanceNumberManager] No Camera.main found. Cannot spawn within view bounds.");
            return false;
        }

        if (!TryGetLeastRepresentedType(out AgentType chosenType))
        {
            Debug.LogWarning("[InstanceNumberManager] No valid prefabs assigned. Cannot spawn.");
            return false;
        }

        GameObject prefab = _prefabsByType[(int)chosenType];
        if (prefab == null)
        {
            return false;
        }

        if (!TryFindSpawnPosition(mainCamera, _spawnAreaScale, _minSpawnDistance, out Vector3 spawnPosition))
        {
            return false;
        }

        return SpawnManagedInstance(chosenType, spawnPosition) != null;
    }

    private void RegisterInstance(GameObject instance, AgentType type)
    {
        if (instance == null)
        {
            return;
        }

        int instanceId = instance.GetInstanceID();
        if (_trackedTypeByInstanceId.ContainsKey(instanceId))
        {
            return;
        }

        _trackedTypeByInstanceId.Add(instanceId, type);
        _trackedTransformByInstanceId.Add(instanceId, instance.transform);

        _currentCount++;
        _countsByType[(int)type]++;
    }

    /// <summary>
    /// Unregisters a tracked instance and returns whether anything was removed.
    /// </summary>
    private bool UnregisterInstanceById(int instanceId)
    {
        if (!_trackedTypeByInstanceId.TryGetValue(instanceId, out AgentType type))
        {
            return false;
        }

        _trackedTypeByInstanceId.Remove(instanceId);
        _trackedTransformByInstanceId.Remove(instanceId);

        _currentCount = Mathf.Max(0, _currentCount - 1);

        int typeIndex = (int)type;
        if (typeIndex >= 0 && typeIndex < _countsByType.Length)
        {
            _countsByType[typeIndex] = Mathf.Max(0, _countsByType[typeIndex] - 1);
        }

        return true;
    }

    /// <summary>
    /// Chooses the type with the lowest tracked count and randomizes ties.
    /// </summary>
    private bool TryGetLeastRepresentedType(out AgentType chosenType)
    {
        chosenType = AgentType.Rock;

        bool hasConfiguredPrefab = false;
        int bestCount = int.MaxValue;

        AgentType[] candidates = new AgentType[3];
        int candidateCount = 0;

        for (int typeIndex = 0; typeIndex < _prefabsByType.Length; typeIndex++)
        {
            if (_prefabsByType[typeIndex] == null)
            {
                continue;
            }

            hasConfiguredPrefab = true;
            int trackedCount = _countsByType[typeIndex];

            if (trackedCount < bestCount)
            {
                bestCount = trackedCount;
                candidateCount = 0;
                candidates[candidateCount++] = (AgentType)typeIndex;
                continue;
            }

            if (trackedCount == bestCount)
            {
                candidates[candidateCount++] = (AgentType)typeIndex;
            }
        }

        if (!hasConfiguredPrefab)
        {
            return false;
        }

        chosenType = candidates[Random.Range(0, candidateCount)];
        return true;
    }

    /// <summary>
    /// Refreshes the prefab cache so runtime spawn logic always reflects the inspector.
    /// </summary>
    private bool RefreshPrefabCache()
    {
        _prefabsByType[(int)AgentType.Rock] = _rockPrefab;
        _prefabsByType[(int)AgentType.Paper] = _paperPrefab;
        _prefabsByType[(int)AgentType.Scissors] = _scissorsPrefab;

        return !(_rockPrefab == null && _paperPrefab == null && _scissorsPrefab == null);
    }

    private GameObject SpawnManagedInstance(AgentType type, Vector3 position)
    {
        if (_isShuttingDown || !RefreshPrefabCache())
        {
            return null;
        }

        GameObject prefab = _prefabsByType[(int)type];
        if (prefab == null)
        {
            return null;
        }

        GameObject spawnedInstance = Instantiate(prefab, position, Quaternion.identity);
        RegisterInstance(spawnedInstance, type);

        int instanceId = spawnedInstance.GetInstanceID();
        ManagedInstance managedInstance = spawnedInstance.GetComponent<ManagedInstance>();
        if (managedInstance == null)
        {
            managedInstance = spawnedInstance.AddComponent<ManagedInstance>();
        }

        managedInstance.Initialize(instanceId);
        return spawnedInstance;
    }

    // Spawn position helpers
    private bool TryFindSpawnPosition(Camera camera, float areaScale, float minDistance, out Vector3 spawnPosition)
    {
        float minDistanceSquared = minDistance * minDistance;

        for (int attempt = 0; attempt < _maxSpawnPositionTries; attempt++)
        {
            Vector3 candidatePosition = GetRandomPointInScaledView(camera, areaScale);
            if (IsFarEnoughFromAliveAgents(candidatePosition, minDistanceSquared))
            {
                spawnPosition = candidatePosition;
                return true;
            }
        }

        spawnPosition = default;
        return false;
    }

    private bool IsFarEnoughFromAliveAgents(Vector3 candidatePosition, float minDistanceSquared)
    {
        if (_trackedTransformByInstanceId.Count == 0)
        {
            return true;
        }

        foreach (KeyValuePair<int, Transform> pair in _trackedTransformByInstanceId)
        {
            Transform trackedTransform = pair.Value;
            if (trackedTransform == null)
            {
                continue;
            }

            Vector3 offset = trackedTransform.position - candidatePosition;
            if (offset.sqrMagnitude < minDistanceSquared)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a random world-space point inside the scaled camera viewport.
    /// </summary>
    private static Vector3 GetRandomPointInScaledView(Camera camera, float scale)
    {
        scale = Mathf.Clamp01(scale);

        float halfSpan = 0.5f * scale;
        float minViewportX = 0.5f - halfSpan;
        float maxViewportX = 0.5f + halfSpan;
        float minViewportY = 0.5f - halfSpan;
        float maxViewportY = 0.5f + halfSpan;

        float viewportX = Random.Range(minViewportX, maxViewportX);
        float viewportY = Random.Range(minViewportY, maxViewportY);

        float spawnPlaneZ = 0f;
        float cameraDistance = Mathf.Abs(spawnPlaneZ - camera.transform.position.z);

        Vector3 worldPosition = camera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, cameraDistance));
        worldPosition.z = spawnPlaneZ;
        return worldPosition;
    }
}
