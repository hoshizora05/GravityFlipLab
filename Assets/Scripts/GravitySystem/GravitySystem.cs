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

        [Header("Original Gravity State")]
        public Vector2 originalGravityDirection = Vector2.down;
        public float originalGravityStrength = 9.81f;

        [Header("Flip Settings")]
        public float flipTransitionTime = 0.1f;
        public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Reset Settings")]
        public float resetTransitionTime = 0.5f;
        public bool smoothReset = true;
        public AnimationCurve resetCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        // Original gravity state (for reset)
        public Vector2 OriginalGravityDirection { get; private set; } = Vector2.down;
        public float OriginalGravityStrength { get; private set; } = 9.81f;

        // Reset state tracking
        private bool isResetting = false;
        private Coroutine resetCoroutine;

        // Events
        public static event System.Action<Vector2> OnGlobalGravityChanged;
        public static event System.Action<float> OnGravityStrengthChanged;
        public static event System.Action<Vector2, float> OnGravityResetStarted;
        public static event System.Action<Vector2, float> OnGravityResetCompleted;

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

            // Store original gravity state
            OriginalGravityDirection = settings.originalGravityDirection.normalized;
            OriginalGravityStrength = settings.originalGravityStrength;

            // Set Unity's global gravity
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

            if (debugMode)
                Debug.Log($"GravitySystem initialized - Current: {CurrentGravityDirection}, Original: {OriginalGravityDirection}");
        }

        /// <summary>
        /// 重力を元の状態にリセット（メイン機能）
        /// </summary>
        public void ResetToOriginalGravity()
        {
            if (isResetting)
            {
                if (debugMode)
                    Debug.Log("Gravity reset already in progress, ignoring request");
                return;
            }

            if (debugMode)
                Debug.Log($"Resetting gravity from {CurrentGravityDirection} to {OriginalGravityDirection}");

            OnGravityResetStarted?.Invoke(OriginalGravityDirection, OriginalGravityStrength);

            if (settings.smoothReset)
            {
                StartSmoothReset();
            }
            else
            {
                InstantReset();
            }
        }

        /// <summary>
        /// 即座の重力リセット
        /// </summary>
        private void InstantReset()
        {
            CurrentGravityDirection = OriginalGravityDirection;
            CurrentGravityStrength = OriginalGravityStrength;

            // Unity物理エンジンに適用
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

            // 全てのGravityAffectedObjectに通知
            NotifyAllGravityObjects();

            // イベント発火
            OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);
            OnGravityStrengthChanged?.Invoke(CurrentGravityStrength);
            OnGravityResetCompleted?.Invoke(CurrentGravityDirection, CurrentGravityStrength);

            if (debugMode)
                Debug.Log("Instant gravity reset completed");
        }

        /// <summary>
        /// スムーズな重力リセット開始
        /// </summary>
        private void StartSmoothReset()
        {
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
            }

            resetCoroutine = StartCoroutine(SmoothResetCoroutine());
        }

        /// <summary>
        /// スムーズなリセットのコルーチン
        /// </summary>
        private IEnumerator SmoothResetCoroutine()
        {
            isResetting = true;

            Vector2 startDirection = CurrentGravityDirection;
            float startStrength = CurrentGravityStrength;

            float elapsedTime = 0f;
            float duration = settings.resetTransitionTime;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float curveValue = settings.resetCurve.Evaluate(t);

                // 重力方向の補間
                CurrentGravityDirection = Vector2.Lerp(startDirection, OriginalGravityDirection, curveValue);

                // 重力強度の補間
                CurrentGravityStrength = Mathf.Lerp(startStrength, OriginalGravityStrength, curveValue);

                // Unity物理エンジンに適用
                UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

                // イベント発火
                OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 最終値を確実に設定
            CurrentGravityDirection = OriginalGravityDirection;
            CurrentGravityStrength = OriginalGravityStrength;
            UnityEngine.Physics2D.gravity = CurrentGravityDirection * CurrentGravityStrength;

            // 全てのGravityAffectedObjectに通知
            NotifyAllGravityObjects();

            // 最終イベント発火
            OnGlobalGravityChanged?.Invoke(CurrentGravityDirection);
            OnGravityStrengthChanged?.Invoke(CurrentGravityStrength);
            OnGravityResetCompleted?.Invoke(CurrentGravityDirection, CurrentGravityStrength);

            isResetting = false;
            resetCoroutine = null;

            if (debugMode)
                Debug.Log("Smooth gravity reset completed");
        }

        /// <summary>
        /// 全てのGravityAffectedObjectに重力リセットを通知
        /// </summary>
        private void NotifyAllGravityObjects()
        {
            GravityAffectedObject[] affectedObjects = FindObjectsByType<GravityAffectedObject>(FindObjectsSortMode.None);

            foreach (var obj in affectedObjects)
            {
                if (obj != null && obj.gameObject.activeInHierarchy)
                {
                    obj.OnGravitySystemReset();
                }
            }

            if (debugMode)
                Debug.Log($"Notified {affectedObjects.Length} GravityAffectedObjects of gravity reset");
        }

        /// <summary>
        /// オリジナル重力設定の変更
        /// </summary>
        public void SetOriginalGravity(Vector2 direction, float strength = 9.81f)
        {
            OriginalGravityDirection = direction.normalized;
            OriginalGravityStrength = strength;

            // 設定も更新
            settings.originalGravityDirection = OriginalGravityDirection;
            settings.originalGravityStrength = OriginalGravityStrength;

            if (debugMode)
                Debug.Log($"Original gravity updated: Direction={OriginalGravityDirection}, Strength={OriginalGravityStrength}");
        }

        /// <summary>
        /// 現在の重力をオリジナルとして保存
        /// </summary>
        public void SaveCurrentGravityAsOriginal()
        {
            SetOriginalGravity(CurrentGravityDirection, CurrentGravityStrength);

            if (debugMode)
                Debug.Log("Current gravity saved as original");
        }

        /// <summary>
        /// 重力が元の状態かどうか判定
        /// </summary>
        public bool IsGravityAtOriginal()
        {
            const float tolerance = 0.01f;
            return Vector2.Distance(CurrentGravityDirection, OriginalGravityDirection) < tolerance &&
                   Mathf.Abs(CurrentGravityStrength - OriginalGravityStrength) < tolerance;
        }

        /// <summary>
        /// 強制的なリセット（緊急時用）
        /// </summary>
        public void ForceResetToOriginalGravity()
        {
            // 進行中のリセットを停止
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
                resetCoroutine = null;
            }

            isResetting = false;

            // 即座にリセット
            InstantReset();

            if (debugMode)
                Debug.Log("Force gravity reset executed");
        }

        // 既存のメソッドは保持
        public void FlipGlobalGravity()
        {
            Vector2 newDirection = -CurrentGravityDirection;
            SetGlobalGravityDirection(newDirection);
        }

        public void SetGlobalGravityDirection(Vector2 direction)
        {
            if (isResetting)
            {
                if (debugMode)
                    Debug.Log("Cannot change gravity direction during reset");
                return;
            }

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

        // Public API for external queries
        public bool IsResetting => isResetting;
        public float ResetProgress => resetCoroutine != null ? 0.5f : (isResetting ? 1f : 0f); // Simplified progress

        // Configuration methods
        public void SetResetSettings(bool smoothReset, float transitionTime)
        {
            settings.smoothReset = smoothReset;
            settings.resetTransitionTime = Mathf.Max(0.1f, transitionTime);
        }

        // Debug and visualization
        private void OnDrawGizmos()
        {
            if (!debugMode || !showGravityVectors) return;

            Vector3 center = Camera.main ? Camera.main.transform.position : Vector3.zero;

            // Draw current gravity (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center + (Vector3)(CurrentGravityDirection * 2f));

            // Draw original gravity (blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center + Vector3.right * 0.5f, center + Vector3.right * 0.5f + (Vector3)(OriginalGravityDirection * 2f));

            // Draw reset status
            if (isResetting)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(center, 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugMode) return;

            // Draw detailed gravity information
            Vector3 center = transform.position;

            // Current gravity info
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(center, 0.1f);

            // Original gravity info
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(center + Vector3.up * 1f, 0.1f);

            // Draw gravity values as text (in editor)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(center + Vector3.up * 2f,
                $"Current: {CurrentGravityDirection} ({CurrentGravityStrength:F1})\n" +
                $"Original: {OriginalGravityDirection} ({OriginalGravityStrength:F1})\n" +
                $"Resetting: {isResetting}");
#endif
        }

        // Validation and error handling
        private void OnValidate()
        {
            if (settings.resetTransitionTime <= 0f)
                settings.resetTransitionTime = 0.5f;

            if (settings.originalGravityStrength <= 0f)
                settings.originalGravityStrength = 9.81f;

            if (settings.globalGravityStrength <= 0f)
                settings.globalGravityStrength = 9.81f;
        }
    }

    #endregion
}