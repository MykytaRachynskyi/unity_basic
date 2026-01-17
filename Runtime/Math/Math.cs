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
