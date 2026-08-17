using System;

namespace Basic.ImGui.Layout
{
    public readonly struct ElementId : IEquatable<ElementId>
    {
        public readonly uint Id;
        public readonly uint Offset;
        public readonly uint BaseId;

        ElementId(uint id, uint offset, uint baseId)
        {
            Id = id;
            Offset = offset;
            BaseId = baseId;
        }

        public static ElementId From(ReadOnlySpan<char> label)
        {
            var id = ClayHash.HashString(label, 0);
            return new ElementId(id, 0, id);
        }

        public static ElementId From(string label) => From(label.AsSpan());

        public static ElementId Indexed(ReadOnlySpan<char> label, uint index)
        {
            var id = ClayHash.HashStringWithOffset(label, index, 0, out var baseId);
            return new ElementId(id, index, baseId);
        }

        public static ElementId Indexed(string label, uint index) => Indexed(label.AsSpan(), index);

        internal static ElementId FromParentSeed(ReadOnlySpan<char> label, uint parentSeed)
        {
            var id = ClayHash.HashString(label, parentSeed);
            return new ElementId(id, 0, id);
        }

        internal static ElementId FromParentSeed(string label, uint parentSeed) => FromParentSeed(label.AsSpan(), parentSeed);

        internal static ElementId FromResolved(uint id) => new ElementId(id, 0, id);

        public static ElementId Auto(uint hierarchyIndex) => new ElementId(hierarchyIndex + 1, 0, hierarchyIndex + 1);

        public bool Equals(ElementId other) => Id == other.Id && Offset == other.Offset && BaseId == other.BaseId;

        public override bool Equals(object obj) => obj is ElementId other && Equals(other);

        public override int GetHashCode() => (int)Id;

        public static bool operator ==(ElementId left, ElementId right) => left.Equals(right);

        public static bool operator !=(ElementId left, ElementId right) => !left.Equals(right);
    }
}
