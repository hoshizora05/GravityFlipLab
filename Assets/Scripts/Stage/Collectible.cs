using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class Collectible : MonoBehaviour
    {
        [Header("Collectible Settings")]
        public CollectibleType collectibleType = CollectibleType.EnergyChip;
        public int value = 1;
        public float rotationSpeed = 90f;
        public float bobHeight = 0.5f;
        public float bobSpeed = 2f;

        [Header("Effects")]
        public ParticleSystem collectEffect;
        public AudioClip collectSound;

        private CollectibleData collectibleData;
        private Vector3 startPosition;
        private bool isCollected = false;

        public void Initialize(CollectibleData data)
        {
            collectibleData = data;
            collectibleType = data.type;
            value = data.value;
            startPosition = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

            // Rotation animation
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

            // Bobbing animation
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 newPosition = startPosition;
            newPosition.y += bobOffset;
            transform.position = newPosition;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                Collect();
            }
        }

        private void Collect()
        {
            isCollected = true;

            // Play effects
            if (collectEffect != null)
                collectEffect.Play();

            if (collectSound != null)
            {
                // AudioManager.Instance.PlaySE(collectSound);
            }

            // Update game state
            StageManager.Instance.CollectibleCollected(this);

            // Hide collectible
            gameObject.SetActive(false);

            // Destroy after effect finishes
            Destroy(gameObject, 2f);
        }
    }
}