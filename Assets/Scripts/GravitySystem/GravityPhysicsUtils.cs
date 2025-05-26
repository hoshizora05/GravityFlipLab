using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public static class GravityPhysicsUtils
    {
        public static Vector2 CalculateTrajectory(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, float time)
        {
            // s = ut + (1/2)at²
            return startPos + startVelocity * time + 0.5f * gravity * time * time;
        }

        public static Vector2 CalculateVelocityAtTime(Vector2 startVelocity, Vector2 gravity, float time)
        {
            // v = u + at
            return startVelocity + gravity * time;
        }

        public static float CalculateTimeToReachHeight(Vector2 startVelocity, Vector2 gravity, float targetHeight)
        {
            // Using quadratic formula for s = ut + (1/2)at²
            float a = 0.5f * gravity.y;
            float b = startVelocity.y;
            float c = -targetHeight;

            float discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return -1f; // No real solution

            float time1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float time2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

            // Return the positive time
            return Mathf.Max(time1, time2);
        }

        public static Vector2[] CalculateTrajectoryPoints(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, int pointCount, float timeStep)
        {
            Vector2[] points = new Vector2[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float time = i * timeStep;
                points[i] = CalculateTrajectory(startPos, startVelocity, gravity, time);
            }

            return points;
        }

        public static bool WillCollideWithGround(Vector2 startPos, Vector2 startVelocity, Vector2 gravity, float groundY, out float timeToCollision)
        {
            timeToCollision = CalculateTimeToReachHeight(startVelocity, gravity, groundY - startPos.y);
            return timeToCollision > 0f;
        }
    }
}