using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class GravityWell : LocalGravityZone
    {
        [Header("Gravity Well Settings")]
        public float wellStrength = 15f;
        public float maxRadius = 10f;
        public bool repulsive = false;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private void Start()
        {
            // Configure as radial gravity
            gravityData.gravityType = repulsive ? LocalGravityType.Radial : LocalGravityType.Radial;
            gravityData.strength = wellStrength;
            radius = maxRadius;
        }

        public override Vector2 GetGravityAtPosition(Vector3 position)
        {
            Vector2 directionToCenter = (transform.position - position);
            float distance = directionToCenter.magnitude;

            if (distance > maxRadius) return Vector2.zero;

            // Apply falloff curve
            float normalizedDistance = distance / maxRadius;
            float strength = wellStrength * falloffCurve.Evaluate(normalizedDistance);

            // Reverse direction if repulsive
            Vector2 direction = repulsive ? -directionToCenter.normalized : directionToCenter.normalized;

            return direction * strength;
        }
    }
}