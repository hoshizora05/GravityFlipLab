using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Player
{
    #region Player Data Structures

    [System.Serializable]
    public class PlayerStats
    {
        public float moveSpeed = 5.0f;
        public float fallSpeed = 10.0f;
        public float maxFallSpeed = 20.0f;
        public float gravityScale = 1.0f;
        public float invincibilityDuration = 0.1f;
        public bool isInvincible = false;
        public int livesRemaining = 3;
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
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerAnimation))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Components")]
        public PlayerMovement movement;
        public PlayerAnimation playerAnimation;
        public PlayerCollision playerCollision;
        public PlayerVisuals playerVisuals;

        [Header("Player Stats")]
        public PlayerStats stats = new PlayerStats();

        [Header("Debug")]
        public bool debugMode = false;

        // State
        public PlayerState currentState { get; private set; } = PlayerState.Running;
        public GravityDirection gravityDirection { get; private set; } = GravityDirection.Down;
        public bool isAlive { get; private set; } = true;

        // Components
        private Rigidbody2D rb2d;
        private Collider2D playerCollider;

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

            // Get or add required components
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (playerAnimation == null) playerAnimation = GetComponent<PlayerAnimation>();
            if (playerCollision == null) playerCollision = GetComponent<PlayerCollision>();
            if (playerVisuals == null) playerVisuals = GetComponent<PlayerVisuals>();

            // Initialize components
            movement.Initialize(this);
            playerAnimation.Initialize(this);
            playerCollision.Initialize(this);
            playerVisuals.Initialize(this);
        }

        private void Initialize()
        {
            // Set initial state
            ChangeState(PlayerState.Running);
            gravityDirection = GravityDirection.Down;
            isAlive = true;

            // Apply assist mode if enabled
            if (GameManager.Instance.playerProgress.settings.assistModeEnabled)
            {
                stats.livesRemaining += ConfigManager.Instance.assistModeBarrierCount;
            }

            if (debugMode)
                Debug.Log("PlayerController initialized");
        }

        private void Update()
        {
            if (!isAlive) return;

            // Handle input
            HandleInput();

            // Update state logic
            UpdateStateLogic();
        }

        private void HandleInput()
        {
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
                    if (Mathf.Abs(rb2d.linearVelocity.y) > 0.1f)
                    {
                        ChangeState(PlayerState.Falling);
                    }
                    break;

                case PlayerState.Falling:
                    if (Mathf.Abs(rb2d.linearVelocity.y) < 0.1f && movement.IsGrounded())
                    {
                        ChangeState(PlayerState.Running);
                    }
                    break;

                case PlayerState.GravityFlipping:
                    // Handle gravity flip duration
                    break;

                case PlayerState.Dead:
                    // Handle death state
                    break;
            }
        }

        public void FlipGravity()
        {
            if (currentState == PlayerState.Dead || currentState == PlayerState.GravityFlipping)
                return;

            StartCoroutine(GravityFlipCoroutine());
        }

        private IEnumerator GravityFlipCoroutine()
        {
            ChangeState(PlayerState.GravityFlipping);

            // Flip gravity direction
            gravityDirection = (gravityDirection == GravityDirection.Down) ?
                GravityDirection.Up : GravityDirection.Down;

            // Apply gravity flip to physics
            movement.ApplyGravityFlip(gravityDirection);

            // Visual effects
            playerVisuals.PlayGravityFlipEffect();

            // Audio
            // AudioManager.Instance.PlaySE("GravityFlip");

            OnGravityFlip?.Invoke(gravityDirection);

            // Wait for flip duration
            yield return new WaitForSeconds(ConfigManager.Instance.gravityFlipDuration);

            // Return to appropriate state
            if (movement.IsGrounded())
                ChangeState(PlayerState.Running);
            else
                ChangeState(PlayerState.Falling);
        }

        public void TakeDamage()
        {
            if (currentState == PlayerState.Dead || stats.isInvincible) return;

            stats.livesRemaining--;
            GameManager.Instance.sessionDeathCount++;

            if (stats.livesRemaining <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvincibilityCoroutine());
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

            // Play death effects
            playerVisuals.PlayDeathEffect();
            playerAnimation.PlayDeathAnimation();

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

            // Reset visuals
            playerVisuals.ResetVisuals();

            OnPlayerRespawn?.Invoke();
        }

        private IEnumerator InvincibilityCoroutine()
        {
            stats.isInvincible = true;
            playerVisuals.SetInvincibleVisuals(true);

            yield return new WaitForSeconds(stats.invincibilityDuration);

            stats.isInvincible = false;
            playerVisuals.SetInvincibleVisuals(false);
        }

        private void ChangeState(PlayerState newState)
        {
            if (currentState != newState)
            {
                PlayerState previousState = currentState;
                currentState = newState;

                OnStateChanged?.Invoke(newState);
                playerAnimation.OnStateChanged(newState);

                if (debugMode)
                    Debug.Log($"Player state: {previousState} -> {newState}");
            }
        }

        public Vector2 GetVelocity()
        {
            return rb2d.linearVelocity;
        }

        public void SetVelocity(Vector2 velocity)
        {
            rb2d.linearVelocity = velocity;
        }

        public void AddForce(Vector2 force)
        {
            rb2d.AddForce(force);
        }

        private void OnDrawGizmos()
        {
            if (debugMode && Application.isPlaying)
            {
                // Draw debug info
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

                // Draw velocity
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb2d.linearVelocity * 0.1f);
            }
        }
    }
}