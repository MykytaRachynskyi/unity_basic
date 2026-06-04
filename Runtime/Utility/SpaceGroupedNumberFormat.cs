using System.Globalization;

namespace Basic
{
    /// <summary>
    /// Number formatting with a space as the thousands separator and '.' as the decimal separator.
    /// Use standard/custom .NET format strings (e.g. <c>#,##0</c>, <c>#,##0.##</c>);
    /// commas in the pattern are grouping placeholders and render as spaces.
    /// </summary>
    public static class SpaceGroupedNumberFormat
    {
        private const string IntegerFormat = "#,##0";
        private const string FloatingFormat = "#,##0.##";

        private static NumberFormatInfo _invariant;

        /// <summary>
        /// Invariant <see cref="NumberFormatInfo"/> with <see cref="NumberFormatInfo.NumberGroupSeparator"/> set to a space.
        /// Pass to <see cref="IFormattable.ToString(string, IFormatProvider)"/> or <see cref="string.Format(IFormatProvider, string, object[])"/>.
        /// </summary>
        public static NumberFormatInfo Invariant => _invariant ??= CreateInvariant();

        public static string Format(int value, string format = IntegerFormat) =>
            value.ToString(format, Invariant);

        public static string Format(long value, string format = IntegerFormat) =>
            value.ToString(format, Invariant);

        public static string Format(float value, string format = FloatingFormat) =>
            value.ToString(format, Invariant);

        public static string Format(double value, string format = FloatingFormat) =>
            value.ToString(format, Invariant);

        public static string Format(decimal value, string format = FloatingFormat) =>
            value.ToString(format, Invariant);

        private static NumberFormatInfo CreateInvariant()
        {
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = " ";
            nfi.NumberGroupSizes = new[] { 3 };
            return nfi;
        }
    }
}
