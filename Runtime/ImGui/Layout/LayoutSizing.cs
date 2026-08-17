using System;

namespace Basic.ImGui.Layout
{
    public enum SizingType : byte
    {
        Fit = 0,
        Grow = 1,
        Fixed = 2,
        Percent = 3
    }

    public struct LayoutSizing : IEquatable<LayoutSizing>
    {
        public SizingType Type;
        public float Min;
        public float Max;
        public float Value;

        public static LayoutSizing Fit(float min = 0f, float max = float.MaxValue) => new LayoutSizing
        {
            Type = SizingType.Fit,
            Min = min,
            Max = max
        };

        public static LayoutSizing Grow(float min = 0f, float max = float.MaxValue) => new LayoutSizing
        {
            Type = SizingType.Grow,
            Min = min,
            Max = max
        };

        public static LayoutSizing Fixed(float size) => new LayoutSizing
        {
            Type = SizingType.Fixed,
            Min = size,
            Max = size,
            Value = size
        };

        public static LayoutSizing Percent(float fraction) => new LayoutSizing
        {
            Type = SizingType.Percent,
            Value = fraction,
            Min = 0f,
            Max = float.MaxValue
        };

        public bool Equals(LayoutSizing other) =>
            Type == other.Type &&
            Min.Equals(other.Min) &&
            Max.Equals(other.Max) &&
            Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is LayoutSizing other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, Min, Max, Value);
    }
}
