using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Basic.ImGui.Layout
{
    public sealed class LayoutArena : IDisposable
    {
        const int DefaultCapacityBytes = 64 * 1024;
        const int Alignment = 8;

        NativeArray<byte> _buffer;
        int _offset;

        public bool IsCreated => _buffer.IsCreated;

        public LayoutArena(int capacityBytes = DefaultCapacityBytes)
        {
            _buffer = new NativeArray<byte>(capacityBytes, Allocator.Persistent);
            _offset = 0;
        }

        public void Reset() => _offset = 0;

        public unsafe ref T AllocateRef<T>() where T : unmanaged
        {
            return ref UnsafeUtility.AsRef<T>(Allocate<T>(1));
        }

        public unsafe T* Allocate<T>(int count = 1) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>() * count;
            var alignedOffset = Align(_offset);
            var newOffset = alignedOffset + size;

            if (newOffset > _buffer.Length)
            {
                throw new InvalidOperationException($"Layout arena overflow: requested {newOffset} bytes, capacity {_buffer.Length}.");
            }

            _offset = newOffset;
            return (T*)((byte*)_buffer.GetUnsafePtr() + alignedOffset);
        }

        public NativeArray<T> AllocateNativeArray<T>(int length, Allocator allocator) where T : unmanaged
        {
            var array = new NativeArray<T>(length, allocator);
            return array;
        }

        public void Dispose()
        {
            if (_buffer.IsCreated)
            {
                _buffer.Dispose();
            }

            _offset = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Align(int offset)
        {
            var remainder = offset % Alignment;
            return remainder == 0 ? offset : offset + (Alignment - remainder);
        }
    }
}
