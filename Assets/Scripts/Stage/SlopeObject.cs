using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 傾斜オブジェクトの完全版実装
    /// PolygonCollider2D対応、最適化されたコライダー管理
    /// </summary>
    [System.Serializable]
    public class SlopeSettings
    {
        [Header("Slope Configuration")]
        [Range(5f, 60f)]
        public float slopeAngle = 30f;
        public SlopeDirection slopeDirection = SlopeDirection.Ascending;
        [Range(2f, 20f)]
        public float slopeLength = 5f;
        public bool enablePhysicsEffects = true;

        [Header("Speed Effects")]
        [Range(0.1f, 3f)]
        public float speedMultiplier = 1.2f;
        public bool affectGravity = true;
        [Range(0f, 2f)]
        public float gravityRedirection = 0.5f;

        [Header("Collider Settings")]
        public bool autoUpdateColliders = true;
        [Range(0.1f, 2f)]
        public float baseThickness = 0.5f;
        public Vector2 triggerSizeMultiplier = new Vector2(1.2f, 1.5f);

        [Header("Visual")]
        public bool showDebugGizmos = true;
        public Color gizmoColor = Color.yellow;
        public bool rotateVisuals = true;
    }

    public enum SlopeDirection
    {
        Ascending,   // 上り坂
        Descending   // 下り坂
    }

    public class SlopeObject : MonoBehaviour
    {
        [Header("Slope Settings")]
        public SlopeSettings slopeSettings = new SlopeSettings();

        [Header("Collision Detection")]
        public LayerMask affectedLayers = 1;
        public ContactFilter2D contactFilter;

        [Header("Components")]
        public SpriteRenderer slopeRenderer;
        public BoxCollider2D triggerCollider;
        public PolygonCollider2D physicsCollider;

        // Slope physics state
        private Vector2 slopeNormal;
        private Vector2 slopeDirection;
        private List<Rigidbody2D> objectsOnSlope = new List<Rigidbody2D>();

        // Collider update tracking
        private SlopeSettings lastSettings;
        private bool needsColliderUpdate = true;

        // Events
        public System.Action<GameObject> OnObjectEnterSlope;
        public System.Action<GameObject> OnObjectExitSlope;
        public System.Action<SlopeSettings> OnSlopeSettingsChanged;

        // Performance optimization
        private readonly Queue<Rigidbody2D> removeQueue = new Queue<Rigidbody2D>();

        private void Awake()
        {
            InitializeComponents();
            CacheInitialSettings();
        }

        private void Start()
        {
            SetupContactFilter();
            UpdateSlope();
            ValidateSetup();
        }

        private void InitializeComponents()
        {
            // SpriteRendererの取得/追加
            if (slopeRenderer == null)
                slopeRenderer = GetComponent<SpriteRenderer>();

            // 既存のコライダーをチェック
            var existingBoxColliders = GetComponents<BoxCollider2D>();
            var existingPolygonColliders = GetComponents<PolygonCollider2D>();

            // BoxCollider2D（トリガー用）の設定
            if (triggerCollider == null)
            {
                // 既存のトリガーコライダーを探す
                triggerCollider = System.Array.Find(existingBoxColliders, c => c.isTrigger);

                if (triggerCollider == null)
                {
                    triggerCollider = gameObject.AddComponent<BoxCollider2D>();
                    triggerCollider.isTrigger = true;
                }
            }

            // PolygonCollider2D（物理用）の設定
            if (physicsCollider == null)
            {
                // 既存の物理コライダーを探す
                physicsCollider = System.Array.Find(existingPolygonColliders, c => !c.isTrigger);

                if (physicsCollider == null)
                {
                    physicsCollider = gameObject.AddComponent<PolygonCollider2D>();
                    physicsCollider.isTrigger = false;
                }
            }

            // 重複するコライダーを削除
            CleanupDuplicateColliders(existingBoxColliders, existingPolygonColliders);
        }

        /// <summary>
        /// 重複するコライダーを削除
        /// </summary>
        private void CleanupDuplicateColliders(BoxCollider2D[] boxColliders, PolygonCollider2D[] polygonColliders)
        {
            // 不要なBoxCollider2Dを削除
            foreach (var collider in boxColliders)
            {
                if (collider != triggerCollider)
                {
                    if (Application.isEditor)
                        DestroyImmediate(collider);
                    else
                        Destroy(collider);

                    Debug.Log($"Removed duplicate BoxCollider2D from {gameObject.name}");
                }
            }

            // 不要なPolygonCollider2Dを削除
            foreach (var collider in polygonColliders)
            {
                if (collider != physicsCollider)
                {
                    if (Application.isEditor)
                        DestroyImmediate(collider);
                    else
                        Destroy(collider);

                    Debug.Log($"Removed duplicate PolygonCollider2D from {gameObject.name}");
                }
            }
        }

        private void CacheInitialSettings()
        {
            lastSettings = new SlopeSettings
            {
                slopeAngle = slopeSettings.slopeAngle,
                slopeDirection = slopeSettings.slopeDirection,
                slopeLength = slopeSettings.slopeLength,
                baseThickness = slopeSettings.baseThickness,
                triggerSizeMultiplier = slopeSettings.triggerSizeMultiplier
            };
        }

        private void SetupContactFilter()
        {
            contactFilter.SetLayerMask(affectedLayers);
            contactFilter.useLayerMask = true;
            contactFilter.useTriggers = false; // 重要: プレイヤーはトリガーでない
            contactFilter.useDepth = false;
            contactFilter.useOutsideDepth = false;
            contactFilter.useNormalAngle = false;
            contactFilter.useOutsideNormalAngle = false;

            if (slopeSettings.showDebugGizmos)
            {
                Debug.Log($"ContactFilter setup - LayerMask: {affectedLayers.value}, useTriggers: {contactFilter.useTriggers}");
            }
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.T)) // テストキー
            {
                TestCollisionManually();
            }
            if (slopeSettings.enablePhysicsEffects)
            {
                ApplySlopePhysics();
            }

            // 設定変更チェック
            CheckForSettingsChanges();
        }
        private void TestCollisionManually()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.Log($"Distance to player: {distance}");

            bool boundsOverlap = triggerCollider.bounds.Intersects(player.GetComponent<Collider2D>().bounds);
            Debug.Log($"Bounds overlap: {boundsOverlap}");

            Collider2D[] results = new Collider2D[1];
            int count = Physics2D.OverlapCollider(triggerCollider, contactFilter, results);
            Debug.Log($"OverlapCollider count: {count}");
        }

        private void CheckForSettingsChanges()
        {
            if (!HasSettingsChanged()) return;

            UpdateSlope();
            CacheInitialSettings();
            OnSlopeSettingsChanged?.Invoke(slopeSettings);
        }

        private bool HasSettingsChanged()
        {
            return !Mathf.Approximately(lastSettings.slopeAngle, slopeSettings.slopeAngle) ||
                   lastSettings.slopeDirection != slopeSettings.slopeDirection ||
                   !Mathf.Approximately(lastSettings.slopeLength, slopeSettings.slopeLength) ||
                   !Mathf.Approximately(lastSettings.baseThickness, slopeSettings.baseThickness) ||
                   lastSettings.triggerSizeMultiplier != slopeSettings.triggerSizeMultiplier;
        }

        /// <summary>
        /// 傾斜の完全更新（物理ベクトル + コライダー）
        /// </summary>
        public void UpdateSlope()
        {
            CalculateSlopeVectors();

            if (slopeSettings.autoUpdateColliders)
            {
                UpdateColliders();
            }

            if (slopeSettings.rotateVisuals)
            {
                UpdateVisualRotation();
            }
        }

        private void CalculateSlopeVectors()
        {
            float angleRad = slopeSettings.slopeAngle * Mathf.Deg2Rad;

            if (slopeSettings.slopeDirection == SlopeDirection.Ascending)
            {
                // 上り坂: 右上方向
                slopeDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                slopeNormal = new Vector2(-Mathf.Sin(angleRad), Mathf.Cos(angleRad));
            }
            else
            {
                // 下り坂: 右下方向
                slopeDirection = new Vector2(Mathf.Cos(-angleRad), Mathf.Sin(-angleRad));
                slopeNormal = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
            }
        }

        /// <summary>
        /// コライダーの更新（PolygonCollider2D + BoxCollider2D）
        /// </summary>
        private void UpdateColliders()
        {
            UpdateTriggerCollider();
            UpdatePhysicsCollider();
            needsColliderUpdate = false;
        }

        /// <summary>
        /// トリガーコライダー（BoxCollider2D）の更新
        /// </summary>
        private void UpdateTriggerCollider()
        {
            if (triggerCollider == null) return;

            // サイズ計算
            float length = slopeSettings.slopeLength;
            float angle = slopeSettings.slopeAngle * Mathf.Deg2Rad;
            float height = Mathf.Tan(angle) * length;

            Vector2 size = new Vector2(
                length * slopeSettings.triggerSizeMultiplier.x,
                Mathf.Max(2f, height * slopeSettings.triggerSizeMultiplier.y)
            );

            // オフセット計算
            float yOffset = slopeSettings.slopeDirection == SlopeDirection.Ascending
                ? height * 0.3f
                : height * 0.7f;

            triggerCollider.size = size;
            triggerCollider.offset = new Vector2(0f, yOffset);
        }

        /// <summary>
        /// 物理コライダー（PolygonCollider2D）の更新
        /// </summary>
        private void UpdatePhysicsCollider()
        {
            if (physicsCollider == null) return;

            Vector2[] slopePoints = GenerateSlopePolygonPoints();
            physicsCollider.points = slopePoints;
        }

        /// <summary>
        /// 傾斜形状のPolygonポイント生成
        /// </summary>
        private Vector2[] GenerateSlopePolygonPoints()
        {
            float length = slopeSettings.slopeLength;
            float angle = slopeSettings.slopeAngle * Mathf.Deg2Rad;
            float height = Mathf.Tan(angle) * length;
            float halfLength = length * 0.5f;
            float thickness = slopeSettings.baseThickness;

            List<Vector2> points = new List<Vector2>();

            if (slopeSettings.slopeDirection == SlopeDirection.Ascending)
            {
                // 上り坂のポイント配置
                points.Add(new Vector2(-halfLength, -thickness));      // 左下
                points.Add(new Vector2(-halfLength, 0f));              // 左上（開始点）
                points.Add(new Vector2(halfLength, height));           // 右上（終了点）
                points.Add(new Vector2(halfLength, height - thickness)); // 右下

                // 底面の接続（必要な場合）
                if (height > thickness)
                {
                    float bottomOffsetX = thickness / Mathf.Tan(angle);
                    points.Add(new Vector2(halfLength - bottomOffsetX, -thickness));
                }
            }
            else
            {
                // 下り坂のポイント配置
                points.Add(new Vector2(-halfLength, height));          // 左上（開始点）
                points.Add(new Vector2(halfLength, 0f));               // 右上（終了点）
                points.Add(new Vector2(halfLength, -thickness));       // 右下

                // 底面の接続（必要な場合）
                if (height > thickness)
                {
                    float bottomOffsetX = thickness / Mathf.Tan(angle);
                    points.Add(new Vector2(-halfLength + bottomOffsetX, -thickness));
                }

                points.Add(new Vector2(-halfLength, height - thickness)); // 左下
            }

            return points.ToArray();
        }

        private void UpdateVisualRotation()
        {
            if (slopeRenderer != null)
            {
                float rotationAngle = slopeSettings.slopeDirection == SlopeDirection.Ascending
                    ? slopeSettings.slopeAngle
                    : -slopeSettings.slopeAngle;

                transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            }
        }

        private void ApplySlopePhysics()
        {
            // 効率的なオブジェクト検知
            UpdateObjectsOnSlope();

            // 物理効果の適用
            foreach (var rb in objectsOnSlope)
            {
                if (rb != null && rb.gameObject.activeInHierarchy)
                {
                    ApplySlopeEffectToObject(rb);
                }
                else
                {
                    removeQueue.Enqueue(rb);
                }
            }

            // 無効なオブジェクトを削除
            while (removeQueue.Count > 0)
            {
                objectsOnSlope.Remove(removeQueue.Dequeue());
            }
        }

        private void UpdateObjectsOnSlope()
        {
            if (triggerCollider == null) return;

            // 現在のオーバーラップチェック
            Collider2D[] overlapping = new Collider2D[10];
            int count = Physics2D.OverlapCollider(triggerCollider, contactFilter, overlapping);

            // 新しいオブジェクトの追加
            for (int i = 0; i < count; i++)
            {
                if (overlapping[i] != null)
                {
                    Rigidbody2D rb = overlapping[i].GetComponent<Rigidbody2D>();
                    if (rb != null && !objectsOnSlope.Contains(rb))
                    {
                        objectsOnSlope.Add(rb);
                    }
                }
            }
        }

        private void ApplySlopeEffectToObject(Rigidbody2D rb)
        {
            Vector2 velocity = rb.linearVelocity;

            // 水平移動にスピード倍率を適用
            if (Mathf.Abs(velocity.x) > 0.1f)
            {
                velocity.x *= slopeSettings.speedMultiplier;
            }

            // 重力方向転換の適用
            if (slopeSettings.affectGravity)
            {
                Vector2 gravityForce = Physics2D.gravity * rb.mass;
                Vector2 slopeGravity = Vector3.Project(gravityForce, slopeDirection);
                rb.AddForce(slopeGravity * slopeSettings.gravityRedirection, ForceMode2D.Force);
            }

            rb.linearVelocity = velocity;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsAffectedLayer(other.gameObject.layer))
            {
                OnObjectEnterSlope?.Invoke(other.gameObject);

                // プレイヤー移動システムとの統合
                NotifyPlayerMovement(other, true);

                if (slopeSettings.showDebugGizmos)
                {
                    Debug.Log($"SlopeObject: {other.name} entered slope");
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsAffectedLayer(other.gameObject.layer))
            {
                OnObjectExitSlope?.Invoke(other.gameObject);

                // プレイヤー移動システムとの統合
                NotifyPlayerMovement(other, false);

                if (slopeSettings.showDebugGizmos)
                {
                    Debug.Log($"SlopeObject: {other.name} exited slope");
                }
            }
        }

        private bool IsAffectedLayer(int layer)
        {
            return (affectedLayers.value & (1 << layer)) != 0;
        }

        private void NotifyPlayerMovement(Collider2D other, bool entering)
        {
            var playerMovement = other.GetComponent<GravityFlipLab.Player.PlayerMovement>();
            if (playerMovement != null)
            {
                // PlayerMovementシステムとの統合
                if (entering)
                {
                    Debug.Log($"Player entered slope: {slopeSettings.slopeDirection} at {slopeSettings.slopeAngle}°");
                }
                else
                {
                    Debug.Log("Player exited slope");
                }
            }
        }

        private void ValidateSetup()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            if (triggerCollider == null)
                errors.Add("Missing trigger collider (BoxCollider2D)");
            else
            {
                if (!triggerCollider.enabled)
                    errors.Add("Trigger collider is disabled");
                if (!triggerCollider.isTrigger)
                    errors.Add("BoxCollider2D should be trigger");
            }

            if (physicsCollider == null)
                errors.Add("Missing physics collider (PolygonCollider2D)");
            else
            {
                if (!physicsCollider.enabled)
                    errors.Add("Physics collider is disabled");
                if (physicsCollider.isTrigger)
                    errors.Add("PolygonCollider2D should not be trigger");
            }

            if (slopeSettings.slopeAngle <= 0f || slopeSettings.slopeAngle > 60f)
                errors.Add($"Invalid slope angle: {slopeSettings.slopeAngle}° (must be 0-60°)");

            if (slopeSettings.slopeLength <= 0f)
                errors.Add($"Invalid slope length: {slopeSettings.slopeLength}");

            if (slopeSettings.speedMultiplier <= 0f)
                errors.Add($"Invalid speed multiplier: {slopeSettings.speedMultiplier}");

            // レイヤー設定の検証
            if (affectedLayers.value == 0)
                warnings.Add("No layers specified in affectedLayers");

            // プレイヤーレイヤーの確認
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                int playerLayer = player.layer;
                bool playerLayerIncluded = (affectedLayers.value & (1 << playerLayer)) != 0;
                if (!playerLayerIncluded)
                {
                    warnings.Add($"Player layer ({playerLayer}) not included in affectedLayers");
                }
            }

            // Physics2D設定の確認
            if (!Physics2D.queriesHitTriggers)
            {
                warnings.Add("Physics2D.queriesHitTriggers is disabled - may affect trigger detection");
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"SlopeObject validation failed on {gameObject.name}:\n" + string.Join("\n", errors));
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning($"SlopeObject warnings on {gameObject.name}:\n" + string.Join("\n", warnings));
            }

            if (errors.Count == 0 && warnings.Count == 0 && slopeSettings.showDebugGizmos)
            {
                Debug.Log($"SlopeObject validation passed for {gameObject.name}");
            }
        }

        // Public API methods
        public void SetSlopeAngle(float angle)
        {
            slopeSettings.slopeAngle = Mathf.Clamp(angle, 5f, 60f);
            UpdateSlope();
        }

        public void SetSlopeDirection(SlopeDirection direction)
        {
            slopeSettings.slopeDirection = direction;
            UpdateSlope();
        }

        public void SetSlopeLength(float length)
        {
            slopeSettings.slopeLength = Mathf.Max(2f, length);
            UpdateSlope();
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            slopeSettings.speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ForceUpdateColliders()
        {
            UpdateColliders();
        }

        // Getters
        public Vector2 GetSlopeNormal() => slopeNormal;
        public Vector2 GetSlopeDirection() => slopeDirection;
        public float GetSlopeAngle() => slopeSettings.slopeAngle;
        public bool IsObjectOnSlope(Rigidbody2D rb) => objectsOnSlope.Contains(rb);
        public int GetObjectsOnSlopeCount() => objectsOnSlope.Count;

        // Configuration methods
        public void SetSlopeSettings(SlopeSettings newSettings)
        {
            slopeSettings = newSettings;
            UpdateSlope();
        }

        public SlopeSettings GetSlopeSettings()
        {
            return slopeSettings;
        }

        public void EnableAutoUpdate(bool enable)
        {
            slopeSettings.autoUpdateColliders = enable;
        }

        // Debug and performance methods
        public void RefreshColliders()
        {
            needsColliderUpdate = true;
            UpdateColliders();
        }

        public bool ValidateColliderSetup()
        {
            if (triggerCollider == null || physicsCollider == null)
                return false;

            if (!triggerCollider.isTrigger || physicsCollider.isTrigger)
                return false;

            if (physicsCollider.points == null || physicsCollider.points.Length < 3)
                return false;

            return true;
        }

        public void OptimizeForPerformance()
        {
            // Remove null references
            objectsOnSlope.RemoveAll(rb => rb == null);

            // Disable auto-update if not needed
            if (!slopeSettings.autoUpdateColliders)
            {
                enabled = false;
            }
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!slopeSettings.showDebugGizmos) return;

            DrawSlopeDirection();
            DrawSlopeNormal();
            DrawColliderBounds();
        }

        private void DrawSlopeDirection()
        {
            Gizmos.color = slopeSettings.gizmoColor;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)slopeDirection * slopeSettings.slopeLength;
            Gizmos.DrawLine(start, end);

            // 矢印の描画
            Vector3 arrowSize = Vector3.one * 0.3f;
            Gizmos.DrawWireCube(end, arrowSize);
        }

        private void DrawSlopeNormal()
        {
            Gizmos.color = Color.blue;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)slopeNormal * 2f;
            Gizmos.DrawLine(start, end);
        }

        private void DrawColliderBounds()
        {
            // トリガーコライダーの境界
            if (triggerCollider != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = transform.position + (Vector3)triggerCollider.offset;
                Gizmos.DrawWireCube(center, triggerCollider.size);
            }

            // PolygonColliderの形状
            if (physicsCollider != null && physicsCollider.points.Length > 0)
            {
                Gizmos.color = Color.green;
                Vector2[] points = physicsCollider.points;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 current = transform.TransformPoint(points[i]);
                    Vector3 next = transform.TransformPoint(points[(i + 1) % points.Length]);
                    Gizmos.DrawLine(current, next);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!slopeSettings.showDebugGizmos) return;

            DrawDetailedDebugInfo();
            DrawAffectedObjects();
            DrawTheoreticalSlope();
        }

        private void DrawDetailedDebugInfo()
        {
            // 傾斜の詳細情報表示
            Vector3 position = transform.position;

            // 角度表示
            Gizmos.color = Color.white;
            Vector3 horizontalEnd = position + Vector3.right * slopeSettings.slopeLength;
            Gizmos.DrawLine(position, horizontalEnd);

            // 高さ表示
            float angle = slopeSettings.slopeAngle * Mathf.Deg2Rad;
            float height = Mathf.Tan(angle) * slopeSettings.slopeLength;
            Vector3 heightEnd = position + Vector3.up * height;
            Gizmos.DrawLine(position, heightEnd);

#if UNITY_EDITOR
            // テキスト情報（エディターのみ）
            UnityEditor.Handles.Label(position + Vector3.up * 3f,
                $"Slope: {slopeSettings.slopeAngle:F1}° {slopeSettings.slopeDirection}\n" +
                $"Length: {slopeSettings.slopeLength:F1}m\n" +
                $"Height: {height:F1}m\n" +
                $"Objects: {objectsOnSlope.Count}");
#endif
        }

        private void DrawAffectedObjects()
        {
            if (!Application.isPlaying || objectsOnSlope.Count == 0) return;

            Gizmos.color = Color.red;
            foreach (var rb in objectsOnSlope)
            {
                if (rb != null)
                {
                    Gizmos.DrawWireSphere(rb.transform.position, 0.5f);

                    // 速度ベクトルの表示
                    Gizmos.color = Color.cyan;
                    Vector3 velocityEnd = rb.transform.position + (Vector3)rb.linearVelocity * 0.1f;
                    Gizmos.DrawLine(rb.transform.position, velocityEnd);
                    Gizmos.color = Color.red;
                }
            }
        }

        private void DrawTheoreticalSlope()
        {
            // 理論的な傾斜線の表示
            Gizmos.color = Color.white;
            float angle = slopeSettings.slopeAngle * Mathf.Deg2Rad;
            float length = slopeSettings.slopeLength;
            float height = Mathf.Tan(angle) * length;
            float halfLength = length * 0.5f;

            Vector3 start = transform.position + Vector3.left * halfLength;
            Vector3 end = transform.position + Vector3.right * halfLength;

            if (slopeSettings.slopeDirection == SlopeDirection.Ascending)
            {
                end += Vector3.up * height;
            }
            else
            {
                start += Vector3.up * height;
            }

            Gizmos.DrawLine(start, end);

            // 傾斜の厚みを表示
            Vector3 thicknessOffset = Vector3.down * slopeSettings.baseThickness;
            Gizmos.DrawLine(start + thicknessOffset, end + thicknessOffset);
        }

        // エディター用の検証メソッド
        private void OnValidate()
        {
            // 値の範囲チェックと自動修正
            slopeSettings.slopeAngle = Mathf.Clamp(slopeSettings.slopeAngle, 5f, 60f);
            slopeSettings.slopeLength = Mathf.Max(2f, slopeSettings.slopeLength);
            slopeSettings.speedMultiplier = Mathf.Max(0.1f, slopeSettings.speedMultiplier);
            slopeSettings.baseThickness = Mathf.Max(0.1f, slopeSettings.baseThickness);
            slopeSettings.gravityRedirection = Mathf.Max(0f, slopeSettings.gravityRedirection);

            // triggerSizeMultiplierの値チェック
            if (slopeSettings.triggerSizeMultiplier.x < 1f)
                slopeSettings.triggerSizeMultiplier.x = 1f;
            if (slopeSettings.triggerSizeMultiplier.y < 1f)
                slopeSettings.triggerSizeMultiplier.y = 1f;

            // ランタイム中の場合は即座に更新
            if (Application.isPlaying)
            {
                UpdateSlope();
            }
            else
            {
                needsColliderUpdate = true;
            }
        }

        // 統計・監視用メソッド
        public SlopeStatistics GetSlopeStatistics()
        {
            return new SlopeStatistics
            {
                slopeAngle = slopeSettings.slopeAngle,
                slopeDirection = slopeSettings.slopeDirection,
                slopeLength = slopeSettings.slopeLength,
                speedMultiplier = slopeSettings.speedMultiplier,
                currentObjectCount = objectsOnSlope.Count,
                triggerColliderSize = triggerCollider?.size ?? Vector2.zero,
                physicsColliderPointCount = physicsCollider?.points?.Length ?? 0,
                isValid = ValidateColliderSetup()
            };
        }

        // クリーンアップ
        private void OnDestroy()
        {
            objectsOnSlope.Clear();
            while (removeQueue.Count > 0)
            {
                removeQueue.Dequeue();
            }
        }

        // パフォーマンス監視
        public void LogPerformanceInfo()
        {
            Debug.Log($"SlopeObject Performance Info ({gameObject.name}):\n" +
                     $"Objects on slope: {objectsOnSlope.Count}\n" +
                     $"Trigger size: {triggerCollider?.size}\n" +
                     $"Polygon points: {physicsCollider?.points?.Length}\n" +
                     $"Auto-update enabled: {slopeSettings.autoUpdateColliders}\n" +
                     $"Physics effects enabled: {slopeSettings.enablePhysicsEffects}");
        }

        // 高度な設定メソッド
        public void ConfigureForPerformance()
        {
            slopeSettings.autoUpdateColliders = false;
            slopeSettings.showDebugGizmos = false;
            enabled = !slopeSettings.enablePhysicsEffects;
        }

        public void ConfigureForDebug()
        {
            slopeSettings.autoUpdateColliders = true;
            slopeSettings.showDebugGizmos = true;
            enabled = true;
        }

        public void SetTriggerSensitivity(float multiplierX, float multiplierY)
        {
            slopeSettings.triggerSizeMultiplier = new Vector2(
                Mathf.Max(1f, multiplierX),
                Mathf.Max(1f, multiplierY)
            );
            UpdateColliders();
        }

        // コライダー形状の取得（外部システム用）
        public Vector2[] GetPhysicsColliderPoints()
        {
            return physicsCollider?.points?.Clone() as Vector2[];
        }

        public Bounds GetTriggerBounds()
        {
            if (triggerCollider == null) return new Bounds();

            Vector3 center = transform.position + (Vector3)triggerCollider.offset;
            Vector3 size = triggerCollider.size;
            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// 傾斜オブジェクトの統計情報
    /// </summary>
    [System.Serializable]
    public struct SlopeStatistics
    {
        public float slopeAngle;
        public SlopeDirection slopeDirection;
        public float slopeLength;
        public float speedMultiplier;
        public int currentObjectCount;
        public Vector2 triggerColliderSize;
        public int physicsColliderPointCount;
        public bool isValid;

        public override string ToString()
        {
            return $"Slope Stats - Angle: {slopeAngle:F1}°, Direction: {slopeDirection}, " +
                   $"Length: {slopeLength:F1}m, Objects: {currentObjectCount}, Valid: {isValid}";
        }
    }

    /// <summary>
    /// SlopeObject用の拡張メソッド
    /// </summary>
    public static class SlopeObjectExtensions
    {
        public static bool IsAscending(this SlopeObject slope)
        {
            return slope.GetSlopeSettings().slopeDirection == SlopeDirection.Ascending;
        }

        public static bool IsDescending(this SlopeObject slope)
        {
            return slope.GetSlopeSettings().slopeDirection == SlopeDirection.Descending;
        }

        public static float GetSlopeHeight(this SlopeObject slope)
        {
            var settings = slope.GetSlopeSettings();
            float angle = settings.slopeAngle * Mathf.Deg2Rad;
            return Mathf.Tan(angle) * settings.slopeLength;
        }

        public static bool IsSteepSlope(this SlopeObject slope, float threshold = 40f)
        {
            return slope.GetSlopeAngle() > threshold;
        }

        public static bool IsGentleSlope(this SlopeObject slope, float threshold = 20f)
        {
            return slope.GetSlopeAngle() < threshold;
        }

        public static void MirrorSlope(this SlopeObject slope)
        {
            var settings = slope.GetSlopeSettings();
            settings.slopeDirection = settings.slopeDirection == SlopeDirection.Ascending
                ? SlopeDirection.Descending
                : SlopeDirection.Ascending;
            slope.SetSlopeSettings(settings);
        }

        public static void ScaleSlope(this SlopeObject slope, float factor)
        {
            var settings = slope.GetSlopeSettings();
            settings.slopeLength *= factor;
            slope.SetSlopeSettings(settings);
        }
    }
}