using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    /// <summary>
    /// 障害物接触時のリスポーン処理を統合管理するコンポーネント
    /// 重力システムとの適切な統合を保証
    /// </summary>
    public class RespawnIntegration : MonoBehaviour
    {
        [Header("Respawn Settings")]
        public float respawnDelay = 0.5f;
        public bool enableRespawnEffect = true;
        public bool resetGravityOnRespawn = true;
        public bool logRespawnEvents = false;

        [Header("Safety Features")]
        public float invulnerabilityDuration = 1.0f;
        public int maxConsecutiveDeaths = 5;
        public float emergencyRespawnDelay = 2.0f;

        [Header("Gravity Fix Settings")]
        public bool forceGravityValidation = true;
        public float gravityValidationDelay = 0.2f;

        // Components
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        private Rigidbody2D rb2d;
        private GravityAffectedObject gravityAffected;
        private PlayerVisuals playerVisuals;

        // State tracking
        private bool isRespawning = false;
        private int consecutiveDeaths = 0;
        private float lastDeathTime = -1f;

        // Events
        public static event System.Action OnRespawnStarted;
        public static event System.Action OnRespawnCompleted;
        public static event System.Action<int> OnConsecutiveDeathDetected;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            playerController = GetComponent<PlayerController>();
            playerMovement = GetComponent<PlayerMovement>();
            rb2d = GetComponent<Rigidbody2D>();
            gravityAffected = GetComponent<GravityAffectedObject>();
            playerVisuals = GetComponent<PlayerVisuals>();

            if (playerController == null)
            {
                Debug.LogError("RespawnIntegration requires PlayerController component");
            }

            if (rb2d == null)
            {
                Debug.LogError("RespawnIntegration requires Rigidbody2D component");
            }

            // PlayerMovementの初期化確認
            EnsurePlayerMovementInitialized();
        }

        private void EnsurePlayerMovementInitialized()
        {
            if (playerMovement != null && playerController != null)
            {
                playerMovement.Initialize(playerController);

                if (logRespawnEvents)
                    Debug.Log("RespawnIntegration: PlayerMovement initialized");
            }
        }

        private void SubscribeToEvents()
        {
            PlayerController.OnPlayerDeath += HandlePlayerDeath;
        }

        private void UnsubscribeFromEvents()
        {
            PlayerController.OnPlayerDeath -= HandlePlayerDeath;
        }

        private void HandlePlayerDeath()
        {
            if (isRespawning) return;

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Player death detected, initiating respawn sequence");

            DetectConsecutiveDeaths();
            StartCoroutine(RespawnSequence());
        }

        public void TriggerInstantRespawn()
        {
            if (isRespawning) return;

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Instant respawn triggered");

            if (playerController != null && playerController.isAlive)
            {
                SetPlayerDead();
            }

            StartCoroutine(RespawnSequence());
        }

        private void SetPlayerDead()
        {
            if (playerController == null) return;

            while (playerController.stats.livesRemaining > 0)
            {
                playerController.TakeDamage();
            }
        }

        /// <summary>
        /// 重要：重力問題を根本的に解決するリスポーン処理
        /// </summary>
        private IEnumerator RespawnSequence()
        {
            isRespawning = true;
            OnRespawnStarted?.Invoke();

            // プレイヤーの動きを停止
            FreezePlayer();

            // リスポーン遅延
            float delay = (consecutiveDeaths >= maxConsecutiveDeaths) ? emergencyRespawnDelay : respawnDelay;
            yield return new WaitForSeconds(delay);

            // チェックポイント位置取得
            Vector3 respawnPosition = GetRespawnPosition();

            if (logRespawnEvents)
                Debug.Log($"RespawnIntegration: Respawning at {respawnPosition}");

            // ===== 重要：正しい順序で実行 =====

            // 1. 位置リセット
            transform.position = respawnPosition;

            // 2. 基本的な物理状態リセット
            ResetBasicPhysics();

            // 4. プレイヤーコントローラーの状態リセット（安全なRespawn呼び出し）
            ResetPlayerControllerState();

            // 5. 重力システムリセット（必要な場合のみ）
            if (resetGravityOnRespawn)
            {
                ResetGravitySystemSafely();
            }

            // 6. 重要：重力が確実に機能するよう強制設定
            ForceEnableGravity();

            // 7. 少し待ってから重力状態を検証
            yield return new WaitForSeconds(gravityValidationDelay);

            if (forceGravityValidation)
            {
                ValidateAndFixGravity();
            }

            // エフェクトと無敵時間
            if (enableRespawnEffect)
            {
                PlayRespawnEffects();
            }

            if (invulnerabilityDuration > 0f)
            {
                StartCoroutine(ApplyInvulnerability());
            }

            isRespawning = false;
            OnRespawnCompleted?.Invoke();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Respawn sequence completed");
        }

        private void FreezePlayer()
        {
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }

        private Vector3 GetRespawnPosition()
        {
            if (CheckpointManager.Instance != null)
            {
                return CheckpointManager.Instance.GetCurrentCheckpointPosition();
            }

            if (Stage.StageManager.Instance != null)
            {
                return Stage.StageManager.Instance.GetPlayerStartPosition();
            }

            return Vector3.zero;
        }

        /// <summary>
        /// 基本的な物理状態のリセット
        /// </summary>
        private void ResetBasicPhysics()
        {
            if (rb2d == null) return;

            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Basic physics reset completed");
        }

        /// <summary>
        /// PlayerControllerの状態リセット
        /// </summary>
        private void ResetPlayerControllerState()
        {
            if (playerController == null) return;

            // PlayerController.ExternalRespawn()を使用（競合回避）
            Vector3 respawnPosition = GetRespawnPosition();
            playerController.ExternalRespawn(respawnPosition);

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: PlayerController.ExternalRespawn() called");
        }

        /// <summary>
        /// 重力システムの安全なリセット
        /// </summary>
        private void ResetGravitySystemSafely()
        {
            try
            {
                if (GravitySystem.Instance != null)
                {
                    GravitySystem.Instance.ResetToOriginalGravity();

                    if (logRespawnEvents)
                        Debug.Log("RespawnIntegration: GravitySystem reset completed");
                }

                //// 代わりに必要最小限の確認のみ
                //if (gravityAffected != null)
                //{
                //    // useCustomGravityがfalseになっていたら修正
                //    if (!gravityAffected.useCustomGravity)
                //    {
                //        gravityAffected.useCustomGravity = true;
                //        if (logRespawnEvents)
                //            Debug.Log("RespawnIntegration: Restored useCustomGravity to true");
                //    }

                //    // gravityScaleの確認
                //    if (gravityAffected.gravityScale <= 0f)
                //    {
                //        gravityAffected.gravityScale = 1f;
                //        if (logRespawnEvents)
                //            Debug.Log("RespawnIntegration: Fixed gravityScale");
                //    }
                //}
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RespawnIntegration: GravitySystem reset failed - {e.Message}");
            }
        }

        /// <summary>
        /// 重要：重力を確実に有効化する
        /// </summary>
        private void ForceEnableGravity()
        {
            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Force enabling gravity...");

            if (gravityAffected != null)
            {
                // 最小限の設定のみ変更（挙動に影響する値は変更しない）
                gravityAffected.useCustomGravity = true;

                // gravityScaleのみ確認・修正
                if (gravityAffected.gravityScale <= 0f)
                {
                    gravityAffected.gravityScale = 1f;
                }

                // maintainInertia, inertiaDecay, smoothGravityTransitionは変更しない
                // （PlayerControllerのオリジナル設定を保持）

                // Rigidbody2Dのgravityスケールを適切に設定
                if (rb2d != null)
                {
                    rb2d.gravityScale = gravityAffected.useCustomGravity ? 0f : 1f;
                }

                if (logRespawnEvents)
                    Debug.Log($"RespawnIntegration: Gravity enabled with minimal changes - useCustomGravity: {gravityAffected.useCustomGravity}, gravityScale: {gravityAffected.gravityScale}");
            }
            else
            {
                // GravityAffectedObjectがない場合はUnity標準重力を使用
                if (rb2d != null)
                {
                    rb2d.gravityScale = 1f;

                    if (logRespawnEvents)
                        Debug.Log($"RespawnIntegration: Using Unity standard gravity - gravityScale: {rb2d.gravityScale}");
                }
            }
        }

        /// <summary>
        /// PlayerMovementの再初期化（削除：PlayerController.Respawn()で処理済み）
        /// </summary>
        private void ReinitializePlayerMovement()
        {
            // PlayerController.Respawn()でInitializePlayerMovementSafely()が呼ばれるため
            // ここでの処理は不要（重複を避ける）

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: PlayerMovement initialization handled by PlayerController.Respawn()");
        }

        /// <summary>
        /// 重力状態の検証と修正
        /// </summary>
        private void ValidateAndFixGravity()
        {
            bool needsFix = false;
            List<string> fixes = new List<string>();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Validating gravity state...");

            if (gravityAffected != null)
            {
                // useCustomGravityのチェックのみ
                if (!gravityAffected.useCustomGravity)
                {
                    gravityAffected.useCustomGravity = true;
                    needsFix = true;
                    fixes.Add("Enabled useCustomGravity");
                }

                // gravityScaleが0以下の場合のみ修正
                if (gravityAffected.gravityScale <= 0f)
                {
                    gravityAffected.gravityScale = 1f;
                    needsFix = true;
                    fixes.Add("Fixed gravityScale to 1.0");
                }

                // Rigidbody2Dのチェック
                if (rb2d != null && gravityAffected.useCustomGravity && rb2d.gravityScale != 0f)
                {
                    rb2d.gravityScale = 0f;
                    needsFix = true;
                    fixes.Add("Set Rigidbody2D gravityScale to 0 (custom gravity mode)");
                }

                // maintainInertia, inertiaDecay, smoothGravityTransitionは修正しない
                // （PlayerControllerのオリジナル設定を尊重）
            }
            else
            {
                // GravityAffectedObjectがない場合
                if (rb2d != null && rb2d.gravityScale <= 0f)
                {
                    rb2d.gravityScale = 1f;
                    needsFix = true;
                    fixes.Add("Set Rigidbody2D gravityScale to 1.0 (standard gravity mode)");
                }
            }

            if (needsFix)
            {
                if (logRespawnEvents)
                    Debug.Log($"RespawnIntegration: Minimal gravity fixes applied: {string.Join(", ", fixes)}");
            }
            else
            {
                if (logRespawnEvents)
                    Debug.Log("RespawnIntegration: Gravity validation passed");
            }
        }

        /// <summary>
        /// 現在の重力状態をログ出力
        /// </summary>
        private void LogCurrentGravityState()
        {
            Debug.Log("=== Current Gravity State ===");

            if (gravityAffected != null)
            {
                Debug.Log($"GravityAffectedObject - useCustomGravity: {gravityAffected.useCustomGravity}, gravityScale: {gravityAffected.gravityScale}");
                Debug.Log($"GravityAffectedObject - currentGravity: {gravityAffected.GetCurrentGravity()}");
            }
            else
            {
                Debug.Log("GravityAffectedObject: NULL");
            }

            if (rb2d != null)
            {
                Debug.Log($"Rigidbody2D - gravityScale: {rb2d.gravityScale}, velocity: {rb2d.linearVelocity}");
            }
            else
            {
                Debug.Log("Rigidbody2D: NULL");
            }

            Debug.Log($"Unity Physics2D.gravity: {Physics2D.gravity}");

            if (GravitySystem.Instance != null)
            {
                Debug.Log($"GravitySystem - Direction: {GravitySystem.Instance.CurrentGravityDirection}, Strength: {GravitySystem.Instance.CurrentGravityStrength}");
            }
            else
            {
                Debug.Log("GravitySystem: NULL");
            }

            Debug.Log("===========================");
        }

        private void PlayRespawnEffects()
        {
            if (playerVisuals != null)
            {
                playerVisuals.ResetVisuals();
            }

            if (CheckpointManager.Instance != null && CheckpointManager.Instance.respawnEffect != null)
            {
                GameObject effect = Instantiate(CheckpointManager.Instance.respawnEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (CheckpointManager.Instance != null && CheckpointManager.Instance.respawnSound != null)
            {
                AudioSource.PlayClipAtPoint(CheckpointManager.Instance.respawnSound, transform.position);
            }
        }

        private IEnumerator ApplyInvulnerability()
        {
            if (playerController == null) yield break;

            playerController.stats.isInvincible = true;

            if (playerVisuals != null)
            {
                playerVisuals.SetInvincibleVisuals(true);
            }

            if (logRespawnEvents)
                Debug.Log($"RespawnIntegration: Invulnerability applied for {invulnerabilityDuration} seconds");

            yield return new WaitForSeconds(invulnerabilityDuration);

            playerController.stats.isInvincible = false;

            if (playerVisuals != null)
            {
                playerVisuals.SetInvincibleVisuals(false);
            }

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Invulnerability ended");
        }

        private void DetectConsecutiveDeaths()
        {
            float currentTime = Time.time;

            if (currentTime - lastDeathTime < 3f)
            {
                consecutiveDeaths++;
            }
            else
            {
                consecutiveDeaths = 1;
            }

            lastDeathTime = currentTime;

            if (consecutiveDeaths >= maxConsecutiveDeaths)
            {
                if (logRespawnEvents)
                    Debug.LogWarning($"RespawnIntegration: Consecutive deaths detected ({consecutiveDeaths})");

                OnConsecutiveDeathDetected?.Invoke(consecutiveDeaths);
            }
        }

        /// <summary>
        /// 緊急時のリスポーン処理
        /// </summary>
        public void EmergencyRespawn()
        {
            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Emergency respawn triggered");

            StopAllCoroutines();
            isRespawning = false;
            consecutiveDeaths = 0;

            // 緊急時は即座に重力を修正
            ForceEnableGravity();

            StartCoroutine(RespawnSequence());
        }

        public void ResetRespawnState()
        {
            isRespawning = false;
            consecutiveDeaths = 0;
            lastDeathTime = -1f;

            InitializeComponents();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Respawn state reset");
        }

        // Public API
        public bool IsRespawning => isRespawning;
        public int ConsecutiveDeaths => consecutiveDeaths;

        public void SetRespawnDelay(float delay)
        {
            respawnDelay = Mathf.Max(0f, delay);
        }

        public void SetInvulnerabilityDuration(float duration)
        {
            invulnerabilityDuration = Mathf.Max(0f, duration);
        }

        public void SetResetGravityOnRespawn(bool reset)
        {
            resetGravityOnRespawn = reset;
        }

        public void SetLogRespawnEvents(bool log)
        {
            logRespawnEvents = log;
        }

        public void SetForceGravityValidation(bool force)
        {
            forceGravityValidation = force;
        }

        /// <summary>
        /// 重力状態の手動診断
        /// </summary>
        public void DiagnoseGravityState()
        {
            LogCurrentGravityState();
        }

        /// <summary>
        /// 重力の手動修正
        /// </summary>
        public void ManualGravityFix()
        {
            ForceEnableGravity();
            ValidateAndFixGravity();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Manual gravity fix applied");
        }

        // デバッグ用
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !logRespawnEvents) return;

            if (isRespawning)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 2f);
            }

            if (CheckpointManager.Instance != null)
            {
                Vector3 checkpointPos = CheckpointManager.Instance.GetCurrentCheckpointPosition();
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, checkpointPos);
                Gizmos.DrawWireCube(checkpointPos, Vector3.one);
            }
        }
    }
}