using UnityEngine;

namespace Basic
{
    public static class Math
    {
#pragma warning disable IDE1006 // Naming Styles
        public static Vector2 xz(this Vector3 vec) => new(vec.x, vec.z);

        public static Vector3 x0z(this Vector3 vec) => new(vec.x, 0f, vec.z);

        public static Vector4 xyzw(this Vector3 vec) => new(vec.x, vec.y, vec.z, 1f);
#pragma warning restore IDE1006 // Naming Styles

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

        public static Quaternion SmoothDampQuaternion(
            Quaternion current,
            Quaternion target,
            ref Vector3 currentVelocity,
            float smoothTime
        )
        {
            Vector3 c = current.eulerAngles;
            Vector3 t = target.eulerAngles;
            return Quaternion.Euler(
                Mathf.SmoothDampAngle(c.x, t.x, ref currentVelocity.x, smoothTime),
                Mathf.SmoothDampAngle(c.y, t.y, ref currentVelocity.y, smoothTime),
                Mathf.SmoothDampAngle(c.z, t.z, ref currentVelocity.z, smoothTime)
            );
        }
    }
}
