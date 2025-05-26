using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class MomentumController : MonoBehaviour
    {
        [Header("Momentum Settings")]
        public float momentumDecay = 0.95f;
        public float maxMomentum = 15f;
        public bool preserveMomentumOnGravityFlip = true;

        [Header("Inertia")]
        public float inertiaStrength = 1f;
        public float velocitySmoothing = 0.1f;

        private Rigidbody2D rb2d;
        private Vector2 previousVelocity;
        private Vector2 momentum;

        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            // Subscribe to gravity flip events
            if (GetComponent<GravityFlipLab.Player.PlayerController>() != null)
            {
                GravityFlipLab.Player.PlayerController.OnGravityFlip += OnGravityFlip;
            }
        }

        private void OnDestroy()
        {
            if (GetComponent<GravityFlipLab.Player.PlayerController>() != null)
            {
                GravityFlipLab.Player.PlayerController.OnGravityFlip -= OnGravityFlip;
            }
        }

        private void FixedUpdate()
        {
            UpdateMomentum();
            ApplyInertia();
            previousVelocity = rb2d.linearVelocity;
        }

        private void UpdateMomentum()
        {
            // Calculate momentum based on velocity change
            Vector2 velocityChange = rb2d.linearVelocity - previousVelocity;
            momentum += velocityChange * inertiaStrength;

            // Apply decay
            momentum *= momentumDecay;

            // Limit momentum
            if (momentum.magnitude > maxMomentum)
            {
                momentum = momentum.normalized * maxMomentum;
            }
        }

        private void ApplyInertia()
        {
            if (momentum.magnitude > 0.1f)
            {
                Vector2 targetVelocity = Vector2.Lerp(rb2d.linearVelocity, rb2d.linearVelocity + momentum, velocitySmoothing);
                rb2d.linearVelocity = targetVelocity;
            }
        }

        private void OnGravityFlip(GravityFlipLab.Player.GravityDirection direction)
        {
            if (preserveMomentumOnGravityFlip)
            {
                // Preserve horizontal momentum, modify vertical momentum
                Vector2 velocity = rb2d.linearVelocity;
                momentum.x = velocity.x * 0.8f; // Preserve some horizontal momentum
                momentum.y = -velocity.y * 0.5f; // Reverse and reduce vertical momentum
            }
        }

        public void AddMomentum(Vector2 additionalMomentum)
        {
            momentum += additionalMomentum;
        }

        public void ResetMomentum()
        {
            momentum = Vector2.zero;
        }

        public Vector2 GetCurrentMomentum()
        {
            return momentum;
        }
    }
}