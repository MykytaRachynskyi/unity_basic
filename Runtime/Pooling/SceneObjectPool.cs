using System.Collections.Generic;
using UnityEngine;

namespace Basic.Pooling
{
    /// <summary>
    /// Generic object pool for MonoBehaviour components.
    /// </summary>
    public class SceneObjectPool<T>
        where T : Component
    {
        private readonly Queue<T> _available = new();
        private readonly HashSet<T> _inUse = new();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly bool _assumeCapacity;

        /// <summary>
        /// Initializes a new object pool by instantiating items from a prefab.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate items from.</param>
        /// <param name="initialCapacity">Number of items to pre-instantiate.</param>
        /// <param name="parent">Parent transform for pooled objects. If null, creates a new empty GameObject.</param>
        /// <param name="assumeCapacity">If true, logs an error when pool needs to expand beyond initial capacity.</param>
        public static SceneObjectPool<T> Init(
            T prefab,
            int initialCapacity,
            Transform parent = null,
            bool assumeCapacity = false
        )
        {
            if (prefab == null)
            {
                Debug.LogError("ObjectPool.Init: Prefab cannot be null.");
                return null;
            }

            if (parent == null)
            {
                GameObject parentObj = new($"Parent - {prefab.name}Pool");
                parent = parentObj.transform;
            }

            var pool = new SceneObjectPool<T>(prefab, parent, assumeCapacity);

            for (int i = 0; i < initialCapacity; i++)
            {
                T item = Object.Instantiate(prefab, parent);
                item.gameObject.SetActive(false);
                pool._available.Enqueue(item);
            }

            return pool;
        }

        /// <summary>
        /// Initializes a new object pool with existing items.
        /// </summary>
        /// <param name="prefab">The prefab reference (required for potential expansion).</param>
        /// <param name="existingItems">Collection of existing items to add to the pool.</param>
        /// <param name="parent">Parent transform for pooled objects. If null, creates a new empty GameObject.</param>
        /// <param name="assumeCapacity">If true, logs an error when pool needs to expand beyond provided items.</param>
        public static SceneObjectPool<T> Init(
            T prefab,
            IEnumerable<T> existingItems,
            Transform parent = null,
            bool assumeCapacity = false
        )
        {
            if (prefab == null)
            {
                Debug.LogError("ObjectPool.Init: Prefab cannot be null.");
                return null;
            }

            if (existingItems == null)
            {
                Debug.LogError("ObjectPool.Init: Existing items collection cannot be null.");
                return null;
            }

            if (parent == null)
            {
                GameObject parentObj = new($"Parent - {prefab.name}Pool");
                parent = parentObj.transform;
            }

            var pool = new SceneObjectPool<T>(prefab, parent, assumeCapacity);

            foreach (T item in existingItems)
            {
                if (item != null)
                {
                    item.transform.SetParent(parent);
                    item.gameObject.SetActive(false);
                    pool._available.Enqueue(item);
                }
            }

            return pool;
        }

        private SceneObjectPool(T prefab, Transform parent, bool assumeCapacity)
        {
            _prefab = prefab;
            _parent = parent;
            _assumeCapacity = assumeCapacity;
        }

        /// <summary>
        /// Gets an item from the pool. If pool is empty, instantiates a new item.
        /// </summary>
        public T GetItem()
        {
            T item;

            if (_available.Count > 0)
            {
                item = _available.Dequeue();
            }
            else
            {
                if (_assumeCapacity)
                {
                    Debug.LogError(
                        $"ObjectPool<{typeof(T).Name}>: Pool exhausted, instantiating new item. Consider increasing initial capacity."
                    );
                }

                item = Object.Instantiate(_prefab, _parent);
            }

            item.gameObject.SetActive(true);
            _inUse.Add(item);
            return item;
        }

        /// <summary>
        /// Returns an item to the pool.
        /// </summary>
        public void ReturnItem(T item)
        {
            if (item == null)
            {
                Debug.LogWarning("ObjectPool.ReturnItem: Cannot return null item.");
                return;
            }

            if (!_inUse.Remove(item))
            {
                Debug.LogWarning(
                    $"ObjectPool.ReturnItem: Item {item.name} was not obtained from this pool."
                );
                return;
            }

            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent);
            _available.Enqueue(item);
        }

        /// <summary>
        /// Gets the total count of all items in the pool (available + in use).
        /// </summary>
        public int GetCount()
        {
            return _available.Count + _inUse.Count;
        }

        /// <summary>
        /// Gets the count of available items in the pool.
        /// </summary>
        public int GetAvailableCount()
        {
            return _available.Count;
        }

        /// <summary>
        /// Gets the count of items currently in use.
        /// </summary>
        public int GetInUseCount()
        {
            return _inUse.Count;
        }

        /// <summary>
        /// Clears and destroys all pooled objects.
        /// </summary>
        public void Deinit()
        {
            foreach (T item in _available)
            {
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            foreach (T item in _inUse)
            {
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            _available.Clear();
            _inUse.Clear();

            if (_parent != null)
            {
                Object.Destroy(_parent.gameObject);
            }
        }
    }
}
