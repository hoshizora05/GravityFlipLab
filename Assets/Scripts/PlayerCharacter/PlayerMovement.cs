using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    public partial class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float autoRunSpeed = 5.0f;
        public float gravityForce = 9.81f;
        public float maxFallSpeed = 20.0f;

        [Header("Ground Detection")]
        public LayerMask groundLayerMask = 1;
        public float groundCheckDistance = 0.6f;
        public Transform groundCheckPoint;

        [Header("Physics")]
        public float springForce = 5.0f;
        public float slopeSpeedMultiplier = 1.2f;

        private PlayerController playerController;
        private Rigidbody2D rb2d;
        private bool isGrounded = false;
        private RaycastHit2D groundHit;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;
            rb2d = GetComponent<Rigidbody2D>();

            if (groundCheckPoint == null)
            {
                GameObject checkPoint = new GameObject("GroundCheckPoint");
                checkPoint.transform.SetParent(transform);
                checkPoint.transform.localPosition = new Vector3(0, -0.5f, 0);
                groundCheckPoint = checkPoint.transform;
            }
        }

        private void FixedUpdate()
        {
            if (playerController == null) return;

            if (!playerController.isAlive) return;

            // Ground detection
            CheckGrounded();

            // Auto-run movement
            ApplyAutoRun();

            // Apply gravity
            ApplyGravity();

            // Limit fall speed
            LimitFallSpeed();
        }

        private void CheckGrounded()
        {
            Vector2 rayDirection = (playerController.gravityDirection == GravityDirection.Down) ?
                Vector2.down : Vector2.up;

            groundHit = Physics2D.Raycast(groundCheckPoint.position, rayDirection,
                groundCheckDistance, groundLayerMask);

            isGrounded = groundHit.collider != null;
        }

        private void ApplyAutoRun()
        {
            Vector2 velocity = rb2d.linearVelocity;
            velocity.x = autoRunSpeed;

            // Apply slope speed modification
            if (isGrounded && groundHit.normal != Vector2.up)
            {
                float slopeAngle = Vector2.Angle(groundHit.normal, Vector2.up);
                if (slopeAngle > 5f) // Only on noticeable slopes
                {
                    velocity.x *= slopeSpeedMultiplier;
                }
            }

            rb2d.linearVelocity = velocity;
        }

        private void ApplyGravity()
        {
            if (isGrounded && Mathf.Abs(rb2d.linearVelocity.y) < 0.1f) return;

            float gravityDirection = (float)playerController.gravityDirection;
            Vector2 gravityForceVector = new Vector2(0, gravityForce * gravityDirection);

            rb2d.AddForce(gravityForceVector, ForceMode2D.Force);
        }

        public void ApplyGravityFlip(GravityDirection newDirection)
        {
            // Instantly reverse Y velocity for immediate gravity flip feeling
            Vector2 velocity = rb2d.linearVelocity;
            velocity.y = -velocity.y * 0.5f; // Reduce speed slightly for control
            rb2d.linearVelocity = velocity;
        }

        private void LimitFallSpeed()
        {
            Vector2 velocity = rb2d.linearVelocity;
            velocity.y = Mathf.Clamp(velocity.y, -maxFallSpeed, maxFallSpeed);
            rb2d.linearVelocity = velocity;
        }

        public bool IsGrounded()
        {
            return isGrounded;
        }

        public void ApplySpringForce(Vector2 direction)
        {
            rb2d.AddForce(direction * springForce, ForceMode2D.Impulse);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector3 rayDirection = (playerController?.gravityDirection == GravityDirection.Down) ?
                    Vector3.down : Vector3.up;
                Gizmos.DrawLine(groundCheckPoint.position,
                    groundCheckPoint.position + rayDirection * groundCheckDistance);
            }
        }
    }
}