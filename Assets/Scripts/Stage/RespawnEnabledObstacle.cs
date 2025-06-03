using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// リスポーン統合機能を持つ障害物のベースクラス
    /// BaseObstacleを継承し、統合リスポーン機能を追加
    /// </summary>
    public abstract class RespawnEnabledObstacle : BaseObstacle
    {
        [Header("Integrated Respawn Settings")]
        public bool useIntegratedRespawn = true;
        public bool bypassInvulnerability = false;
        public float contactCooldown = 0.5f;
        public bool logContactEvents = false;

        [Header("Contact Effect Settings")]
        public bool enableContactEffect = true;
        public Color dangerColor = Color.red;
        public float flashDuration = 0.1f;

        // 接触状態の管理
        private float lastContactTime = -1f;
        private Dictionary<GameObject, float> playerContactTimes = new Dictionary<GameObject, float>();

        // ビジュアル関連
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashCoroutine;

        public override void StartObstacle()
        {
            base.StartObstacle();
            InitializeRespawnObstacle();
        }

        private void InitializeRespawnObstacle()
        {
            // スプライトレンダラーの取得
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // 障害物レイヤーの設定
            if (gameObject.layer == 0) // Default layer
            {
                gameObject.layer = LayerMask.NameToLayer("Obstacles");
            }
        }

        /// <summary>
        /// プレイヤーとの接触処理（統一インターフェース）
        /// 継承クラスはこのメソッドを呼び出す
        /// </summary>
        protected bool HandlePlayerContact(Collider2D playerCollider)
        {
            // プレイヤーかどうかチェック
            if (!playerCollider.CompareTag("Player")) return false;

            // 対象の有効性チェック
            if (!IsTargetValid(playerCollider.gameObject)) return false;

            // 個別プレイヤーのクールダウンチェック
            if (!CheckPlayerContactCooldown(playerCollider.gameObject)) return false;

            // プレイヤーコンポーネントの取得
            var playerController = playerCollider.GetComponent<PlayerController>();
            if (playerController == null) return false;

            // 無敵状態のチェック（バイパスオプション考慮）
            if (!bypassInvulnerability && playerController.stats.isInvincible)
            {
                if (logContactEvents)
                    Debug.Log($"{GetType().Name}: Player is invulnerable, ignoring contact");
                return false;
            }

            // 接触時間を記録
            UpdatePlayerContactTime(playerCollider.gameObject);

            // 接触処理実行
            ExecutePlayerContact(playerCollider.gameObject, playerController);

            return true;
        }

        /// <summary>
        /// プレイヤー個別のクールダウンチェック
        /// </summary>
        private bool CheckPlayerContactCooldown(GameObject player)
        {
            float currentTime = Time.time;

            if (playerContactTimes.ContainsKey(player))
            {
                float timeSinceLastContact = currentTime - playerContactTimes[player];
                if (timeSinceLastContact < contactCooldown)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// プレイヤーの接触時間を更新
        /// </summary>
        private void UpdatePlayerContactTime(GameObject player)
        {
            float currentTime = Time.time;
            playerContactTimes[player] = currentTime;
            lastContactTime = currentTime;

            // 古いエントリーのクリーンアップ（メモリリーク防止）
            CleanupOldContactTimes();
        }

        /// <summary>
        /// 古い接触時間エントリーのクリーンアップ
        /// </summary>
        private void CleanupOldContactTimes()
        {
            float currentTime = Time.time;
            List<GameObject> toRemove = new List<GameObject>();

            foreach (var kvp in playerContactTimes)
            {
                if (kvp.Key == null || currentTime - kvp.Value > 10f) // 10秒以上古いエントリー
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                playerContactTimes.Remove(key);
            }
        }

        /// <summary>
        /// プレイヤー接触時の処理実行
        /// </summary>
        private void ExecutePlayerContact(GameObject player, PlayerController playerController)
        {
            if (logContactEvents)
                Debug.Log($"{GetType().Name}: Player contact detected at {transform.position}");

            // ビジュアルエフェクト
            if (enableContactEffect)
            {
                PlayContactEffect();
            }

            // 障害物トリガーイベント
            TriggerObstacle();

            // 継承クラス固有の接触処理
            OnPlayerContactDetected(player, playerController);

            if (useIntegratedRespawn)
            {
                // 統合リスポーンシステムを使用
                HandleIntegratedRespawn(player);
            }
            else
            {
                // 従来のダメージシステムを使用
                HandleTraditionalDamage(player, playerController);
            }
        }

        /// <summary>
        /// 継承クラスでオーバーライド可能な接触検出時の処理
        /// </summary>
        protected virtual void OnPlayerContactDetected(GameObject player, PlayerController playerController)
        {
            // 継承クラスで具体的な処理を実装
        }

        /// <summary>
        /// 統合リスポーンシステムによる処理
        /// </summary>
        private void HandleIntegratedRespawn(GameObject player)
        {
            var respawnIntegration = player.GetComponent<RespawnIntegration>();
            if (respawnIntegration != null)
            {
                // 即座にリスポーンを実行
                respawnIntegration.TriggerInstantRespawn();

                if (logContactEvents)
                    Debug.Log($"{GetType().Name}: Triggered instant respawn for player");

                // 継承クラスに通知
                OnRespawnTriggered(player);
            }
            else
            {
                if (logContactEvents)
                    Debug.LogWarning($"{GetType().Name}: RespawnIntegration not found, using fallback damage system");

                // フォールバック: 従来のダメージシステム
                var playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    HandleTraditionalDamage(player, playerController);
                }
            }
        }

        /// <summary>
        /// 従来のダメージシステム（フォールバック用）
        /// </summary>
        private void HandleTraditionalDamage(GameObject player, PlayerController playerController)
        {
            // 既存のダメージ処理
            DealDamage(player);

            if (logContactEvents)
                Debug.Log($"{GetType().Name}: Dealt damage to player using traditional system");

            // 継承クラスに通知
            OnTraditionalDamageDealt(player, playerController);
        }

        /// <summary>
        /// リスポーンがトリガーされた時の処理（継承クラスでオーバーライド可能）
        /// </summary>
        protected virtual void OnRespawnTriggered(GameObject player)
        {
            // 継承クラスで具体的な処理を実装
        }

        /// <summary>
        /// 従来のダメージが発生した時の処理（継承クラスでオーバーライド可能）
        /// </summary>
        protected virtual void OnTraditionalDamageDealt(GameObject player, PlayerController playerController)
        {
            // 継承クラスで具体的な処理を実装
        }

        /// <summary>
        /// 接触時のビジュアルエフェクト
        /// </summary>
        protected void PlayContactEffect()
        {
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null)
                {
                    StopCoroutine(flashCoroutine);
                }
                flashCoroutine = StartCoroutine(ContactFlashEffect());
            }

            // 継承クラス固有のエフェクト
            PlayCustomContactEffect();
        }

        /// <summary>
        /// 継承クラス固有の接触エフェクト（オーバーライド可能）
        /// </summary>
        protected virtual void PlayCustomContactEffect()
        {
            // 継承クラスで具体的なエフェクトを実装
        }

        /// <summary>
        /// フラッシュエフェクトのコルーチン
        /// </summary>
        private IEnumerator ContactFlashEffect()
        {
            if (spriteRenderer == null) yield break;

            Color currentColor = spriteRenderer.color;
            spriteRenderer.color = dangerColor;

            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = currentColor;
            flashCoroutine = null;
        }

        /// <summary>
        /// 手動での危険判定トリガー
        /// 範囲内のすべてのプレイヤーをチェック
        /// </summary>
        public void TriggerManualHazardCheck()
        {
            if (!isActive) return;

            Collider2D obstacleCollider = GetComponent<Collider2D>();
            if (obstacleCollider == null) return;

            // 範囲内のプレイヤーを検索
            Collider2D[] colliders = Physics2D.OverlapAreaAll(
                obstacleCollider.bounds.min,
                obstacleCollider.bounds.max,
                LayerMask.GetMask("Player")
            );

            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Player"))
                {
                    HandlePlayerContact(collider);
                }
            }
        }

        /// <summary>
        /// リスポーン設定の動的変更
        /// </summary>
        public void SetRespawnConfiguration(bool useIntegrated, bool bypassInvuln, float cooldown)
        {
            useIntegratedRespawn = useIntegrated;
            bypassInvulnerability = bypassInvuln;
            contactCooldown = Mathf.Max(0f, cooldown);
        }

        /// <summary>
        /// 接触エフェクト設定の変更
        /// </summary>
        public void SetContactEffectConfiguration(bool enableEffect, Color color, float duration)
        {
            enableContactEffect = enableEffect;
            dangerColor = color;
            flashDuration = Mathf.Max(0f, duration);
        }

        /// <summary>
        /// 特定プレイヤーの接触履歴をクリア
        /// </summary>
        public void ClearPlayerContactHistory(GameObject player)
        {
            if (playerContactTimes.ContainsKey(player))
            {
                playerContactTimes.Remove(player);
            }
        }

        /// <summary>
        /// すべての接触履歴をクリア
        /// </summary>
        public void ClearAllContactHistory()
        {
            playerContactTimes.Clear();
            lastContactTime = -1f;
        }

        // パブリック API
        public float LastContactTime => lastContactTime;
        public int ActivePlayerCount => playerContactTimes.Count;
        public bool HasRecentContact => Time.time - lastContactTime < contactCooldown;

        public bool IsPlayerOnCooldown(GameObject player)
        {
            if (!playerContactTimes.ContainsKey(player)) return false;
            return Time.time - playerContactTimes[player] < contactCooldown;
        }

        public float GetPlayerLastContactTime(GameObject player)
        {
            return playerContactTimes.ContainsKey(player) ? playerContactTimes[player] : -1f;
        }

        // デバッグ用の可視化
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            // 最近の接触を可視化
            if (HasRecentContact)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }

            // アクティブなプレイヤー接触数を可視化
            if (ActivePlayerCount > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < ActivePlayerCount; i++)
                {
                    Vector3 pos = transform.position + Vector3.right * (i + 1) * 0.5f;
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.2f);
                }
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // 接触範囲の可視化
            Collider2D obstacleCollider = GetComponent<Collider2D>();
            if (obstacleCollider != null)
            {
                Gizmos.color = Color.cyan;
                Bounds bounds = obstacleCollider.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            // クールダウン時間の可視化
            if (Application.isPlaying && contactCooldown > 0f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, contactCooldown);
            }
        }

        // 設定検証
        protected virtual void OnValidate()
        {
            if (contactCooldown < 0f)
                contactCooldown = 0f;

            if (flashDuration <= 0f)
                flashDuration = 0.1f;
        }

        // クリーンアップ
        protected virtual void OnDestroy()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            playerContactTimes.Clear();
        }
    }
}