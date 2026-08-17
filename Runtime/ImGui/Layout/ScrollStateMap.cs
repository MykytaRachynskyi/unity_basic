using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Basic.ImGui.Layout
{
    internal sealed class ScrollStateMap : IDisposable
    {
        NativeParallelHashMap<uint, ScrollState> _states;

        public ScrollStateMap(int capacity = 64)
        {
            _states = new NativeParallelHashMap<uint, ScrollState>(capacity, Allocator.Persistent);
        }

        public bool IsCreated => _states.IsCreated;

        public bool TryGetValue(uint elementId, out ScrollState state) => _states.TryGetValue(elementId, out state);

        public void Set(uint elementId, ScrollState state) => _states[elementId] = state;

        public NativeParallelHashMap<uint, ScrollState>.Enumerator GetEnumerator() => _states.GetEnumerator();

        public void Dispose()
        {
            if (_states.IsCreated)
            {
                _states.Dispose();
            }
        }
    }
}
