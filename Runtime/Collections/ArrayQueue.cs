using System;
using System.Collections;
using System.Collections.Generic;

namespace Basic
{
    public class ArrayQueue<T> : IEnumerable<T>
    {
        private const int DEFAULT_INITIAL_CAPACITY = 8;
        private const float GROWTH_FACTOR = 1.5f;

        private T[] _items;

        public ArrayQueue()
        {
            Initialize(DEFAULT_INITIAL_CAPACITY);
        }

        public ArrayQueue(int initialCapacity)
        {
            var cap = initialCapacity > 0 ? initialCapacity : DEFAULT_INITIAL_CAPACITY;
            Initialize(cap);
        }

        public int Count { get; private set; }

        public int Capacity { get; private set; }

        public int Head { get; private set; }

        public int Tail { get; private set; }

        public T this[int index]
        {
            get
            {
                ValidateRelativeIndex(index);
                return GetAtRelativeIndex(index);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return GetAtRelativeIndex(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        public struct Enumerator
        {
            private readonly ArrayQueue<T> _queue;
            private int _index;

            internal Enumerator(ArrayQueue<T> queue)
            {
                _queue = queue;
                _index = -1;
            }

            public bool MoveNext()
            {
                _index++;
                return _queue != null && _index < _queue.Count;
            }

            public T Current
            {
                get
                {
                    if (_queue == null || _index < 0 || _index >= _queue.Count)
                    {
                        throw new InvalidOperationException();
                    }

                    return _queue.GetAtRelativeIndex(_index);
                }
            }
        }

        public bool TryEnqueue(T item)
        {
            if (Count == _items.Length)
            {
                Grow();
            }

            _items[Tail] = item;
            Tail = (Tail + 1) % _items.Length;
            Count++;
            return true;
        }

        public bool TryDequeue(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = _items[Head];
            Head = (Head + 1) % _items.Length;
            Count--;
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < Count; i++)
            {
                _items[(Head + i) % _items.Length] = default;
            }

            Count = 0;
            Head = 0;
            Tail = 0;
        }

        private void Grow()
        {
            var newCapacity = (int)(Capacity * GROWTH_FACTOR);
            if (newCapacity <= Capacity)
            {
                newCapacity = Capacity + 1;
            }

            var newArray = new T[newCapacity];
            for (var i = 0; i < Count; i++)
            {
                newArray[i] = _items[(Head + i) % _items.Length];
            }

            Head = 0;
            Tail = Count;
            _items = newArray;
            Capacity = newCapacity;
        }

        private void Initialize(int capacity)
        {
            _items = new T[capacity];
            Head = 0;
            Tail = 0;
            Capacity = capacity;
            Count = 0;
        }

        private void ValidateRelativeIndex(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private T GetAtRelativeIndex(int relativeIndex) =>
            _items[(Head + relativeIndex) % _items.Length];
    }
}
