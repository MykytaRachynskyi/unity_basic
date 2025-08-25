using System.Collections.Generic;

namespace Basic.Pooling
{
    public interface IPoolElement
    {
        void Reset();
    }

    public class ObjectPool<T>
        where T : IPoolElement, new()
    {
        public bool AssumeCapacity { get; set; }

        private readonly Queue<T> _queue;

        public ObjectPool(int initialCapacity = 8, bool assumeCapacity = false)
        {
            AssumeCapacity = assumeCapacity;

            _queue = new Queue<T>(initialCapacity);
            for (int i = 0; i < initialCapacity; ++i)
            {
                _queue.Enqueue(new T());
            }
        }

        public T Get()
        {
            if (_queue.TryDequeue(out var element))
            {
                return element;
            }

            if (!AssumeCapacity)
            {
                return new T();
            }

            return default;
        }

        public void Return(T item)
        {
            item.Reset();
            _queue.Enqueue(item);
        }
    }
}
