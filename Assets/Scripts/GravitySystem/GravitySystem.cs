using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Physics
{
    #region Gravity System Core

    [System.Serializable]
    public class GravitySettings
    {
        [Header("Global Gravity")]
        public float globalGravityStrength = 9.81f;
        public Vector2 globalGravityDirection = Vector2.down;

        [Header("Flip Settings")]
        public float flipTransitionTime = 0.1f;
        public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Physics")]
        public float maxGravityForce = 50f;
        public float gravityAcceleration = 2f;
        public bool useRealisticPhysics = true;
    }

    public class GravitySystem : MonoBehaviour
    {
        private static GravitySystem _instance;
        public static GravitySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GravitySystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GravitySystem");
                        _instance = go.AddComponent<GravitySystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Gravity Configuration")]
        public GravitySettings settings = new GravitySettings();

        [Header("Local Gravity Zones")]
        public List<LocalGravityZone> gravityZones = new List<LocalGravityZone>();

        [Header("Debug")]
        public bool debugMode = false;
        public bool showGravityVectors = false;

        // Current gravity state
        public Vector2 CurrentGravityDirection { get; private set; } = Vector2.down;
        public float CurrentGravityStrength { get; private set; } = 9.81f;

        // Events
        public static event System.Action<Vector2> OnGlobalGravityChanged;
        public static event System.Action<float> OnGravityStrengthChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            CurrentGravityDirection = settings.globalGravityDirection.normalized;
            CurrentGravityStrength = settings.globalGravityStrength;

            // Set Unity's global gravity
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

            Debug.Log("GravitySystem initialized");
        }

        public void FlipGlobalGravity()
        {
            Vector2 newDirection = -CurrentGravityDirection;
            SetGlobalGravityDirection(newDirection);
        }

        public void SetGlobalGravityDirection(Vector2 direction)
        {
            StartCoroutine(GravityTransitionCoroutine(direction.normalized));
        }

        private IEnumerator GravityTransitionCoroutine(Vector2 targetDirection)
        {
            Vector2 startDirection = CurrentGravityDirection;
            float elapsedTime = 0f;

            while (elapsedTime < settings.flipTransitionTime)
            {
                float t = elapsedTime / settings.flipTransitionTime;
                float curveValue = settings.flipCurve.Evaluate(t);

                Vector2 currentDirection = Vector2.Lerp(startDirection, targetDirection, curveValue);
                CurrentGravityDirection = currentDirection.normalized;

                // Update Unity's physics gravity
                UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

                OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure final values are set
            CurrentGravityDirection = targetDirection;
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;
            OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);
        }

        public void SetGravityStrength(float strength)
        {
            CurrentGravityStrength = Mathf.Clamp(strength, 0f, settings.maxGravityForce);
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;
            OnGravityStrengthChanged?.Invoke(CurrentGravityStrength);
        }

        public Vector2 GetGravityAtPosition(Vector3 position)
        {
            // Check for local gravity zones
            foreach (var zone in gravityZones)
            {
                if (zone != null && zone.IsPositionInZone(position))
                {
                    return zone.GetGravityVector();
                }
            }

            // Return global gravity
            return CurrentGravityDirection * CurrentGravityStrength;
        }

        public void RegisterGravityZone(LocalGravityZone zone)
        {
            if (!gravityZones.Contains(zone))
            {
                gravityZones.Add(zone);
            }
        }

        public void UnregisterGravityZone(LocalGravityZone zone)
        {
            gravityZones.Remove(zone);
        }

        private void OnDrawGizmos()
        {
            if (debugMode && showGravityVectors)
            {
                Gizmos.color = Color.yellow;
                Vector3 center = Camera.main ? Camera.main.transform.position : Vector3.zero;
                Gizmos.DrawLine(center, center + (Vector3)(CurrentGravityDirection * 2f));
            }
        }
    }

    #endregion
}