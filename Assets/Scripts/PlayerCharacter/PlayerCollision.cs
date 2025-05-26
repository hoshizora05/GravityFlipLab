using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    public class PlayerCollision : MonoBehaviour
    {
        [Header("Collision Settings")]
        public LayerMask obstacleLayerMask = 1;
        public LayerMask collectibleLayerMask = 1;
        public LayerMask checkpointLayerMask = 1;

        private PlayerController playerController;
        private float lastCollisionTime = -1f;

        public void Initialize(PlayerController controller)
        {
            playerController = controller;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.collider);
        }

        private void HandleCollision(Collider2D other)
        {
            // Prevent multiple collisions in quick succession
            if (Time.time - lastCollisionTime < 0.1f) return;

            // Check layer and handle accordingly
            int layer = other.gameObject.layer;

            if (IsInLayerMask(layer, obstacleLayerMask))
            {
                HandleObstacleCollision(other);
            }
            else if (IsInLayerMask(layer, collectibleLayerMask))
            {
                HandleCollectibleCollision(other);
            }
            else if (IsInLayerMask(layer, checkpointLayerMask))
            {
                HandleCheckpointCollision(other);
            }
        }

        private void HandleObstacleCollision(Collider2D obstacle)
        {
            if (playerController.stats.isInvincible) return;

            // Apply invincibility frames for head/foot collisions
            bool isHeadFeetCollision = IsHeadOrFeetCollision(obstacle);

            if (isHeadFeetCollision)
            {
                lastCollisionTime = Time.time;
                StartCoroutine(DelayedDamage());
            }
            else
            {
                playerController.TakeDamage();
            }
        }

        private IEnumerator DelayedDamage()
        {
            yield return new WaitForSeconds(0.1f); // 0.1 second invincibility
            if (!playerController.stats.isInvincible)
            {
                playerController.TakeDamage();
            }
        }

        private bool IsHeadOrFeetCollision(Collider2D obstacle)
        {
            Vector2 playerCenter = transform.position;
            Vector2 obstacleCenter = obstacle.bounds.center;

            float verticalDistance = Mathf.Abs(playerCenter.y - obstacleCenter.y);
            float horizontalDistance = Mathf.Abs(playerCenter.x - obstacleCenter.x);

            // Consider it head/feet collision if vertical distance is greater
            return verticalDistance > horizontalDistance;
        }

        private void HandleCollectibleCollision(Collider2D collectible)
        {
            // Handle energy chip collection
            if (collectible.CompareTag("EnergyChip"))
            {
                GameManager.Instance.sessionEnergyChips++;
                collectible.gameObject.SetActive(false);

                // Play collection effect
                // AudioManager.Instance.PlaySE("Collect");
            }
        }

        private void HandleCheckpointCollision(Collider2D checkpoint)
        {
            CheckpointManager.Instance.SetCheckpoint(checkpoint.transform.position);
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
    }

}