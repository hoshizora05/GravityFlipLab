using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    /// <summary>
    /// PlayerMovement.csと統合された傾斜物理システム
    /// 既存のPlayerMovementの設定を尊重しつつ傾斜機能を追加
    /// </summary>
    [System.Serializable]
    public class SlopePhysicsSettings
    {
        [Header("Core Settings")]
        public bool enableSlopePhysics = true;
        public float maxWalkableAngle = 45f;
        public float slopeDetectionThreshold = 5f;

        [Header("Speed Modifiers")]
        public float uphillSpeedMultiplier = 0.8f;
        public float downhillSpeedMultiplier = 1.2f;
        public AnimationCurve slopeSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Header("Physics Effects")]
        public bool enableSlopeGravityComponent = true;
        public float slopeGravityInfluence = 0.3f;
        public bool enableSlopeSnapping = true;
        public float snapForce = 10f;

        [Header("Integration")]
        public bool respectPlayerMovementSettings = true;
        public bool usePlayerMovementGroundDetection = true;
        public bool logDebugInfo = false;
    }

    [System.Serializable]
    public class SlopeData
    {
        public bool isOnSlope;
        public bool canWalkOnSlope;
        public float angle;
        public Vector2 normal;
        public Vector2 direction;
        public float influence; // 0-1
        public bool isUphill;
        public float speedMultiplier;

        public void Reset()
        {
            isOnSlope = false;
            canWalkOnSlope = true;
            angle = 0f;
            normal = Vector2.up;
            direction = Vector2.right;
            influence = 0f;
            isUphill = false;
            speedMultiplier = 1f;
        }
    }

    [RequireComponent(typeof(PlayerMovement))]
    public class SlopePhysicsSystem : MonoBehaviour
    {
        [Header("Slope Physics Configuration")]
        public SlopePhysicsSettings settings = new SlopePhysicsSettings();

        [Header("Debug")]
        public bool showDebugVisualization = true;

        // Core references
        private PlayerMovement playerMovement;
        private PlayerController playerController;
        private Rigidbody2D rb2d;
        private GravityAffectedObject gravityAffected;

        // Current slope state
        public SlopeData currentSlope { get; private set; } = new SlopeData();
        private SlopeData previousSlope = new SlopeData();

        // Integration state
        private bool isIntegratedWithPlayerMovement = false;
        private float originalBaseSpeed = 0f;
        private MovementSettings originalMovementSettings;

        // Physics state
        private Vector2 slopeForce = Vector2.zero;
        private Vector2 currentGravityDirection = Vector2.down;

        // Events
        public System.Action<bool> OnSlopeStateChanged;
        public System.Action<float> OnSlopeAngleChanged;
        public System.Action<float> OnSpeedMultiplierChanged;

        #region Initialization

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            IntegrateWithPlayerMovement();
        }

        private void OnDestroy()
        {
            RestoreOriginalSettings();
        }

        private void InitializeComponents()
        {
            playerMovement = GetComponent<PlayerMovement>();
            playerController = GetComponent<PlayerController>();
            rb2d = GetComponent<Rigidbody2D>();
            gravityAffected = GetComponent<GravityAffectedObject>();

            if (playerMovement == null)
            {
                Debug.LogError("SlopePhysicsSystem requires PlayerMovement component!");
                enabled = false;
                return;
            }

            if (rb2d == null)
            {
                Debug.LogError("SlopePhysicsSystem requires Rigidbody2D component!");
                enabled = false;
                return;
            }

            currentSlope = new SlopeData();
            previousSlope = new SlopeData();
        }

        private void IntegrateWithPlayerMovement()
        {
            if (!settings.enableSlopePhysics || playerMovement == null) return;

            // Store original settings
            if (settings.respectPlayerMovementSettings)
            {
                originalMovementSettings = playerMovement.GetCurrentSettings();
                originalBaseSpeed = originalMovementSettings.baseRunSpeed;
            }

            isIntegratedWithPlayerMovement = true;

            if (settings.logDebugInfo)
                Debug.Log("SlopePhysicsSystem integrated with PlayerMovement");
        }

        #endregion

        #region Update Loop

        private void FixedUpdate()
        {
            if (!settings.enableSlopePhysics || !isIntegratedWithPlayerMovement) return;

            UpdateSlopeDetection();
            ProcessSlopePhysics();
            ApplySlopeEffects();
        }

        private void UpdateSlopeDetection()
        {
            // Store previous state
            previousSlope.isOnSlope = currentSlope.isOnSlope;
            previousSlope.angle = currentSlope.angle;

            // Reset current slope data
            currentSlope.Reset();

            // Get slope data from PlayerMovement if available
            if (settings.usePlayerMovementGroundDetection && playerMovement != null)
            {
                GetSlopeDataFromPlayerMovement();
            }
            else
            {
                PerformIndependentSlopeDetection();
            }

            // Calculate slope properties
            CalculateSlopeProperties();

            // Check for state changes
            CheckForSlopeStateChanges();
        }

        private void GetSlopeDataFromPlayerMovement()
        {
            // Use PlayerMovement's ground detection results
            currentSlope.isOnSlope = playerMovement.IsOnSlope();

            if (currentSlope.isOnSlope)
            {
                currentSlope.angle = playerMovement.GetGroundAngle();
                currentSlope.normal = playerMovement.GetGroundNormal();
                currentSlope.direction = playerMovement.GetSlopeDirection();
                currentSlope.canWalkOnSlope = currentSlope.angle <= settings.maxWalkableAngle;
            }
        }

        private void PerformIndependentSlopeDetection()
        {
            // Fallback independent slope detection
            Vector2 rayOrigin = transform.position;
            Vector2 rayDirection = GetCurrentGravityDirection();

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, 1f, playerMovement.groundLayerMask);

            if (hit.collider != null)
            {
                currentSlope.normal = hit.normal;
                Vector2 upDirection = -rayDirection;
                currentSlope.angle = Vector2.Angle(currentSlope.normal, upDirection);
                currentSlope.isOnSlope = currentSlope.angle > settings.slopeDetectionThreshold;
                currentSlope.canWalkOnSlope = currentSlope.angle <= settings.maxWalkableAngle;

                if (currentSlope.isOnSlope)
                {
                    currentSlope.direction = Vector2.Perpendicular(currentSlope.normal);
                    if (currentSlope.direction.x < 0)
                        currentSlope.direction = -currentSlope.direction;
                }
            }
        }

        private void CalculateSlopeProperties()
        {
            if (!currentSlope.isOnSlope) return;

            // Calculate slope influence (0-1)
            currentSlope.influence = Mathf.Clamp01(currentSlope.angle / settings.maxWalkableAngle);

            // Determine if uphill or downhill
            Vector2 movementDirection = Vector2.right; // Auto-run direction
            currentSlope.isUphill = Vector2.Dot(currentSlope.direction, movementDirection) < 0;

            // Calculate speed multiplier
            float normalizedAngle = currentSlope.influence;
            float curveValue = settings.slopeSpeedCurve.Evaluate(normalizedAngle);

            if (currentSlope.isUphill)
            {
                currentSlope.speedMultiplier = Mathf.Lerp(1f, settings.uphillSpeedMultiplier, curveValue);
            }
            else
            {
                currentSlope.speedMultiplier = Mathf.Lerp(1f, settings.downhillSpeedMultiplier, curveValue);
            }
        }

        private void ProcessSlopePhysics()
        {
            if (!currentSlope.isOnSlope)
            {
                slopeForce = Vector2.zero;
                return;
            }

            // Calculate slope gravity component
            if (settings.enableSlopeGravityComponent)
            {
                Vector2 gravity = GetCurrentGravity();
                Vector2 slopeGravityComponent = Vector3.Project(gravity, currentSlope.direction);
                slopeForce = slopeGravityComponent * settings.slopeGravityInfluence * currentSlope.influence;
            }
            else
            {
                slopeForce = Vector2.zero;
            }
        }

        private void ApplySlopeEffects()
        {
            // Apply speed modification to PlayerMovement
            ApplySpeedModification();

            // Apply slope forces
            ApplySlopeForces();

            // Apply slope snapping if enabled
            if (settings.enableSlopeSnapping && currentSlope.isOnSlope)
            {
                ApplySlopeSnapping();
            }
        }

        #endregion

        #region Effect Application

        private void ApplySpeedModification()
        {
            if (!isIntegratedWithPlayerMovement || playerMovement == null) return;

            MovementSettings currentSettings = playerMovement.GetCurrentSettings();

            if (currentSlope.isOnSlope)
            {
                // Apply slope speed multiplier
                float targetSpeed = originalBaseSpeed * currentSlope.speedMultiplier;
                if (Mathf.Abs(currentSettings.baseRunSpeed - targetSpeed) > 0.01f)
                {
                    currentSettings.baseRunSpeed = targetSpeed;
                    playerMovement.UpdateMovementSettings(currentSettings);

                    OnSpeedMultiplierChanged?.Invoke(currentSlope.speedMultiplier);

                    if (settings.logDebugInfo)
                        Debug.Log($"Speed modified: {currentSlope.speedMultiplier:F2}x");
                }
            }
            else
            {
                // Restore original speed
                if (Mathf.Abs(currentSettings.baseRunSpeed - originalBaseSpeed) > 0.01f)
                {
                    currentSettings.baseRunSpeed = originalBaseSpeed;
                    playerMovement.UpdateMovementSettings(currentSettings);

                    OnSpeedMultiplierChanged?.Invoke(1f);
                }
            }
        }

        private void ApplySlopeForces()
        {
            if (slopeForce.magnitude > 0.1f && rb2d != null)
            {
                rb2d.AddForce(slopeForce, ForceMode2D.Force);
            }
        }

        private void ApplySlopeSnapping()
        {
            if (!playerMovement.IsGrounded()) return;

            // Simple slope snapping - keep player on slope surface
            Vector2 snapDirection = currentSlope.normal;
            Vector2 snapForceVector = snapDirection * settings.snapForce * Time.fixedDeltaTime;

            // Only apply if moving away from slope
            if (Vector2.Dot(rb2d.linearVelocity, snapDirection) > 0)
            {
                rb2d.AddForce(-snapForceVector, ForceMode2D.Force);
            }
        }

        #endregion

        #region Utility Methods

        private Vector2 GetCurrentGravityDirection()
        {
            if (gravityAffected != null && playerMovement.movementSettings.useGravityAffectedObject)
            {
                Vector2 gravity = gravityAffected.GetCurrentGravity();
                return gravity.magnitude > 0.1f ? gravity.normalized : Vector2.down;
            }
            return Vector2.down;
        }

        private Vector2 GetCurrentGravity()
        {
            if (gravityAffected != null && playerMovement.movementSettings.useGravityAffectedObject)
            {
                return gravityAffected.GetCurrentGravity();
            }
            return Physics2D.gravity;
        }

        private void CheckForSlopeStateChanges()
        {
            // Check for slope state change
            if (currentSlope.isOnSlope != previousSlope.isOnSlope)
            {
                OnSlopeStateChanged?.Invoke(currentSlope.isOnSlope);

                if (settings.logDebugInfo)
                    Debug.Log($"Slope state changed: {currentSlope.isOnSlope}");
            }

            // Check for angle change
            if (Mathf.Abs(currentSlope.angle - previousSlope.angle) > 1f)
            {
                OnSlopeAngleChanged?.Invoke(currentSlope.angle);

                if (settings.logDebugInfo)
                    Debug.Log($"Slope angle changed: {currentSlope.angle:F1}°");
            }
        }

        private void RestoreOriginalSettings()
        {
            if (!isIntegratedWithPlayerMovement || playerMovement == null) return;
            if (!settings.respectPlayerMovementSettings) return;

            // Restore original movement settings
            if (originalMovementSettings != null)
            {
                playerMovement.UpdateMovementSettings(originalMovementSettings);

                if (settings.logDebugInfo)
                    Debug.Log("Original PlayerMovement settings restored");
            }
        }

        #endregion

        #region Public API

        public bool IsOnSlope() => currentSlope.isOnSlope;
        public float GetSlopeAngle() => currentSlope.angle;
        public Vector2 GetSlopeDirection() => currentSlope.direction;
        public Vector2 GetSlopeNormal() => currentSlope.normal;
        public float GetSlopeInfluence() => currentSlope.influence;
        public bool IsUphillSlope() => currentSlope.isUphill;
        public float GetCurrentSpeedMultiplier() => currentSlope.speedMultiplier;
        public bool CanWalkOnCurrentSlope() => currentSlope.canWalkOnSlope;

        public void SetSlopePhysicsEnabled(bool enabled)
        {
            settings.enableSlopePhysics = enabled;
            if (!enabled)
            {
                currentSlope.Reset();
                RestoreOriginalSettings();
            }
        }

        public void UpdateSlopeSettings(SlopePhysicsSettings newSettings)
        {
            settings = newSettings;
            if (settings.respectPlayerMovementSettings && playerMovement != null)
            {
                originalMovementSettings = playerMovement.GetCurrentSettings();
                originalBaseSpeed = originalMovementSettings.baseRunSpeed;
            }
        }

        public SlopeData GetCurrentSlopeData()
        {
            return currentSlope;
        }

        public bool IsIntegratedWithPlayerMovement()
        {
            return isIntegratedWithPlayerMovement;
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showDebugVisualization || !Application.isPlaying) return;

            if (currentSlope.isOnSlope)
            {
                Vector3 playerPos = transform.position;

                // Draw slope direction
                Gizmos.color = Color.green;
                Gizmos.DrawLine(playerPos, playerPos + (Vector3)currentSlope.direction * 2f);

                // Draw slope normal
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(playerPos, playerPos + (Vector3)currentSlope.normal * 1.5f);

                // Draw slope force
                if (slopeForce.magnitude > 0.1f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(playerPos, playerPos + (Vector3)slopeForce);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugVisualization || !Application.isPlaying) return;

            if (currentSlope.isOnSlope)
            {
                // Draw slope info
                Vector3 infoPos = transform.position + Vector3.up * 2f;

                Gizmos.color = currentSlope.canWalkOnSlope ? Color.green : Color.red;
                Gizmos.DrawWireCube(infoPos, Vector3.one * 0.2f);

#if UNITY_EDITOR
                string info = $"Angle: {currentSlope.angle:F1}°\n" +
                             $"Speed: {currentSlope.speedMultiplier:F2}x\n" +
                             $"Uphill: {currentSlope.isUphill}";
                UnityEditor.Handles.Label(infoPos + Vector3.up * 0.5f, info);
#endif
            }
        }

        #endregion

        #region Configuration Presets

        public void SetRealisticSlopePhysics()
        {
            settings.uphillSpeedMultiplier = 0.7f;
            settings.downhillSpeedMultiplier = 1.3f;
            settings.slopeGravityInfluence = 0.5f;
            settings.enableSlopeSnapping = true;
            settings.snapForce = 15f;
        }

        public void SetArcadeSlopePhysics()
        {
            settings.uphillSpeedMultiplier = 0.9f;
            settings.downhillSpeedMultiplier = 1.1f;
            settings.slopeGravityInfluence = 0.2f;
            settings.enableSlopeSnapping = true;
            settings.snapForce = 10f;
        }

        public void SetMinimalSlopePhysics()
        {
            settings.uphillSpeedMultiplier = 0.95f;
            settings.downhillSpeedMultiplier = 1.05f;
            settings.slopeGravityInfluence = 0.1f;
            settings.enableSlopeSnapping = false;
        }

        #endregion
    }

    /// <summary>
    /// PlayerMovementの拡張メソッド（傾斜システム用）
    /// </summary>
    public static class PlayerMovementSlopeExtensions
    {
        public static SlopePhysicsSystem GetSlopePhysicsSystem(this PlayerMovement playerMovement)
        {
            return playerMovement.GetComponent<SlopePhysicsSystem>();
        }

        public static bool HasSlopePhysics(this PlayerMovement playerMovement)
        {
            return playerMovement.GetComponent<SlopePhysicsSystem>() != null;
        }

        public static SlopeData GetCurrentSlopeInfo(this PlayerMovement playerMovement)
        {
            var slopeSystem = playerMovement.GetSlopePhysicsSystem();
            return slopeSystem != null ? slopeSystem.GetCurrentSlopeData() : new SlopeData();
        }

        public static void EnableSlopePhysics(this PlayerMovement playerMovement, bool enable = true)
        {
            var slopeSystem = playerMovement.GetSlopePhysicsSystem();
            if (slopeSystem == null && enable)
            {
                slopeSystem = playerMovement.gameObject.AddComponent<SlopePhysicsSystem>();
            }

            if (slopeSystem != null)
            {
                slopeSystem.SetSlopePhysicsEnabled(enable);
            }
        }
    }
}