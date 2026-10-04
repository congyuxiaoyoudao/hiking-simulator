using UnityEngine;
namespace Hiking.Journey
{
    // PCG coordinates: distance along X, height Y, lateral offset Z.
    public static class RouteGeometry
    {
        public static Vector3 Point(Vector3 route, float length, float bend)
        {
            float s = route.x - length * .5f;
            float k = Mathf.Clamp01(bend) * 2 * Mathf.PI / length;
            if (k < .00001f) return new Vector3(s, route.y, route.z);
            float a = k * s;
            return new Vector3(Mathf.Sin(a) * (1 / k + route.z), route.y,
                (Mathf.Cos(a) - 1) / k + Mathf.Cos(a) * route.z);
        }
        public static Quaternion Rotation(float distance, float length, float bend) =>
            Quaternion.Euler(0, (distance / length - .5f) * 360 * bend, 0);
    }
}

