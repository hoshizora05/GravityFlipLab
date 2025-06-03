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

        [Header("Gravity Physics")]
        public float customGravityStrength = 9.81f;
        public float gravityTransitionSpeed = 8f;
        public bool useGravitySystemGravity = true;

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
        public bool enablePhysicsIntegration = true;
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
        private float currentGravityStrength;
        private Vector2 currentGravityDirection;

        // Slope physics
        private bool isOnSlope = false;
        private Vector2 slopeDirection;
        private float slopeInfluence = 0f;

        // Performance optimization
        private int groundCheckFrame = 0;
        private const int GROUND_CHECK_FREQUENCY = 2; // Every 2 physics frames

        // Cache for physics calculations
        private Vector2 gravityForce;
        private Vector2 movementForce;
        private Vector2 frictionForce;

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

            // Ground check pointsの設定（既存のものがあれば保持）
            if (groundCheckPoints == null || groundCheckPoints.Length == 0)
            {
                SetupGroundCheckPoints();
            }

            // Physics cacheの初期化
            InitializePhysicsCache();

            // イベント購読（重複購読を防ぐ）
            UnsubscribeFromEvents(); // 既存の購読をクリア
            SubscribeToEvents();     // 新しく購読

            isInitialized = true;

            if (showDebugInfo)
                Debug.Log("Enhanced PlayerMovement initialized with advanced physics");
        }
        /// <summary>
        /// イベント購読処理
        /// </summary>
        private void SubscribeToEvents()
        {
            // Subscribe to gravity system events
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged += OnGravityChanged;
            }

            // Subscribe to player events
            PlayerController.OnGravityFlip += OnGravityFlip;
        }

        /// <summary>
        /// イベント購読解除処理
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from gravity system events
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged -= OnGravityChanged;
            }

            // Unsubscribe from player events
            PlayerController.OnGravityFlip -= OnGravityFlip;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// 安全な再初期化メソッド
        /// リスポーン時や緊急時に使用
        /// </summary>
        public void SafeReinitialize(PlayerController controller)
        {
            if (controller == null)
            {
                Debug.LogError("PlayerMovement: Cannot reinitialize with null controller");
                return;
            }

            // 状態をリセット
            isInitialized = false;

            // 完全に再初期化
            Initialize(controller);

            // 物理状態の検証と修正
            ValidatePhysicsState();

            if (showDebugInfo)
                Debug.Log("PlayerMovement: Safe reinitialization completed");
        }

        /// <summary>
        /// コンポーネントの状態を検証
        /// </summary>
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

                // 可能な場合は自動修復を試行
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
                // Create default ground check points
                groundCheckPoints = new Transform[3];

                for (int i = 0; i < 3; i++)
                {
                    GameObject checkPoint = new GameObject($"GroundCheck_{i}");
                    checkPoint.transform.SetParent(transform);

                    float xOffset = (i - 1) * 0.3f; // Left, center, right
                    checkPoint.transform.localPosition = new Vector3(xOffset, -0.5f, 0);
                    groundCheckPoints[i] = checkPoint.transform;
                }
            }
        }

        private void InitializePhysicsCache()
        {
            groundHits = new RaycastHit2D[groundCheckPoints.Length];
            currentVelocity = Vector2.zero;
            targetVelocity = Vector2.zero;
            lastFrameVelocity = Vector2.zero;

            UpdateGravityState();
        }

        private void FixedUpdate()
        {
            if (playerController == null || !playerController.isAlive) return;

            // Store last frame data
            lastFrameVelocity = rb2d.linearVelocity;
            wasGrounded = isGrounded;

            // Update physics state
            UpdateGravityState();

            // Perform ground detection (optimized frequency)
            if (groundCheckFrame % GROUND_CHECK_FREQUENCY == 0)
            {
                PerformGroundDetection();
            }
            groundCheckFrame++;

            // Calculate and apply physics
            CalculateTargetVelocity();
            ApplyMovementPhysics();
            ApplyConstraints();

            // Update state for next frame
            currentVelocity = rb2d.linearVelocity;
        }

        private void UpdateGravityState()
        {
            if (movementSettings.useGravitySystemGravity && GravitySystem.Instance != null)
            {
                Vector2 systemGravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);
                currentGravityDirection = systemGravity.normalized;
                currentGravityStrength = systemGravity.magnitude;
            }
            else
            {
                currentGravityDirection = Vector2.down;
                currentGravityStrength = movementSettings.customGravityStrength;
            }
        }

        private void PerformGroundDetection()
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            groundAngle = 0f;
            isOnSlope = false;

            Vector2 rayDirection = currentGravityDirection;
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
                // Calculate average ground normal
                groundNormal = (averageNormal / validHits).normalized;
                groundAngle = Vector2.Angle(groundNormal, -currentGravityDirection);

                // Determine if on slope
                isOnSlope = groundAngle > 5f && groundAngle < movementSettings.maxSlopeAngle;

                if (isOnSlope)
                {
                    CalculateSlopeDirection();
                }
            }

            // Handle landing/takeoff events
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
            // Calculate slope direction perpendicular to ground normal
            slopeDirection = Vector2.Perpendicular(groundNormal);

            // Ensure slope direction points in movement direction
            if (slopeDirection.x < 0)
                slopeDirection = -slopeDirection;

            // Calculate slope influence based on angle
            slopeInfluence = Mathf.Clamp01(groundAngle / movementSettings.maxSlopeAngle);
        }

        private void CalculateTargetVelocity()
        {
            targetVelocity = Vector2.zero;

            // Calculate horizontal movement
            CalculateHorizontalMovement();

            // Apply gravity effects
            ApplyGravityEffects();

            // Apply slope modifications
            if (isOnSlope && movementSettings.enableSlopeBoost)
            {
                ApplySlopePhysics();
            }

            // Apply momentum conservation if enabled
            if (movementSettings.enableMomentumConservation)
            {
                ApplyMomentumConservation();
            }
        }

        private void CalculateHorizontalMovement()
        {
            float baseSpeed = movementSettings.baseRunSpeed;

            // Add speed variation for more organic movement
            if (movementSettings.enableSpeedVariation)
            {
                float variation = Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2f - 1f;
                baseSpeed += variation * movementSettings.speedVariation;
            }

            targetVelocity.x = baseSpeed;
        }

        private void ApplyGravityEffects()
        {
            if (!isGrounded)
            {
                // Apply gravity when in air
                gravityForce = currentGravityDirection * currentGravityStrength;
                targetVelocity += gravityForce * Time.fixedDeltaTime;
            }
        }

        private void ApplySlopePhysics()
        {
            if (!isOnSlope) return;

            // Determine if going uphill or downhill
            bool isUphill = Vector2.Dot(slopeDirection, Vector2.right) < 0;

            if (isUphill)
            {
                // Slight deceleration on uphill
                targetVelocity.x *= Mathf.Lerp(1f, movementSettings.slopeDeceleration, slopeInfluence);
            }
            else
            {
                // Acceleration on downhill
                targetVelocity.x *= Mathf.Lerp(1f, movementSettings.slopeAcceleration, slopeInfluence);
            }

            // Add slope-following component
            Vector2 slopeVelocity = slopeDirection * targetVelocity.x * slopeInfluence * 0.3f;
            targetVelocity += slopeVelocity;
        }

        private void ApplyMomentumConservation()
        {
            // Preserve some of the existing velocity for smoother movement
            Vector2 velocityDelta = targetVelocity - currentVelocity;
            targetVelocity = currentVelocity + velocityDelta * (1f - movementSettings.velocitySmoothing);
        }

        private void ApplyMovementPhysics()
        {
            Vector2 newVelocity = rb2d.linearVelocity;

            // Apply calculated target velocity
            if (movementSettings.enablePhysicsIntegration)
            {
                // Smooth integration with physics
                newVelocity = Vector2.Lerp(newVelocity, targetVelocity, movementSettings.gravityTransitionSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Direct velocity application
                newVelocity = targetVelocity;
            }

            // Apply environmental effects
            ApplyEnvironmentalEffects(ref newVelocity);

            rb2d.linearVelocity = newVelocity;
        }

        private void ApplyEnvironmentalEffects(ref Vector2 velocity)
        {
            if (isGrounded)
            {
                // Apply ground friction
                velocity.x *= movementSettings.groundFriction;
                velocity.y *= movementSettings.groundFriction;
            }
            else
            {
                // Apply air resistance
                velocity *= movementSettings.airResistance;
            }
        }

        private void ApplyConstraints()
        {
            Vector2 velocity = rb2d.linearVelocity;

            // Limit terminal velocity
            if (velocity.magnitude > movementSettings.terminalVelocity)
            {
                velocity = velocity.normalized * movementSettings.terminalVelocity;
            }

            // Apply bounce damping for hard impacts
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
                Debug.Log($"Player landed. Impact speed: {Vector2.Dot(lastFrameVelocity, groundNormal):F2}");

            // Apply landing effects
            float impactForce = Mathf.Abs(Vector2.Dot(lastFrameVelocity, groundNormal));
            if (impactForce > movementSettings.bounceThreshold)
            {
                // Create landing effect
                CreateLandingEffect(impactForce);
            }
        }

        private void OnTakeoff()
        {
            if (showDebugInfo)
                Debug.Log("Player took off from ground");
        }

        private void CreateLandingEffect(float impactForce)
        {
            // This would create particle effects, screen shake, etc.
            // For now, just log the impact
            if (showDebugInfo)
                Debug.Log($"Hard landing with force: {impactForce:F2}");
        }

        private void OnGravityChanged(Vector2 newGravityDirection)
        {
            // Respond to gravity system changes
            if (showDebugInfo)
                Debug.Log($"Gravity changed to: {newGravityDirection}");
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
            movementSettings.baseRunSpeed = 5.0f; // Default value
        }

        // Gravity flip integration
        public void OnGravityFlip(GravityDirection newDirection)
        {
            // Handle gravity flip specific to movement
            Vector2 velocity = rb2d.linearVelocity;

            // Preserve horizontal momentum, adjust vertical based on new gravity
            if (newDirection == GravityDirection.Up)
            {
                velocity.y = Mathf.Max(0, velocity.y); // Prevent downward motion when gravity flips up
            }
            else
            {
                velocity.y = Mathf.Min(0, velocity.y); // Prevent upward motion when gravity flips down
            }

            rb2d.linearVelocity = velocity;

            if (showDebugInfo)
                Debug.Log($"Movement system responded to gravity flip: {newDirection}");
        }

        // Advanced physics features
        public Vector2 PredictPositionAfterTime(float time)
        {
            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = currentGravityDirection * currentGravityStrength;

            return GravityPhysicsUtils.CalculateTrajectory(currentPos, currentVel, gravity, time);
        }

        public bool WillCollideWithGround(float lookAheadTime, out float timeToCollision)
        {
            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = currentGravityDirection * currentGravityStrength;

            // Find the ground level below the player
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentGravityDirection, 50f, groundLayerMask);
            if (hit.collider != null)
            {
                float groundY = hit.point.y;
                return GravityPhysicsUtils.WillCollideWithGround(currentPos, currentVel, gravity, groundY, out timeToCollision);
            }

            timeToCollision = -1f;
            return false;
        }

        // Performance optimization methods
        public void SetGroundCheckFrequency(int frequency)
        {
            // Allow dynamic adjustment of ground check frequency for performance
            // Lower frequency = better performance, higher frequency = more accuracy
        }

        // Debug and visualization
        private void OnDrawGizmos()
        {
            if (!visualizeGroundChecks || groundCheckPoints == null) return;

            // Draw ground check points and rays
            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                if (groundCheckPoints[i] == null) continue;

                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPoints[i].position, groundCheckRadius);

                Vector3 rayDirection = currentGravityDirection;
                Vector3 rayEnd = groundCheckPoints[i].position + rayDirection * groundCheckDistance;
                Gizmos.DrawLine(groundCheckPoints[i].position, rayEnd);
            }

            // Draw ground normal
            if (isGrounded)
            {
                Gizmos.color = Color.blue;
                Vector3 normalStart = transform.position;
                Vector3 normalEnd = normalStart + (Vector3)groundNormal * 2f;
                Gizmos.DrawLine(normalStart, normalEnd);
            }

            // Draw slope direction
            if (isOnSlope)
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

            // Draw current velocity
            Gizmos.color = Color.cyan;
            Vector3 velocityStart = transform.position;
            Vector3 velocityEnd = velocityStart + (Vector3)currentVelocity * 0.2f;
            Gizmos.DrawLine(velocityStart, velocityEnd);

            // Draw target velocity
            Gizmos.color = Color.magenta;
            Vector3 targetStart = transform.position + Vector3.up * 0.5f;
            Vector3 targetEnd = targetStart + (Vector3)targetVelocity * 0.2f;
            Gizmos.DrawLine(targetStart, targetEnd);

            // Draw gravity direction
            Gizmos.color = Color.white;
            Vector3 gravityStart = transform.position + Vector3.right * 0.5f;
            Vector3 gravityEnd = gravityStart + (Vector3)currentGravityDirection * 1.5f;
            Gizmos.DrawLine(gravityStart, gravityEnd);
        }

        // Safety and error handling
        public void ValidatePhysicsState()
        {
            // Check for invalid physics states and correct them
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
        }

        // Integration with other systems
        public void IntegrateWithGravitySystem()
        {
            if (gravityAffected != null)
            {
                // Sync settings with GravityAffectedObject
                gravityAffected.gravityScale = 1f;
                gravityAffected.smoothGravityTransition = true;
                gravityAffected.transitionSpeed = movementSettings.gravityTransitionSpeed;
            }
        }

        // Configuration methods for runtime adjustment
        public void UpdateMovementSettings(MovementSettings newSettings)
        {
            movementSettings = newSettings;
            IntegrateWithGravitySystem();
        }

        public MovementSettings GetCurrentSettings()
        {
            return movementSettings;
        }
    }
}