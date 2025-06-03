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

        [Header("Respawn Integration")]
        public bool useEnhancedRespawn = true;
        public bool logCollisionEvents = false;

        private PlayerController playerController;
        private float lastCollisionTime = -1f;
        private float lastCheckpointTime = -1f;

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

            if (logCollisionEvents)
                Debug.Log($"Player collision with obstacle: {obstacle.name}");

            // Apply invincibility frames for head/foot collisions
            bool isHeadFeetCollision = IsHeadOrFeetCollision(obstacle);

            if (isHeadFeetCollision)
            {
                lastCollisionTime = Time.time;
                StartCoroutine(DelayedDamage());
            }
            else
            {
                // Direct damage for side collisions
                if (useEnhancedRespawn)
                {
                    HandleEnhancedPlayerDamage();
                }
                else
                {
                    playerController.TakeDamage();
                }
            }
        }

        private IEnumerator DelayedDamage()
        {
            yield return new WaitForSeconds(0.1f); // 0.1 second invincibility
            if (!playerController.stats.isInvincible)
            {
                if (useEnhancedRespawn)
                {
                    HandleEnhancedPlayerDamage();
                }
                else
                {
                    playerController.TakeDamage();
                }
            }
        }

        private void HandleEnhancedPlayerDamage()
        {
            // Enhanced damage handling with better respawn integration
            if (playerController != null)
            {
                // Check if player has lives remaining
                if (playerController.stats.livesRemaining > 1)
                {
                    // Take damage normally
                    playerController.TakeDamage();

                    if (logCollisionEvents)
                        Debug.Log($"Player took damage. Lives remaining: {playerController.stats.livesRemaining}");
                }
                else
                {
                    // This will be the final life - prepare for death and respawn
                    if (logCollisionEvents)
                        Debug.Log("Player taking final damage - will trigger respawn sequence");

                    playerController.TakeDamage();

                    // The CheckpointManager will handle the respawn sequence automatically
                    // through the PlayerController.OnPlayerDeath event
                }
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
            if (logCollisionEvents)
                Debug.Log($"Player collected: {collectible.name}");

            // Handle energy chip collection
            if (collectible.CompareTag("EnergyChip"))
            {
                CollectEnergyChip(collectible);
            }
            // Handle other collectibles
            else if (collectible.CompareTag("PowerUp"))
            {
                HandlePowerUpCollection(collectible);
            }
            else if (collectible.CompareTag("ExtraLife"))
            {
                HandleExtraLifeCollection(collectible);
            }
        }

        private void CollectEnergyChip(Collider2D energyChip)
        {
            // Update game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.sessionEnergyChips++;
            }

            // Notify stage manager
            var collectible = energyChip.GetComponent<Stage.Collectible>();
            if (collectible != null && Stage.StageManager.Instance != null)
            {
                Stage.StageManager.Instance.CollectibleCollected(collectible);
            }

            // Play collection effect
            PlayCollectionEffect(energyChip.transform.position, "EnergyChip");

            // Deactivate collectible
            energyChip.gameObject.SetActive(false);

            if (logCollisionEvents)
                Debug.Log($"Energy chip collected. Total: {GameManager.Instance.sessionEnergyChips}");
        }

        private void HandlePowerUpCollection(Collider2D powerUp)
        {
            // Handle power-up specific logic
            PlayCollectionEffect(powerUp.transform.position, "PowerUp");
            powerUp.gameObject.SetActive(false);

            if (logCollisionEvents)
                Debug.Log("Power-up collected");
        }

        private void HandleExtraLifeCollection(Collider2D extraLife)
        {
            // Give player an extra life
            if (playerController != null)
            {
                playerController.stats.livesRemaining++;
            }

            PlayCollectionEffect(extraLife.transform.position, "ExtraLife");
            extraLife.gameObject.SetActive(false);

            if (logCollisionEvents)
                Debug.Log($"Extra life collected. Lives: {playerController.stats.livesRemaining}");
        }

        private void HandleCheckpointCollision(Collider2D checkpoint)
        {
            // Prevent checkpoint spam
            if (Time.time - lastCheckpointTime < 1f) return;
            lastCheckpointTime = Time.time;

            if (logCollisionEvents)
                Debug.Log($"Player reached checkpoint: {checkpoint.name}");

            // Enhanced checkpoint handling
            var checkpointTrigger = checkpoint.GetComponent<Stage.CheckpointTrigger>();
            if (checkpointTrigger != null)
            {
                // The CheckpointTrigger will handle the checkpoint activation
                // No need to manually set checkpoint here
                if (logCollisionEvents)
                    Debug.Log("Checkpoint trigger will handle activation");
                return;
            }

            // Fallback for basic checkpoint objects
            if (CheckpointManager.Instance != null)
            {
                // Get current gravity state for enhanced checkpoint
                GravityDirection currentGravity = GravityDirection.Down;
                if (playerController != null)
                {
                    currentGravity = playerController.gravityDirection;
                }

                CheckpointManager.Instance.SetCheckpoint(checkpoint.transform.position, currentGravity);

                if (logCollisionEvents)
                    Debug.Log($"Checkpoint set at: {checkpoint.transform.position}");
            }
        }

        private void PlayCollectionEffect(Vector3 position, string effectType)
        {
            // Play audio effect based on type
            // AudioManager.Instance.PlaySE($"Collect_{effectType}");

            // Create visual effect
            CreateCollectionEffect(position, effectType);
        }

        private void CreateCollectionEffect(Vector3 position, string effectType)
        {
            // Create particle effect or animation
            // This could be expanded to use different effects for different collectible types

            // For now, just log the effect
            if (logCollisionEvents)
                Debug.Log($"Collection effect played at {position} for {effectType}");
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        // Public methods for external access
        public void SetLogCollisionEvents(bool enable)
        {
            logCollisionEvents = enable;
        }

        public void SetUseEnhancedRespawn(bool enable)
        {
            useEnhancedRespawn = enable;
        }

        public float GetLastCollisionTime()
        {
            return lastCollisionTime;
        }

        public float GetLastCheckpointTime()
        {
            return lastCheckpointTime;
        }

        // Integration methods for external systems
        public void HandleIntegratedCollision(Collider2D other)
        {
            // This method can be called by other systems like PlayerStageAdapter
            HandleCollision(other);
        }

        public void HandleObstacleCollisionExternal(Collider2D obstacle)
        {
            // External method for obstacle collision handling
            HandleObstacleCollision(obstacle);
        }

        public void HandleCollectibleCollectionExternal(Collider2D collectible)
        {
            // External method for collectible collection
            HandleCollectibleCollision(collectible);
        }

        public void HandleCheckpointActivationExternal(Collider2D checkpoint)
        {
            // External method for checkpoint activation
            HandleCheckpointCollision(checkpoint);
        }

        // Debug information
        private void OnDrawGizmosSelected()
        {
            if (!logCollisionEvents) return;

            // Draw collision detection areas
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw last collision position if available
            if (lastCollisionTime > 0 && Time.time - lastCollisionTime < 2f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.7f);
            }

            // Draw checkpoint detection
            if (lastCheckpointTime > 0 && Time.time - lastCheckpointTime < 2f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.9f);
            }
        }

        // Collision statistics for debugging
        [System.Serializable]
        public class CollisionStatistics
        {
            public int obstacleCollisions;
            public int collectibleCollections;
            public int checkpointActivations;
            public float lastCollisionTime;
            public float lastCheckpointTime;

            public override string ToString()
            {
                return $"Obstacles: {obstacleCollisions}, Collectibles: {collectibleCollections}, " +
                       $"Checkpoints: {checkpointActivations}";
            }
        }

        private CollisionStatistics stats = new CollisionStatistics();

        public CollisionStatistics GetCollisionStatistics()
        {
            stats.lastCollisionTime = lastCollisionTime;
            stats.lastCheckpointTime = lastCheckpointTime;
            return stats;
        }

        // Update statistics when collisions occur
        private void UpdateCollisionStatistics(string collisionType)
        {
            switch (collisionType)
            {
                case "obstacle":
                    stats.obstacleCollisions++;
                    break;
                case "collectible":
                    stats.collectibleCollections++;
                    break;
                case "checkpoint":
                    stats.checkpointActivations++;
                    break;
            }
        }

        // Reset statistics
        public void ResetCollisionStatistics()
        {
            stats = new CollisionStatistics();
        }
    }
}