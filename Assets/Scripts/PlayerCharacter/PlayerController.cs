using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;

namespace GravityFlipLab.Player
{
    #region Player Data Structures

    [System.Serializable]
    public class PlayerStats
    {
        [Header("Movement")]
        public float moveSpeed = 5.0f;
        public float fallSpeed = 10.0f;
        public float maxFallSpeed = 20.0f;
        public float gravityScale = 1.0f;

        [Header("Gravity Flip")]
        public float gravityFlipCooldown = 0.1f;
        public float flipInvincibilityDuration = 0.1f;
        public bool preserveHorizontalMomentum = true;
        public float verticalMomentumRetention = 0.3f;

        [Header("Safety")]
        public float invincibilityDuration = 0.1f;
        public bool isInvincible = false;
        public int livesRemaining = 3;

        [Header("Physics")]
        public float airDrag = 0.95f;
        public float groundFriction = 0.98f;
        public float maxVelocityMagnitude = 25f;
    }

    public enum PlayerState
    {
        Running,
        Falling,
        GravityFlipping,
        Dead,
        Invincible
    }

    public enum GravityDirection
    {
        Down = -1,
        Up = 1
    }

    #endregion

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(GravityAffectedObject))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Components")]
        public PlayerAnimation playerAnimation;
        public PlayerCollision playerCollision;
        public PlayerVisuals playerVisuals;

        [Header("Player Stats")]
        public PlayerStats stats = new PlayerStats();

        [Header("Ground Detection")]
        public LayerMask groundLayerMask = 1;
        public float groundCheckDistance = 0.6f;
        public Transform groundCheckPoint;

        [Header("Debug")]
        public bool debugMode = false;
        public bool showTrajectoryPrediction = false;
        public int trajectoryPoints = 20;

        // State
        public PlayerState currentState { get; private set; } = PlayerState.Running;
        public GravityDirection gravityDirection { get; private set; } = GravityDirection.Down;
        public bool isAlive { get; private set; } = true;

        // Components
        private Rigidbody2D rb2d;
        private Collider2D playerCollider;
        private GravityAffectedObject gravityAffected;

        // Physics state
        private bool isGrounded = false;
        private RaycastHit2D groundHit;
        private Vector2 lastValidVelocity;
        private float lastGravityFlipTime = -1f;

        // Trajectory prediction
        private LineRenderer trajectoryRenderer;
        private Vector2[] trajectoryPoints_array;

        // Events
        public static event System.Action OnPlayerDeath;
        public static event System.Action OnPlayerRespawn;
        public static event System.Action<GravityDirection> OnGravityFlip;
        public static event System.Action<PlayerState> OnStateChanged;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void InitializeComponents()
        {
            rb2d = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            gravityAffected = GetComponent<GravityAffectedObject>();

            // Get or add required components
            if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
            if (playerCollision == null) playerCollision = GetComponent<PlayerCollision>();
            if (playerVisuals == null) playerVisuals = GetComponent<PlayerVisuals>();

            // Initialize components
            if (playerAnimation != null) playerAnimation.Initialize(this);
            if (playerCollision != null) playerCollision.Initialize(this);
            if (playerVisuals != null) playerVisuals.Initialize(this);

            // Setup ground check point
            if (groundCheckPoint == null)
            {
                GameObject checkPoint = new GameObject("GroundCheckPoint");
                checkPoint.transform.SetParent(transform);
                checkPoint.transform.localPosition = new Vector3(0, -0.5f, 0);
                groundCheckPoint = checkPoint.transform;
            }

            // Setup trajectory visualization
            if (showTrajectoryPrediction)
            {
                SetupTrajectoryVisualization();
            }

            // Configure GravityAffectedObject for player-specific behavior
            ConfigureGravityAffectedObject();
        }

        private void ConfigureGravityAffectedObject()
        {
            if (gravityAffected != null)
            {
                gravityAffected.gravityScale = stats.gravityScale;
                gravityAffected.useCustomGravity = true;
                gravityAffected.smoothGravityTransition = true;
                gravityAffected.transitionSpeed = 10f; // Fast transition for responsive feel
                gravityAffected.maintainInertia = stats.preserveHorizontalMomentum;
                gravityAffected.inertiaDecay = 0.95f;
                gravityAffected.maxVelocityChange = stats.maxVelocityMagnitude;
            }
        }

        private void Initialize()
        {
            // Set initial state
            ChangeState(PlayerState.Running);
            gravityDirection = GravityDirection.Down;
            isAlive = true;

            // Subscribe to gravity system events
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged += OnGlobalGravityChanged;
            }

            // Apply assist mode if enabled
            if (GameManager.Instance != null && GameManager.Instance.playerProgress.settings.assistModeEnabled)
            {
                stats.livesRemaining += ConfigManager.Instance.assistModeBarrierCount;
            }

            if (debugMode)
                Debug.Log("Enhanced PlayerController initialized with GravitySystem integration");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (GravitySystem.Instance != null)
            {
                GravitySystem.OnGlobalGravityChanged -= OnGlobalGravityChanged;
            }
        }

        private void Update()
        {
            if (!isAlive) return;

            // Handle input
            HandleInput();

            // Update state logic
            UpdateStateLogic();

            // Update trajectory prediction
            if (showTrajectoryPrediction && trajectoryRenderer != null)
            {
                UpdateTrajectoryPrediction();
            }
        }

        private void FixedUpdate()
        {
            if (!isAlive) return;

            // Ground detection
            CheckGrounded();

            // Auto-run movement
            ApplyAutoRun();

            // Apply physics constraints
            ApplyPhysicsConstraints();

            // Store last valid velocity for safety
            if (rb2d.linearVelocity.magnitude < stats.maxVelocityMagnitude)
            {
                lastValidVelocity = rb2d.linearVelocity;
            }
        }

        private void HandleInput()
        {
            // Check cooldown
            if (Time.time - lastGravityFlipTime < stats.gravityFlipCooldown) return;

            // Gravity flip input
            if (Input.GetKeyDown(GameManager.Instance.playerProgress.settings.primaryInput) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetMouseButtonDown(0))
            {
                FlipGravity();
            }
        }

        private void UpdateStateLogic()
        {
            switch (currentState)
            {
                case PlayerState.Running:
                    if (Mathf.Abs(rb2d.linearVelocity.y) > 0.1f && !isGrounded)
                    {
                        ChangeState(PlayerState.Falling);
                    }
                    break;

                case PlayerState.Falling:
                    if (Mathf.Abs(rb2d.linearVelocity.y) < 0.1f && isGrounded)
                    {
                        ChangeState(PlayerState.Running);
                    }
                    break;

                case PlayerState.GravityFlipping:
                    // Automatically return to appropriate state after flip
                    if (Time.time - lastGravityFlipTime > stats.flipInvincibilityDuration)
                    {
                        if (isGrounded && Mathf.Abs(rb2d.linearVelocity.y) < 0.1f)
                            ChangeState(PlayerState.Running);
                        else
                            ChangeState(PlayerState.Falling);
                    }
                    break;

                case PlayerState.Dead:
                    // Handle death state
                    break;
            }
        }

        private void CheckGrounded()
        {
            Vector2 rayDirection = (gravityDirection == GravityDirection.Down) ?
                Vector2.down : Vector2.up;

            groundHit = Physics2D.Raycast(groundCheckPoint.position, rayDirection,
                groundCheckDistance, groundLayerMask);

            isGrounded = groundHit.collider != null;
        }

        private void ApplyAutoRun()
        {
            Vector2 velocity = rb2d.linearVelocity;

            // Maintain constant horizontal speed
            velocity.x = stats.moveSpeed;

            // Apply slope physics if on a slope
            if (isGrounded && groundHit.normal != Vector2.up)
            {
                ApplySlopePhysics(ref velocity);
            }

            rb2d.linearVelocity = velocity;
        }

        private void ApplySlopePhysics(ref Vector2 velocity)
        {
            float slopeAngle = Vector2.Angle(groundHit.normal, Vector2.up);

            if (slopeAngle > 5f && slopeAngle < 45f) // Only on reasonable slopes
            {
                // Boost speed on slopes for momentum conservation
                float slopeMultiplier = 1f + (slopeAngle / 45f) * 0.2f; // Up to 20% boost
                velocity.x *= slopeMultiplier;

                // Add slight upward velocity on upward slopes
                if (groundHit.normal.y > 0.7f) // Not too steep
                {
                    Vector2 slopeDirection = Vector2.Perpendicular(groundHit.normal);
                    if (slopeDirection.x < 0) slopeDirection = -slopeDirection;

                    float slopeForce = slopeAngle / 45f * stats.moveSpeed * 0.1f;
                    velocity.y += slopeDirection.y * slopeForce;
                }
            }
        }

        private void ApplyPhysicsConstraints()
        {
            Vector2 velocity = rb2d.linearVelocity;

            // Apply drag
            if (isGrounded)
            {
                velocity.y *= stats.groundFriction;
            }
            else
            {
                velocity *= stats.airDrag;
            }

            // Limit maximum velocity
            if (velocity.magnitude > stats.maxVelocityMagnitude)
            {
                velocity = velocity.normalized * stats.maxVelocityMagnitude;
            }

            // Limit fall speed
            float gravityDir = (float)gravityDirection;
            float maxFall = stats.maxFallSpeed * gravityDir;

            if (gravityDirection == GravityDirection.Down)
            {
                velocity.y = Mathf.Max(velocity.y, -stats.maxFallSpeed);
            }
            else
            {
                velocity.y = Mathf.Min(velocity.y, stats.maxFallSpeed);
            }

            rb2d.linearVelocity = velocity;
        }

        public void FlipGravity()
        {
            if (currentState == PlayerState.Dead) return;
            if (Time.time - lastGravityFlipTime < stats.gravityFlipCooldown) return;

            // Use GravitySystem for global gravity flip
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.FlipGlobalGravity();
            }

            lastGravityFlipTime = Time.time;
            StartCoroutine(GravityFlipCoroutine());
        }

        private IEnumerator GravityFlipCoroutine()
        {
            ChangeState(PlayerState.GravityFlipping);

            // Apply momentum preservation
            if (stats.preserveHorizontalMomentum)
            {
                Vector2 velocity = rb2d.linearVelocity;

                // Preserve horizontal momentum, modify vertical
                velocity.y = -velocity.y * stats.verticalMomentumRetention;

                rb2d.linearVelocity = velocity;
            }

            // Visual and audio effects
            if (playerVisuals != null)
                playerVisuals.PlayGravityFlipEffect();

            // Temporary invincibility
            if (stats.flipInvincibilityDuration > 0)
            {
                StartCoroutine(TemporaryInvincibility(stats.flipInvincibilityDuration));
            }

            // Update gravity direction tracking
            gravityDirection = (gravityDirection == GravityDirection.Down) ?
                GravityDirection.Up : GravityDirection.Down;

            OnGravityFlip?.Invoke(gravityDirection);

            // Wait for flip effect duration
            yield return new WaitForSeconds(stats.flipInvincibilityDuration);

            // Return to appropriate state
            if (isGrounded && Mathf.Abs(rb2d.linearVelocity.y) < 0.1f)
                ChangeState(PlayerState.Running);
            else
                ChangeState(PlayerState.Falling);
        }

        private IEnumerator TemporaryInvincibility(float duration)
        {
            stats.isInvincible = true;
            if (playerVisuals != null)
                playerVisuals.SetInvincibleVisuals(true);

            yield return new WaitForSeconds(duration);

            stats.isInvincible = false;
            if (playerVisuals != null)
                playerVisuals.SetInvincibleVisuals(false);
        }

        private void OnGlobalGravityChanged(Vector2 newGravityDirection)
        {
            // Respond to global gravity changes
            if (playerAnimation != null)
            {
                GravityDirection newDir = (newGravityDirection.y < 0) ?
                    GravityDirection.Down : GravityDirection.Up;
                playerAnimation.UpdateGravityDirection(newDir);
            }
        }

        public void TakeDamage()
        {
            if (currentState == PlayerState.Dead || stats.isInvincible) return;

            stats.livesRemaining--;
            if (GameManager.Instance != null)
                GameManager.Instance.sessionDeathCount++;

            if (stats.livesRemaining <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(TemporaryInvincibility(stats.invincibilityDuration));
            }
        }

        private void Die()
        {
            if (!isAlive) return;

            isAlive = false;
            ChangeState(PlayerState.Dead);

            // Stop movement
            rb2d.linearVelocity = Vector2.zero;
            rb2d.gravityScale = 0;

            // Disable gravity effects
            if (gravityAffected != null)
                gravityAffected.useCustomGravity = false;

            // Play death effects
            if (playerVisuals != null) playerVisuals.PlayDeathEffect();
            if (playerAnimation != null) playerAnimation.PlayDeathAnimation();

            OnPlayerDeath?.Invoke();

            // Respawn after delay
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(1.0f);
            Respawn();
        }

        public void Respawn()
        {
            // Reset position to last checkpoint
            Vector3 respawnPosition = CheckpointManager.Instance.GetCurrentCheckpointPosition();
            transform.position = respawnPosition;

            // Reset state
            isAlive = true;
            gravityDirection = GravityDirection.Down;
            ChangeState(PlayerState.Running);

            // Reset physics
            rb2d.gravityScale = stats.gravityScale;
            rb2d.linearVelocity = Vector2.zero;

            // Re-enable gravity effects
            if (gravityAffected != null)
            {
                gravityAffected.useCustomGravity = true;
                gravityAffected.ResetToOriginalGravity();
            }

            // Reset visuals
            if (playerVisuals != null) playerVisuals.ResetVisuals();

            OnPlayerRespawn?.Invoke();
        }

        private void ChangeState(PlayerState newState)
        {
            if (currentState != newState)
            {
                PlayerState previousState = currentState;
                currentState = newState;

                OnStateChanged?.Invoke(newState);
                if (playerAnimation != null)
                    playerAnimation.OnStateChanged(newState);

                if (debugMode)
                    Debug.Log($"Player state: {previousState} -> {newState}");
            }
        }

        private void SetupTrajectoryVisualization()
        {
            trajectoryRenderer = gameObject.AddComponent<LineRenderer>();
            trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryRenderer.endColor = Color.yellow;
            trajectoryRenderer.startWidth = 0.05f;
            trajectoryRenderer.endWidth = 0.05f;
            trajectoryRenderer.positionCount = trajectoryPoints;
            trajectoryRenderer.useWorldSpace = true;

            trajectoryPoints_array = new Vector2[trajectoryPoints];
        }

        private void UpdateTrajectoryPrediction()
        {
            if (trajectoryRenderer == null || !showTrajectoryPrediction) return;

            Vector2 currentPos = transform.position;
            Vector2 currentVel = rb2d.linearVelocity;
            Vector2 gravity = GravitySystem.Instance.GetGravityAtPosition(transform.position);

            // Calculate trajectory points
            trajectoryPoints_array = GravityPhysicsUtils.CalculateTrajectoryPoints(
                currentPos, currentVel, gravity, trajectoryPoints, 0.1f);

            // Update line renderer
            for (int i = 0; i < trajectoryPoints; i++)
            {
                trajectoryRenderer.SetPosition(i, trajectoryPoints_array[i]);
            }
        }

        // Public API methods
        public Vector2 GetVelocity() => rb2d.linearVelocity;
        public void SetVelocity(Vector2 velocity) => rb2d.linearVelocity = velocity;
        public void AddForce(Vector2 force) => rb2d.AddForce(force);
        public bool IsGrounded() => isGrounded;
        public Vector2 GetCurrentGravity() => gravityAffected?.GetCurrentGravity() ?? Physics2D.gravity;

        // Safety methods
        public void EmergencyReset()
        {
            if (rb2d.linearVelocity.magnitude > stats.maxVelocityMagnitude * 2f)
            {
                rb2d.linearVelocity = lastValidVelocity;
                Debug.LogWarning("Emergency velocity reset applied");
            }

            if (float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y))
            {
                Respawn();
                Debug.LogError("Emergency position reset applied");
            }
        }

        private void OnDrawGizmos()
        {
            if (!debugMode) return;

            // Draw player bounds
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

            // Draw velocity
            if (Application.isPlaying && rb2d != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb2d.linearVelocity * 0.1f);
            }

            // Draw ground check
            if (groundCheckPoint != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.yellow;
                Vector3 rayDirection = (gravityDirection == GravityDirection.Down) ?
                    Vector3.down : Vector3.up;
                Gizmos.DrawLine(groundCheckPoint.position,
                    groundCheckPoint.position + rayDirection * groundCheckDistance);
            }

            // Draw current gravity
            if (Application.isPlaying && gravityAffected != null)
            {
                Gizmos.color = Color.cyan;
                Vector2 currentGravity = gravityAffected.GetCurrentGravity();
                Gizmos.DrawLine(transform.position,
                    transform.position + (Vector3)currentGravity.normalized * 1.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugMode || !Application.isPlaying) return;

            // Draw predicted trajectory
            if (trajectoryPoints_array != null && trajectoryPoints_array.Length > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < trajectoryPoints_array.Length - 1; i++)
                {
                    Gizmos.DrawLine(trajectoryPoints_array[i], trajectoryPoints_array[i + 1]);
                }
            }
        }
    }
}