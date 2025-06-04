using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class GravityAffectedObject : MonoBehaviour
    {
        [Header("Gravity Settings")]
        public float gravityScale = 1f;
        public bool useCustomGravity = true;
        public bool smoothGravityTransition = true;
        public float transitionSpeed = 5f;

        [Header("Original State")]
        public Vector2 originalGravityDirection = Vector2.down;
        public float originalGravityScale = 1f;
        public bool storeOriginalOnStart = true;

        [Header("Reset Behavior")]
        public bool respondToSystemReset = true;
        public bool smoothReset = true;
        public float resetTransitionSpeed = 8f;
        public bool preserveVelocityOnReset = false;

        [Header("Physics")]
        public bool maintainInertia = true;
        public float inertiaDecay = 0.9f;
        public float maxVelocityChange = 20f;

        private Rigidbody2D rb2d;
        private Vector2 currentGravity;
        private Vector2 targetGravity;
        private LocalGravityZone currentZone;
        private List<LocalGravityZone> activeZones = new List<LocalGravityZone>();

        // Original gravity scale for restoration
        private float originalRigidbodyGravityScale;
        private bool isResetting = false;
        private Coroutine resetCoroutine;

        // Events
        public System.Action OnGravityReset;

        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                Debug.LogError($"GravityAffectedObject requires Rigidbody2D component on {gameObject.name}");
                enabled = false;
                return;
            }

            // Store original Rigidbody2D gravity scale
            originalRigidbodyGravityScale = rb2d.gravityScale;

            // Store original gravity settings if enabled
            if (storeOriginalOnStart)
            {
                StoreOriginalGravityState();
            }
        }

        private void Start()
        {
            // Initialize with global gravity
            if (GravitySystem.Instance != null)
            {
                currentGravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;
                targetGravity = currentGravity;

                // Subscribe to system reset events
                if (respondToSystemReset)
                {
                    GravitySystem.OnGravityResetStarted += OnSystemResetStarted;
                    GravitySystem.OnGravityResetCompleted += OnSystemResetCompleted;
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGravityResetStarted -= OnSystemResetStarted;
                GravitySystem.OnGravityResetCompleted -= OnSystemResetCompleted;
            }

            // Stop any running coroutines
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }
        }

        /// <summary>
        /// オリジナルの重力状態を保存
        /// </summary>
        private void StoreOriginalGravityState()
        {
            // GravitySystemから取得するか、デフォルト値を使用
            if (GravitySystem.Instance != null)
            {
                originalGravityDirection = GravitySystem.Instance.OriginalGravityDirection;
            }
            else if (originalGravityDirection == Vector2.zero)
            {
                originalGravityDirection = Vector2.down;
            }

            if (originalGravityScale <= 0f)
            {
                originalGravityScale = 1f;
            }
        }

        private void FixedUpdate()
        {
            if (!useCustomGravity || isResetting) return;

            UpdateGravity();
            ApplyGravity();
        }

        private void UpdateGravity()
        {
            // Determine target gravity based on current zones
            Vector2 newTargetGravity = CalculateEffectiveGravity();

            if (smoothGravityTransition)
            {
                targetGravity = Vector2.Lerp(targetGravity, newTargetGravity, Time.fixedDeltaTime * transitionSpeed);
            }
            else
            {
                targetGravity = newTargetGravity;
            }
        }

        private Vector2 CalculateEffectiveGravity()
        {
            if (activeZones.Count == 0)
            {
                // Use global gravity
                return GravitySystem.Instance.GetGravityAtPosition(transform.position);
            }

            // Use the most recent zone (highest priority)
            LocalGravityZone priorityZone = activeZones[activeZones.Count - 1];
            return priorityZone.GetGravityAtPosition(transform.position);
        }

        private void ApplyGravity()
        {
            if (rb2d == null) return;

            // Disable Unity's automatic gravity
            rb2d.gravityScale = 0f;

            // Apply custom gravity
            Vector2 gravityForce = targetGravity * gravityScale;

            // Limit velocity change to prevent extreme accelerations
            Vector2 velocityChange = gravityForce * Time.fixedDeltaTime;
            if (velocityChange.magnitude > maxVelocityChange)
            {
                velocityChange = velocityChange.normalized * maxVelocityChange;
            }

            if (maintainInertia)
            {
                // Gradually apply gravity while maintaining existing velocity
                Vector2 newVelocity = rb2d.linearVelocity + velocityChange;
                rb2d.linearVelocity = Vector2.Lerp(rb2d.linearVelocity, newVelocity, inertiaDecay);
            }
            else
            {
                // Direct gravity application
                rb2d.AddForce(gravityForce, ForceMode2D.Force);
            }

            currentGravity = targetGravity;
        }

        /// <summary>
        /// 重力を元の状態にリセット（ローカル）
        /// </summary>
        public void ResetToOriginalGravity()
        {
            if (isResetting) return;

            if (smoothReset)
            {
                StartSmoothReset();
            }
            else
            {
                // InstantReset()を修正版に変更
                InstantResetMinimal();
            }
        }
        /// <summary>
        /// 最小限の即座リセット（useCustomGravityは保持）
        /// </summary>
        private void InstantResetMinimal()
        {
            // 重力方向と強度のみリセット
            currentGravity = originalGravityDirection * originalGravityScale * 9.81f;
            targetGravity = currentGravity;

            // useCustomGravityは変更しない（保持）
            // rb2d.gravityScaleも現在の設定に合わせて調整
            if (useCustomGravity)
            {
                rb2d.gravityScale = 0f;
            }
            // else は既存の値を保持

            // 速度調整（必要最小限）
            if (!preserveVelocityOnReset && rb2d != null)
            {
                Vector2 velocity = rb2d.linearVelocity;
                Vector2 currentGravityDir = currentGravity.normalized;
                Vector2 originalGravityDir = originalGravityDirection.normalized;

                if (Vector2.Dot(currentGravityDir, originalGravityDir) < 0.9f)
                {
                    Vector2 gravityVelocity = Vector3.Project(velocity, originalGravityDir);
                    velocity -= gravityVelocity;
                    rb2d.linearVelocity = velocity;
                }
            }

            // 他のコンポーネントに通知
            NotifyOtherComponents();

            // イベント発火
            OnGravityReset?.Invoke();

            Debug.Log("GravityAffectedObject: Minimal reset completed (useCustomGravity preserved)");
        }

        /// <summary>
        /// GravitySystemからの重力リセット通知を受信
        /// </summary>
        public void OnGravitySystemReset()
        {
            if (!respondToSystemReset) return;

            // システムレベルのリセットでは、グローバル重力に合わせる
            if (GravitySystem.Instance != null)
            {
                originalGravityDirection = GravitySystem.Instance.OriginalGravityDirection;
                ResetToOriginalGravity();
            }
        }

        /// <summary>
        /// 即座の重力リセット
        /// </summary>
        private void InstantReset()
        {
            // Rigidbody2Dの設定は現在のuseCustomGravityに応じて設定
            if (useCustomGravity)
            {
                rb2d.gravityScale = 0f; // カスタム重力使用時
            }
            else
            {
                rb2d.gravityScale = originalRigidbodyGravityScale; // Unity標準重力使用時
            }

            // 速度リセット（オプション）
            if (!preserveVelocityOnReset && rb2d != null)
            {
                Vector2 velocity = rb2d.linearVelocity;

                // 重力方向が変わった場合、垂直速度をリセット
                Vector2 currentGravityDir = currentGravity.normalized;
                Vector2 originalGravityDir = originalGravityDirection.normalized;

                if (Vector2.Dot(currentGravityDir, originalGravityDir) < 0.9f)
                {
                    // 重力方向に沿った速度成分をリセット
                    Vector2 gravityVelocity = Vector3.Project(velocity, originalGravityDir);
                    velocity -= gravityVelocity;
                    rb2d.linearVelocity = velocity;
                }
            }

            // 現在の重力状態を更新
            currentGravity = originalGravityDirection * originalGravityScale * 9.81f;
            targetGravity = currentGravity;

            // 他のコンポーネントに通知
            NotifyOtherComponents();

            // イベント発火
            OnGravityReset?.Invoke();
        }

        /// <summary>
        /// スムーズな重力リセット開始
        /// </summary>
        private void StartSmoothReset()
        {
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }

            resetCoroutine = StartCoroutine(SmoothResetCoroutine());
        }

        /// <summary>
        /// スムーズなリセットのコルーチン
        /// </summary>
        private IEnumerator SmoothResetCoroutine()
        {
            isResetting = true;

            Vector2 startGravity = currentGravity;
            Vector2 targetResetGravity = originalGravityDirection * originalGravityScale * 9.81f;
            float startGravityScale = rb2d.gravityScale;

            float elapsedTime = 0f;
            float duration = 1f / resetTransitionSpeed;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                // 重力の補間
                currentGravity = Vector2.Lerp(startGravity, targetResetGravity, t);
                targetGravity = currentGravity;

                // Rigidbody gravity scaleの補間（useCustomGravityに応じて）
                if (useCustomGravity)
                {
                    // カスタム重力使用時は0を維持
                    rb2d.gravityScale = 0f;
                }
                else
                {
                    // Unity標準重力使用時は元の値に向かって補間
                    rb2d.gravityScale = Mathf.Lerp(startGravityScale, originalRigidbodyGravityScale, t);
                }

                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // 最終値を確実に設定
            currentGravity = targetResetGravity;
            targetGravity = currentGravity;

            // 最終的なgravityScaleを設定
            if (useCustomGravity)
            {
                rb2d.gravityScale = 0f;
            }
            else
            {
                rb2d.gravityScale = originalRigidbodyGravityScale;
            }

            // 速度調整
            if (!preserveVelocityOnReset)
            {
                AdjustVelocityForReset();
            }

            isResetting = false;
            resetCoroutine = null;

            // 他のコンポーネントに通知
            NotifyOtherComponents();

            // イベント発火
            OnGravityReset?.Invoke();
        }

        /// <summary>
        /// リセット時の速度調整
        /// </summary>
        private void AdjustVelocityForReset()
        {
            if (rb2d == null) return;

            Vector2 velocity = rb2d.linearVelocity;
            Vector2 originalGravityDir = originalGravityDirection.normalized;

            // 重力方向に沿った過度な速度をリセット
            Vector2 gravityVelocity = Vector3.Project(velocity, originalGravityDir);
            if (gravityVelocity.magnitude > 10f) // 閾値を超えた場合のみ調整
            {
                velocity -= gravityVelocity * 0.5f; // 50%削減
                rb2d.linearVelocity = velocity;
            }
        }

        /// <summary>
        /// 他のコンポーネントに重力リセットを通知
        /// </summary>
        private void NotifyOtherComponents()
        {
            // プレイヤーアニメーションコンポーネントに通知
            var playerAnimation = GetComponent<GravityFlipLab.Player.PlayerAnimation>();
            if (playerAnimation != null)
            {
                GravityFlipLab.Player.GravityDirection gravityDir =
                    (originalGravityDirection.y < 0) ?
                    GravityFlipLab.Player.GravityDirection.Down :
                    GravityFlipLab.Player.GravityDirection.Up;
                playerAnimation.UpdateGravityDirection(gravityDir);
            }

            // 地面検出コンポーネントに通知
            var groundDetector = GetComponent<GravityFlipLab.Player.AdvancedGroundDetector>();
            if (groundDetector != null)
            {
                groundDetector.ForceDetection();
            }

            // プレイヤー移動コンポーネントに通知
            var playerMovement = GetComponent<GravityFlipLab.Player.PlayerMovement>();
            if (playerMovement != null)
            {
                GravityFlipLab.Player.GravityDirection gravityDir =
                    (originalGravityDirection.y < 0) ?
                    GravityFlipLab.Player.GravityDirection.Down :
                    GravityFlipLab.Player.GravityDirection.Up;
                playerMovement.OnGravityFlip(gravityDir);
            }
        }

        /// <summary>
        /// システムリセット開始時のイベントハンドラー
        /// </summary>
        private void OnSystemResetStarted(Vector2 targetDirection, float targetStrength)
        {
            // システムレベルのリセットが開始された時の処理
            originalGravityDirection = targetDirection;
            originalGravityScale = targetStrength / 9.81f;
        }

        /// <summary>
        /// システムリセット完了時のイベントハンドラー
        /// </summary>
        private void OnSystemResetCompleted(Vector2 finalDirection, float finalStrength)
        {
            // システムレベルのリセットが完了した時の処理
            if (!isResetting) // 自身がリセット中でなければ同期
            {
                currentGravity = finalDirection * finalStrength;
                targetGravity = currentGravity;
            }
        }

        /// <summary>
        /// オリジナル重力設定の変更
        /// </summary>
        public void SetOriginalGravity(Vector2 direction, float scale = 1f)
        {
            originalGravityDirection = direction.normalized;
            originalGravityScale = scale;
        }

        /// <summary>
        /// 現在の重力をオリジナルとして保存
        /// </summary>
        public void SaveCurrentGravityAsOriginal()
        {
            originalGravityDirection = currentGravity.normalized;
            originalGravityScale = currentGravity.magnitude / 9.81f;
        }

        /// <summary>
        /// 強制的なリセット（緊急時用）
        /// </summary>
        public void ForceResetToOriginalGravity()
        {
            // 進行中のリセットを停止
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
                resetCoroutine = null;
            }

            isResetting = false;

            // 即座にリセット
            InstantReset();
        }

        // Gravity Zone管理（既存機能）
        public void EnterGravityZone(LocalGravityZone zone)
        {
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
                currentZone = zone;
            }
        }

        public void ExitGravityZone(LocalGravityZone zone)
        {
            activeZones.Remove(zone);

            if (currentZone == zone)
            {
                currentZone = activeZones.Count > 0 ? activeZones[activeZones.Count - 1] : null;
            }
        }

        // Public API
        public Vector2 GetCurrentGravity()
        {
            return currentGravity;
        }

        public Vector2 GetOriginalGravity()
        {
            return originalGravityDirection * originalGravityScale * 9.81f;
        }

        public bool IsGravityAtOriginal()
        {
            const float tolerance = 0.01f;
            Vector2 originalGrav = GetOriginalGravity();
            return Vector2.Distance(currentGravity, originalGrav) < tolerance;
        }

        public void SetGravityScale(float scale)
        {
            gravityScale = scale;
        }

        public bool IsResetting => isResetting;

        // Configuration methods
        public void SetResetBehavior(bool smooth, float speed, bool preserveVel)
        {
            smoothReset = smooth;
            resetTransitionSpeed = speed;
            preserveVelocityOnReset = preserveVel;
        }

        public void SetSystemResetResponse(bool respond)
        {
            if (respondToSystemReset != respond)
            {
                respondToSystemReset = respond;

                // Subscribe/unsubscribe from system events
                if (GravitySystem.Instance != null)
                {
                    if (respond)
                    {
                        GravitySystem.OnGravityResetStarted += OnSystemResetStarted;
                        GravitySystem.OnGravityResetCompleted += OnSystemResetCompleted;
                    }
                    else
                    {
                        GravitySystem.OnGravityResetStarted -= OnSystemResetStarted;
                        GravitySystem.OnGravityResetCompleted -= OnSystemResetCompleted;
                    }
                }
            }
        }

        // Debug and validation
        public bool ValidateGravityState()
        {
            bool isValid = true;

            if (rb2d == null)
            {
                Debug.LogError($"GravityAffectedObject on {gameObject.name}: Rigidbody2D is null");
                isValid = false;
            }

            if (originalGravityScale <= 0f)
            {
                Debug.LogWarning($"GravityAffectedObject on {gameObject.name}: Invalid original gravity scale");
                originalGravityScale = 1f;
            }

            if (float.IsNaN(currentGravity.x) || float.IsNaN(currentGravity.y))
            {
                Debug.LogError($"GravityAffectedObject on {gameObject.name}: Invalid gravity values detected");
                currentGravity = originalGravityDirection * originalGravityScale * 9.81f;
                isValid = false;
            }

            return isValid;
        }

        // Visualization and debugging
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Vector3 pos = transform.position;

            // Draw current gravity (green)
            Gizmos.color = Color.green;
            if (currentGravity.magnitude > 0.01f)
            {
                Vector3 gravityDir = currentGravity.normalized * 2f;
                Gizmos.DrawLine(pos, pos + gravityDir);
                Gizmos.DrawWireSphere(pos + gravityDir, 0.1f);
            }

            // Draw target gravity (yellow)
            Gizmos.color = Color.yellow;
            if (targetGravity.magnitude > 0.01f)
            {
                Vector3 targetDir = targetGravity.normalized * 2f;
                Gizmos.DrawLine(pos + Vector3.right * 0.2f, pos + Vector3.right * 0.2f + targetDir);
            }

            // Draw original gravity (blue)
            Gizmos.color = Color.blue;
            Vector3 originalDir = originalGravityDirection * 2f;
            Gizmos.DrawLine(pos + Vector3.left * 0.2f, pos + Vector3.left * 0.2f + originalDir);
            Gizmos.DrawWireSphere(pos + Vector3.left * 0.2f + originalDir, 0.08f);

            // Draw reset status
            if (isResetting)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.5f);
            }

            // Draw gravity zones
            if (activeZones.Count > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(pos + Vector3.up * 0.5f, Vector3.one * 0.3f);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !useCustomGravity) return;

            // Draw simple gravity indicator
            Gizmos.color = Color.green;
            Vector3 gravityDir = currentGravity.normalized * 1f;
            Gizmos.DrawLine(transform.position, transform.position + gravityDir);
        }

        // Validation in editor
        private void OnValidate()
        {
            if (gravityScale <= 0f)
                gravityScale = 1f;

            if (resetTransitionSpeed <= 0f)
                resetTransitionSpeed = 8f;

            if (transitionSpeed <= 0f)
                transitionSpeed = 5f;

            if (originalGravityScale <= 0f)
                originalGravityScale = 1f;

            if (originalGravityDirection == Vector2.zero)
                originalGravityDirection = Vector2.down;
        }

        // Statistics and monitoring
        public GravityObjectStats GetStats()
        {
            return new GravityObjectStats
            {
                currentGravity = currentGravity,
                targetGravity = targetGravity,
                originalGravity = GetOriginalGravity(),
                isResetting = isResetting,
                useCustomGravity = useCustomGravity,
                activeZoneCount = activeZones.Count,
                respondsToSystemReset = respondToSystemReset
            };
        }
        /// <summary>
        /// useCustomGravityの状態を強制的に有効化
        /// </summary>
        public void EnsureCustomGravityEnabled()
        {
            if (!useCustomGravity)
            {
                useCustomGravity = true;

                // Rigidbody2Dの設定も更新
                if (rb2d != null)
                {
                    rb2d.gravityScale = 0f;
                }

                Debug.Log("GravityAffectedObject: useCustomGravity forcibly enabled");
            }
        }
    }

    /// <summary>
    /// 重力オブジェクトの統計情報
    /// </summary>
    [System.Serializable]
    public struct GravityObjectStats
    {
        public Vector2 currentGravity;
        public Vector2 targetGravity;
        public Vector2 originalGravity;
        public bool isResetting;
        public bool useCustomGravity;
        public int activeZoneCount;
        public bool respondsToSystemReset;

        public override string ToString()
        {
            return $"Gravity Stats - Current: {currentGravity}, Target: {targetGravity}, " +
                   $"Original: {originalGravity}, Resetting: {isResetting}, " +
                   $"CustomGravity: {useCustomGravity}, Zones: {activeZoneCount}";
        }
    }
}