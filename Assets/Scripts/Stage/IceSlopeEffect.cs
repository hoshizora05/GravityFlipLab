using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 氷傾斜エフェクト - 滑りやすい表面
    /// </summary>
    public class IceSlopeEffect : SpecialSlopeEffect
    {
        [Header("Ice Settings")]
        public float friction = 0.1f;
        public float slideAcceleration = 1.5f;
        public float maxSlideSpeed = 20f;

        [Header("Visual Effects")]
        public ParticleSystem slideEffect;
        public Material iceMaterial;

        private Dictionary<Rigidbody2D, float> originalDrag = new Dictionary<Rigidbody2D, float>();

        protected override void OnEffectStart(Rigidbody2D rb)
        {
            // Store original drag and apply ice physics
            originalDrag[rb] = rb.linearDamping;
            rb.linearDamping = friction;

            Debug.Log($"IceSlope: Object {rb.name} entered ice slope");
        }

        protected override void OnEffectEnd(Rigidbody2D rb)
        {
            // Restore original drag
            if (originalDrag.ContainsKey(rb))
            {
                rb.linearDamping = originalDrag[rb];
                originalDrag.Remove(rb);
            }

            if (slideEffect != null && slideEffect.isPlaying)
            {
                slideEffect.Stop();
            }

            Debug.Log($"IceSlope: Object {rb.name} exited ice slope");
        }

        protected override void ApplyEffect(Rigidbody2D rb)
        {
            // Apply slide acceleration along slope
            if (slopeObject != null)
            {
                Vector2 slopeDirection = slopeObject.GetSlopeDirection();
                Vector2 velocity = rb.linearVelocity;

                // Add sliding force along slope
                Vector2 slideForce = slopeDirection * slideAcceleration;
                rb.AddForce(slideForce, ForceMode2D.Force);

                // Limit maximum slide speed
                if (velocity.magnitude > maxSlideSpeed)
                {
                    velocity = velocity.normalized * maxSlideSpeed;
                    rb.linearVelocity = velocity;
                }

                // Play slide effects
                if (slideEffect != null && !slideEffect.isPlaying && velocity.magnitude > 2f)
                {
                    slideEffect.transform.position = rb.transform.position;
                    slideEffect.Play();
                }
            }
        }
    }
}