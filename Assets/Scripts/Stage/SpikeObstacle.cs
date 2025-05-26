using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class SpikeObstacle : BaseObstacle
    {
        [Header("Spike Settings")]
        public bool pointsUp = true;
        public float spikeHeight = 1f;
        public bool retractable = false;
        public float retractDelay = 2f;

        private bool isExtended = true;
        private Coroutine retractCoroutine;

        protected override void OnObstacleStart()
        {
            if (retractable)
            {
                StartRetractCycle();
            }
        }

        private void StartRetractCycle()
        {
            if (retractCoroutine != null)
                StopCoroutine(retractCoroutine);
            retractCoroutine = StartCoroutine(RetractCycle());
        }

        private IEnumerator RetractCycle()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(retractDelay);
                ToggleSpikes();
                yield return new WaitForSeconds(retractDelay);
                ToggleSpikes();
            }
        }

        private void ToggleSpikes()
        {
            isExtended = !isExtended;
            // Animate spike extension/retraction
            StartCoroutine(AnimateSpikes());
        }

        private IEnumerator AnimateSpikes()
        {
            float startScale = transform.localScale.y;
            float targetScale = isExtended ? 1f : 0.1f;
            float duration = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                Vector3 scale = transform.localScale;
                scale.y = Mathf.Lerp(startScale, targetScale, t);
                transform.localScale = scale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Vector3 finalScale = transform.localScale;
            finalScale.y = targetScale;
            transform.localScale = finalScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isExtended)
            {
                DealDamage(other.gameObject);
            }
        }
    }
}