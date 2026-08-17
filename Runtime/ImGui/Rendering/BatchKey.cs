using System;
using UnityEngine;

namespace Basic.ImGui.Rendering
{
    public readonly struct BatchKey : IEquatable<BatchKey>
    {
        public readonly int MaterialId;
        public readonly int TextureId;
        public readonly Vector4 ClipRect;
        public readonly bool HasClip;

        public BatchKey(int materialId, int textureId, Vector4 clipRect, bool hasClip)
        {
            MaterialId = materialId;
            TextureId = textureId;
            ClipRect = clipRect;
            HasClip = hasClip;
        }

        public static BatchKey Solid(int materialId) => new BatchKey(materialId, 0, Vector4.zero, false);

        public bool Equals(BatchKey other) =>
            MaterialId == other.MaterialId
            && TextureId == other.TextureId
            && HasClip == other.HasClip
            && (!HasClip || ClipRect == other.ClipRect);

        public override bool Equals(object obj) => obj is BatchKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MaterialId;
                hash = (hash * 397) ^ TextureId;
                hash = (hash * 397) ^ HasClip.GetHashCode();
                if (HasClip)
                {
                    hash = (hash * 397) ^ ClipRect.GetHashCode();
                }

                return hash;
            }
        }
    }
}
