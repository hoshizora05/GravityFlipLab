using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class SlopePhysics : MonoBehaviour
    {
        [Header("Slope Settings")]
        public float slopeSpeedMultiplier = 1.2f;
        public float maxSlopeAngle = 45f;
        public bool affectGravity = true;
        public float gravityRedirection = 0.5f;

        private void OnCollisionStay2D(Collision2D collision)
        {
            Rigidbody2D rb = collision.rigidbody;
            if (rb == null) return;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                ApplySlopePhysics(rb, contact);
            }
        }

        private void ApplySlopePhysics(Rigidbody2D rb, ContactPoint2D contact)
        {
            Vector2 normal = contact.normal;
            float slopeAngle = Vector2.Angle(normal, Vector2.up);

            if (slopeAngle > maxSlopeAngle) return;

            // Calculate slope direction
            Vector2 slopeDirection = Vector2.Perpendicular(normal);
            if (slopeDirection.x < 0) slopeDirection = -slopeDirection;

            // Apply slope speed boost
            Vector2 velocity = rb.linearVelocity;
            float slopeInfluence = Mathf.Clamp01(slopeAngle / maxSlopeAngle);
            velocity.x *= 1f + (slopeSpeedMultiplier - 1f) * slopeInfluence;

            // Redirect gravity along slope
            if (affectGravity)
            {
                Vector2 gravityRedirect = Vector3.Project(Physics2D.gravity, slopeDirection);
                rb.AddForce(gravityRedirect * gravityRedirection, ForceMode2D.Force);
            }

            rb.linearVelocity = velocity;
        }
    }

}