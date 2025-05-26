using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    public class LocalGravityZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        public LocalGravityData gravityData = new LocalGravityData();

        [Header("Zone Shape")]
        public bool useColliderBounds = true;
        public float radius = 5f;
        public Vector2 size = Vector2.one * 10f;

        [Header("Visual")]
        public bool showGizmos = true;
        public Color gizmoColor = Color.cyan;

        private Collider2D zoneCollider;
        private List<GravityAffectedObject> affectedObjects = new List<GravityAffectedObject>();

        // Events
        public System.Action<GravityAffectedObject> OnObjectEnterZone;
        public System.Action<GravityAffectedObject> OnObjectExitZone;

        private void Awake()
        {
            if (useColliderBounds)
            {
                zoneCollider = GetComponent<Collider2D>();
                if (zoneCollider == null)
                {
                    // Create a trigger collider
                    zoneCollider = gameObject.AddComponent<CircleCollider2D>();
                    zoneCollider.isTrigger = true;
                    ((CircleCollider2D)zoneCollider).radius = radius;
                }
            }
        }

        private void Start()
        {
            // Register with gravity system
            GravitySystem.Instance.RegisterGravityZone(this);
        }

        private void OnDestroy()
        {
            // Unregister from gravity system
            if (GravitySystem.Instance != null)
            {
                GravitySystem.Instance.UnregisterGravityZone(this);
            }
        }

        public bool IsPositionInZone(Vector3 position)
        {
            if (useColliderBounds && zoneCollider != null)
            {
                return zoneCollider.bounds.Contains(position);
            }
            else
            {
                Vector2 distance = position - transform.position;
                return distance.magnitude <= radius;
            }
        }

        public Vector2 GetGravityVector()
        {
            return GetGravityAtPosition(transform.position);
        }

        public virtual Vector2 GetGravityAtPosition(Vector3 position)
        {
            Vector2 gravityVector = Vector2.zero;

            switch (gravityData.gravityType)
            {
                case LocalGravityType.Directional:
                    gravityVector = gravityData.direction.normalized * gravityData.strength;
                    break;

                case LocalGravityType.Radial:
                    Vector2 directionToCenter = (transform.position - position).normalized;
                    gravityVector = directionToCenter * gravityData.strength;
                    break;

                case LocalGravityType.Orbital:
                    Vector2 toCenter = transform.position - position;
                    Vector2 perpendicular = new Vector2(-toCenter.y, toCenter.x).normalized;
                    gravityVector = perpendicular * gravityData.strength;
                    break;

                case LocalGravityType.Custom:
                    gravityVector = CalculateCustomGravity(position);
                    break;
            }

            // Apply strength curve based on distance
            float distance = Vector2.Distance(position, transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / gravityData.transitionDistance);
            float strengthMultiplier = gravityData.strengthCurve.Evaluate(1f - normalizedDistance);

            return gravityVector * strengthMultiplier;
        }

        private Vector2 CalculateCustomGravity(Vector3 position)
        {
            // Override this method in derived classes for custom gravity effects
            return gravityData.direction.normalized * gravityData.strength;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            GravityAffectedObject gravityObj = other.GetComponent<GravityAffectedObject>();
            if (gravityObj != null && !affectedObjects.Contains(gravityObj))
            {
                affectedObjects.Add(gravityObj);
                gravityObj.EnterGravityZone(this);
                OnObjectEnterZone?.Invoke(gravityObj);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            GravityAffectedObject gravityObj = other.GetComponent<GravityAffectedObject>();
            if (gravityObj != null && affectedObjects.Contains(gravityObj))
            {
                affectedObjects.Remove(gravityObj);
                gravityObj.ExitGravityZone(this);
                OnObjectExitZone?.Invoke(gravityObj);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = gizmoColor;

            if (useColliderBounds && zoneCollider != null)
            {
                Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            // Draw gravity direction
            Gizmos.color = Color.red;
            Vector3 gravityDir = GetGravityVector().normalized;
            Gizmos.DrawLine(transform.position, transform.position + gravityDir * 2f);
        }
    }
}