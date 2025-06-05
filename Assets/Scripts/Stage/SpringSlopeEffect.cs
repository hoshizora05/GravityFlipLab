using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// スプリング傾斜エフェクト - 加速・跳躍効果
    /// </summary>
    public class SpringSlopeEffect : SpecialSlopeEffect
    {
        [Header("Spring Settings")]
        public float bounceForce = 15f;
        public float accelerationMultiplier = 1.5f;
        public bool enableBounce = true;
        public bool enableAcceleration = true;

        [Header("Visual Effects")]
        public ParticleSystem bounceEffect;
        public AudioClip bounceSound;

        private Dictionary<Rigidbody2D, bool> hasBouncedThisContact = new Dictionary<Rigidbody2D, bool>();

        protected override void OnEffectStart(Rigidbody2D rb)
        {
            hasBouncedThisContact[rb] = false;

            if (enableAcceleration)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.x *= accelerationMultiplier;
                rb.linearVelocity = velocity;
            }

            Debug.Log($"SpringSlope: Object {rb.name} entered spring slope");
        }

        protected override void OnEffectEnd(Rigidbody2D rb)
        {
            hasBouncedThisContact.Remove(rb);
            Debug.Log($"SpringSlope: Object {rb.name} exited spring slope");
        }

        protected override void ApplyEffect(Rigidbody2D rb)
        {
            if (!enableBounce || hasBouncedThisContact.GetValueOrDefault(rb, false)) return;

            // Check if object is moving downward (landing)
            if (rb.linearVelocity.y < -1f)
            {
                // Apply bounce
                Vector2 velocity = rb.linearVelocity;
                velocity.y = bounceForce;
                rb.linearVelocity = velocity;

                hasBouncedThisContact[rb] = true;

                // Play effects
                PlayBounceEffects(rb.transform.position);

                Debug.Log($"SpringSlope: Applied bounce force to {rb.name}");
            }
        }

        private void PlayBounceEffects(Vector3 position)
        {
            if (bounceEffect != null)
            {
                bounceEffect.transform.position = position;
                bounceEffect.Play();
            }

            if (bounceSound != null)
            {
                AudioSource.PlayClipAtPoint(bounceSound, position);
            }
        }
    }
}