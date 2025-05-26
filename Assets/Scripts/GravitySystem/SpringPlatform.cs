using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class SpringPlatform : MonoBehaviour
    {
        [Header("Spring Settings")]
        public float springForce = 15f;
        public float springDamping = 0.8f;
        public float compressionDistance = 0.5f;
        public AnimationCurve springCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual")]
        public Transform springVisual;
        public float animationDuration = 0.3f;

        private Vector3 originalPosition;
        private bool isCompressed = false;
        private Coroutine springAnimation;

        private void Start()
        {
            if (springVisual == null)
                springVisual = transform;

            originalPosition = springVisual.localPosition;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Rigidbody2D rb = collision.rigidbody;
            if (rb != null)
            {
                ApplySpringForce(rb, collision);
            }
        }

        private void ApplySpringForce(Rigidbody2D rb, Collision2D collision)
        {
            // Calculate spring direction based on collision normal
            Vector2 springDirection = collision.contacts[0].normal;

            // Apply spring force
            Vector2 force = -springDirection * springForce;
            rb.AddForce(force, ForceMode2D.Impulse);

            // Apply damping to prevent infinite bouncing
            rb.linearVelocity *= springDamping;

            // Play spring animation
            PlaySpringAnimation();
        }

        private void PlaySpringAnimation()
        {
            if (springAnimation != null)
                StopCoroutine(springAnimation);

            springAnimation = StartCoroutine(SpringAnimationCoroutine());
        }

        private IEnumerator SpringAnimationCoroutine()
        {
            isCompressed = true;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                float t = elapsedTime / animationDuration;
                float curveValue = springCurve.Evaluate(t);

                Vector3 offset = Vector3.down * compressionDistance * (1f - curveValue);
                springVisual.localPosition = originalPosition + offset;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            springVisual.localPosition = originalPosition;
            isCompressed = false;
        }
    }
}