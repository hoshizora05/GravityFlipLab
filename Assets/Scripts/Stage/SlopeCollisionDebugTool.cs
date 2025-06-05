using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// SlopeObjectの衝突検知問題をデバッグ・修正するツール
    /// </summary>
    public class SlopeCollisionDebugTool : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool enableRealTimeDebug = true;
        public bool showDetailedLogs = true;
        public bool visualizeColliders = true;
        public bool autoFix = true;

        [Header("Target Objects")]
        public SlopeObject targetSlope;
        public GameObject targetPlayer;

        private void Start()
        {
            if (targetSlope == null)
                targetSlope = GetComponent<SlopeObject>();

            if (targetPlayer == null)
                targetPlayer = GameObject.FindGameObjectWithTag("Player");
        }

        /// <summary>
        /// 完全な衝突検知診断を実行
        /// </summary>
        [ContextMenu("Diagnose Collision Issues")]
        public void DiagnoseCollisionIssues()
        {
            Debug.Log("=== Slope Collision Diagnosis ===");

            if (targetSlope == null)
            {
                Debug.LogError("No target slope specified!");
                return;
            }

            if (targetPlayer == null)
            {
                Debug.LogError("No target player found!");
                return;
            }

            // 1. レイヤー設定チェック
            CheckLayerSettings();

            // 2. コライダー設定チェック
            CheckColliderSettings();

            // 3. 物理設定チェック
            CheckPhysicsSettings();

            // 4. ContactFilterチェック
            CheckContactFilter();

            // 5. 位置関係チェック
            CheckPositionAndOverlap();

            // 6. イベント設定チェック
            CheckEventSubscriptions();

            // 7. 自動修正（有効な場合）
            if (autoFix)
            {
                AttemptAutoFix();
            }
        }

        private void CheckLayerSettings()
        {
            Debug.Log("--- Layer Settings Check ---");

            // プレイヤーのレイヤー
            int playerLayer = targetPlayer.layer;
            string playerLayerName = LayerMask.LayerToName(playerLayer);
            Debug.Log($"Player Layer: {playerLayer} ({playerLayerName})");

            // SlopeObjectのaffectedLayersチェック
            LayerMask affectedLayers = targetSlope.affectedLayers;
            bool playerLayerIncluded = (affectedLayers.value & (1 << playerLayer)) != 0;

            Debug.Log($"Slope affectedLayers: {affectedLayers.value} (binary: {System.Convert.ToString(affectedLayers.value, 2)})");
            Debug.Log($"Player layer included in affectedLayers: {playerLayerIncluded}");

            if (!playerLayerIncluded)
            {
                Debug.LogError($"❌ ISSUE: Player layer ({playerLayer}) is NOT included in slope's affectedLayers!");
                Debug.Log($"💡 FIX: Add layer {playerLayer} to SlopeObject.affectedLayers");
            }
            else
            {
                Debug.Log("✅ Layer settings are correct");
            }
        }

        private void CheckColliderSettings()
        {
            Debug.Log("--- Collider Settings Check ---");

            // TriggerColliderチェック
            if (targetSlope.triggerCollider == null)
            {
                Debug.LogError("❌ ISSUE: TriggerCollider is null!");
                return;
            }

            var triggerCollider = targetSlope.triggerCollider;
            Debug.Log($"Trigger Collider:");
            Debug.Log($"  - Enabled: {triggerCollider.enabled}");
            Debug.Log($"  - IsTrigger: {triggerCollider.isTrigger}");
            Debug.Log($"  - Size: {triggerCollider.size}");
            Debug.Log($"  - Offset: {triggerCollider.offset}");
            Debug.Log($"  - Bounds: {triggerCollider.bounds}");

            if (!triggerCollider.enabled)
            {
                Debug.LogError("❌ ISSUE: Trigger collider is disabled!");
            }

            if (!triggerCollider.isTrigger)
            {
                Debug.LogError("❌ ISSUE: BoxCollider2D should be a trigger!");
            }

            // プレイヤーのコライダーチェック
            var playerCollider = targetPlayer.GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                Debug.LogError("❌ ISSUE: Player has no Collider2D!");
                return;
            }

            Debug.Log($"Player Collider:");
            Debug.Log($"  - Type: {playerCollider.GetType().Name}");
            Debug.Log($"  - Enabled: {playerCollider.enabled}");
            Debug.Log($"  - IsTrigger: {playerCollider.isTrigger}");
            Debug.Log($"  - Bounds: {playerCollider.bounds}");

            if (!playerCollider.enabled)
            {
                Debug.LogError("❌ ISSUE: Player collider is disabled!");
            }
        }

        private void CheckPhysicsSettings()
        {
            Debug.Log("--- Physics Settings Check ---");

            // プレイヤーのRigidbody2Dチェック
            var playerRb = targetPlayer.GetComponent<Rigidbody2D>();
            if (playerRb == null)
            {
                Debug.LogError("❌ ISSUE: Player has no Rigidbody2D!");
                return;
            }

            Debug.Log($"Player Rigidbody2D:");
            Debug.Log($"  - Body Type: {playerRb.bodyType}");
            Debug.Log($"  - Simulated: {playerRb.simulated}");
            Debug.Log($"  - Position: {playerRb.position}");
            Debug.Log($"  - Velocity: {playerRb.linearVelocity}");

            if (playerRb.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogWarning("⚠️ WARNING: Player Rigidbody2D is not Dynamic!");
            }

            if (!playerRb.simulated)
            {
                Debug.LogError("❌ ISSUE: Player Rigidbody2D simulation is disabled!");
            }

            // Physics2D設定チェック
            Debug.Log($"Physics2D Settings:");
            Debug.Log($"  - Gravity: {Physics2D.gravity}");
            Debug.Log($"  - Auto Simulation: {Physics2D.simulationMode}");
            Debug.Log($"  - Queries Hit Triggers: {Physics2D.queriesHitTriggers}");

            if (!Physics2D.queriesHitTriggers)
            {
                Debug.LogError("❌ ISSUE: Physics2D.queriesHitTriggers is disabled!");
            }
        }

        private void CheckContactFilter()
        {
            Debug.Log("--- ContactFilter Check ---");

            var contactFilter = targetSlope.contactFilter;
            Debug.Log($"ContactFilter:");
            Debug.Log($"  - useLayerMask: {contactFilter.useLayerMask}");
            Debug.Log($"  - layerMask: {contactFilter.layerMask.value}");
            Debug.Log($"  - useTriggers: {contactFilter.useTriggers}");

            int playerLayer = targetPlayer.layer;
            bool playerLayerInFilter = (contactFilter.layerMask.value & (1 << playerLayer)) != 0;
            Debug.Log($"  - Player layer in filter: {playerLayerInFilter}");

            if (contactFilter.useLayerMask && !playerLayerInFilter)
            {
                Debug.LogError($"❌ ISSUE: Player layer ({playerLayer}) not in ContactFilter!");
            }

            if (contactFilter.useTriggers)
            {
                Debug.LogWarning("⚠️ WARNING: ContactFilter.useTriggers is enabled (may interfere)");
            }
        }

        private void CheckPositionAndOverlap()
        {
            Debug.Log("--- Position and Overlap Check ---");

            var triggerCollider = targetSlope.triggerCollider;
            var playerCollider = targetPlayer.GetComponent<Collider2D>();

            Vector3 slopePos = targetSlope.transform.position;
            Vector3 playerPos = targetPlayer.transform.position;
            float distance = Vector3.Distance(slopePos, playerPos);

            Debug.Log($"Positions:");
            Debug.Log($"  - Slope: {slopePos}");
            Debug.Log($"  - Player: {playerPos}");
            Debug.Log($"  - Distance: {distance:F2}");

            // 手動でOverlapテスト
            bool manualOverlap = triggerCollider.OverlapPoint(playerPos);
            Debug.Log($"Manual overlap test (point): {manualOverlap}");

            bool colliderOverlap = triggerCollider.bounds.Intersects(playerCollider.bounds);
            Debug.Log($"Bounds intersection: {colliderOverlap}");

            // Physics2D.OverlapColliderテスト
            Collider2D[] results = new Collider2D[1];
            ContactFilter2D testFilter = new ContactFilter2D();
            testFilter.SetLayerMask(targetSlope.affectedLayers);
            testFilter.useLayerMask = true;
            testFilter.useTriggers = false;

            int overlapCount = Physics2D.OverlapCollider(triggerCollider, testFilter, results);
            Debug.Log($"Physics2D.OverlapCollider result: {overlapCount}");

            if (overlapCount > 0)
            {
                Debug.Log($"  - Found collider: {results[0].name}");
            }
        }

        private void CheckEventSubscriptions()
        {
            Debug.Log("--- Event Subscription Check ---");

            // MonoBehaviourのイベントメソッドが有効かチェック
            bool hasOnTriggerEnter = targetSlope.GetType().GetMethod("OnTriggerEnter2D") != null;
            bool hasOnTriggerExit = targetSlope.GetType().GetMethod("OnTriggerExit2D") != null;

            Debug.Log($"OnTriggerEnter2D method exists: {hasOnTriggerEnter}");
            Debug.Log($"OnTriggerExit2D method exists: {hasOnTriggerExit}");

            // SlopeObjectが有効かチェック
            Debug.Log($"SlopeObject enabled: {targetSlope.enabled}");
            Debug.Log($"SlopeObject gameObject active: {targetSlope.gameObject.activeInHierarchy}");
        }

        private void AttemptAutoFix()
        {
            Debug.Log("--- Attempting Auto Fix ---");

            // 1. レイヤー設定の修正
            int playerLayer = targetPlayer.layer;
            if ((targetSlope.affectedLayers.value & (1 << playerLayer)) == 0)
            {
                targetSlope.affectedLayers |= (1 << playerLayer);
                Debug.Log($"✅ Added player layer {playerLayer} to affectedLayers");
            }

            // 2. ContactFilterの修正
            var contactFilter = targetSlope.contactFilter;
            contactFilter.SetLayerMask(targetSlope.affectedLayers);
            contactFilter.useLayerMask = true;
            contactFilter.useTriggers = false;

            // リフレクションでcontactFilterを更新
            var contactFilterField = typeof(SlopeObject).GetField("contactFilter",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (contactFilterField != null)
            {
                contactFilterField.SetValue(targetSlope, contactFilter);
                Debug.Log("✅ Updated ContactFilter settings");
            }

            // 3. コライダー設定の修正
            if (targetSlope.triggerCollider != null)
            {
                targetSlope.triggerCollider.enabled = true;
                targetSlope.triggerCollider.isTrigger = true;
                Debug.Log("✅ Fixed trigger collider settings");
            }

            // 4. Physics2D設定の修正
            if (!Physics2D.queriesHitTriggers)
            {
                Physics2D.queriesHitTriggers = true;
                Debug.Log("✅ Enabled Physics2D.queriesHitTriggers");
            }

            // 5. プレイヤーのRigidbody2D修正
            var playerRb = targetPlayer.GetComponent<Rigidbody2D>();
            if (playerRb != null && !playerRb.simulated)
            {
                playerRb.simulated = true;
                Debug.Log("✅ Enabled player Rigidbody2D simulation");
            }

            Debug.Log("Auto fix completed!");
        }

        /// <summary>
        /// リアルタイムでの衝突状態監視
        /// </summary>
        private void Update()
        {
            if (!enableRealTimeDebug || targetSlope == null || targetPlayer == null) return;

            // 1秒ごとにチェック
            if (Time.time % 1f < Time.deltaTime)
            {
                CheckRealTimeCollision();
            }
        }

        private void CheckRealTimeCollision()
        {
            if (targetSlope.triggerCollider == null) return;

            var playerCollider = targetPlayer.GetComponent<Collider2D>();
            if (playerCollider == null) return;

            // 距離チェック
            float distance = Vector3.Distance(targetSlope.transform.position, targetPlayer.transform.position);

            // Boundsの重複チェック
            bool boundsOverlap = targetSlope.triggerCollider.bounds.Intersects(playerCollider.bounds);

            // Physics2Dでの検出チェック
            Collider2D[] results = new Collider2D[1];
            int overlapCount = Physics2D.OverlapCollider(targetSlope.triggerCollider, targetSlope.contactFilter, results);

            if (showDetailedLogs && (distance < 5f || boundsOverlap))
            {
                Debug.Log($"Real-time check - Distance: {distance:F2}, Bounds overlap: {boundsOverlap}, Physics overlap: {overlapCount}");
            }
        }

        /// <summary>
        /// ビジュアルデバッグ表示
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!visualizeColliders) return;

            // SlopeObjectの表示
            if (targetSlope != null && targetSlope.triggerCollider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(targetSlope.triggerCollider.bounds.center, targetSlope.triggerCollider.bounds.size);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(targetSlope.transform.position, 0.2f);
            }

            // プレイヤーの表示
            if (targetPlayer != null)
            {
                var playerCollider = targetPlayer.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireCube(playerCollider.bounds.center, playerCollider.bounds.size);
                }

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targetPlayer.transform.position, 0.2f);
            }

            // 距離線の表示
            if (targetSlope != null && targetPlayer != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(targetSlope.transform.position, targetPlayer.transform.position);
            }
        }

        /// <summary>
        /// 手動でのテスト衝突実行
        /// </summary>
        [ContextMenu("Force Test Collision")]
        public void ForceTestCollision()
        {
            if (targetSlope == null || targetPlayer == null) return;

            Debug.Log("=== Force Test Collision ===");

            // 手動でOnTriggerEnter2Dを呼び出し
            var playerCollider = targetPlayer.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                targetSlope.SendMessage("OnTriggerEnter2D", playerCollider, SendMessageOptions.DontRequireReceiver);
                Debug.Log("Manually called OnTriggerEnter2D");
            }
        }
    }
}