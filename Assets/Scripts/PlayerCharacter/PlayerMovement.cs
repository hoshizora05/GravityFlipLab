using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Auto-Run Settings")]
        public float baseRunSpeed = 5.0f;
        public float speedVariation = 0.2f;
        public bool enableSpeedVariation = true;

        [Header("Gravity Integration")]
        public bool useGravityAffectedObject = true;
        public float fallbackGravityStrength = 9.81f;
        public bool overrideGravitySystem = false;

        [Header("Slope Physics")]
        public float slopeAcceleration = 1.3f;
        public float slopeDeceleration = 0.8f;
        public float maxSlopeAngle = 50f;
        public bool enableSlopeBoost = true;

        [Header("Air Physics")]
        public float airResistance = 0.98f;
        public float terminalVelocity = 25f;
        public float airControlFactor = 0.1f;

        [Header("Ground Interaction")]
        public float groundFriction = 0.99f;
        public float bounceThreshold = 10f;
        public float bounceDamping = 0.7f;

        [Header("Advanced Features")]
        public bool enableMomentumConservation = true;
        public float velocitySmoothing = 0.1f;
    }

    public partial class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Configuration")]
        public MovementSettings movementSettings = new MovementSettings();

        [Header("Ground Detection")]
        public LayerMask groundLayerMask = 1;
        public float groundCheckDistance = 0.6f;
        public float groundCheckRadius = 0.3f;
        public Transform[] groundCheckPoints;

        [Header("Debug & Visualization")]
        public bool showDebugInfo = false;
        public bool visualizeGroundChecks = true;
        public bool showVelocityVector = true;

        private PlayerController playerController;
        private Rigidbody2D rb2d;
        private GravityAffectedObject gravityAffected;

        // Ground detection state
        private bool isGrounded = false;
        private bool wasGrounded = false;
        private RaycastHit2D[] groundHits;
        private Vector2 groundNormal = Vector2.up;
        private float groundAngle = 0f;

        // Physics state
        private Vector2 currentVelocity;
        private Vector2 targetVelocity;
        private Vector2 lastFrameVelocity;

        // Slope physics
        private bool isOnSlope = false;
        private Vector2 slopeDirection;
        private float slopeInfluence = 0f;

        // Performance optimization
        private int groundCheckFrame = 0;
        private const int GROUND_CHECK_FREQUENCY = 2;

        // 初期化状態を追跡するフラグ
        private bool isInitialized = false;

        public void Initialize(PlayerController controller)
        {
            // 既に初期化済みの場合は重要な参照のみ更新
            if (isInitialized && controller != null)
            {
                playerController = controller;
                if (showDebugInfo)
                    Debug.Log("PlayerMovement: Updated controller reference (already initialized)");
                return;
            }

            playerController = controller;

            // コンポーネント参照の安全な取得
            if (rb2d == null)
                rb2d = GetComponent<Rigidbody2D>();

            if (gravityAffected == null)
                gravityAffected = GetComponent<GravityAffectedObject>();

            // Ground check pointsの設定
            if (groundCheckPoints == null || groundCheckPoints.Length == 0)
            {
                SetupGroundCheckPoints();
            }

            // Physics cacheの初期化
            InitializePhysicsCache();

            // 重力処理の設定
            ConfigureGravityIntegration();

            // イベント購読
            UnsubscribeFromEvents();
            SubscribeToEvents();

            isInitialized = true;

            if (showDebugInfo)
                Debug.Log("Enhanced PlayerMovement initialized with proper gravity integration");
        }

        /// <summary>
        /// 重力システムとの統合設定
        /// </summary>
        private void ConfigureGravityIntegration()
        {
            if (movementSettings.useGravityAffectedObject && gravityAffected != null)
            {
                // GravityAffectedObjectに重力処理を委譲
                gravityAffected.useCustomGravity = true;
                gravityAffected.gravityScale = 1f;
                gravityAffected.maintainInertia = false; // 慣性による減衰を無効化

                if (showDebugInfo)
                    Debug.Log("PlayerMovement: Delegating gravity to GravityAffectedObject");
            }
            else
            {
                // 独自の重力処理を使用
                if (rb2d != null)
                {
                    rb2d.gravityScale = 1f; // Unity標準重力を使用
                }

                if (showDebugInfo)
                    Debug.Log("PlayerMovement: Using Unity standard gravity");
            }
        }

        private void SubscribeToEvents()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged += OnGravityChanged;
            }

            PlayerController.OnGravityFlip += OnGravityFlip;
        }

        private void UnsubscribeFromEvents()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged -= OnGravityChanged;
            }

            PlayerController.OnGravityFlip -= OnGravityFlip;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void SafeReinitialize(PlayerController controller)
        {
            if (controller == null)
            {
                Debug.LogError("PlayerMovement: Cannot reinitialize with null controller");
                return;
            }

            isInitialized = false;
            Initialize(controller);
            ValidatePhysicsState();

            if (showDebugInfo)
                Debug.Log("PlayerMovement: Safe reinitialization completed");
        }

        public bool ValidateComponentState()
        {
            bool isValid = true;
            List<string> errors = new List<string>();

            if (playerController == null)
            {
                errors.Add("PlayerController reference is null");
                isValid = false;
            }

            if (rb2d == null)
            {
                errors.Add("Rigidbody2D reference is null");
                isValid = false;
            }

            if (groundCheckPoints == null || groundCheckPoints.Length == 0)
            {
                errors.Add("Ground check points not properly initialized");
                isValid = false;
            }

            if (!isValid)
            {
                Debug.LogError($"PlayerMovement validation failed: {string.Join(", ", errors)}");

                if (rb2d == null)
                    rb2d = GetComponent<Rigidbody2D>();

                if (groundCheckPoints == null || groundCheckPoints.Length == 0)
                    SetupGroundCheckPoints();
            }

            return isValid;
        }

        private void SetupGroundCheckPoints()
        {
            if (groundCheckPoints == null || groundCheckPoints.Length == 0)
            {
                groundCheckPoints = new Transform[3];

                for (int i = 0; i < 3; i++)
                {
                    GameObject checkPoint = new GameObject($"GroundCheck_{i}");
                    checkPoint.transform.SetParent(transform);

                    float xOffset = (i - 1) * 0.3f;
                    checkPoint.transform.localPosition = new Vector3(xOffset, -0.5f, 0);
                    groundCheckPoints[i] = checkPoint.transform;
                }
            }
        }

        private void InitializePhysicsCache()
        {
            if (groundCheckPoints != null)
            {
                groundHits = new RaycastHit2D[groundCheckPoints.Length];
            }

            currentVelocity = Vector2.zero;
            targetVelocity = Vector2.zero;
            lastFrameVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (playerController == null || !playerController.isAlive) return;

            // Store last frame data
            lastFrameVelocity = rb2d.linearVelocity;
            wasGrounded = isGrounded;

            // Perform ground detection (optimized frequency)
            if (groundCheckFrame % GROUND_CHECK_FREQUENCY == 0)
            {
                PerformGroundDetection();
            }
            groundCheckFrame++;

            // Apply movement (重力はGravityAffectedObjectまたはUnityが処理)
            ApplyHorizontalMovement();
            ApplyEnvironmentalEffects();
            ApplyConstraints();

            // Update state
            currentVelocity = rb2d.linearVelocity;
        }

        private void PerformGroundDetection()
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            groundAngle = 0f;
            isOnSlope = false;

            // 重力方向を取得
            Vector2 rayDirection = Vector2.down;
            if (movementSettings.useGravityAffectedObject && gravityAffected != null)
            {
                Vector2 currentGravity = gravityAffected.GetCurrentGravity();
                if (currentGravity.magnitude > 0.1f)
                {
                    rayDirection = currentGravity.normalized;
                }
            }

            int validHits = 0;
            Vector2 averageNormal = Vector2.zero;

            // Perform raycast from each ground check point
            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                groundHits[i] = Physics2D.Raycast(
                    groundCheckPoints[i].position,
                    rayDirection,
                    groundCheckDistance,
                    groundLayerMask
                );

                if (groundHits[i].collider != null)
                {
                    isGrounded = true;
                    averageNormal += groundHits[i].normal;
                    validHits++;
                }
            }

            if (validHits > 0)
            {
                groundNormal = (averageNormal / validHits).normalized;
                groundAngle = Vector2.Angle(groundNormal, -rayDirection);
                isOnSlope = groundAngle > 5f && groundAngle < movementSettings.maxSlopeAngle;

                if (isOnSlope)
                {
                    CalculateSlopeDirection();
                }
            }

            if (isGrounded && !wasGrounded)
            {
                OnLanding();
            }
            else if (!isGrounded && wasGrounded)
            {
                OnTakeoff();
            }
        }

        private void CalculateSlopeDirection()
        {
            slopeDirection = Vector2.Perpendicular(groundNormal);

            if (slopeDirection.x < 0)
                slopeDirection = -slopeDirection;

            slopeInfluence = Mathf.Clamp01(groundAngle / movementSettings.maxSlopeAngle);
        }

        /// <summary>
        /// 水平移動のみを処理（重力は別システムが担当）
        /// </summary>
        private void ApplyHorizontalMovement()
        {
            Vector2 velocity = rb2d.linearVelocity;

            // 水平速度の計算
            float targetHorizontalSpeed = CalculateTargetHorizontalSpeed();

            // 水平速度の適用
            velocity.x = targetHorizontalSpeed;

            // スロープ効果の適用（水平成分のみ）
            if (isOnSlope && movementSettings.enableSlopeBoost)
            {
                ApplySlopeEffects(ref velocity);
            }

            rb2d.linearVelocity = velocity;
        }

        private float CalculateTargetHorizontalSpeed()
        {
            float baseSpeed = movementSettings.baseRunSpeed;

            // Add speed variation
            if (movementSettings.enableSpeedVariation)
            {
                float variation = Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2f - 1f;
                baseSpeed += variation * movementSettings.speedVariation;
            }

            return baseSpeed;
        }

        private void ApplySlopeEffects(ref Vector2 velocity)
        {
            if (!isOnSlope) return;

            bool isUphill = Vector2.Dot(slopeDirection, Vector2.right) < 0;

            if (isUphill)
            {
                velocity.x *= Mathf.Lerp(1f, movementSettings.slopeDeceleration, slopeInfluence);
            }
            else
            {
                velocity.x *= Mathf.Lerp(1f, movementSettings.slopeAcceleration, slopeInfluence);
            }
        }

        private void ApplyEnvironmentalEffects()
        {
            Vector2 velocity = rb2d.linearVelocity;

            if (isGrounded)
            {
                // 地面摩擦（水平方向のみ）
                velocity.x *= movementSettings.groundFriction;
            }
            else
            {
                // 空気抵抗
                velocity *= movementSettings.airResistance;
            }

            rb2d.linearVelocity = velocity;
        }

        private void ApplyConstraints()
        {
            Vector2 velocity = rb2d.linearVelocity;

            // 最大速度制限
            if (velocity.magnitude > movementSettings.terminalVelocity)
            {
                velocity = velocity.normalized * movementSettings.terminalVelocity;
            }

            // 着地時のバウンス処理
            if (isGrounded && !wasGrounded)
            {
                float impactSpeed = Mathf.Abs(Vector2.Dot(lastFrameVelocity, groundNormal));
                if (impactSpeed > movementSettings.bounceThreshold)
                {
                    Vector2 reflectedVelocity = Vector2.Reflect(velocity, groundNormal);
                    velocity = Vector2.Lerp(velocity, reflectedVelocity * movementSettings.bounceDamping, 0.5f);
                }
            }

            rb2d.linearVelocity = velocity;
        }

        private void OnLanding()
        {
            if (showDebugInfo)
            {
                float impactSpeed = Mathf.Abs(Vector2.Dot(lastFrameVelocity, groundNormal));
                Debug.Log($"Player landed. Impact speed: {impactSpeed:F2}");
            }
        }

        private void OnTakeoff()
        {
            if (showDebugInfo)
                Debug.Log("Player took off from ground");
        }

        private void OnGravityChanged(Vector2 newGravityDirection)
        {
            if (showDebugInfo)
                Debug.Log($"PlayerMovement: Gravity changed to: {newGravityDirection}");
        }

        public void OnGravityFlip(GravityDirection newDirection)
        {
            // 重力反転時の特別処理
            if (!movementSettings.useGravityAffectedObject)
            {
                Vector2 velocity = rb2d.linearVelocity;

                // 垂直速度の調整
                if (newDirection == GravityDirection.Up)
                {
                    velocity.y = Mathf.Max(0, velocity.y);
                }
                else
                {
                    velocity.y = Mathf.Min(0, velocity.y);
                }

                rb2d.linearVelocity = velocity;
            }

            if (showDebugInfo)
                Debug.Log($"PlayerMovement: Responded to gravity flip: {newDirection}");
        }

        // Public API methods
        public bool IsGrounded() => isGrounded;
        public bool IsOnSlope() => isOnSlope;
        public Vector2 GetSlopeDirection() => slopeDirection;
        public float GetGroundAngle() => groundAngle;
        public Vector2 GetGroundNormal() => groundNormal;

        public void ApplyExternalForce(Vector2 force)
        {
            rb2d.AddForce(force, ForceMode2D.Impulse);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            movementSettings.baseRunSpeed *= multiplier;
        }

        public void ResetSpeed()
        {
            movementSettings.baseRunSpeed = 5.0f;
        }

        public Vector2 PredictPositionAfterTime(float time)
        {
            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = Vector2.down * movementSettings.fallbackGravityStrength;

            if (movementSettings.useGravityAffectedObject && gravityAffected != null)
            {
                gravity = gravityAffected.GetCurrentGravity();
            }

            return GravityPhysicsUtils.CalculateTrajectory(currentPos, currentVel, gravity, time);
        }

        public bool WillCollideWithGround(float lookAheadTime, out float timeToCollision)
        {
            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = Vector2.down * movementSettings.fallbackGravityStrength;

            if (movementSettings.useGravityAffectedObject && gravityAffected != null)
            {
                gravity = gravityAffected.GetCurrentGravity();
            }

            RaycastHit2D hit = Physics2D.Raycast(currentPos, gravity.normalized, 50f, groundLayerMask);
            if (hit.collider != null)
            {
                float groundY = hit.point.y;
                return GravityPhysicsUtils.WillCollideWithGround(currentPos, currentVel, gravity, groundY, out timeToCollision);
            }

            timeToCollision = -1f;
            return false;
        }

        public void ValidatePhysicsState()
        {
            Vector2 velocity = rb2d.linearVelocity;

            if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) ||
                float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y))
            {
                Debug.LogError("Invalid velocity detected, resetting to zero");
                rb2d.linearVelocity = Vector2.zero;
            }

            if (velocity.magnitude > movementSettings.terminalVelocity * 2f)
            {
                Debug.LogWarning("Excessive velocity detected, clamping to safe values");
                rb2d.linearVelocity = velocity.normalized * movementSettings.terminalVelocity;
            }

            // 重力設定の検証
            if (movementSettings.useGravityAffectedObject && gravityAffected == null)
            {
                Debug.LogWarning("GravityAffectedObject is missing, falling back to Unity gravity");
                movementSettings.useGravityAffectedObject = false;
                rb2d.gravityScale = 1f;
            }
        }

        public void UpdateMovementSettings(MovementSettings newSettings)
        {
            movementSettings = newSettings;
            ConfigureGravityIntegration();
        }

        public MovementSettings GetCurrentSettings()
        {
            return movementSettings;
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!visualizeGroundChecks || groundCheckPoints == null) return;

            Vector2 rayDirection = Vector2.down;
            if (Application.isPlaying && movementSettings.useGravityAffectedObject && gravityAffected != null)
            {
                Vector2 currentGravity = gravityAffected.GetCurrentGravity();
                if (currentGravity.magnitude > 0.1f)
                {
                    rayDirection = currentGravity.normalized;
                }
            }

            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                if (groundCheckPoints[i] == null) continue;

                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPoints[i].position, groundCheckRadius);

                Vector3 rayEnd = groundCheckPoints[i].position + (Vector3)rayDirection * groundCheckDistance;
                Gizmos.DrawLine(groundCheckPoints[i].position, rayEnd);
            }

            if (Application.isPlaying && isGrounded)
            {
                Gizmos.color = Color.blue;
                Vector3 normalStart = transform.position;
                Vector3 normalEnd = normalStart + (Vector3)groundNormal * 2f;
                Gizmos.DrawLine(normalStart, normalEnd);
            }

            if (Application.isPlaying && isOnSlope)
            {
                Gizmos.color = Color.yellow;
                Vector3 slopeStart = transform.position;
                Vector3 slopeEnd = slopeStart + (Vector3)slopeDirection * 2f;
                Gizmos.DrawLine(slopeStart, slopeEnd);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showVelocityVector || !Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Vector3 velocityStart = transform.position;
            Vector3 velocityEnd = velocityStart + (Vector3)currentVelocity * 0.2f;
            Gizmos.DrawLine(velocityStart, velocityEnd);
        }
    }
}