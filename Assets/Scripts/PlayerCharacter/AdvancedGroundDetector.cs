using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    [System.Serializable]
    public class GroundDetectionSettings
    {
        [Header("Detection Configuration")]
        public LayerMask groundLayers = 1;
        public LayerMask oneWayPlatformLayers = 1;
        public LayerMask slopeLayerMask = 1;

        [Header("Raycast Settings")]
        public float groundCheckDistance = 0.6f;
        public float sideCheckDistance = 0.3f;
        public int groundRayCount = 5;
        public float raySpread = 0.6f;

        [Header("Edge Detection")]
        public bool enableEdgeDetection = true;
        public float edgeDetectionDistance = 1.0f;
        public float edgeThreshold = 0.3f;

        [Header("Slope Detection")]
        public float maxWalkableAngle = 45f;
        public float slopeDetectionSensitivity = 0.1f;
        public bool enableSlopeSnapping = true;

        [Header("Performance")]
        public int detectionFrequency = 1; // Every N physics frames
        public bool useFixedUpdate = true;
        public bool enableCaching = true;
    }

    [System.Serializable]
    public class GroundInfo
    {
        public bool isGrounded;
        public bool isOnSlope;
        public bool isOnEdge;
        public bool isOnOneWayPlatform;

        public Vector2 groundNormal;
        public Vector2 slopeDirection;
        public float groundAngle;
        public float distanceToGround;

        public Collider2D groundCollider;
        public Vector2 groundPoint;
        public PhysicsMaterial2D groundMaterial;

        public float slopeInfluence;
        public bool canWalkOnSlope;

        // Edge detection
        public bool hasLeftEdge;
        public bool hasRightEdge;
        public float leftEdgeDistance;
        public float rightEdgeDistance;
    }

    public class AdvancedGroundDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        public GroundDetectionSettings settings = new GroundDetectionSettings();

        [Header("Debug Visualization")]
        public bool showDebugRays = true;
        public bool showGroundInfo = true;
        public bool logGroundChanges = false;

        // Current ground state
        public GroundInfo currentGroundInfo { get; private set; } = new GroundInfo();

        // Components
        private PlayerController playerController;
        private Rigidbody2D rb2d;
        private Collider2D playerCollider;
        private GravityAffectedObject gravityAffected;

        // Detection state
        private Transform[] groundCheckPoints;
        private RaycastHit2D[] groundHits;
        private Vector2 currentGravityDirection;

        // Performance optimization
        private int frameCounter = 0;
        private GroundInfo cachedGroundInfo;
        private float lastDetectionTime = 0f;

        // Events
        public System.Action<GroundInfo> OnGroundStateChanged;
        public System.Action OnLanded;
        public System.Action OnLeftGround;
        public System.Action<bool> OnSlopeChanged;
        public System.Action<bool> OnEdgeDetected;

        private void Awake()
        {
            InitializeComponents();
            SetupGroundCheckPoints();
            InitializeGroundInfo();
        }

        private void InitializeComponents()
        {
            playerController = GetComponent<PlayerController>();
            rb2d = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            gravityAffected = GetComponent<GravityAffectedObject>();

            groundHits = new RaycastHit2D[settings.groundRayCount];
        }

        private void SetupGroundCheckPoints()
        {
            groundCheckPoints = new Transform[settings.groundRayCount];

            for (int i = 0; i < settings.groundRayCount; i++)
            {
                GameObject checkPoint = new GameObject($"GroundCheck_{i}");
                checkPoint.transform.SetParent(transform);

                // Distribute points across the bottom of the player
                float normalizedPosition = (float)i / (settings.groundRayCount - 1); // 0 to 1
                float xOffset = (normalizedPosition - 0.5f) * settings.raySpread;
                checkPoint.transform.localPosition = new Vector3(xOffset, -0.5f, 0);

                groundCheckPoints[i] = checkPoint.transform;
            }
        }

        private void InitializeGroundInfo()
        {
            currentGroundInfo = new GroundInfo();
            cachedGroundInfo = new GroundInfo();
        }

        private void FixedUpdate()
        {
            if (!settings.useFixedUpdate) return;

            frameCounter++;
            if (frameCounter % settings.detectionFrequency == 0)
            {
                PerformGroundDetection();
            }
        }

        private void Update()
        {
            if (settings.useFixedUpdate) return;

            frameCounter++;
            if (frameCounter % settings.detectionFrequency == 0)
            {
                PerformGroundDetection();
            }
        }

        public void PerformGroundDetection()
        {
            // Store previous state for change detection
            GroundInfo previousInfo = CopyGroundInfo(currentGroundInfo);

            // Update gravity direction
            UpdateGravityDirection();

            // Perform main ground detection
            DetectGround();

            // Perform slope analysis
            AnalyzeSlope();

            // Perform edge detection
            if (settings.enableEdgeDetection)
            {
                DetectEdges();
            }

            // Calculate additional properties
            CalculateGroundProperties();

            // Check for state changes and fire events
            CheckForStateChanges(previousInfo);

            // Cache results if enabled
            if (settings.enableCaching)
            {
                cachedGroundInfo = CopyGroundInfo(currentGroundInfo);
                lastDetectionTime = Time.time;
            }
        }

        private void UpdateGravityDirection()
        {
            if (gravityAffected != null)
            {
                Vector2 gravity = gravityAffected.GetCurrentGravity();
                currentGravityDirection = gravity.normalized;
            }
            else if (GravitySystem.Instance != null)
            {
                currentGravityDirection = GravitySystem.Instance.CurrentGravityDirection;
            }
            else
            {
                currentGravityDirection = Vector2.down;
            }
        }

        private void DetectGround()
        {
            currentGroundInfo.isGrounded = false;
            currentGroundInfo.distanceToGround = float.MaxValue;
            currentGroundInfo.groundNormal = -currentGravityDirection;

            int validHits = 0;
            Vector2 averageNormal = Vector2.zero;
            Vector2 closestPoint = Vector2.zero;
            float closestDistance = float.MaxValue;
            Collider2D closestCollider = null;

            // Perform raycasts from all check points
            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                Vector2 rayOrigin = groundCheckPoints[i].position;
                Vector2 rayDirection = currentGravityDirection;

                groundHits[i] = Physics2D.Raycast(
                    rayOrigin,
                    rayDirection,
                    settings.groundCheckDistance,
                    settings.groundLayers
                );

                if (groundHits[i].collider != null)
                {
                    currentGroundInfo.isGrounded = true;
                    validHits++;
                    averageNormal += groundHits[i].normal;

                    if (groundHits[i].distance < closestDistance)
                    {
                        closestDistance = groundHits[i].distance;
                        closestPoint = groundHits[i].point;
                        closestCollider = groundHits[i].collider;
                    }
                }
            }

            if (validHits > 0)
            {
                currentGroundInfo.groundNormal = (averageNormal / validHits).normalized;
                currentGroundInfo.distanceToGround = closestDistance;
                currentGroundInfo.groundPoint = closestPoint;
                currentGroundInfo.groundCollider = closestCollider;

                // Check for one-way platforms
                CheckOneWayPlatform(closestCollider);

                // Get ground material
                GetGroundMaterial(closestCollider);
            }
        }

        private void CheckOneWayPlatform(Collider2D groundCollider)
        {
            if (groundCollider == null) return;

            currentGroundInfo.isOnOneWayPlatform =
                ((1 << groundCollider.gameObject.layer) & settings.oneWayPlatformLayers) != 0;

            // Additional check for PlatformEffector2D
            PlatformEffector2D effector = groundCollider.GetComponent<PlatformEffector2D>();
            if (effector != null && effector.useOneWay)
            {
                currentGroundInfo.isOnOneWayPlatform = true;
            }
        }

        private void GetGroundMaterial(Collider2D groundCollider)
        {
            if (groundCollider == null) return;

            currentGroundInfo.groundMaterial = groundCollider.sharedMaterial;
        }

        private void AnalyzeSlope()
        {
            if (!currentGroundInfo.isGrounded)
            {
                currentGroundInfo.isOnSlope = false;
                return;
            }

            // Calculate ground angle relative to gravity
            Vector2 upDirection = -currentGravityDirection;
            currentGroundInfo.groundAngle = Vector2.Angle(currentGroundInfo.groundNormal, upDirection);

            // Determine if on slope
            currentGroundInfo.isOnSlope = currentGroundInfo.groundAngle > settings.slopeDetectionSensitivity;
            currentGroundInfo.canWalkOnSlope = currentGroundInfo.groundAngle <= settings.maxWalkableAngle;

            if (currentGroundInfo.isOnSlope)
            {
                // Calculate slope direction (perpendicular to normal)
                currentGroundInfo.slopeDirection = Vector2.Perpendicular(currentGroundInfo.groundNormal);

                // Ensure slope direction points in the movement direction
                if (currentGroundInfo.slopeDirection.x < 0)
                    currentGroundInfo.slopeDirection = -currentGroundInfo.slopeDirection;

                // Calculate slope influence (0 = flat, 1 = max angle)
                currentGroundInfo.slopeInfluence = currentGroundInfo.groundAngle / settings.maxWalkableAngle;
                currentGroundInfo.slopeInfluence = Mathf.Clamp01(currentGroundInfo.slopeInfluence);
            }
            else
            {
                currentGroundInfo.slopeDirection = Vector2.right;
                currentGroundInfo.slopeInfluence = 0f;
            }
        }

        private void DetectEdges()
        {
            currentGroundInfo.isOnEdge = false;
            currentGroundInfo.hasLeftEdge = false;
            currentGroundInfo.hasRightEdge = false;

            if (!currentGroundInfo.isGrounded) return;

            // Check for left edge
            Vector2 leftCheckPosition = transform.position + Vector3.left * settings.edgeDetectionDistance;
            RaycastHit2D leftHit = Physics2D.Raycast(
                leftCheckPosition,
                currentGravityDirection,
                settings.groundCheckDistance + settings.edgeThreshold,
                settings.groundLayers
            );

            currentGroundInfo.hasLeftEdge = leftHit.collider == null;
            currentGroundInfo.leftEdgeDistance = leftHit.collider != null ? leftHit.distance : float.MaxValue;

            // Check for right edge
            Vector2 rightCheckPosition = transform.position + Vector3.right * settings.edgeDetectionDistance;
            RaycastHit2D rightHit = Physics2D.Raycast(
                rightCheckPosition,
                currentGravityDirection,
                settings.groundCheckDistance + settings.edgeThreshold,
                settings.groundLayers
            );

            currentGroundInfo.hasRightEdge = rightHit.collider == null;
            currentGroundInfo.rightEdgeDistance = rightHit.collider != null ? rightHit.distance : float.MaxValue;

            // Set overall edge state
            currentGroundInfo.isOnEdge = currentGroundInfo.hasLeftEdge || currentGroundInfo.hasRightEdge;
        }

        private void CalculateGroundProperties()
        {
            // Additional calculations and refinements can be added here

            // Adjust ground distance based on player collider
            if (currentGroundInfo.isGrounded && playerCollider != null)
            {
                float colliderBottom = playerCollider.bounds.min.y;
                float adjustedDistance = currentGroundInfo.groundPoint.y - colliderBottom;
                currentGroundInfo.distanceToGround = Mathf.Max(0, adjustedDistance);
            }
        }

        private void CheckForStateChanges(GroundInfo previousInfo)
        {
            // Check for landing
            if (currentGroundInfo.isGrounded && !previousInfo.isGrounded)
            {
                OnLanded?.Invoke();
                if (logGroundChanges)
                    Debug.Log("Player landed");
            }

            // Check for leaving ground
            if (!currentGroundInfo.isGrounded && previousInfo.isGrounded)
            {
                OnLeftGround?.Invoke();
                if (logGroundChanges)
                    Debug.Log("Player left ground");
            }

            // Check for slope changes
            if (currentGroundInfo.isOnSlope != previousInfo.isOnSlope)
            {
                OnSlopeChanged?.Invoke(currentGroundInfo.isOnSlope);
                if (logGroundChanges)
                    Debug.Log($"Slope state changed: {currentGroundInfo.isOnSlope}");
            }

            // Check for edge detection changes
            if (currentGroundInfo.isOnEdge != previousInfo.isOnEdge)
            {
                OnEdgeDetected?.Invoke(currentGroundInfo.isOnEdge);
                if (logGroundChanges)
                    Debug.Log($"Edge state changed: {currentGroundInfo.isOnEdge}");
            }

            // Fire general state change event
            OnGroundStateChanged?.Invoke(currentGroundInfo);
        }

        private GroundInfo CopyGroundInfo(GroundInfo source)
        {
            return new GroundInfo
            {
                isGrounded = source.isGrounded,
                isOnSlope = source.isOnSlope,
                isOnEdge = source.isOnEdge,
                isOnOneWayPlatform = source.isOnOneWayPlatform,
                groundNormal = source.groundNormal,
                slopeDirection = source.slopeDirection,
                groundAngle = source.groundAngle,
                distanceToGround = source.distanceToGround,
                groundCollider = source.groundCollider,
                groundPoint = source.groundPoint,
                groundMaterial = source.groundMaterial,
                slopeInfluence = source.slopeInfluence,
                canWalkOnSlope = source.canWalkOnSlope,
                hasLeftEdge = source.hasLeftEdge,
                hasRightEdge = source.hasRightEdge,
                leftEdgeDistance = source.leftEdgeDistance,
                rightEdgeDistance = source.rightEdgeDistance
            };
        }

        // Public API methods
        public bool IsGrounded() => currentGroundInfo.isGrounded;
        public bool IsOnSlope() => currentGroundInfo.isOnSlope;
        public bool IsOnEdge() => currentGroundInfo.isOnEdge;
        public bool CanWalkOnSlope() => currentGroundInfo.canWalkOnSlope;

        public Vector2 GetGroundNormal() => currentGroundInfo.groundNormal;
        public Vector2 GetSlopeDirection() => currentGroundInfo.slopeDirection;
        public float GetGroundAngle() => currentGroundInfo.groundAngle;
        public float GetSlopeInfluence() => currentGroundInfo.slopeInfluence;

        public float GetDistanceToGround() => currentGroundInfo.distanceToGround;
        public Collider2D GetGroundCollider() => currentGroundInfo.groundCollider;
        public PhysicsMaterial2D GetGroundMaterial() => currentGroundInfo.groundMaterial;

        // Advanced queries
        public bool WillLandOnGround(float lookAheadTime)
        {
            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = currentGravityDirection * 9.81f; // Use standard gravity for prediction

            Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(currentPos, currentVel, gravity, lookAheadTime);

            // Check if there's ground at the predicted position
            RaycastHit2D hit = Physics2D.Raycast(futurePos, currentGravityDirection, settings.groundCheckDistance, settings.groundLayers);
            return hit.collider != null;
        }

        public float GetTimeToLanding()
        {
            if (currentGroundInfo.isGrounded) return 0f;

            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = currentGravityDirection * 9.81f;

            // Find ground level below
            RaycastHit2D hit = Physics2D.Raycast(currentPos, currentGravityDirection, 50f, settings.groundLayers);
            if (hit.collider != null)
            {
                float timeToCollision;
                if (GravityPhysicsUtils.WillCollideWithGround(currentPos, currentVel, gravity, hit.point.y, out timeToCollision))
                {
                    return timeToCollision;
                }
            }

            return -1f; // No landing predicted
        }

        public bool IsNearEdge(float threshold = 1f)
        {
            if (!currentGroundInfo.isGrounded) return false;

            return (currentGroundInfo.hasLeftEdge && currentGroundInfo.leftEdgeDistance < threshold) ||
                   (currentGroundInfo.hasRightEdge && currentGroundInfo.rightEdgeDistance < threshold);
        }

        // Configuration methods
        public void UpdateSettings(GroundDetectionSettings newSettings)
        {
            settings = newSettings;

            // Recreate ground check points if ray count changed
            if (groundCheckPoints.Length != settings.groundRayCount)
            {
                // Clean up existing points
                for (int i = 0; i < groundCheckPoints.Length; i++)
                {
                    if (groundCheckPoints[i] != null)
                    {
                        DestroyImmediate(groundCheckPoints[i].gameObject);
                    }
                }

                SetupGroundCheckPoints();
                groundHits = new RaycastHit2D[settings.groundRayCount];
            }
        }

        public GroundDetectionSettings GetSettings()
        {
            return settings;
        }

        // Force immediate detection (useful for teleports, etc.)
        public void ForceDetection()
        {
            PerformGroundDetection();
        }

        // Snap to ground if close enough
        public bool SnapToGround(float snapDistance = 0.1f)
        {
            if (currentGroundInfo.isGrounded) return true;

            Vector2 rayOrigin = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, currentGravityDirection, snapDistance, settings.groundLayers);

            if (hit.collider != null)
            {
                Vector3 snapPosition = transform.position;
                snapPosition.y = hit.point.y - (playerCollider.bounds.size.y * 0.5f);
                transform.position = snapPosition;

                // Force detection update
                PerformGroundDetection();
                return true;
            }

            return false;
        }

        // Ground material-based physics properties
        public float GetGroundFriction()
        {
            if (currentGroundInfo.groundMaterial != null)
            {
                return currentGroundInfo.groundMaterial.friction;
            }
            return 0.4f; // Default friction
        }

        public float GetGroundBounciness()
        {
            if (currentGroundInfo.groundMaterial != null)
            {
                return currentGroundInfo.groundMaterial.bounciness;
            }
            return 0f; // Default no bounce
        }

        // Performance monitoring
        public float GetDetectionFrequency()
        {
            return 1f / settings.detectionFrequency;
        }

        public void SetDetectionFrequency(int frequency)
        {
            settings.detectionFrequency = Mathf.Max(1, frequency);
        }

        // Debug and visualization
        private void OnDrawGizmos()
        {
            if (!showDebugRays || groundCheckPoints == null) return;

            // Draw ground check rays
            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                if (groundCheckPoints[i] == null) continue;

                Vector3 rayStart = groundCheckPoints[i].position;
                Vector3 rayEnd = rayStart + (Vector3)currentGravityDirection * settings.groundCheckDistance;

                // Color based on hit detection
                if (Application.isPlaying && i < groundHits.Length && groundHits[i].collider != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(rayStart, groundHits[i].point);
                    Gizmos.DrawWireSphere(groundHits[i].point, 0.05f);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(rayStart, rayEnd);
                }
            }

            if (!Application.isPlaying) return;

            // Draw ground normal
            if (currentGroundInfo.isGrounded)
            {
                Gizmos.color = Color.blue;
                Vector3 normalStart = currentGroundInfo.groundPoint;
                Vector3 normalEnd = normalStart + (Vector3)currentGroundInfo.groundNormal * 1f;
                Gizmos.DrawLine(normalStart, normalEnd);
            }

            // Draw slope direction
            if (currentGroundInfo.isOnSlope)
            {
                Gizmos.color = Color.yellow;
                Vector3 slopeStart = transform.position;
                Vector3 slopeEnd = slopeStart + (Vector3)currentGroundInfo.slopeDirection * 1f;
                Gizmos.DrawLine(slopeStart, slopeEnd);
            }

            // Draw edge detection
            if (settings.enableEdgeDetection)
            {
                Gizmos.color = currentGroundInfo.hasLeftEdge ? Color.red : Color.green;
                Vector3 leftCheck = transform.position + Vector3.left * settings.edgeDetectionDistance;
                Gizmos.DrawWireCube(leftCheck, Vector3.one * 0.1f);

                Gizmos.color = currentGroundInfo.hasRightEdge ? Color.red : Color.green;
                Vector3 rightCheck = transform.position + Vector3.right * settings.edgeDetectionDistance;
                Gizmos.DrawWireCube(rightCheck, Vector3.one * 0.1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGroundInfo || !Application.isPlaying) return;

            // Draw detailed ground information
            Vector3 infoPosition = transform.position + Vector3.up * 2f;

            // Ground state indicator
            Gizmos.color = currentGroundInfo.isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(infoPosition, Vector3.one * 0.2f);

            // Slope indicator
            if (currentGroundInfo.isOnSlope)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(infoPosition + Vector3.right * 0.5f, Vector3.one * 0.15f);
            }

            // Edge indicator
            if (currentGroundInfo.isOnEdge)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(infoPosition + Vector3.left * 0.5f, Vector3.one * 0.15f);
            }
        }

        // Ground state validation
        public bool ValidateGroundState()
        {
            bool isValid = true;

            // Check for impossible states
            if (currentGroundInfo.isGrounded && currentGroundInfo.distanceToGround > settings.groundCheckDistance)
            {
                Debug.LogWarning("Invalid ground state: Grounded but distance too large");
                isValid = false;
            }

            if (currentGroundInfo.groundAngle < 0 || currentGroundInfo.groundAngle > 90f)
            {
                Debug.LogWarning($"Invalid ground angle: {currentGroundInfo.groundAngle}");
                isValid = false;
            }

            if (currentGroundInfo.slopeInfluence < 0 || currentGroundInfo.slopeInfluence > 1f)
            {
                Debug.LogWarning($"Invalid slope influence: {currentGroundInfo.slopeInfluence}");
                isValid = false;
            }

            return isValid;
        }

        // Integration with player systems
        public void OnPlayerGravityFlip(GravityDirection newDirection)
        {
            // Immediately update gravity direction and perform detection
            UpdateGravityDirection();
            PerformGroundDetection();

            if (logGroundChanges)
                Debug.Log($"Ground detector responded to gravity flip: {newDirection}");
        }

        // Advanced ground prediction
        public GroundInfo PredictGroundState(float timeOffset)
        {
            Vector2 futurePos = GravityPhysicsUtils.CalculateTrajectory(
                transform.position,
                rb2d.linearVelocity,
                currentGravityDirection * 9.81f,
                timeOffset
            );

            // Temporarily move check points to future position
            Vector3 positionDelta = futurePos - (Vector2)transform.position;

            GroundInfo predictedInfo = new GroundInfo();

            // Perform detection at predicted position
            for (int i = 0; i < groundCheckPoints.Length; i++)
            {
                Vector2 rayOrigin = (Vector2)groundCheckPoints[i].position + (Vector2)positionDelta;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, currentGravityDirection, settings.groundCheckDistance, settings.groundLayers);

                if (hit.collider != null)
                {
                    predictedInfo.isGrounded = true;
                    predictedInfo.groundNormal = hit.normal;
                    predictedInfo.distanceToGround = hit.distance;
                    predictedInfo.groundPoint = hit.point;
                    break;
                }
            }

            return predictedInfo;
        }

        // Cleanup
        private void OnDestroy()
        {
            // Clean up ground check points
            if (groundCheckPoints != null)
            {
                for (int i = 0; i < groundCheckPoints.Length; i++)
                {
                    if (groundCheckPoints[i] != null)
                    {
                        DestroyImmediate(groundCheckPoints[i].gameObject);
                    }
                }
            }
        }
    }
}