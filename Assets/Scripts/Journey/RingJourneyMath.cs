using UnityEngine;

namespace Hiking.Journey
{
    public static class RingJourneyMath
    {
        public const float VisibleDegrees = 60;
        public const float HalfView = VisibleDegrees / 2;
        // Keep this angle unwrapped. 360 is the finish, not a second visit to station one.
        public static float ViewCenter(float progress, float firstBlockMidpoint = HalfView) => HalfView + Mathf.Max(0, progress - firstBlockMidpoint);

        /// <summary>
        /// 在XZ平面上的圆环点（Y=0），环平躺，逆时针方向
        /// </summary>
        public static Vector3 Point(float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;
            // 使用负角度实现逆时针：X = -Sin, Z = Cos
            return new Vector3(-Mathf.Sin(radians) * radius, 0, Mathf.Cos(radians) * radius);
        }

        public static float RelativeAngle(float angle, float viewCenter) => Mathf.DeltaAngle(viewCenter, angle);
        public static bool IsVisible(float angle, float viewCenter) => Mathf.Abs(RelativeAngle(angle, viewCenter)) <= HalfView + .001f;
    }
}
