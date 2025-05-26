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
        private float originalGravityScale;

        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
            if (rb2d == null)
            {
                Debug.LogError($"GravityAffectedObject requires Rigidbody2D component on {gameObject.name}");
                enabled = false;
                return;
            }

            originalGravityScale = rb2d.gravityScale;
        }

        private void Start()
        {
            // Initialize with global gravity
            currentGravity = GravitySystem.Instance.CurrentGravityDirection * GravitySystem.Instance.CurrentGravityStrength;
            targetGravity = currentGravity;
        }

        private void FixedUpdate()
        {
            if (!useCustomGravity) return;

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

        public Vector2 GetCurrentGravity()
        {
            return currentGravity;
        }

        public void SetGravityScale(float scale)
        {
            gravityScale = scale;
        }

        public void ResetToOriginalGravity()
        {
            rb2d.gravityScale = originalGravityScale;
            useCustomGravity = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentGravity.normalized * 2f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)targetGravity.normalized * 2f);
            }
        }
    }
}