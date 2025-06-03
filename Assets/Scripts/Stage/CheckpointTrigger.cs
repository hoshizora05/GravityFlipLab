using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    public class CheckpointTrigger : MonoBehaviour
    {
        [Header("Checkpoint Configuration")]
        public Vector3 checkpointPosition;
        public bool useTransformPosition = true;
        public bool isReusable = false;
        public float activationCooldown = 0.5f;

        [Header("Visual Feedback")]
        public GameObject inactiveVisual;
        public GameObject activeVisual;
        public ParticleSystem activationParticles;
        public bool enableGlow = true;

        [Header("Audio")]
        public AudioClip activationSound;
        public AudioSource audioSource;

        [Header("Advanced Features")]
        public bool saveGravityState = true;
        public bool saveVelocityState = false;
        public bool requireGrounded = false;
        public LayerMask playerLayerMask = 1;

        private bool triggered = false;
        private bool isActivated = false;
        private float lastActivationTime = -1f;
        private Collider2D triggerCollider;

        // Events
        public System.Action<CheckpointTrigger> OnCheckpointActivated;

        // Property for external access
        public bool IsActivated => isActivated;

        private void Awake()
        {
            InitializeCheckpoint();
        }

        private void Start()
        {
            RegisterWithManager();
            UpdateVisualState();
        }

        private void OnDestroy()
        {
            UnregisterFromManager();
        }

        private void InitializeCheckpoint()
        {
            // Setup checkpoint position
            if (useTransformPosition)
            {
                checkpointPosition = transform.position;
            }

            // Get or create trigger collider
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
                triggerCollider.isTrigger = true;
                ((BoxCollider2D)triggerCollider).size = Vector2.one;
            }

            // Ensure it's a trigger
            triggerCollider.isTrigger = true;

            // Setup audio source
            if (audioSource == null && activationSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // 3D sound
            }
        }

        private void RegisterWithManager()
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RegisterCheckpoint(this);
            }
        }

        private void UnregisterFromManager()
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.UnregisterCheckpoint(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if it's the player
            if (!IsPlayerValid(other)) return;

            // Check cooldown
            if (Time.time - lastActivationTime < activationCooldown) return;

            // Check if already activated and not reusable
            if (isActivated && !isReusable) return;

            // Check if player is grounded (if required)
            if (requireGrounded && !IsPlayerGrounded(other)) return;

            ActivateCheckpoint(other);
        }

        private bool IsPlayerValid(Collider2D other)
        {
            // Check tag
            if (!other.CompareTag("Player")) return false;

            // Check layer mask
            if ((playerLayerMask.value & (1 << other.gameObject.layer)) == 0) return false;

            return true;
        }

        private bool IsPlayerGrounded(Collider2D other)
        {
            var groundDetector = other.GetComponent<AdvancedGroundDetector>();
            if (groundDetector != null)
            {
                return groundDetector.IsGrounded();
            }

            // Fallback: simple ground check
            var playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                return playerMovement.IsGrounded();
            }

            return true; // Default to true if no ground detection available
        }

        private void ActivateCheckpoint(Collider2D playerCollider)
        {
            triggered = true;
            isActivated = true;
            lastActivationTime = Time.time;

            // Get additional state if needed
            GravityDirection gravity = GravityDirection.Down;

            if (saveGravityState)
            {
                var playerController = playerCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    gravity = playerController.gravityDirection;
                }
            }

            // Activate checkpoint in manager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(checkpointPosition, gravity);
            }

            // Play effects
            PlayCheckpointEffect();

            // Update visuals
            UpdateVisualState();

            // Fire event
            OnCheckpointActivated?.Invoke(this);

            Debug.Log($"Checkpoint activated at: {checkpointPosition}");
        }

        public void MarkAsActivated()
        {
            isActivated = true;
            triggered = true;
            UpdateVisualState();
        }

        private void PlayCheckpointEffect()
        {
            // Play particle effect
            if (activationParticles != null)
            {
                activationParticles.Play();
            }

            // Play sound
            if (audioSource != null && activationSound != null)
            {
                audioSource.clip = activationSound;
                audioSource.Play();
            }
            else if (activationSound != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, transform.position);
            }

            // Legacy method call for backward compatibility
            if (!triggered) // Prevent double call
            {
                PlayCheckpointEffectLegacy();
            }
        }

        private void PlayCheckpointEffectLegacy()
        {
            // Add particle effect or sound here (legacy implementation)
            Debug.Log($"Checkpoint activated at {checkpointPosition}");
        }

        private void UpdateVisualState()
        {
            // Update visual objects
            if (inactiveVisual != null)
            {
                inactiveVisual.SetActive(!isActivated);
            }

            if (activeVisual != null)
            {
                activeVisual.SetActive(isActivated);
            }

            // Update glow effect
            if (enableGlow)
            {
                UpdateGlowEffect();
            }
        }

        private void UpdateGlowEffect()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (isActivated)
                {
                    renderer.color = Color.green;
                    // Add glow shader effect if available
                }
                else
                {
                    renderer.color = Color.white;
                }
            }
        }

        // Public methods
        public void SetCheckpointPosition(Vector3 position)
        {
            checkpointPosition = position;
        }

        public void ResetCheckpoint()
        {
            isActivated = false;
            triggered = false;
            lastActivationTime = -1f;
            UpdateVisualState();
        }

        public void ForceActivate()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    ActivateCheckpoint(playerCollider);
                }
            }
        }

        // Configuration methods
        public void SetReusable(bool reusable)
        {
            isReusable = reusable;
        }

        public void SetActivationCooldown(float cooldown)
        {
            activationCooldown = Mathf.Max(0f, cooldown);
        }

        public void SetRequireGrounded(bool require)
        {
            requireGrounded = require;
        }

        // Editor support
        private void OnValidate()
        {
            if (useTransformPosition)
            {
                checkpointPosition = transform.position;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw checkpoint position
            Gizmos.color = isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(checkpointPosition, 0.5f);

            // Draw trigger area
            if (triggerCollider != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, triggerCollider.bounds.size);
            }
            else
            {
                // Default trigger area if no collider
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detailed information
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(checkpointPosition, 0.3f);

            // Draw activation range
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

            // Draw position line if different from transform
            if (!useTransformPosition && Vector3.Distance(transform.position, checkpointPosition) > 0.1f)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, checkpointPosition);
            }

            // Draw player layer mask visualization
            if (Application.isPlaying)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    bool playerValid = IsPlayerValid(player.GetComponent<Collider2D>());
                    Gizmos.color = playerValid ? Color.green : Color.red;
                    Gizmos.DrawLine(transform.position, player.transform.position);
                }
            }
        }
    }
}