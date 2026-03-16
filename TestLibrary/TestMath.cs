using System;
using System.Runtime.CompilerServices;
using TestLibrary.Types;

namespace TestLibrary
{
    public static class TestMath
    {
        public const float PI = (float)System.Math.PI;
        public const float RADIANS_TO_DEGREES = 180f / PI;
        public const float DEGREES_TO_RADIANS = PI / 180f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleDeg(this Vector2 vector) => (float)Math.Atan2(vector.Y, vector.X) * TestMath.RADIANS_TO_DEGREES;

        public static float RotateTowardsDegrees(
            float currentDegrees,
            float targetDegrees,
            float maxTurnSpeedDegreesPerSecond,
            float deltaTimeSeconds)
        {
            float delta = DeltaAngleDegrees(currentDegrees, targetDegrees);
            float maxStep = maxTurnSpeedDegreesPerSecond * deltaTimeSeconds;
            if (delta > maxStep) delta = maxStep;
            if (delta < -maxStep) delta = -maxStep;
            return currentDegrees + delta;
        }

        public static float DeltaAngleDegrees(float currentDegrees, float targetDegrees)
        {
            float current = NormalizeAngleDegrees(currentDegrees);
            float target = NormalizeAngleDegrees(targetDegrees);
            float delta = target - current;
            if (delta > 180f) delta -= 360f;
            if (delta <= -180f) delta += 360f;
            return delta;
        }

        public static float NormalizeAngleDegrees(float degrees)
        {
            float r = degrees % 360f;
            return r < 0 ? r + 360f : r;
        }

        public static Vector2 FromAngleDegrees(float degrees)
        {
            float radians = degrees * DEGREES_TO_RADIANS;
            return new Vector2((float)Math.Cos(radians), (float)Math.Sin(radians));
        }

    }
}
