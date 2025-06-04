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
        public bool resetGravityOnRespawn = false; // デフォルトをfalseに変更

        [Header("Respawn Integration")]
        public bool useRespawnIntegration = true; // 新機能：RespawnIntegrationとの統合
        public bool delegateToRespawnIntegration = true; // リスポーン処理を委譲

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
            if (currentCheckpoint != null && !isDefault)
            {
                checkpointHistory.Add(currentCheckpoint);
            }

            currentCheckpoint = new CheckpointData(position, gravity);
            currentCheckpointPosition = position;

            if (Stage.StageManager.Instance != null)
            {
                currentCheckpoint.additionalData["stageTime"] = Time.time - Stage.StageManager.Instance.stageStartTime;
                currentCheckpoint.additionalData["energyChipsCollected"] = GameManager.Instance.sessionEnergyChips;
            }

            OnCheckpointSet?.Invoke(currentCheckpoint);
            PlayCheckpointEffect(position);

            if (debugMode || logRespawnEvents)
                Debug.Log($"Checkpoint set at: {position} with gravity: {gravity}");
        }

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

        /// <summary>
        /// プレイヤー死亡時のハンドラー - RespawnIntegrationとの統合考慮
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (debugMode || logRespawnEvents)
                Debug.Log("CheckpointManager: Player death detected");

            // RespawnIntegrationが存在し、使用する設定の場合は委譲
            if (useRespawnIntegration && delegateToRespawnIntegration)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var respawnIntegration = player.GetComponent<RespawnIntegration>();
                    if (respawnIntegration != null)
                    {
                        if (debugMode || logRespawnEvents)
                            Debug.Log("CheckpointManager: Delegating respawn to RespawnIntegration");
                        return; // RespawnIntegrationに任せる
                    }

                    // PlayerControllerに処理中であることを通知
                    var playerController = player.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.NotifyRespawnHandled();
                    }
                }
            }

            // フォールバック：従来のリスポーン処理
            StartCoroutine(RespawnSequence());
        }

        private void HandleGravityFlip(GravityDirection newDirection)
        {
            if (currentCheckpoint != null)
            {
                currentCheckpoint.gravityDirection = newDirection;
            }
        }

        private IEnumerator RespawnSequence()
        {
            Vector3 respawnPosition = GetCurrentCheckpointPosition();
            OnRespawnStarted?.Invoke(respawnPosition);

            yield return new WaitForSeconds(respawnDelay);

            RespawnPlayer();

            OnRespawnCompleted?.Invoke(respawnPosition);
        }

        /// <summary>
        /// プレイヤーリスポーン処理 - 重力競合問題を修正
        /// </summary>
        private void RespawnPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player not found for respawn!");
                return;
            }

            var playerController = player.GetComponent<PlayerController>();
            var rigidbody = player.GetComponent<Rigidbody2D>();
            var gravityAffected = player.GetComponent<GravityAffectedObject>();
            var respawnIntegration = player.GetComponent<RespawnIntegration>();

            if (currentCheckpoint == null)
            {
                Debug.LogError("No checkpoint data available for respawn!");
                return;
            }

            Vector3 respawnPosition = currentCheckpoint.position;

            if (enableSafetyRespawn)
            {
                respawnPosition = ValidateAndCorrectRespawnPosition(respawnPosition);
            }

            // 位置を移動
            player.transform.position = respawnPosition;

            // 物理状態のリセット
            if (rigidbody != null)
            {
                if (preserveVelocityOnRespawn)
                {
                    Vector2 velocity = rigidbody.linearVelocity;
                    velocity.y = 0f;
                    rigidbody.linearVelocity = velocity;
                }
                else
                {
                    rigidbody.linearVelocity = Vector2.zero;
                    rigidbody.angularVelocity = 0f;
                }
            }

            // 重要：重力システムの処理を修正
            if (useRespawnIntegration && respawnIntegration != null)
            {
                // RespawnIntegrationが存在する場合は重力処理を委譲
                if (debugMode || logRespawnEvents)
                    Debug.Log("CheckpointManager: Gravity handling delegated to RespawnIntegration");
            }
            else
            {
                // RespawnIntegrationがない場合のみ従来の重力処理
                HandleGravityForLegacyMode(gravityAffected);
            }

            // PlayerControllerの状態リセット（重力に影響しない方法）
            if (playerController != null)
            {
                // 重力設定を保護しながらリセット
                ResetPlayerControllerSafely(playerController, gravityAffected);
            }

            // 地面検出の更新
            var groundDetector = player.GetComponent<AdvancedGroundDetector>();
            if (groundDetector != null)
            {
                groundDetector.ForceDetection();
            }

            // PlayerMovementの物理状態検証のみ
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ValidatePhysicsState();
            }

            PlayRespawnEffect(respawnPosition);
            RestoreAdditionalState();

            if (debugMode || logRespawnEvents)
                Debug.Log($"CheckpointManager: Player respawned at: {respawnPosition}");
        }

        /// <summary>
        /// RespawnIntegrationがない場合の従来モード重力処理
        /// </summary>
        private void HandleGravityForLegacyMode(GravityAffectedObject gravityAffected)
        {
            if (!resetGravityOnRespawn) return;

            try
            {
                if (GravitySystem.Instance != null)
                {
                    if (currentCheckpoint.gravityDirection == GravityDirection.Down)
                    {
                        GravitySystem.Instance.ResetToOriginalGravity();
                    }
                    else
                    {
                        GravitySystem.Instance.SetGlobalGravityDirection(Vector2.up);
                    }
                }

                // GravityAffectedObjectのリセット（ResetToOriginalGravityは呼ばない）
                if (gravityAffected != null)
                {
                    // useCustomGravityを保持したまま、必要最小限の設定のみ
                    gravityAffected.gravityScale = 1f;

                    // useCustomGravityがtrueであることを確認
                    if (!gravityAffected.useCustomGravity)
                    {
                        gravityAffected.useCustomGravity = true;
                        if (debugMode || logRespawnEvents)
                            Debug.Log("CheckpointManager: Re-enabled useCustomGravity");
                    }

                    if (debugMode || logRespawnEvents)
                        Debug.Log("CheckpointManager: Legacy gravity handling applied (useCustomGravity preserved)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CheckpointManager: Legacy gravity handling failed - {e.Message}");
            }
        }

        /// <summary>
        /// PlayerControllerの安全なリセット（重力設定を保護）
        /// </summary>
        private void ResetPlayerControllerSafely(PlayerController playerController, GravityAffectedObject gravityAffected)
        {
            // 重力設定を保存
            bool useCustomGravity = true;
            float gravityScale = 1f;

            if (gravityAffected != null)
            {
                useCustomGravity = gravityAffected.useCustomGravity;
                gravityScale = gravityAffected.gravityScale;
            }

            // PlayerControllerのExternalRespawnを呼ぶ（競合回避）
            Vector3 respawnPosition = currentCheckpoint.position;
            playerController.ExternalRespawn(respawnPosition);

            // 重力設定を復元
            if (gravityAffected != null)
            {
                gravityAffected.useCustomGravity = useCustomGravity;
                gravityAffected.gravityScale = gravityScale;

                if (debugMode || logRespawnEvents)
                    Debug.Log($"CheckpointManager: Gravity settings preserved - useCustom: {useCustomGravity}, scale: {gravityScale}");
            }
        }

        private Vector3 ValidateAndCorrectRespawnPosition(Vector3 originalPosition)
        {
            if (IsPositionSafe(originalPosition))
            {
                return originalPosition;
            }

            if (debugMode)
                Debug.LogWarning($"Unsafe respawn position detected: {originalPosition}. Attempting correction.");

            Vector3 safePosition = FindSafeRespawnPosition(originalPosition);

            if (safePosition != Vector3.zero)
            {
                if (debugMode)
                    Debug.Log($"Safe respawn position found: {safePosition}");
                return safePosition;
            }

            Debug.LogWarning($"No safe position found, using default checkpoint: {defaultCheckpointPosition}");
            return defaultCheckpointPosition;
        }

        private bool IsPositionSafe(Vector3 position)
        {
            if (Physics2D.OverlapPoint(position, LayerMask.GetMask("Obstacles", "Hazards")))
            {
                return false;
            }

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

            return Vector3.zero;
        }

        private void RestoreAdditionalState()
        {
            if (currentCheckpoint?.additionalData == null) return;

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

        public void SetUseRespawnIntegration(bool use)
        {
            useRespawnIntegration = use;
        }

        public void SetDelegateToRespawnIntegration(bool delegate_)
        {
            delegateToRespawnIntegration = delegate_;
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

        // Debug用メソッド
        public void DiagnoseRespawnIntegration()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var respawnIntegration = player.GetComponent<RespawnIntegration>();
                Debug.Log($"CheckpointManager Integration Status:");
                Debug.Log($"- useRespawnIntegration: {useRespawnIntegration}");
                Debug.Log($"- delegateToRespawnIntegration: {delegateToRespawnIntegration}");
                Debug.Log($"- RespawnIntegration component: {(respawnIntegration != null ? "Present" : "Missing")}");
                Debug.Log($"- resetGravityOnRespawn: {resetGravityOnRespawn}");
            }
        }

        // Debug and visualization
        private void OnDrawGizmos()
        {
            if (!showCheckpointGizmos) return;

            if (currentCheckpoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentCheckpoint.position, 1f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(defaultCheckpointPosition, Vector3.one * 0.5f);

            Gizmos.color = Color.yellow;
            for (int i = 0; i < checkpointHistory.Count; i++)
            {
                Vector3 pos = checkpointHistory[i].position;
                Gizmos.DrawWireSphere(pos, 0.5f);

                if (i > 0)
                {
                    Gizmos.DrawLine(checkpointHistory[i - 1].position, pos);
                }
            }

            if (checkpointHistory.Count > 0 && currentCheckpoint != null)
            {
                Vector3 lastHistory = checkpointHistory[checkpointHistory.Count - 1].position;
                Gizmos.DrawLine(lastHistory, currentCheckpoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugMode) return;

            foreach (var checkpoint in registeredCheckpoints)
            {
                if (checkpoint != null)
                {
                    Vector3 pos = checkpoint.checkpointPosition;

                    Gizmos.color = checkpoint.IsActivated ? Color.green : Color.gray;
                    Gizmos.DrawWireCube(pos, Vector3.one);

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