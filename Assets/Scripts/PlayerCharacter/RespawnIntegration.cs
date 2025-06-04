using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    /// <summary>
    /// 障害物接触時のリスポーン処理を統合管理するコンポーネント
    /// 既存のCheckpointManagerとPlayerControllerを連携させる
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

        // Components
        private PlayerController playerController;
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

            // PlayerMovementコンポーネントの初期化確認と実行
            InitializePlayerMovement();
        }

        /// <summary>
        /// PlayerMovementコンポーネントの初期化を確認・実行
        /// </summary>
        private void InitializePlayerMovement()
        {
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null && playerController != null)
            {
                // PlayerMovementが初期化されていない場合は初期化
                playerMovement.Initialize(playerController);

                if (logRespawnEvents)
                    Debug.Log("RespawnIntegration: PlayerMovement initialized");
            }
        }

        private void SubscribeToEvents()
        {
            // PlayerControllerの死亡イベントを監視
            PlayerController.OnPlayerDeath += HandlePlayerDeath;
        }

        private void UnsubscribeFromEvents()
        {
            PlayerController.OnPlayerDeath -= HandlePlayerDeath;
        }

        /// <summary>
        /// プレイヤー死亡時のメインハンドラー
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (isRespawning) return;

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Player death detected, initiating respawn sequence");

            // 連続死亡の検出
            DetectConsecutiveDeaths();

            // リスポーン処理開始
            StartCoroutine(RespawnSequence());
        }

        /// <summary>
        /// 外部から直接呼び出し可能なリスポーン処理
        /// 障害物コンポーネントから直接呼び出される
        /// </summary>
        public void TriggerInstantRespawn()
        {
            if (isRespawning) return;

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Instant respawn triggered");

            // プレイヤーの死亡状態を設定
            if (playerController != null && playerController.isAlive)
            {
                SetPlayerDead();
            }

            // 即座にリスポーン
            StartCoroutine(RespawnSequence());
        }

        /// <summary>
        /// プレイヤーを死亡状態に設定
        /// </summary>
        private void SetPlayerDead()
        {
            if (playerController == null) return;

            // プライベートフィールドにアクセスできないため、TakeDamageを使用
            // ライフが残っている場合は0にする
            while (playerController.stats.livesRemaining > 0)
            {
                playerController.TakeDamage();
            }
        }

        /// <summary>
        /// メインリスポーン処理
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

            // プレイヤーをリスポーン
            ExecuteRespawn(respawnPosition);

            // リスポーン完了
            isRespawning = false;
            OnRespawnCompleted?.Invoke();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Respawn sequence completed");
        }

        /// <summary>
        /// プレイヤーの動きを停止
        /// </summary>
        private void FreezePlayer()
        {
            if (rb2d != null)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
                rb2d.gravityScale = 0f; // 一時的に重力を無効化
            }

            // 入力を無効化（PlayerControllerのisAliveがfalseなので自動的に無効化される）
        }

        /// <summary>
        /// リスポーン位置を取得
        /// </summary>
        private Vector3 GetRespawnPosition()
        {
            if (CheckpointManager.Instance != null)
            {
                return CheckpointManager.Instance.GetCurrentCheckpointPosition();
            }

            // フォールバック: プレイヤーの初期位置
            if (Stage.StageManager.Instance != null)
            {
                return Stage.StageManager.Instance.GetPlayerStartPosition();
            }

            // 最終フォールバック
            return Vector3.zero;
        }

        /// <summary>
        /// 実際のリスポーン実行
        /// </summary>
        private void ExecuteRespawn(Vector3 respawnPosition)
        {
            // 位置をリセット
            transform.position = respawnPosition;

            // プレイヤーコントローラーの状態をリセット
            if (playerController != null)
            {
                ResetPlayerController();
            }

            // 物理状態をリセット
            ResetPhysicsState();

            // 重力状態をリセット
            if (resetGravityOnRespawn)
            {
                ResetGravityState();
            }

            // ビジュアルエフェクト
            if (enableRespawnEffect)
            {
                PlayRespawnEffects();
            }

            // 無敵時間を付与
            if (invulnerabilityDuration > 0f)
            {
                StartCoroutine(ApplyInvulnerability());
            }
        }

        /// <summary>
        /// PlayerControllerの状態をリセット
        /// </summary>
        private void ResetPlayerController()
        {
            if (playerController == null) return;

            // 既存のRespawnメソッドを使用
            playerController.Respawn();

            // 追加の状態リセット
            playerController.stats.livesRemaining = 1; // 最低1ライフを保証
            playerController.stats.isInvincible = false;

            // PlayerMovementコンポーネントのリセットと再初期化
            ResetPlayerMovement();
        }

        /// <summary>
        /// PlayerMovementコンポーネントのリセットと再初期化
        /// </summary>
        private void ResetPlayerMovement()
        {
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null && playerController != null)
            {
                // PlayerMovementを再初期化してnull参照を防ぐ
                playerMovement.Initialize(playerController);

                // 物理状態の検証
                playerMovement.ValidatePhysicsState();

                if (logRespawnEvents)
                    Debug.Log("RespawnIntegration: PlayerMovement reset and reinitialized");
            }
        }

        /// <summary>
        /// 物理状態をリセット
        /// </summary>
        private void ResetPhysicsState()
        {
            if (rb2d == null) return;

            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            rb2d.gravityScale = playerController.stats.gravityScale; // 元の重力スケールに戻す
        }

        /// <summary>
        /// 重力状態をリセット
        /// </summary>
        private void ResetGravityState()
        {
            // GravitySystemの新しいResetToOriginalGravityメソッドを使用
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.ResetToOriginalGravity();
            }

            // GravityAffectedObjectの重力リセット
            if (gravityAffected != null)
            {
                gravityAffected.ResetToOriginalGravity();
            }

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Gravity state reset completed");
        }

        /// <summary>
        /// リスポーンエフェクトを再生
        /// </summary>
        private void PlayRespawnEffects()
        {
            if (playerVisuals != null)
            {
                // パーティクルエフェクトなどの再生
                playerVisuals.ResetVisuals();
            }

            // CheckpointManagerのリスポーンエフェクトを使用
            if (CheckpointManager.Instance != null && CheckpointManager.Instance.respawnEffect != null)
            {
                GameObject effect = Instantiate(CheckpointManager.Instance.respawnEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            // サウンド再生
            if (CheckpointManager.Instance != null && CheckpointManager.Instance.respawnSound != null)
            {
                AudioSource.PlayClipAtPoint(CheckpointManager.Instance.respawnSound, transform.position);
            }
        }

        /// <summary>
        /// 無敵時間を適用
        /// </summary>
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

        /// <summary>
        /// 連続死亡の検出
        /// </summary>
        private void DetectConsecutiveDeaths()
        {
            float currentTime = Time.time;

            // 短時間での連続死亡を検出
            if (currentTime - lastDeathTime < 3f) // 3秒以内
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
        /// プレイヤーが無限ループにハマった場合などに使用
        /// </summary>
        public void EmergencyRespawn()
        {
            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Emergency respawn triggered");

            StopAllCoroutines();
            isRespawning = false;
            consecutiveDeaths = 0;

            // 緊急時はコンポーネントを完全に再初期化
            InitializeComponents();

            // 緊急時の重力リセット
            EmergencyGravityReset();

            // 強制的にリスポーン
            StartCoroutine(RespawnSequence());
        }

        /// <summary>
        /// 緊急時の重力リセット
        /// </summary>
        private void EmergencyGravityReset()
        {
            try
            {
                // GravitySystemの強制リセット
                if (GravitySystem.Instance != null)
                {
                    GravitySystem.Instance.ForceResetToOriginalGravity();
                }

                // GravityAffectedObjectの強制リセット
                if (gravityAffected != null)
                {
                    gravityAffected.ForceResetToOriginalGravity();
                }

                if (logRespawnEvents)
                    Debug.Log("RespawnIntegration: Emergency gravity reset completed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RespawnIntegration: Emergency gravity reset failed - {e.Message}");
            }
        }

        /// <summary>
        /// リスポーン状態のリセット
        /// ステージ開始時などに呼び出す
        /// </summary>
        public void ResetRespawnState()
        {
            isRespawning = false;
            consecutiveDeaths = 0;
            lastDeathTime = -1f;

            // コンポーネントの再初期化を確実に実行
            InitializeComponents();

            if (logRespawnEvents)
                Debug.Log("RespawnIntegration: Respawn state reset with component reinitialization");
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

        // デバッグ用
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !logRespawnEvents) return;

            // リスポーン状態の可視化
            if (isRespawning)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 2f);
            }

            // 現在のチェックポイント位置
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