using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class ElectricFence : BaseObstacle
    {
        [Header("Electric Fence Settings")]
        public float pulseInterval = 1f;
        public float pulseDuration = 0.5f;
        public bool alwaysActive = false;

        private bool isPulsing = false;
        private Coroutine pulseCoroutine;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void OnObstacleStart()
        {
            if (!alwaysActive)
            {
                StartPulseCycle();
            }
            else
            {
                isPulsing = true;
                UpdateVisuals();
            }
        }

        private void StartPulseCycle()
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseCycle());
        }

        private IEnumerator PulseCycle()
        {
            while (isActive)
            {
                isPulsing = true;
                UpdateVisuals();
                TriggerObstacle();

                yield return new WaitForSeconds(pulseDuration);

                isPulsing = false;
                UpdateVisuals();

                yield return new WaitForSeconds(pulseInterval - pulseDuration);
            }
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isPulsing ? Color.white : Color.gray;
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (isPulsing)
            {
                DealDamage(other.gameObject);
            }
        }
    }
}