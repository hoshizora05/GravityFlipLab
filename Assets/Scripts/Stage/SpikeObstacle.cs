using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// スパイク障害物 - RespawnEnabledObstacle継承版
    /// 統合リスポーン機能を持つベースクラスから継承し、スパイク固有の機能を実装
    /// </summary>
    public class SpikeObstacle : RespawnEnabledObstacle
    {
        [Header("Spike Settings")]
        public bool pointsUp = true;
        public float spikeHeight = 1f;
        public bool retractable = false;
        public float retractDelay = 2f;

        [Header("Spike Visual")]
        public bool animateOnContact = true;
        public float spikeAnimationSpeed = 2f;

        // スパイク固有の状態
        private bool isExtended = true;
        private Coroutine retractCoroutine;
        private Coroutine spikeAnimationCoroutine;

        // 初期化フラグ
        private bool spikeInitialized = false;

        public override void StartObstacle()
        {
            base.StartObstacle();

            if (!spikeInitialized)
            {
                InitializeSpikeSpecific();
            }
        }

        /// <summary>
        /// スパイク固有の初期化
        /// </summary>
        private void InitializeSpikeSpecific()
        {
            if (spikeInitialized) return;

            // コライダーの設定（ベースクラスで基本設定済み、スパイク固有の調整）
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector2(1f, spikeHeight);
            }
            else
            {
                // 既存のコライダーをスパイクサイズに調整
                if (collider is BoxCollider2D boxCollider)
                {
                    Vector2 size = boxCollider.size;
                    size.y = spikeHeight;
                    boxCollider.size = size;
                }
            }

            spikeInitialized = true;
        }

        protected override void OnObstacleStart()
        {
            base.OnObstacleStart();

            if (retractable)
            {
                StartRetractCycle();
            }
        }

        protected override void OnObstacleStop()
        {
            base.OnObstacleStop();

            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
                retractCoroutine = null;
            }

            if (spikeAnimationCoroutine != null)
            {
                StopCoroutine(spikeAnimationCoroutine);
                spikeAnimationCoroutine = null;
            }
        }

        // Unity衝突イベントをベースクラスに委譲
        private void OnTriggerEnter2D(Collider2D other)
        {
            // スパイクが格納されている場合は接触判定しない
            if (!isExtended) return;

            // ベースクラスの統合接触処理を使用
            HandlePlayerContact(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // スパイクが格納されている場合は接触判定しない
            if (!isExtended) return;

            // ベースクラスの統合接触処理を使用
            HandlePlayerContact(collision.collider);
        }

        /// <summary>
        /// プレイヤー接触検出時の処理（ベースクラスからの呼び出し）
        /// </summary>
        protected override void OnPlayerContactDetected(GameObject player, PlayerController playerController)
        {
            // スパイク固有の接触処理
            if (animateOnContact)
            {
                PlaySpikeContactAnimation();
            }

            if (logContactEvents)
                Debug.Log($"SpikeObstacle: Player contacted spike at {transform.position}, extended: {isExtended}");
        }

        /// <summary>
        /// リスポーンがトリガーされた時の処理
        /// </summary>
        protected override void OnRespawnTriggered(GameObject player)
        {
            // スパイク接触でリスポーンした時の処理
            if (logContactEvents)
                Debug.Log($"SpikeObstacle: Player respawn triggered by spike at {transform.position}");
        }

        /// <summary>
        /// 従来のダメージシステムが使用された時の処理
        /// </summary>
        protected override void OnTraditionalDamageDealt(GameObject player, PlayerController playerController)
        {
            // 従来システムでダメージを与えた時の処理
            if (logContactEvents)
                Debug.Log($"SpikeObstacle: Traditional damage dealt by spike at {transform.position}");
        }

        /// <summary>
        /// スパイク固有の接触エフェクト
        /// </summary>
        protected override void PlayCustomContactEffect()
        {
            // スパイク固有のエフェクト処理
            if (animateOnContact)
            {
                PlaySpikeContactAnimation();
            }
        }

        /// <summary>
        /// スパイク接触時のアニメーション
        /// </summary>
        private void PlaySpikeContactAnimation()
        {
            if (spikeAnimationCoroutine != null)
            {
                StopCoroutine(spikeAnimationCoroutine);
            }

            spikeAnimationCoroutine = StartCoroutine(SpikeContactAnimationCoroutine());
        }

        /// <summary>
        /// スパイク接触アニメーションのコルーチン
        /// </summary>
        private IEnumerator SpikeContactAnimationCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.2f; // 20%拡大

            float duration = 1f / spikeAnimationSpeed;
            float elapsedTime = 0f;

            // 拡大フェーズ
            while (elapsedTime < duration * 0.3f)
            {
                float t = elapsedTime / (duration * 0.3f);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 縮小フェーズ
            elapsedTime = 0f;
            while (elapsedTime < duration * 0.7f)
            {
                float t = elapsedTime / (duration * 0.7f);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
            spikeAnimationCoroutine = null;
        }

        #region Retractable Spike Logic

        /// <summary>
        /// 格納可能スパイクのサイクル開始
        /// </summary>
        private void StartRetractCycle()
        {
            if (retractCoroutine != null)
                StopCoroutine(retractCoroutine);

            retractCoroutine = StartCoroutine(RetractCycle());
        }

        /// <summary>
        /// 格納サイクルのコルーチン
        /// </summary>
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

        /// <summary>
        /// スパイクの展開/格納を切り替え
        /// </summary>
        private void ToggleSpikes()
        {
            isExtended = !isExtended;
            StartCoroutine(AnimateSpikes());
        }

        /// <summary>
        /// スパイクの展開/格納アニメーション
        /// </summary>
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

        #endregion

        #region Public API

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
        /// 手動でスパイクを作動させる
        /// </summary>
        public void ManualTrigger()
        {
            if (!isActive || !isExtended) return;

            // ベースクラスの手動トリガー機能を使用
            TriggerManualHazardCheck();
        }

        /// <summary>
        /// スパイクを強制的に展開
        /// </summary>
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

        /// <summary>
        /// スパイクを強制的に格納
        /// </summary>
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

        /// <summary>
        /// スパイクの高さを設定
        /// </summary>
        public void SetSpikeHeight(float height)
        {
            spikeHeight = Mathf.Max(0.1f, height);

            // コライダーサイズも更新
            Collider2D collider = GetComponent<Collider2D>();
            if (collider is BoxCollider2D boxCollider)
            {
                Vector2 size = boxCollider.size;
                size.y = spikeHeight;
                boxCollider.size = size;
            }
        }

        // プロパティ
        public bool IsExtended => isExtended;
        public bool IsRetractable => retractable;
        public float SpikeHeight => spikeHeight;
        public bool PointsUp => pointsUp;

        #endregion

        #region Debug and Visualization

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
            Vector3 spikeDirection = pointsUp ? Vector3.up : Vector3.down;
            Gizmos.DrawLine(transform.position, transform.position + spikeDirection * spikeHeight);
            Gizmos.DrawWireSphere(transform.position + spikeDirection * spikeHeight, 0.1f);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // スパイク固有の詳細情報
            Gizmos.color = Color.cyan;

            // 接触範囲の表示
            if (GetComponent<Collider2D>() != null)
            {
                Bounds bounds = GetComponent<Collider2D>().bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size * 1.2f);
            }

            // 格納可能スパイクの範囲表示
            if (retractable)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position, Vector3.one * retractDelay * 0.5f);
            }

            // スパイクの高さ表示
            Gizmos.color = Color.white;
            Vector3 heightStart = transform.position + Vector3.left * 0.7f;
            Vector3 heightEnd = heightStart + (pointsUp ? Vector3.up : Vector3.down) * spikeHeight;
            Gizmos.DrawLine(heightStart, heightEnd);

            // 高さの数値表示用の球
            Gizmos.DrawWireSphere(heightEnd, 0.05f);
        }

        #endregion

        #region Validation and Configuration

        protected override void OnValidate()
        {
            base.OnValidate();

            // スパイク固有の検証
            if (spikeHeight <= 0f)
                spikeHeight = 1f;

            if (retractDelay <= 0f)
                retractDelay = 2f;

            if (spikeAnimationSpeed <= 0f)
                spikeAnimationSpeed = 2f;
        }

        /// <summary>
        /// スパイク設定の検証
        /// </summary>
        public bool ValidateSpikeConfiguration()
        {
            bool isValid = true;
            List<string> errors = new List<string>();

            if (spikeHeight <= 0f)
            {
                errors.Add("Spike height must be greater than 0");
                isValid = false;
            }

            if (retractable && retractDelay <= 0f)
            {
                errors.Add("Retract delay must be greater than 0 for retractable spikes");
                isValid = false;
            }

            if (GetComponent<Collider2D>() == null)
            {
                errors.Add("Spike obstacle requires a Collider2D component");
                isValid = false;
            }

            if (!isValid)
            {
                Debug.LogError($"SpikeObstacle validation failed on {gameObject.name}: {string.Join(", ", errors)}");
            }

            return isValid;
        }

        /// <summary>
        /// 実行時設定の更新
        /// </summary>
        public void UpdateSpikeSettings(bool useIntegratedRespawn, bool bypassInvulnerability,
                                       float contactCooldown, bool enableEffect, float animSpeed)
        {
            // ベースクラスの設定更新
            SetRespawnConfiguration(useIntegratedRespawn, bypassInvulnerability, contactCooldown);
            SetContactEffectConfiguration(enableEffect, dangerColor, flashDuration);

            // スパイク固有の設定更新
            animateOnContact = enableEffect;
            spikeAnimationSpeed = animSpeed;
        }

        #endregion

        #region Cleanup

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // スパイク固有のコルーチンクリーンアップ
            if (retractCoroutine != null)
            {
                StopCoroutine(retractCoroutine);
            }

            if (spikeAnimationCoroutine != null)
            {
                StopCoroutine(spikeAnimationCoroutine);
            }
        }

        #endregion
    }
}