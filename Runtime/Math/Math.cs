using UnityEngine;

namespace Basic
{
    public class Math
    {
        public static bool RayIntersectsPlane(
            Ray ray,
            Vector3 planePoint,
            Vector3 planeNormal,
            out float distance
        )
        {
            distance = 0f;

            float denominator = Vector3.Dot(planeNormal, ray.direction);

            if (Mathf.Abs(denominator) < 0.0001f)
            {
                return false;
            }

            Vector3 pointToOrigin = planePoint - ray.origin;
            distance = Vector3.Dot(pointToOrigin, planeNormal) / denominator;

            return distance >= 0f;
        }
    }
}
