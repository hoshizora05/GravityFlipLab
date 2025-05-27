using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    [System.Serializable]
    public class PlayerGravitySettings
    {
        [Header("Gravity Response")]
        public float gravityResponseSpeed = 8f;
        public bool preserveMomentumOnFlip = true;
        public float momentumPreservationRatio = 0.8f;

        [Header("Gravity Zone Interaction")]
        public float zoneTransitionSpeed = 5f;
        public bool enableZoneEffects = true;
        public float zoneInfluenceThreshold = 0.1f;

        [Header("Advanced Physics")]
        public bool enableGravityPrediction = true;
        public float predictionTimeWindow = 2f;
        public bool adaptToLocalGravity = true;

        [Header("Safety Systems")]
        public bool enableGravityValidation = true;
        public float maxGravityStrength = 50f;
        public bool autoRecoverFromAnomalies = true;
    }

    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(GravityAffectedObject))]
    public class PlayerGravityIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        public PlayerGravitySettings gravitySettings = new PlayerGravitySettings();

        [Header("Debug")]
        public bool showDebugInfo = false;
        public bool visualizeGravityEffects = true;

        // Core components
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        private GravityAffectedObject gravityAffected;
        private AdvancedGroundDetector groundDetector;
        private Rigidbody2D rb2d;

        // Gravity state tracking
        private Vector2 currentGravity;
        private Vector2 targetGravity;
        private Vector2 lastGravity;
        private bool isInGravityZone = false;
        private LocalGravityZone currentZone;
        private List<LocalGravityZone> activeZones = new List<LocalGravityZone>();

        // Physics state
        private Vector2 gravityVelocityComponent;
        private Vector2 momentumBeforeFlip;
        private bool isTransitioningGravity = false;
        private float transitionStartTime;

        // Performance optimization
        private int gravityUpdateFrame = 0;
        private const int GRAVITY_UPDATE_FREQUENCY = 1;

        // Events
        public System.Action<Vector2, Vector2> OnGravityTransition; // oldGravity, newGravity
        public System.Action<LocalGravityZone> OnEnteredGravityZone;
        public System.Action<LocalGravityZone> OnExitedGravityZone;
        public System.Action<Vector2> OnGravityAnomalyDetected;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeGravityIntegration();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            playerController = GetComponent<PlayerController>();
            playerMovement = GetComponent<PlayerMovement>();
            gravityAffected = GetComponent<GravityAffectedObject>();
            groundDetector = GetComponent<AdvancedGroundDetector>();
            rb2d = GetComponent<Rigidbody2D>();

            if (gravityAffected == null)
            {
                gravityAffected = gameObject.AddComponent<GravityAffectedObject>();
            }

            if (groundDetector == null)
            {
                groundDetector = gameObject.AddComponent<AdvancedGroundDetector>();
            }
        }

        private void InitializeGravityIntegration()
        {
            // Configure GravityAffectedObject for optimal player behavior
            ConfigureGravityAffectedObject();

            // Initialize gravity state
            UpdateGravityState();
            currentGravity = GetEffectiveGravity();
            targetGravity = currentGravity;
            lastGravity = currentGravity;

            if (showDebugInfo)
                Debug.Log("Player-Gravity integration initialized");
        }

        private void ConfigureGravityAffectedObject()
        {
            if (gravityAffected == null) return;

            gravityAffected.gravityScale = 1f;
            gravityAffected.useCustomGravity = true;
            gravityAffected.smoothGravityTransition = true;
            gravityAffected.transitionSpeed = gravitySettings.gravityResponseSpeed;
            gravityAffected.maintainInertia = gravitySettings.preserveMomentumOnFlip;
            gravityAffected.inertiaDecay = gravitySettings.momentumPreservationRatio;
            gravityAffected.maxVelocityChange = 30f; // Reasonable limit for player
        }

        private void SubscribeToEvents()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged += OnGlobalGravityChanged;
            }

            if (playerController != null)
            {
                PlayerController.OnGravityFlip += OnPlayerGravityFlip;
            }

            if (gravityAffected != null)
            {
                // Subscribe to zone enter/exit if available
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged -= OnGlobalGravityChanged;
            }

            PlayerController.OnGravityFlip -= OnPlayerGravityFlip;
        }

        private void FixedUpdate()
        {
            gravityUpdateFrame++;

            if (gravityUpdateFrame % GRAVITY_UPDATE_FREQUENCY == 0)
            {
                UpdateGravityIntegration();
            }

            // Always perform safety checks
            if (gravitySettings.enableGravityValidation)
            {
                ValidateGravityState();
            }
        }

        private void UpdateGravityIntegration()
        {
            // Update gravity state
            UpdateGravityState();

            // Handle gravity transitions
            ProcessGravityTransitions();

            // Apply gravity effects to movement
            ApplyGravityToMovement();

            // Handle local gravity zones
            if (gravitySettings.enableZoneEffects)
            {
                ProcessGravityZoneEffects();
            }

            // Update prediction if enabled
            if (gravitySettings.enableGravityPrediction)
            {
                UpdateGravityPrediction();
            }
        }

        private void UpdateGravityState()
        {
            lastGravity = currentGravity;
            targetGravity = GetEffectiveGravity();

            // Smooth transition to target gravity
            if (isTransitioningGravity)
            {
                float transitionProgress = (Time.time - transitionStartTime) / (1f / gravitySettings.gravityResponseSpeed);
                currentGravity = Vector2.Lerp(lastGravity, targetGravity, transitionProgress);

                if (transitionProgress >= 1f)
                {
                    currentGravity = targetGravity;
                    isTransitioningGravity = false;
                }
            }
            else
            {
                currentGravity = targetGravity;
            }
        }

        private Vector2 GetEffectiveGravity()
        {
            Vector2 effectiveGravity;

            if (isInGravityZone && currentZone != null)
            {
                effectiveGravity = currentZone.GetGravityAtPosition(transform.position);
            }
            else if (GravitySystem.Instance != null)
            {
                effectiveGravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);
            }
            else
            {
                effectiveGravity = Physics2D.gravity;
            }

            // Apply safety limits
            if (effectiveGravity.magnitude > gravitySettings.maxGravityStrength)
            {
                effectiveGravity = effectiveGravity.normalized * gravitySettings.maxGravityStrength;
            }

            return effectiveGravity;
        }

        private void ProcessGravityTransitions()
        {
            // Check if gravity has changed significantly
            float gravityChange = Vector2.Distance(targetGravity, lastGravity);

            if (gravityChange > gravitySettings.zoneInfluenceThreshold)
            {
                if (!isTransitioningGravity)
                {
                    StartGravityTransition();
                }
            }
        }

        private void StartGravityTransition()
        {
            isTransitioningGravity = true;
            transitionStartTime = Time.time;

            // Store momentum before transition
            if (gravitySettings.preserveMomentumOnFlip)
            {
                momentumBeforeFlip = rb2d.linearVelocity;
            }

            OnGravityTransition?.Invoke(lastGravity, targetGravity);

            if (showDebugInfo)
                Debug.Log($"Gravity transition started: {lastGravity} -> {targetGravity}");
        }

        private void ApplyGravityToMovement()
        {
            if (playerMovement == null) return;

            // Calculate gravity component of velocity
            gravityVelocityComponent = currentGravity * Time.fixedDeltaTime;

            // Apply to movement system if it supports direct gravity input
            // This would require extending PlayerMovement to accept external gravity
        }

        private void ProcessGravityZoneEffects()
        {
            // Update active zones list
            UpdateActiveGravityZones();

            // Handle zone-specific effects
            foreach (var zone in activeZones)
            {
                ApplyZoneEffects(zone);
            }
        }

        private void UpdateActiveGravityZones()
        {
            // This would typically be called by zone enter/exit triggers
            // For now, we'll simulate by checking distance to known zones

            if (GravitySystem.Instance != null)
            {
                var allZones = GravitySystem.Instance.gravityZones;

                foreach (var zone in allZones)
                {
                    if (zone == null) continue;

                    bool wasInZone = activeZones.Contains(zone);
                    bool isNowInZone = zone.IsPositionInZone(transform.position);

                    if (isNowInZone && !wasInZone)
                    {
                        EnterGravityZone(zone);
                    }
                    else if (!isNowInZone && wasInZone)
                    {
                        ExitGravityZone(zone);
                    }
                }
            }
        }

        private void EnterGravityZone(LocalGravityZone zone)
        {
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
                currentZone = zone; // Use most recent zone
                isInGravityZone = true;

                OnEnteredGravityZone?.Invoke(zone);

                if (showDebugInfo)
                    Debug.Log($"Entered gravity zone: {zone.name}");
            }
        }

        private void ExitGravityZone(LocalGravityZone zone)
        {
            if (activeZones.Contains(zone))
            {
                activeZones.Remove(zone);

                if (currentZone == zone)
                {
                    currentZone = activeZones.Count > 0 ? activeZones[activeZones.Count - 1] : null;
                    isInGravityZone = activeZones.Count > 0;
                }

                OnExitedGravityZone?.Invoke(zone);

                if (showDebugInfo)
                    Debug.Log($"Exited gravity zone: {zone.name}");
            }
        }

        private void ApplyZoneEffects(LocalGravityZone zone)
        {
            // Apply zone-specific effects beyond just gravity
            // This could include visual effects, sound changes, etc.

            if (zone is GravityWell well)
            {
                ApplyGravityWellEffects(well);
            }
            else if (zone is WindTunnel tunnel)
            {
                ApplyWindTunnelEffects(tunnel);
            }
        }

        private void ApplyGravityWellEffects(GravityWell well)
        {
            // Apply additional effects from gravity wells
            // Could include particle effects, camera shake, etc.
        }

        private void ApplyWindTunnelEffects(WindTunnel tunnel)
        {
            // Apply wind tunnel effects
            // Could include visual distortion, sound effects, etc.
        }

        private void UpdateGravityPrediction()
        {
            if (groundDetector == null) return;

            // Predict gravity effects over time window
            for (float t = 0.1f; t <= gravitySettings.predictionTimeWindow; t += 0.1f)
            {
                Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(
                    transform.position,
                    rb2d.linearVelocity,
                    currentGravity,
                    t
                );

                // Check if gravity will change at future position
                Vector2 futureGravity = GetGravityAtPosition(futurePos);

                if (Vector2.Distance(futureGravity, currentGravity) > gravitySettings.zoneInfluenceThreshold)
                {
                    // Prepare for upcoming gravity change
                    PrepareForGravityChange(futureGravity, t);
                    break;
                }
            }
        }

        private Vector2 GetGravityAtPosition(Vector2 position)
        {
            if (GravitySystem.Instance != null)
            {
                return GravitySystem.Instance.GetGravityAtPosition(position);
            }
            return Physics2D.gravity;
        }

        private void PrepareForGravityChange(Vector2 futureGravity, float timeUntilChange)
        {
            // Pre-adjust movement or warn player of upcoming gravity change
            if (showDebugInfo)
                Debug.Log($"Upcoming gravity change in {timeUntilChange:F1}s: {futureGravity}");
        }

        private void ValidateGravityState()
        {
            // Check for invalid gravity values
            if (float.IsNaN(currentGravity.x) || float.IsNaN(currentGravity.y) ||
                float.IsInfinity(currentGravity.x) || float.IsInfinity(currentGravity.y))
            {
                OnGravityAnomalyDetected?.Invoke(currentGravity);

                if (gravitySettings.autoRecoverFromAnomalies)
                {
                    RecoverFromGravityAnomaly();
                }
            }

            // Check for excessive gravity
            if (currentGravity.magnitude > gravitySettings.maxGravityStrength * 1.5f)
            {
                OnGravityAnomalyDetected?.Invoke(currentGravity);

                if (gravitySettings.autoRecoverFromAnomalies)
                {
                    currentGravity = currentGravity.normalized * gravitySettings.maxGravityStrength;
                }
            }
        }

        private void RecoverFromGravityAnomaly()
        {
            // Reset to safe gravity values
            if (GravitySystem.Instance != null)
            {
                currentGravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;
            }
            else
            {
                currentGravity = Vector2.down * 9.81f;
            }

            targetGravity = currentGravity;
            isTransitioningGravity = false;

            Debug.LogWarning("Recovered from gravity anomaly");
        }

        // Event handlers
        private void OnGlobalGravityChanged(Vector2 newGravityDirection)
        {
            if (showDebugInfo)
                Debug.Log($"Global gravity changed: {newGravityDirection}");
        }

        private void OnPlayerGravityFlip(GravityDirection direction)
        {
            // Handle player-initiated gravity flip
            if (gravitySettings.preserveMomentumOnFlip)
            {
                Vector2 velocity = rb2d.linearVelocity;
                velocity.y *= -gravitySettings.momentumPreservationRatio;
                rb2d.linearVelocity = velocity;
            }

            if (showDebugInfo)
                Debug.Log($"Player gravity flip: {direction}");
        }

        // Public API methods
        public Vector2 GetCurrentGravity() => currentGravity;
        public Vector2 GetTargetGravity() => targetGravity;
        public bool IsInGravityZone() => isInGravityZone;
        public LocalGravityZone GetCurrentGravityZone() => currentZone;
        public bool IsTransitioningGravity() => isTransitioningGravity;

        public void ForceGravityUpdate()
        {
            UpdateGravityState();
        }

        public void SetGravitySettings(PlayerGravitySettings newSettings)
        {
            gravitySettings = newSettings;
            ConfigureGravityAffectedObject();
        }

        // Advanced API
        public float GetGravityTransitionProgress()
        {
            if (!isTransitioningGravity) return 1f;

            float elapsed = Time.time - transitionStartTime;
            float duration = 1f / gravitySettings.gravityResponseSpeed;
            return Mathf.Clamp01(elapsed / duration);
        }

        public Vector2 PredictGravityAtPosition(Vector2 position)
        {
            return GetGravityAtPosition(position);
        }

        public bool WillEnterGravityZone(float lookAheadTime)
        {
            Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(
                transform.position,
                rb2d.linearVelocity,
                currentGravity,
                lookAheadTime
            );

            Vector2 futureGravity = GetGravityAtPosition(futurePos);
            return Vector2.Distance(futureGravity, currentGravity) > gravitySettings.zoneInfluenceThreshold;
        }

        // Debug visualization
        private void OnDrawGizmos()
        {
            if (!visualizeGravityEffects || !Application.isPlaying) return;

            // Draw current gravity
            Gizmos.color = Color.cyan;
            Vector3 gravityStart = transform.position;
            Vector3 gravityEnd = gravityStart + (Vector3)currentGravity.normalized * 2f;
            Gizmos.DrawLine(gravityStart, gravityEnd);
            Gizmos.DrawWireSphere(gravityEnd, 0.1f);

            // Draw target gravity if different
            if (Vector2.Distance(currentGravity, targetGravity) > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 targetEnd = gravityStart + (Vector3)targetGravity.normalized * 2f;
                Gizmos.DrawLine(gravityStart, targetEnd);
                Gizmos.DrawWireSphere(targetEnd, 0.08f);
            }

            // Draw gravity zones influence
            if (isInGravityZone)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            // Draw gravity prediction
            if (gravitySettings.enableGravityPrediction)
            {
                Gizmos.color = Color.green;
                Vector3 currentPos = transform.position;

                for (float t = 0.2f; t <= gravitySettings.predictionTimeWindow; t += 0.2f)
                {
                    Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(
                        currentPos,
                        rb2d.linearVelocity,
                        currentGravity,
                        t
                    );

                    Gizmos.DrawWireCube(futurePos, Vector3.one * 0.1f);
                }
            }

            // Draw momentum preservation visualization
            if (gravitySettings.preserveMomentumOnFlip && momentumBeforeFlip != Vector2.zero)
            {
                Gizmos.color = Color.red;
                Vector3 momentumEnd = transform.position + (Vector3)momentumBeforeFlip * 0.1f;
                Gizmos.DrawLine(transform.position, momentumEnd);
            }
        }

        // Performance profiling
        public float GetGravityUpdateFrequency()
        {
            return 1f / GRAVITY_UPDATE_FREQUENCY;
        }

        public int GetActiveZoneCount()
        {
            return activeZones.Count;
        }

        // Integration testing methods
        public bool TestGravityIntegration()
        {
            bool testsPassed = true;

            // Test 1: Component references
            if (playerController == null || gravityAffected == null)
            {
                Debug.LogError("Missing required components for gravity integration");
                testsPassed = false;
            }

            // Test 2: Gravity system connection
            if (GravitySystem.Instance == null)
            {
                Debug.LogWarning("GravitySystem not found - using fallback gravity");
            }

            // Test 3: Settings validation
            if (gravitySettings.gravityResponseSpeed <= 0)
            {
                Debug.LogError("Invalid gravity response speed");
                testsPassed = false;
            }

            // Test 4: Physics state validation
            if (currentGravity.magnitude == 0)
            {
                Debug.LogWarning("Zero gravity detected");
            }

            return testsPassed;
        }

        // Configuration helpers
        public void SetupForNormalPlay()
        {
            gravitySettings.gravityResponseSpeed = 8f;
            gravitySettings.preserveMomentumOnFlip = true;
            gravitySettings.enableZoneEffects = true;
            gravitySettings.enableGravityPrediction = true;
            ConfigureGravityAffectedObject();
        }

        public void SetupForPerformanceMode()
        {
            gravitySettings.enableGravityPrediction = false;
            gravitySettings.enableZoneEffects = false;
            gravitySettings.enableGravityValidation = false;
            // Reduce update frequency for better performance
        }

        public void SetupForDebugMode()
        {
            showDebugInfo = true;
            visualizeGravityEffects = true;
            gravitySettings.enableGravityValidation = true;
            gravitySettings.autoRecoverFromAnomalies = true;
        }
    }

    // Extension methods for easier integration
    public static class PlayerGravityExtensions
    {
        public static bool HasGravityIntegration(this PlayerController player)
        {
            return player.GetComponent<PlayerGravityIntegration>() != null;
        }

        public static PlayerGravityIntegration GetGravityIntegration(this PlayerController player)
        {
            return player.GetComponent<PlayerGravityIntegration>();
        }

        public static Vector2 GetEffectiveGravity(this PlayerController player)
        {
            var integration = player.GetGravityIntegration();
            return integration != null ? integration.GetCurrentGravity() : Physics2D.gravity;
        }

        public static bool IsInCustomGravityZone(this PlayerController player)
        {
            var integration = player.GetGravityIntegration();
            return integration != null && integration.IsInGravityZone();
        }
    }
}