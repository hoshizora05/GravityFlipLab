using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// スパイク障害物 - リスポーン統合対応版
    /// プレイヤーとの接触時に即座にリスポーン処理を実行
    /// </summary>
    public class SpikeObstacle : RespawnEnabledObstacle
    {
        [Header("Spike Settings")]
        public bool pointsUp = true;
        public float spikeHeight = 1f;
        public bool retractable = false;
        public float retractDelay = 2f;


        private bool isExtended = true;
        private Coroutine retractCoroutine;
        private float lastContactTime = -1f;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;

        public override void StartObstacle()
        {
            base.StartObstacle();
            InitializeSpike();
        }

        private void InitializeSpike()
        {
            // スプライトレンダラーの取得
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            // コライダーがない場合は追加
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector2(1f, spikeHeight);
            }
            else
            {
                collider.isTrigger = true;
            }

            // 危険地帯として設定
            if (gameObject.layer == 0) // Default layer
            {
                gameObject.layer = LayerMask.NameToLayer("Obstacles");
            }
        }

        protected override void OnObstacleStart()
        {
            if (retractable)
            {
                StartRetractCycle();
            }
        }

        protected override void OnObstacleStop()
        {
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
                retractCoroutine = null;
            }
        }

        private void StartRetractCycle()
        {
            if (retractCoroutine != null)
                StopCoroutine(retractCoroutine);
            retractCoroutine = StartCoroutine(RetractCycle());
        }

        private IEnumerator RetractCycle()
        {
            while (isActive)
            {
                yield return new WaitForSeconds(retractDelay);
                ToggleSpikes();
                yield return new WaitForSeconds(retractDelay);
                ToggleSpikes();
            }
        }

        private void ToggleSpikes()
        {
            isExtended = !isExtended;
            StartCoroutine(AnimateSpikes());
        }

        private IEnumerator AnimateSpikes()
        {
            float startScale = transform.localScale.y;
            float targetScale = isExtended ? 1f : 0.1f;
            float duration = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                Vector3 scale = transform.localScale;
                scale.y = Mathf.Lerp(startScale, targetScale, t);
                transform.localScale = scale;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Vector3 finalScale = transform.localScale;
            finalScale.y = targetScale;
            transform.localScale = finalScale;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.collider);
        }

        /// <summary>
        /// プレイヤーとの接触処理
        /// </summary>
        private void HandleContact(Collider2D other)
        {
            // スパイクが格納されている場合はダメージなし
            if (!isExtended) return;

            // プレイヤーかどうかチェック
            if (!other.CompareTag("Player")) return;

            // 接触クールダウンチェック
            if (Time.time - lastContactTime < contactCooldown) return;

            // 対象の有効性チェック
            if (!IsTargetValid(other.gameObject)) return;

            // プレイヤーコンポーネントの取得
            var playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;

            // 無敵状態のチェック（バイパスオプション考慮）
            if (!bypassInvulnerability && playerController.stats.isInvincible)
            {
                return;
            }

            lastContactTime = Time.time;

            // 接触処理実行
            ExecutePlayerContact(other.gameObject, playerController);
        }

        /// <summary>
        /// プレイヤー接触時の処理実行
        /// </summary>
        private void ExecutePlayerContact(GameObject player, PlayerController playerController)
        {
            // ビジュアルエフェクト
            if (enableContactEffect)
            {
                StartCoroutine(ContactFlashEffect());
            }

            // 障害物トリガーイベント
            TriggerObstacle();

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
        /// 統合リスポーンシステムによる処理
        /// </summary>
        private void HandleIntegratedRespawn(GameObject player)
        {
            var respawnIntegration = player.GetComponent<RespawnIntegration>();
            if (respawnIntegration != null)
            {
                // 即座にリスポーンを実行
                respawnIntegration.TriggerInstantRespawn();

                if (Debug.isDebugBuild)
                    Debug.Log($"SpikeObstacle: Triggered instant respawn for player at {transform.position}");
            }
            else
            {
                Debug.LogWarning($"SpikeObstacle: RespawnIntegration component not found on player. Using fallback damage system.");

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

            if (Debug.isDebugBuild)
                Debug.Log($"SpikeObstacle: Dealt damage to player at {transform.position}");
        }

        /// <summary>
        /// 接触時のフラッシュエフェクト
        /// </summary>
        private IEnumerator ContactFlashEffect()
        {
            if (spriteRenderer == null) yield break;

            // 危険色に変更
            spriteRenderer.color = dangerColor;

            yield return new WaitForSeconds(flashDuration);

            // 元の色に戻す
            spriteRenderer.color = originalColor;
        }

        /// <summary>
        /// スパイクの設定変更
        /// </summary>
        public void SetSpikeConfiguration(bool extended, bool retract, float delay)
        {
            isExtended = extended;
            retractable = retract;
            retractDelay = delay;

            if (retractable && isActive)
            {
                StartRetractCycle();
            }
            else if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
                retractCoroutine = null;
            }
        }

        /// <summary>
        /// リスポーン統合設定の変更
        /// </summary>
        public void SetRespawnIntegration(bool useIntegrated, bool bypassInvuln, float cooldown)
        {
            useIntegratedRespawn = useIntegrated;
            bypassInvulnerability = bypassInvuln;
            contactCooldown = cooldown;
        }

        /// <summary>
        /// 手動でスパイクを作動させる
        /// </summary>
        public void ManualTrigger()
        {
            if (!isActive) return;

            // 範囲内のプレイヤーを検索
            Collider2D[] colliders = Physics2D.OverlapBoxAll(
                transform.position,
                GetComponent<Collider2D>().bounds.size,
                0f,
                LayerMask.GetMask("Player")
            );

            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Player"))
                {
                    HandleContact(collider);
                    break; // 一人のプレイヤーのみ処理
                }
            }
        }

        // デバッグ用の可視化
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying) return;

            // スパイクの状態を色で表示
            if (isExtended)
            {
                Gizmos.color = isActive ? Color.red : Color.gray;
            }
            else
            {
                Gizmos.color = Color.yellow; // 格納状態
            }

            // スパイクの範囲を表示
            Vector3 size = GetComponent<Collider2D>()?.bounds.size ?? Vector3.one;
            Gizmos.DrawWireCube(transform.position, size);

            // スパイクの向きを表示
            if (pointsUp)
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * spikeHeight);
            }
            else
            {
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * spikeHeight);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            // 詳細情報の表示
            Gizmos.color = Color.cyan;

            // 接触範囲の表示
            if (GetComponent<Collider2D>() != null)
            {
                Bounds bounds = GetComponent<Collider2D>().bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size * 1.2f);
            }

            // 最後の接触時間の可視化
            if (Application.isPlaying && lastContactTime > 0 && Time.time - lastContactTime < 2f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);
            }
        }

        // エディター用の設定検証
        protected override void OnValidate()
        {
            if (spikeHeight <= 0f)
                spikeHeight = 1f;

            if (retractDelay <= 0f)
                retractDelay = 2f;

            if (contactCooldown < 0f)
                contactCooldown = 0f;

            if (flashDuration <= 0f)
                flashDuration = 0.1f;
        }

        // パブリック API
        public bool IsExtended => isExtended;
        public bool IsRetractable => retractable;

        public void SetDangerColor(Color color)
        {
            dangerColor = color;
        }

        public void SetContactCooldown(float cooldown)
        {
            contactCooldown = Mathf.Max(0f, cooldown);
        }

        public void ForceExtend()
        {
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
                retractCoroutine = null;
            }
            isExtended = true;
            transform.localScale = new Vector3(transform.localScale.x, 1f, transform.localScale.z);
        }

        public void ForceRetract()
        {
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
                retractCoroutine = null;
            }
            isExtended = false;
            transform.localScale = new Vector3(transform.localScale.x, 0.1f, transform.localScale.z);
        }
    }
}