using UnityEngine;

namespace Basic
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Calculates the Euclidean distance between two colors in RGB space.
        /// </summary>
        public static float DistanceTo(this Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>
        /// Calculates the squared distance between two colors in RGB space.
        /// Faster than DistanceTo since it avoids the square root operation.
        /// Use this when you only need to compare distances.
        /// </summary>
        public static float SqrDistanceTo(this Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }
}
