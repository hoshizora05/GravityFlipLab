using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    /// <summary>
    /// 既存のPlayerControllerとステージシステムを統合するためのアダプター
    /// </summary>
    public class PlayerStageAdapter : MonoBehaviour
    {
        [Header("Integration Settings")]
        public PlayerController playerController;
        public PlayerCollision playerCollision;

        [Header("Stage Events")]
        public bool enableStageEvents = true;

        // Events for stage system
        public static event System.Action OnPlayerDeath;
        public static event System.Action OnPlayerRespawn;
        public static event System.Action<int> OnEnergyChipCollected;

        private void Awake()
        {
            // 既存コンポーネントの自動取得
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerCollision == null)
                playerCollision = GetComponent<PlayerCollision>();
        }

        private void Start()
        {
            InitializeIntegration();
        }

        private void InitializeIntegration()
        {
            if (enableStageEvents)
            {
                // ステージシステムとの連携初期化
                SetupStageIntegration();
            }
        }

        private void SetupStageIntegration()
        {
            // PlayerCollisionとの連携
            if (playerCollision != null)
            {
                playerCollision.Initialize(playerController);
            }

            // GameManagerとの連携
            if (GameManager.Instance != null)
            {
                // プレイヤーの初期位置をチェックポイントマネージャーに登録
                Vector3 startPos = transform.position;
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(startPos);
                }
            }
        }

        /// <summary>
        /// 既存PlayerControllerのTakeDamageを拡張
        /// </summary>
        public void HandlePlayerDamage()
        {
            if (playerController != null)
            {
                // 既存の処理を呼び出し
                playerController.TakeDamage();

                // ステージシステムへの通知
                OnPlayerDeath?.Invoke();

                // GameManagerの死亡カウント増加
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.sessionDeathCount++;
                }

                // リスポーン処理
                StartCoroutine(HandleRespawn());
            }
        }

        private IEnumerator HandleRespawn()
        {
            yield return new WaitForSeconds(1f);

            // チェックポイントからリスポーン
            if (CheckpointManager.Instance != null)
            {
                Vector3 respawnPos = CheckpointManager.Instance.GetCurrentCheckpointPosition();
                transform.position = respawnPos;

                // プレイヤー状態リセット
                ResetPlayerState();

                OnPlayerRespawn?.Invoke();
            }
        }

        public void ResetPlayerState()
        {
            if (playerController != null)
            {
                // 既存のリセット処理があれば呼び出し
                // playerController.ResetPlayer();

                // 基本的な状態リセット
                Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// エナジーチップ収集処理
        /// </summary>
        public void CollectEnergyChip()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.sessionEnergyChips++;
                OnEnergyChipCollected?.Invoke(GameManager.Instance.sessionEnergyChips);
            }
        }

        /// <summary>
        /// ゴール到達処理
        /// </summary>
        public void ReachGoal()
        {
            // プレイヤーの動きを停止
            if (playerController != null)
            {
                Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    // 必要に応じてオートラン停止
                    // playerController.SetAutoRun(false);
                }
            }

            // ステージクリア処理
            if (Stage.StageManager.Instance != null)
            {
                Stage.StageManager.Instance.CompleteStage();
            }
        }

        /// <summary>
        /// 既存のPlayerCollisionから呼び出される統合ハンドラー
        /// </summary>
        public void HandleIntegratedCollision(Collider2D other)
        {
            // ゴール判定
            if (other.CompareTag("Goal"))
            {
                ReachGoal();
                return;
            }

            // チェックポイント判定
            if (other.CompareTag("Checkpoint"))
            {
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(other.transform.position);
                }
                return;
            }

            // エナジーチップ判定
            if (other.CompareTag("EnergyChip"))
            {
                CollectEnergyChip();

                // コレクティブルオブジェクトへの通知
                var collectible = other.GetComponent<Stage.Collectible>();
                if (collectible != null && Stage.StageManager.Instance != null)
                {
                    Stage.StageManager.Instance.CollectibleCollected(collectible);
                }

                other.gameObject.SetActive(false);
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleIntegratedCollision(other);
        }
    }
}