using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    /// <summary>
    /// チェックポイント詳細データ構造
    /// </summary>
    [System.Serializable]
    public class CheckpointData
    {
        public Vector3 position;
        public GravityDirection gravityDirection;
        public float timestamp;
        public int stageProgressIndex;
        public Dictionary<string, object> additionalData;

        public CheckpointData(Vector3 pos, GravityDirection gravity = GravityDirection.Down)
        {
            position = pos;
            gravityDirection = gravity;
            timestamp = Time.time;
            stageProgressIndex = 0;
            additionalData = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// チェックポイント統計情報
    /// </summary>
    [System.Serializable]
    public class CheckpointStatistics
    {
        public int totalCheckpoints;
        public int activatedCheckpoints;
        public int checkpointHistory;
        public float timeSinceLastCheckpoint;
        public float completionPercentage;

        public override string ToString()
        {
            return $"Checkpoints: {activatedCheckpoints}/{totalCheckpoints} ({completionPercentage:F1}%), " +
                   $"History: {checkpointHistory}, Time: {timeSinceLastCheckpoint:F1}s";
        }
    }

    public class CheckpointManager : MonoBehaviour
    {
        private static CheckpointManager _instance;
        public static CheckpointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CheckpointManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CheckpointManager");
                        _instance = go.AddComponent<CheckpointManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Checkpoint Settings")]
        public Vector3 defaultCheckpointPosition = Vector3.zero;
        public float respawnDelay = 1.0f;
        public bool preserveVelocityOnRespawn = false;
        public bool resetGravityOnRespawn = true;

        [Header("Visual Effects")]
        public GameObject checkpointEffect;
        public GameObject respawnEffect;
        public AudioClip checkpointSound;
        public AudioClip respawnSound;

        [Header("Safety Features")]
        public bool enableSafetyRespawn = true;
        public float safetyCheckDistance = 50f;
        public int maxRespawnAttempts = 3;

        [Header("Debug")]
        public bool debugMode = false;
        public bool showCheckpointGizmos = true;
        public bool logRespawnEvents = true;

        // Enhanced checkpoint data
        private CheckpointData currentCheckpoint;
        private List<CheckpointData> checkpointHistory = new List<CheckpointData>();
        private List<Stage.CheckpointTrigger> registeredCheckpoints = new List<Stage.CheckpointTrigger>();

        // Legacy support
        private Vector3 currentCheckpointPosition;

        // Events
        public static event System.Action<CheckpointData> OnCheckpointSet;
        public static event System.Action<CheckpointData> OnCheckpointActivated;
        public static event System.Action<Vector3> OnRespawnStarted;
        public static event System.Action<Vector3> OnRespawnCompleted;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCheckpointSystem();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupInitialCheckpoint();
            DiscoverAndRegisterCheckpoints();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            PlayerController.OnPlayerDeath -= HandlePlayerDeath;
            PlayerController.OnGravityFlip -= HandleGravityFlip;
        }

        private void InitializeCheckpointSystem()
        {
            checkpointHistory = new List<CheckpointData>();

            // Subscribe to player events
            PlayerController.OnPlayerDeath += HandlePlayerDeath;
            PlayerController.OnGravityFlip += HandleGravityFlip;

            if (debugMode)
                Debug.Log("Enhanced Checkpoint Manager initialized");
        }

        private void SetupInitialCheckpoint()
        {
            // Set initial checkpoint based on player start position or default
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 startPosition = defaultCheckpointPosition;

            if (player != null)
            {
                startPosition = player.transform.position;
            }
            else if (Stage.StageManager.Instance != null &&
                     Stage.StageManager.Instance.currentStageData != null)
            {
                startPosition = Stage.StageManager.Instance.currentStageData.stageInfo.playerStartPosition;
            }

            if (startPosition == Vector3.zero)
            {
                startPosition = defaultCheckpointPosition;
            }

            SetCheckpoint(startPosition, GravityDirection.Down, true);
        }

        private void DiscoverAndRegisterCheckpoints()
        {
            // Find all checkpoint triggers in the scene and register them
            Stage.CheckpointTrigger[] triggers = FindObjectsByType<Stage.CheckpointTrigger>(FindObjectsSortMode.None);

            foreach (var trigger in triggers)
            {
                RegisterCheckpoint(trigger);
            }

            if (debugMode)
                Debug.Log($"Discovered and registered {triggers.Length} checkpoints");
        }

        public void RegisterCheckpoint(Stage.CheckpointTrigger checkpoint)
        {
            if (!registeredCheckpoints.Contains(checkpoint))
            {
                registeredCheckpoints.Add(checkpoint);

                if (debugMode)
                    Debug.Log($"Registered checkpoint at {checkpoint.transform.position}");
            }
        }

        public void UnregisterCheckpoint(Stage.CheckpointTrigger checkpoint)
        {
            if (registeredCheckpoints.Contains(checkpoint))
            {
                registeredCheckpoints.Remove(checkpoint);

                if (debugMode)
                    Debug.Log($"Unregistered checkpoint at {checkpoint.transform.position}");
            }
        }

        public void SetCheckpoint(Vector3 position, GravityDirection gravity = GravityDirection.Down, bool isDefault = false)
        {
            // Save previous checkpoint to history
            if (currentCheckpoint != null && !isDefault)
            {
                checkpointHistory.Add(currentCheckpoint);
            }

            // Create new checkpoint data
            currentCheckpoint = new CheckpointData(position, gravity);

            // Update legacy position for backward compatibility
            currentCheckpointPosition = position;

            // Add stage-specific data
            if (Stage.StageManager.Instance != null)
            {
                currentCheckpoint.additionalData["stageTime"] = Time.time - Stage.StageManager.Instance.stageStartTime;
                currentCheckpoint.additionalData["energyChipsCollected"] = GameManager.Instance.sessionEnergyChips;
            }

            OnCheckpointSet?.Invoke(currentCheckpoint);

            // Play effects
            PlayCheckpointEffect(position);

            if (debugMode || logRespawnEvents)
                Debug.Log($"Checkpoint set at: {position} with gravity: {gravity}");
        }

        // Legacy method for backward compatibility
        public void SetCheckpoint(Vector3 position)
        {
            SetCheckpoint(position, GravityDirection.Down, false);
        }

        public Vector3 GetCurrentCheckpointPosition()
        {
            return currentCheckpoint?.position ?? defaultCheckpointPosition;
        }

        public CheckpointData GetCurrentCheckpointData()
        {
            return currentCheckpoint;
        }

        public void ResetToDefaultCheckpoint()
        {
            checkpointHistory.Clear();
            SetCheckpoint(defaultCheckpointPosition, GravityDirection.Down, true);

            if (debugMode)
                Debug.Log("Reset to default checkpoint");
        }

        public void RevertToPreviousCheckpoint()
        {
            if (checkpointHistory.Count > 0)
            {
                CheckpointData previousCheckpoint = checkpointHistory[checkpointHistory.Count - 1];
                checkpointHistory.RemoveAt(checkpointHistory.Count - 1);

                currentCheckpoint = previousCheckpoint;
                currentCheckpointPosition = previousCheckpoint.position;

                if (debugMode)
                    Debug.Log($"Reverted to previous checkpoint: {currentCheckpoint.position}");
            }
        }

        private void HandlePlayerDeath()
        {
            if (debugMode || logRespawnEvents)
                Debug.Log("Player death detected, initiating respawn sequence");

            StartCoroutine(RespawnSequence());
        }

        private void HandleGravityFlip(GravityDirection newDirection)
        {
            // Update current checkpoint's gravity direction if player flips gravity
            if (currentCheckpoint != null)
            {
                currentCheckpoint.gravityDirection = newDirection;
            }
        }

        private IEnumerator RespawnSequence()
        {
            Vector3 respawnPosition = GetCurrentCheckpointPosition();
            OnRespawnStarted?.Invoke(respawnPosition);

            // Wait for respawn delay
            yield return new WaitForSeconds(respawnDelay);

            // Execute respawn
            RespawnPlayer();

            OnRespawnCompleted?.Invoke(respawnPosition);
        }

        private void RespawnPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player not found for respawn!");
                return;
            }

            // Get player components
            var playerController = player.GetComponent<PlayerController>();
            var rigidbody = player.GetComponent<Rigidbody2D>();
            var gravityAffected = player.GetComponent<GravityAffectedObject>();

            if (currentCheckpoint == null)
            {
                Debug.LogError("No checkpoint data available for respawn!");
                return;
            }

            Vector3 respawnPosition = currentCheckpoint.position;

            // Validate respawn position safety
            if (enableSafetyRespawn)
            {
                respawnPosition = ValidateAndCorrectRespawnPosition(respawnPosition);
            }

            // Move player to checkpoint position
            player.transform.position = respawnPosition;

            // Reset physics
            if (rigidbody != null)
            {
                if (preserveVelocityOnRespawn)
                {
                    // Preserve some velocity for smoother respawn
                    Vector2 velocity = rigidbody.linearVelocity;
                    velocity.y = 0f; // Reset vertical velocity to prevent fall damage
                    rigidbody.linearVelocity = velocity;
                }
                else
                {
                    rigidbody.linearVelocity = Vector2.zero;
                    rigidbody.angularVelocity = 0f;
                }
            }

            //// Reset gravity if needed
            //if (resetGravityOnRespawn && GravitySystem.Instance != null)
            //{
            //    if (currentCheckpoint.gravityDirection == GravityDirection.Down)
            //    {
            //        GravitySystem.Instance.ResetToOriginalGravity();
            //    }
            //    else
            //    {
            //        GravitySystem.Instance.SetGlobalGravityDirection(Vector2.up);
            //    }
            //}

            // Reset player controller state
            if (playerController != null)
            {
                playerController.Respawn();
            }

            // Reset gravity affected object
            if (gravityAffected != null)
            {
                gravityAffected.ResetToOriginalGravity();
            }

            // Reset advanced ground detector
            var groundDetector = player.GetComponent<AdvancedGroundDetector>();
            if (groundDetector != null)
            {
                groundDetector.ForceDetection();
            }

            // Reset player movement
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ValidatePhysicsState();
            }

            // Play respawn effects
            PlayRespawnEffect(respawnPosition);

            // Restore additional state if needed
            RestoreAdditionalState();

            if (debugMode || logRespawnEvents)
                Debug.Log($"Player respawned at: {respawnPosition}");
        }

        private Vector3 ValidateAndCorrectRespawnPosition(Vector3 originalPosition)
        {
            // Check if respawn position is safe
            if (IsPositionSafe(originalPosition))
            {
                return originalPosition;
            }

            if (debugMode)
                Debug.LogWarning($"Unsafe respawn position detected: {originalPosition}. Attempting correction.");

            // Try to find a safe position nearby
            Vector3 safePosition = FindSafeRespawnPosition(originalPosition);

            if (safePosition != Vector3.zero)
            {
                if (debugMode)
                    Debug.Log($"Safe respawn position found: {safePosition}");
                return safePosition;
            }

            // Emergency fallback to default checkpoint
            Debug.LogWarning($"No safe position found, using default checkpoint: {defaultCheckpointPosition}");
            return defaultCheckpointPosition;
        }

        private bool IsPositionSafe(Vector3 position)
        {
            // Check for obstacles
            if (Physics2D.OverlapPoint(position, LayerMask.GetMask("Obstacles", "Hazards")))
            {
                return false;
            }

            // Check for ground below
            RaycastHit2D groundCheck = Physics2D.Raycast(position, Vector2.down, safetyCheckDistance, LayerMask.GetMask("Ground"));
            return groundCheck.collider != null;
        }

        private Vector3 FindSafeRespawnPosition(Vector3 center)
        {
            float searchRadius = 5f;
            int searchSteps = 8;

            for (int i = 0; i < searchSteps; i++)
            {
                float angle = (360f / searchSteps) * i;
                Vector3 searchPos = center + Quaternion.Euler(0, 0, angle) * Vector3.right * searchRadius;

                if (IsPositionSafe(searchPos))
                {
                    return searchPos;
                }
            }

            return Vector3.zero; // No safe position found
        }

        private void RestoreAdditionalState()
        {
            if (currentCheckpoint?.additionalData == null) return;

            // Restore any additional state stored in checkpoint data
            // This could include power-ups, special abilities, etc.

            // Example: Restore stage-specific state
            if (currentCheckpoint.additionalData.ContainsKey("specialState"))
            {
                // Restore special state
            }
        }

        private void PlayCheckpointEffect(Vector3 position)
        {
            if (checkpointEffect != null)
            {
                GameObject effect = Instantiate(checkpointEffect, position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (checkpointSound != null)
            {
                AudioSource.PlayClipAtPoint(checkpointSound, position);
            }
        }

        private void PlayRespawnEffect(Vector3 position)
        {
            if (respawnEffect != null)
            {
                GameObject effect = Instantiate(respawnEffect, position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (respawnSound != null)
            {
                AudioSource.PlayClipAtPoint(respawnSound, position);
            }
        }

        // Public API methods
        public int GetCheckpointCount()
        {
            return registeredCheckpoints.Count;
        }

        public int GetCheckpointHistoryCount()
        {
            return checkpointHistory.Count;
        }

        public List<Vector3> GetAllCheckpointPositions()
        {
            List<Vector3> positions = new List<Vector3>();

            foreach (var checkpoint in registeredCheckpoints)
            {
                if (checkpoint != null)
                {
                    positions.Add(checkpoint.checkpointPosition);
                }
            }

            return positions;
        }

        public float GetTimeSinceLastCheckpoint()
        {
            return currentCheckpoint != null ? Time.time - currentCheckpoint.timestamp : 0f;
        }

        public bool IsCheckpointNear(Vector3 position, float threshold = 2f)
        {
            foreach (var checkpoint in registeredCheckpoints)
            {
                if (checkpoint != null)
                {
                    float distance = Vector3.Distance(position, checkpoint.checkpointPosition);
                    if (distance <= threshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Configuration methods
        public void SetRespawnDelay(float delay)
        {
            respawnDelay = Mathf.Max(0f, delay);
        }

        public void SetPreserveVelocityOnRespawn(bool preserve)
        {
            preserveVelocityOnRespawn = preserve;
        }

        public void SetResetGravityOnRespawn(bool reset)
        {
            resetGravityOnRespawn = reset;
        }

        // Immediate respawn methods
        public void ForceRespawn()
        {
            RespawnPlayer();
        }

        public void ForceRespawnAtPosition(Vector3 position)
        {
            SetCheckpoint(position);
            RespawnPlayer();
        }

        // Statistics
        public CheckpointStatistics GetCheckpointStatistics()
        {
            CheckpointStatistics stats = new CheckpointStatistics();

            stats.totalCheckpoints = registeredCheckpoints.Count;
            stats.activatedCheckpoints = 0;
            stats.checkpointHistory = checkpointHistory.Count;
            stats.timeSinceLastCheckpoint = GetTimeSinceLastCheckpoint();

            foreach (var checkpoint in registeredCheckpoints)
            {
                if (checkpoint != null && checkpoint.IsActivated)
                {
                    stats.activatedCheckpoints++;
                }
            }

            stats.completionPercentage = stats.totalCheckpoints > 0 ?
                (float)stats.activatedCheckpoints / stats.totalCheckpoints * 100f : 0f;

            return stats;
        }

        // Debug and visualization
        private void OnDrawGizmos()
        {
            if (!showCheckpointGizmos) return;

            // Draw current checkpoint
            if (currentCheckpoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentCheckpoint.position, 1f);
            }

            // Draw default checkpoint
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(defaultCheckpointPosition, Vector3.one * 0.5f);

            // Draw checkpoint history
            Gizmos.color = Color.yellow;
            for (int i = 0; i < checkpointHistory.Count; i++)
            {
                Vector3 pos = checkpointHistory[i].position;
                Gizmos.DrawWireSphere(pos, 0.5f);

                // Draw connection line to previous checkpoint
                if (i > 0)
                {
                    Gizmos.DrawLine(checkpointHistory[i - 1].position, pos);
                }
            }

            // Draw connection from last history to current
            if (checkpointHistory.Count > 0 && currentCheckpoint != null)
            {
                Vector3 lastHistory = checkpointHistory[checkpointHistory.Count - 1].position;
                Gizmos.DrawLine(lastHistory, currentCheckpoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugMode) return;

            // Draw detailed checkpoint information
            foreach (var checkpoint in registeredCheckpoints)
            {
                if (checkpoint != null)
                {
                    Vector3 pos = checkpoint.checkpointPosition;

                    // Draw checkpoint area
                    Gizmos.color = checkpoint.IsActivated ? Color.green : Color.gray;
                    Gizmos.DrawWireCube(pos, Vector3.one);

                    // Draw trigger area
                    Gizmos.color = Color.cyan;
                    var collider = checkpoint.GetComponent<Collider2D>();
                    if (collider != null)
                    {
                        Gizmos.DrawWireCube(pos, collider.bounds.size);
                    }
                }
            }
        }
    }
}