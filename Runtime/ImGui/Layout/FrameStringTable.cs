using System;
using Unity.Collections;

namespace Basic.ImGui.Layout
{
    internal sealed class FrameStringTable : IDisposable
    {
        NativeList<char> _chars;
        NativeList<int> _starts;
        NativeList<int> _lengths;

        public FrameStringTable()
        {
            _chars = new NativeList<char>(Allocator.Persistent);
            _starts = new NativeList<int>(Allocator.Persistent);
            _lengths = new NativeList<int>(Allocator.Persistent);
        }

        public bool IsCreated => _chars.IsCreated;

        public int Count => _starts.Length;

        public NativeArray<char> Chars => _chars.AsArray();

        public void Reset()
        {
            _chars.Clear();
            _starts.Clear();
            _lengths.Clear();
        }

        public int Add(ReadOnlySpan<char> text)
        {
            var start = _chars.Length;
            for (var i = 0; i < text.Length; i++)
            {
                _chars.Add(text[i]);
            }

            _starts.Add(start);
            _lengths.Add(text.Length);
            return _starts.Length - 1;
        }

        public TextSlice GetSlice(int stringIndex)
        {
            return new TextSlice(_starts[stringIndex], _lengths[stringIndex]);
        }

        public ReadOnlySpan<char> GetSpan(TextSlice slice)
        {
            var array = _chars.AsArray();
            return array.AsReadOnlySpan().Slice(slice.StartIndex, slice.Length);
        }

        public void Dispose()
        {
            if (_chars.IsCreated)
            {
                _chars.Dispose();
            }

            if (_starts.IsCreated)
            {
                _starts.Dispose();
            }

            if (_lengths.IsCreated)
            {
                _lengths.Dispose();
            }
        }
    }
}
