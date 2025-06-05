using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 荒い傾斜エフェクト - 摩擦による減速
    /// </summary>
    public class RoughSlopeEffect : SpecialSlopeEffect
    {
        [Header("Rough Surface Settings")]
        public float friction = 2.0f;
        public float decelerationFactor = 0.8f;
        public float roughnessIntensity = 1.5f;

        [Header("Visual Effects")]
        public ParticleSystem dustEffect;
        public AudioClip scrapeSound;

        private Dictionary<Rigidbody2D, float> originalDrag = new Dictionary<Rigidbody2D, float>();

        protected override void OnEffectStart(Rigidbody2D rb)
        {
            originalDrag[rb] = rb.linearDamping;
            rb.linearDamping = friction;

            Debug.Log($"RoughSlope: Object {rb.name} entered rough slope");
        }

        protected override void OnEffectEnd(Rigidbody2D rb)
        {
            if (originalDrag.ContainsKey(rb))
            {
                rb.linearDamping = originalDrag[rb];
                originalDrag.Remove(rb);
            }

            if (dustEffect != null && dustEffect.isPlaying)
            {
                dustEffect.Stop();
            }

            Debug.Log($"RoughSlope: Object {rb.name} exited rough slope");
        }

        protected override void ApplyEffect(Rigidbody2D rb)
        {
            // Apply deceleration
            Vector2 velocity = rb.linearVelocity;
            velocity.x *= decelerationFactor;
            rb.linearVelocity = velocity;

            // Add roughness vibration
            Vector2 roughness = new Vector2(
                Random.Range(-roughnessIntensity, roughnessIntensity),
                Random.Range(-roughnessIntensity * 0.5f, roughnessIntensity * 0.5f)
            );
            rb.AddForce(roughness, ForceMode2D.Impulse);

            // Play dust effects
            if (dustEffect != null && !dustEffect.isPlaying && velocity.magnitude > 1f)
            {
                dustEffect.transform.position = rb.transform.position;
                dustEffect.Play();
            }
        }
    }
}