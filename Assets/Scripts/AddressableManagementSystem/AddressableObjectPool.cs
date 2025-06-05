using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Object pool implementation for Addressable prefabs.
    /// Provides efficient reuse of instantiated objects to reduce memory allocations and loading times.
    /// </summary>
    public class AddressableObjectPool : MonoBehaviour
    {
        #region Singleton

        private static AddressableObjectPool _instance;
        
        /// <summary>
        /// Singleton instance of the AddressableObjectPool
        /// </summary>
        public static AddressableObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AddressableObjectPool]");
                    _instance = go.AddComponent<AddressableObjectPool>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create root transform for inactive objects
            _poolRoot = new GameObject("PooledObjects").transform;
            _poolRoot.SetParent(transform);
        }

        #endregion

        #region Inspector Properties
        
        [Tooltip("Default capacity for newly created pools")]
        [SerializeField] private int _defaultInitialPoolSize = 5;
        
        [Tooltip("Maximum instances per prefab type")]
        [SerializeField] private int _defaultMaxPoolSize = 20;
        
        [Tooltip("Whether to expand pools automatically when needed")]
        [SerializeField] private bool _autoExpandPools = true;
        
        [Tooltip("Whether to collect unused objects periodically")]
        [SerializeField] private bool _enablePoolCleaning = true;
        
        [Tooltip("How often to check for and remove excess pooled objects (seconds)")]
        [SerializeField] private float _poolCleanInterval = 60f;
        
        [Tooltip("Prefabs to pre-warm on initialization")]
        [SerializeField] private List<PoolPreWarmData> _preWarmData = new List<PoolPreWarmData>();
        
        #endregion

        #region Fields

        // Root transform for inactive pooled objects
        private Transform _poolRoot;
        
        // Dictionary of object pools by prefab key
        private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        
        // Track whether we've initialized yet
        private bool _isInitialized = false;
        
        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the object pool system and pre-warms pools as configured.
        /// </summary>
        /// <returns>Task that completes when initialization is complete</returns>
        public async Task Initialize()
        {
            if (_isInitialized)
                return;
            
            Debug.Log($"[AddressableObjectPool] Initializing with {_preWarmData.Count} pre-warm entries");
            
            // Pre-warm specified pools
            foreach (var preWarmEntry in _preWarmData)
            {
                await PreWarmPool(preWarmEntry.PrefabKey, preWarmEntry.InitialSize, preWarmEntry.MaxSize);
            }
            
            // Start automatic pool cleaning if enabled
            if (_enablePoolCleaning)
            {
                StartCoroutine(CleanPoolsRoutine());
            }
            
            _isInitialized = true;
            Debug.Log("[AddressableObjectPool] Initialization complete");
        }
        
        /// <summary>
        /// Pre-warms a pool with the specified number of instances.
        /// </summary>
        /// <param name="prefabKey">Addressable key for the prefab</param>
        /// <param name="initialSize">Initial number of instances to create</param>
        /// <param name="maxSize">Maximum pool size</param>
        /// <returns>Task that completes when pre-warming is complete</returns>
        public async Task PreWarmPool(string prefabKey, int initialSize, int maxSize = -1)
        {
            if (initialSize <= 0)
                return;
            
            if (maxSize <= 0)
                maxSize = _defaultMaxPoolSize;
            
            Debug.Log($"[AddressableObjectPool] Pre-warming pool for '{prefabKey}' with {initialSize} instances (max: {maxSize})");
            
            // Check if pool already exists
            if (_pools.TryGetValue(prefabKey, out ObjectPool existingPool))
            {
                // If pool exists, expand it if needed
                int toCreate = initialSize - existingPool.CountAll;
                if (toCreate > 0)
                {
                    await existingPool.Expand(toCreate);
                }
                return;
            }
            
            // Load the prefab
            var resourceManager = AddressableResourceManager.Instance;
            var prefabHandle = resourceManager.LoadAssetAsync<GameObject>(prefabKey, AddressableResourceManager.LoadPriority.Low);
            await prefabHandle.Task;
            
            if (prefabHandle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableObjectPool] Failed to load prefab for pool: {prefabKey}");
                return;
            }
            
            // Create pool
            GameObject prefab = prefabHandle.Result;
            ObjectPool pool = new ObjectPool(prefabKey, prefab, _poolRoot, maxSize);
            
            // Add to pools dictionary
            _pools[prefabKey] = pool;
            
            // Create initial instances
            await pool.Expand(initialSize);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets an instance from the pool, creating a new one if necessary.
        /// </summary>
        /// <param name="prefabKey">Addressable key for the prefab</param>
        /// <param name="position">Position to place the instance</param>
        /// <param name="rotation">Rotation to apply to the instance</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>Task that returns the instantiated GameObject</returns>
        public async Task<GameObject> GetAsync(string prefabKey, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            // Ensure the pool system is initialized
            if (!_isInitialized)
            {
                await Initialize();
            }

            // Check if we already have a pool for this prefab
            if (!_pools.TryGetValue(prefabKey, out ObjectPool pool))
            {
                // Create a new pool if it doesn't exist
                pool = await CreatePoolAsync(prefabKey);
                
                if (pool == null)
                {
                    Debug.LogError($"[AddressableObjectPool] Failed to create pool for: {prefabKey}");
                    return null;
                }
            }
            
            // Get an object from the pool
            GameObject instance = pool.Get();
            
            if (instance == null && _autoExpandPools)
            {
                // Expand the pool if it's empty and auto-expand is enabled
                await pool.Expand(1);
                instance = pool.Get();
                
                if (instance == null)
                {
                    Debug.LogError($"[AddressableObjectPool] Failed to expand pool for: {prefabKey}");
                    return null;
                }
            }
            
            if (instance != null)
            {
                // Set position, rotation, and parent
                if (parent != null)
                {
                    instance.transform.SetParent(parent, false);
                }
                else
                {
                    instance.transform.SetParent(null);
                }
                
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                
                // Activate the GameObject
                instance.SetActive(true);
                
                // Initialize IPoolable objects
                IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
                foreach (var poolable in poolables)
                {
                    poolable.OnGetFromPool();
                }
            }
            
            return instance;
        }
        
        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="prefabKey">Addressable key for the prefab</param>
        /// <param name="instance">Instance to return to the pool</param>
        public void Release(string prefabKey, GameObject instance)
        {
            if (instance == null)
                return;
            
            if (!_pools.TryGetValue(prefabKey, out ObjectPool pool))
            {
                Debug.LogWarning($"[AddressableObjectPool] No pool found for {prefabKey}. Destroying instance instead.");
                Destroy(instance);
                return;
            }
            
            // Notify IPoolable components
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            foreach (var poolable in poolables)
            {
                poolable.OnReturnToPool();
            }
            
            // Deactivate and return to pool
            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);
            
            pool.Release(instance);
        }
        
        /// <summary>
        /// Clears a specific pool, destroying all instances.
        /// </summary>
        /// <param name="prefabKey">Addressable key for the pool to clear</param>
        public void ClearPool(string prefabKey)
        {
            if (_pools.TryGetValue(prefabKey, out ObjectPool pool))
            {
                pool.Clear();
                _pools.Remove(prefabKey);
                
                // Release the prefab asset
                AddressableResourceManager.Instance.ReleaseAsset(prefabKey);
                
                Debug.Log($"[AddressableObjectPool] Cleared pool: {prefabKey}");
            }
        }
        
        /// <summary>
        /// Clears all pools, destroying all instances.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
                
                // Release the prefab asset
                AddressableResourceManager.Instance.ReleaseAsset(pool.PrefabKey);
            }
            
            _pools.Clear();
            Debug.Log("[AddressableObjectPool] Cleared all pools");
        }
        
        /// <summary>
        /// Gets information about a specific pool.
        /// </summary>
        /// <param name="prefabKey">Addressable key for the pool</param>
        /// <returns>PoolInfo containing statistics about the pool</returns>
        public PoolInfo GetPoolInfo(string prefabKey)
        {
            if (!_pools.TryGetValue(prefabKey, out ObjectPool pool))
            {
                return new PoolInfo
                {
                    PrefabKey = prefabKey,
                    TotalCount = 0,
                    ActiveCount = 0,
                    InactiveCount = 0,
                    MaxSize = _defaultMaxPoolSize
                };
            }
            
            return new PoolInfo
            {
                PrefabKey = prefabKey,
                TotalCount = pool.CountAll,
                ActiveCount = pool.CountActive,
                InactiveCount = pool.CountInactive,
                MaxSize = pool.MaxSize
            };
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates and registers a new pool for the given prefab key.
        /// </summary>
        private async Task<ObjectPool> CreatePoolAsync(string prefabKey)
        {
            // Load the prefab
            var resourceManager = AddressableResourceManager.Instance;
            var prefabHandle = resourceManager.LoadAssetAsync<GameObject>(prefabKey, AddressableResourceManager.LoadPriority.Normal);
            await prefabHandle.Task;

            if (prefabHandle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableObjectPool] Failed to load prefab for new pool: {prefabKey}");
                return null;
            }

            GameObject prefab = prefabHandle.Result;
            ObjectPool newPool = new ObjectPool(prefabKey, prefab, _poolRoot, _defaultMaxPoolSize);
            _pools[prefabKey] = newPool;

            // (Optionally) expand the pool a little bit up front
            await newPool.Expand(_defaultInitialPoolSize);

            return newPool;
        }

        /// <summary>
        /// Periodically cleans pools by removing excess inactive objects above each pool's max size.
        /// </summary>
        private IEnumerator CleanPoolsRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_poolCleanInterval);

                foreach (var kvp in _pools)
                {
                    var pool = kvp.Value;
                    pool.CleanExcess();
                }
            }
        }

        #endregion
    }

    #region Helper Classes / Structures

    /// <summary>
    /// Simple interface for objects that receive pool callbacks.
    /// </summary>
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// Data used to pre-warm a specific pool.
    /// </summary>
    [Serializable]
    public struct PoolPreWarmData
    {
        public string PrefabKey;
        public int InitialSize;
        public int MaxSize;
    }

    /// <summary>
    /// Information about a specific pool.
    /// </summary>
    public struct PoolInfo
    {
        public string PrefabKey;
        public int TotalCount;
        public int ActiveCount;
        public int InactiveCount;
        public int MaxSize;
    }

    /// <summary>
    /// Internal class representing a single prefab pool.
    /// </summary>
    internal class ObjectPool
    {
        public string PrefabKey { get; private set; }
        public GameObject Prefab { get; private set; }
        public int MaxSize { get; private set; }
        
        public int CountAll => _countAll;
        public int CountActive => _activeObjects.Count;
        public int CountInactive => _inactiveObjects.Count;
        
        private Transform _parent;
        private int _countAll;
        
        // You could use either Stack or Queue for the inactive list
        private readonly Stack<GameObject> _inactiveObjects = new Stack<GameObject>();
        private readonly HashSet<GameObject> _activeObjects = new HashSet<GameObject>();

        public ObjectPool(string prefabKey, GameObject prefab, Transform parent, int maxSize)
        {
            PrefabKey = prefabKey;
            Prefab = prefab;
            _parent = parent;
            MaxSize = maxSize;
            _countAll = 0;
        }

        /// <summary>
        /// Dynamically adds more instances to the pool.
        /// </summary>
        public async Task Expand(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(Prefab, _parent);
                instance.SetActive(false);

                _inactiveObjects.Push(instance);
                _countAll++;

                // Minimal yield to avoid blocking for large expansions
                await Task.Yield();
            }
        }

        /// <summary>
        /// Retrieves an instance from this pool if available.
        /// </summary>
        public GameObject Get()
        {
            if (_inactiveObjects.Count > 0)
            {
                var instance = _inactiveObjects.Pop();
                _activeObjects.Add(instance);
                return instance;
            }
            return null;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null)
                return;
            
            if (_activeObjects.Contains(instance))
            {
                _activeObjects.Remove(instance);
                _inactiveObjects.Push(instance);
            }
            else
            {
                // If it's an unknown object, just destroy or ignore.
                UnityEngine.Object.Destroy(instance);
            }
        }

        /// <summary>
        /// Destroys all pooled instances (active and inactive).
        /// </summary>
        public void Clear()
        {
            // Destroy active
            foreach (var instance in _activeObjects)
            {
                UnityEngine.Object.Destroy(instance);
            }
            _activeObjects.Clear();

            // Destroy inactive
            while (_inactiveObjects.Count > 0)
            {
                var instance = _inactiveObjects.Pop();
                UnityEngine.Object.Destroy(instance);
            }

            _countAll = 0;
        }

        /// <summary>
        /// Removes extra inactive objects if we're over the max size.
        /// </summary>
        public void CleanExcess()
        {
            while (_inactiveObjects.Count > 0 && _countAll > MaxSize)
            {
                var instance = _inactiveObjects.Pop();
                UnityEngine.Object.Destroy(instance);
                _countAll--;
            }
        }
    }

    #endregion
}
