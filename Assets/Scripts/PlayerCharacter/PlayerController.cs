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

        [Header("Respawn Coordination")]
        public bool useIntegratedRespawnSystem = true; // 統合リスポーンシステムを使用
        public float fallbackRespawnDelay = 1.0f; // フォールバック用のリスポーン遅延

        private bool isRespawnHandled = false; // リスポーン処理が他システムで処理済みかフラグ
        private Coroutine respawnCoroutine; // リスポーンコルーチンの参照

        // 初期設定値を保存するための構造体
        [System.Serializable]
        public struct GravityConfiguration
        {
            public bool useCustomGravity;
            public float gravityScale;
            public bool maintainInertia;
            public float inertiaDecay;
            public bool smoothGravityTransition;
            public float transitionSpeed;
            public float maxVelocityChange;
        }

        // オリジナル設定値を保存
        private GravityConfiguration originalGravityConfig;
        private bool gravityConfigSaved = false;

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
                gravityAffected.transitionSpeed = 10f;
                gravityAffected.maintainInertia = stats.preserveHorizontalMomentum;
                gravityAffected.inertiaDecay = 0.95f;
                gravityAffected.maxVelocityChange = stats.maxVelocityMagnitude;

                // 初期設定値を保存（一度だけ）
                SaveOriginalGravityConfiguration();
            }
        }
        /// <summary>
        /// オリジナルの重力設定を保存
        /// </summary>
        private void SaveOriginalGravityConfiguration()
        {
            if (gravityConfigSaved || gravityAffected == null) return;

            originalGravityConfig = new GravityConfiguration
            {
                useCustomGravity = gravityAffected.useCustomGravity,
                gravityScale = gravityAffected.gravityScale,
                maintainInertia = gravityAffected.maintainInertia,
                inertiaDecay = gravityAffected.inertiaDecay,
                smoothGravityTransition = gravityAffected.smoothGravityTransition,
                transitionSpeed = gravityAffected.transitionSpeed,
                maxVelocityChange = gravityAffected.maxVelocityChange
            };

            gravityConfigSaved = true;

            if (debugMode)
                Debug.Log($"PlayerController: Original gravity config saved - maintainInertia: {originalGravityConfig.maintainInertia}, inertiaDecay: {originalGravityConfig.inertiaDecay}");
        }
        /// <summary>
        /// オリジナルの重力設定を復元
        /// </summary>
        public void RestoreOriginalGravityConfiguration()
        {
            if (!gravityConfigSaved || gravityAffected == null) return;

            gravityAffected.useCustomGravity = originalGravityConfig.useCustomGravity;
            gravityAffected.gravityScale = originalGravityConfig.gravityScale;
            gravityAffected.maintainInertia = originalGravityConfig.maintainInertia;
            gravityAffected.inertiaDecay = originalGravityConfig.inertiaDecay;
            gravityAffected.smoothGravityTransition = originalGravityConfig.smoothGravityTransition;
            gravityAffected.transitionSpeed = originalGravityConfig.transitionSpeed;
            gravityAffected.maxVelocityChange = originalGravityConfig.maxVelocityChange;

            if (debugMode)
                Debug.Log($"PlayerController: Original gravity config restored - maintainInertia: {gravityAffected.maintainInertia}, inertiaDecay: {gravityAffected.inertiaDecay}");
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

            // 移動を停止
            rb2d.linearVelocity = Vector2.zero;
            rb2d.gravityScale = 0;

            // 重力エフェクトを無効化
            if (gravityAffected != null)
                gravityAffected.useCustomGravity = false;

            // エフェクト再生
            if (playerVisuals != null) playerVisuals.PlayDeathEffect();
            if (playerAnimation != null) playerAnimation.PlayDeathAnimation();

            // リスポーン処理の決定
            isRespawnHandled = false;

            // イベント発火（他システムが処理する可能性）
            OnPlayerDeath?.Invoke();

            // 少し待ってから、他システムが処理していない場合のフォールバック
            StartCoroutine(CheckAndFallbackRespawn());
        }

        /// <summary>
        /// 他システムがリスポーンを処理しているかチェックし、
        /// 処理されていない場合はフォールバック処理を実行
        /// </summary>
        private IEnumerator CheckAndFallbackRespawn()
        {
            // 他システムの処理を待つ
            yield return new WaitForSeconds(0.1f);

            // 統合システムの存在チェック
            bool hasIntegratedSystem = CheckForIntegratedRespawnSystems();

            if (useIntegratedRespawnSystem && hasIntegratedSystem)
            {
                if (debugMode)
                    Debug.Log("PlayerController: Respawn handled by integrated system");
                yield break; // 統合システムに任せる
            }

            // フォールバック：従来のリスポーン処理
            if (debugMode)
                Debug.Log("PlayerController: Using fallback respawn");

            respawnCoroutine = StartCoroutine(FallbackRespawnCoroutine());
        }

        /// <summary>
        /// 統合リスポーンシステムの存在チェック
        /// </summary>
        private bool CheckForIntegratedRespawnSystems()
        {
            // RespawnIntegrationの存在チェック
            var respawnIntegration = GetComponent<RespawnIntegration>();
            if (respawnIntegration != null)
            {
                return true;
            }

            // CheckpointManagerの統合設定チェック
            if (CheckpointManager.Instance != null &&
                CheckpointManager.Instance.useRespawnIntegration)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// フォールバック用のリスポーンコルーチン
        /// </summary>
        private IEnumerator FallbackRespawnCoroutine()
        {
            yield return new WaitForSeconds(fallbackRespawnDelay);

            if (!isRespawnHandled)
            {
                FallbackRespawn();
            }
        }

        /// <summary>
        /// フォールバック用のリスポーン処理（重力に配慮）
        /// </summary>
        private void FallbackRespawn()
        {
            if (isRespawnHandled) return;

            isRespawnHandled = true;

            // チェックポイント位置を取得
            Vector3 respawnPosition = Vector3.zero;
            if (CheckpointManager.Instance != null)
            {
                respawnPosition = CheckpointManager.Instance.GetCurrentCheckpointPosition();
            }
            else
            {
                // 緊急フォールバック
                respawnPosition = transform.position + Vector3.up * 2f;
            }

            // 位置リセット
            transform.position = respawnPosition;

            // 基本状態リセット
            isAlive = true;
            gravityDirection = GravityDirection.Down;
            ChangeState(PlayerState.Running);

            // 物理状態リセット（重力設定は保守的に）
            rb2d.linearVelocity = Vector2.zero;
            rb2d.gravityScale = stats.gravityScale;

            // 重力システムを有効化（保守的）
            if (gravityAffected != null)
            {
                gravityAffected.useCustomGravity = true;
                gravityAffected.gravityScale = stats.gravityScale;
            }

            // PlayerMovementの再初期化
            InitializePlayerMovementSafely();

            // ビジュアルリセット
            if (playerVisuals != null)
                playerVisuals.ResetVisuals();

            OnPlayerRespawn?.Invoke();

            if (debugMode)
                Debug.Log("PlayerController: Fallback respawn completed");
        }

        /// <summary>
        /// 外部システムからリスポーン完了を通知
        /// </summary>
        public void NotifyRespawnHandled()
        {
            isRespawnHandled = true;

            // 進行中のフォールバックコルーチンを停止
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            if (debugMode)
                Debug.Log("PlayerController: Respawn handled notification received");
        }

        /// <summary>
        /// 外部システム用の軽量リスポーン（位置設定とイベント発火のみ）
        /// </summary>
        public void ExternalRespawn(Vector3 position)
        {
            if (isRespawnHandled) return;

            // 他システムがリスポーン処理中であることを記録
            NotifyRespawnHandled();

            // 最小限の状態リセット
            transform.position = position;
            isAlive = true;
            gravityDirection = GravityDirection.Down;
            ChangeState(PlayerState.Running);

            // 物理状態の最小限リセット
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;

            // 重要：オリジナル重力設定を復元（新しい値を設定しない）
            RestoreOriginalGravityConfiguration();

            // ビジュアルリセット
            if (playerVisuals != null)
                playerVisuals.ResetVisuals();

            OnPlayerRespawn?.Invoke();

            if (debugMode)
                Debug.Log($"PlayerController: External respawn at {position}");
        }

        /// <summary>
        /// 従来のRespawnメソッド（既存システムとの互換性のため保持）
        /// </summary>
        public void Respawn()
        {
            // 既に他システムで処理済みの場合はスキップ
            if (isRespawnHandled)
            {
                if (debugMode)
                    Debug.Log("PlayerController: Respawn skipped (already handled)");
                return;
            }

            // フォールバックリスポーンを実行
            FallbackRespawn();
        }

        /// <summary>
        /// PlayerMovementコンポーネントの安全な初期化（重力設定保護版）
        /// </summary>
        private void InitializePlayerMovementSafely()
        {
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                // PlayerMovementを初期化
                playerMovement.Initialize(this);

                // 物理状態の検証
                playerMovement.ValidatePhysicsState();

                // 重要：オリジナル設定を復元（上書きしない）
                RestoreOriginalGravityConfiguration();

                if (debugMode)
                    Debug.Log("PlayerController: PlayerMovement safely reinitialized with original gravity config");
            }
        }

        /// <summary>
        /// リスポーン状態のリセット（新しいステージ開始時等に使用）
        /// </summary>
        public void ResetRespawnState()
        {
            isRespawnHandled = false;

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            if (debugMode)
                Debug.Log("PlayerController: Respawn state reset");
        }

        /// <summary>
        /// 緊急時のリスポーン（デバッグ用）
        /// </summary>
        public void EmergencyRespawn()
        {
            isRespawnHandled = false;

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            FallbackRespawn();

            if (debugMode)
                Debug.Log("PlayerController: Emergency respawn executed");
        }

        // Public API for respawn status
        public bool IsRespawnHandled => isRespawnHandled;
        public bool UseIntegratedRespawnSystem => useIntegratedRespawnSystem;

        // Configuration methods
        public void SetUseIntegratedRespawnSystem(bool use)
        {
            useIntegratedRespawnSystem = use;
        }

        public void SetFallbackRespawnDelay(float delay)
        {
            fallbackRespawnDelay = Mathf.Max(0.1f, delay);
        }

        /// <summary>
        /// 重力設定の緊急確認と修正
        /// </summary>
        public void ValidateGravitySettings()
        {
            if (gravityAffected != null)
            {
                gravityAffected.EnsureCustomGravityEnabled();

                if (debugMode)
                    Debug.Log($"PlayerController: Gravity validation - useCustomGravity: {gravityAffected.useCustomGravity}, rb2d.gravityScale: {rb2d.gravityScale}");
            }
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