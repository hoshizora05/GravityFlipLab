using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityFlipLab.Physics;
using GravityFlipLab.Player;

/// <summary>
/// リアルタイムで重力状態を監視・デバッグするツール
/// </summary>
public class GravityDebugTool : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableRealTimeLogging = true;
    public float logInterval = 1f;
    public bool logOnRespawn = true;
    public bool showGUI = true;

    [Header("Auto Fix")]
    public bool enableAutoFix = false;
    public KeyCode manualFixKey = KeyCode.F9;

    // Components
    private PlayerController playerController;
    private Rigidbody2D rb2d;
    private GravityAffectedObject gravityAffected;
    private RespawnIntegration respawnIntegration;

    // Debug data
    private float lastLogTime = 0f;
    private Vector2 lastVelocity;
    private float lastGravityScale;
    private bool lastUseCustomGravity;
    private int frameCount = 0;

    // GUI
    private Rect windowRect = new Rect(10, 10, 400, 300);
    private bool showWindow = true;

    private void Awake()
    {
        InitializeComponents();
        SubscribeToEvents();
    }

    private void Start()
    {
        if (enableRealTimeLogging)
        {
            StartCoroutine(RealTimeLoggingCoroutine());
        }
    }

    private void Update()
    {
        frameCount++;

        // Manual fix key
        if (Input.GetKeyDown(manualFixKey))
        {
            LogCurrentState("MANUAL_FIX_TRIGGERED");
            ApplyManualFix();
        }

        // Auto fix detection
        if (enableAutoFix && frameCount % 60 == 0) // Every 60 frames
        {
            DetectAndFixIssues();
        }
    }

    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        rb2d = GetComponent<Rigidbody2D>();
        gravityAffected = GetComponent<GravityAffectedObject>();
        respawnIntegration = GetComponent<RespawnIntegration>();

        if (playerController == null)
        {
            Debug.LogError("GravityDebugTool: PlayerController not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (playerController != null)
        {
            PlayerController.OnPlayerDeath += OnPlayerDeath;
            PlayerController.OnPlayerRespawn += OnPlayerRespawn;
        }

        if (respawnIntegration != null)
        {
            RespawnIntegration.OnRespawnStarted += OnRespawnStarted;
            RespawnIntegration.OnRespawnCompleted += OnRespawnCompleted;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        PlayerController.OnPlayerDeath -= OnPlayerDeath;
        PlayerController.OnPlayerRespawn -= OnPlayerRespawn;
        RespawnIntegration.OnRespawnStarted -= OnRespawnStarted;
        RespawnIntegration.OnRespawnCompleted -= OnRespawnCompleted;
    }

    /// <summary>
    /// リアルタイムログのコルーチン
    /// </summary>
    private IEnumerator RealTimeLoggingCoroutine()
    {
        while (enableRealTimeLogging)
        {
            LogCurrentState("REALTIME");
            yield return new WaitForSeconds(logInterval);
        }
    }

    /// <summary>
    /// 現在の重力状態をログ出力
    /// </summary>
    private void LogCurrentState(string context)
    {
        if (rb2d == null) return;

        float currentTime = Time.time;
        Vector2 currentVelocity = rb2d.linearVelocity;

        string log = $"[{context}] Time: {currentTime:F2}s, Frame: {frameCount}\n";

        // Rigidbody2D state
        log += $"RB2D - Velocity: {currentVelocity}, GravityScale: {rb2d.gravityScale}, Mass: {rb2d.mass}\n";
        log += $"RB2D - Drag: {rb2d.linearDamping}, AngularDrag: {rb2d.angularDamping}, Kinematic: {rb2d.isKinematic}\n";

        // GravityAffectedObject state
        if (gravityAffected != null)
        {
            Vector2 currentGravity = gravityAffected.GetCurrentGravity();
            log += $"GAO - UseCustom: {gravityAffected.useCustomGravity}, Scale: {gravityAffected.gravityScale}\n";
            log += $"GAO - CurrentGravity: {currentGravity}, MaintainInertia: {gravityAffected.maintainInertia}\n";
            log += $"GAO - InertiaDecay: {gravityAffected.inertiaDecay}, SmoothTransition: {gravityAffected.smoothGravityTransition}\n";
        }
        else
        {
            log += "GAO - NULL\n";
        }

        // Physics2D global settings
        log += $"Physics2D - Gravity: {Physics2D.gravity}, QueryHitTriggers: {Physics2D.queriesHitTriggers}\n";

        // GravitySystem state
        if (GravitySystem.Instance != null)
        {
            log += $"GravitySystem - Direction: {GravitySystem.Instance.CurrentGravityDirection}, Strength: {GravitySystem.Instance.CurrentGravityStrength}\n";
            log += $"GravitySystem - IsResetting: {GravitySystem.Instance.IsResetting}\n";
        }
        else
        {
            log += "GravitySystem - NULL\n";
        }

        // PlayerController state
        if (playerController != null)
        {
            log += $"PlayerController - IsAlive: {playerController.isAlive}, State: {playerController.currentState}\n";
            log += $"PlayerController - GravityDirection: {playerController.gravityDirection}\n";
        }

        // Velocity change detection
        Vector2 velocityChange = currentVelocity - lastVelocity;
        if (velocityChange.magnitude > 0.1f)
        {
            log += $"VELOCITY_CHANGE - Delta: {velocityChange}, Magnitude: {velocityChange.magnitude:F3}\n";
        }

        // Gravity scale change detection
        if (Mathf.Abs(rb2d.gravityScale - lastGravityScale) > 0.01f)
        {
            log += $"GRAVITY_SCALE_CHANGE - From: {lastGravityScale} To: {rb2d.gravityScale}\n";
        }

        // useCustomGravity change detection
        if (gravityAffected != null && gravityAffected.useCustomGravity != lastUseCustomGravity)
        {
            log += $"USE_CUSTOM_GRAVITY_CHANGE - From: {lastUseCustomGravity} To: {gravityAffected.useCustomGravity}\n";
        }

        Debug.Log(log);

        // Update last values
        lastVelocity = currentVelocity;
        lastGravityScale = rb2d.gravityScale;
        if (gravityAffected != null)
        {
            lastUseCustomGravity = gravityAffected.useCustomGravity;
        }
    }

    /// <summary>
    /// 問題の自動検出と修正
    /// </summary>
    private void DetectAndFixIssues()
    {
        if (rb2d == null || !playerController.isAlive) return;

        List<string> issues = new List<string>();
        bool needsFix = false;

        // Issue 1: 重力が無効になっている
        if (gravityAffected != null)
        {
            if (!gravityAffected.useCustomGravity && rb2d.gravityScale == 0f)
            {
                issues.Add("No gravity applied (both custom and standard gravity disabled)");
                needsFix = true;
            }
        }
        else if (rb2d.gravityScale == 0f)
        {
            issues.Add("No gravity applied (standard gravity disabled, no GravityAffectedObject)");
            needsFix = true;
        }

        // Issue 2: 異常な重力値
        if (gravityAffected != null)
        {
            Vector2 currentGravity = gravityAffected.GetCurrentGravity();
            if (currentGravity.magnitude < 1f)
            {
                issues.Add($"Abnormally low gravity magnitude: {currentGravity.magnitude:F3}");
                needsFix = true;
            }
        }

        // Issue 3: 異常な慣性設定
        if (gravityAffected != null && gravityAffected.maintainInertia && gravityAffected.inertiaDecay < 0.8f)
        {
            issues.Add($"Low inertia decay causing gravity reduction: {gravityAffected.inertiaDecay:F3}");
            needsFix = true;
        }

        // Issue 4: プレイヤーが落下していない
        if (!playerController.IsGrounded() && Mathf.Abs(rb2d.linearVelocity.y) < 0.1f)
        {
            issues.Add("Player not falling despite being airborne");
            needsFix = true;
        }

        if (needsFix)
        {
            Debug.LogWarning($"GravityDebugTool: Issues detected: {string.Join(", ", issues)}");

            if (enableAutoFix)
            {
                ApplyManualFix();
            }
        }
    }

    /// <summary>
    /// 手動修正の適用
    /// </summary>
    private void ApplyManualFix()
    {
        Debug.Log("GravityDebugTool: Applying manual fix...");

        if (gravityAffected != null)
        {
            // GravityAffectedObjectの修正
            gravityAffected.useCustomGravity = true;
            gravityAffected.gravityScale = 1f;
            gravityAffected.maintainInertia = true; // PlayerControllerの初期設定に合わせる
            gravityAffected.inertiaDecay = 0.95f;   // PlayerControllerの初期設定に合わせる
            gravityAffected.smoothGravityTransition = true;

            // Rigidbody2Dの修正
            rb2d.gravityScale = 0f; // カスタム重力使用時

            Debug.Log("GravityDebugTool: GravityAffectedObject settings restored");
        }
        else
        {
            // Unity標準重力を使用
            rb2d.gravityScale = 1f;
            Debug.Log("GravityDebugTool: Unity standard gravity enabled");
        }

        // 速度リセット（必要に応じて）
        if (Mathf.Abs(rb2d.linearVelocity.y) < 0.1f && !playerController.IsGrounded())
        {
            rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, -2f); // 軽い下向き速度を与える
            Debug.Log("GravityDebugTool: Applied initial downward velocity");
        }

        LogCurrentState("AFTER_MANUAL_FIX");
    }

    // Event handlers
    private void OnPlayerDeath()
    {
        if (logOnRespawn)
        {
            LogCurrentState("PLAYER_DEATH");
        }
    }

    private void OnPlayerRespawn()
    {
        if (logOnRespawn)
        {
            LogCurrentState("PLAYER_RESPAWN");

            // リスポーン後の遅延チェック
            StartCoroutine(DelayedRespawnCheck());
        }
    }

    private void OnRespawnStarted()
    {
        if (logOnRespawn)
        {
            LogCurrentState("RESPAWN_STARTED");
        }
    }

    private void OnRespawnCompleted()
    {
        if (logOnRespawn)
        {
            LogCurrentState("RESPAWN_COMPLETED");
        }
    }

    /// <summary>
    /// リスポーン後の遅延チェック
    /// </summary>
    private IEnumerator DelayedRespawnCheck()
    {
        yield return new WaitForSeconds(0.5f);
        LogCurrentState("RESPAWN_DELAYED_CHECK");

        yield return new WaitForSeconds(1f);
        LogCurrentState("RESPAWN_1SEC_LATER");

        yield return new WaitForSeconds(2f);
        LogCurrentState("RESPAWN_3SEC_LATER");
    }

    // GUI
    private void OnGUI()
    {
        if (!showGUI || !showWindow) return;

        windowRect = GUI.Window(0, windowRect, DrawDebugWindow, "Gravity Debug Tool");
    }

    private void DrawDebugWindow(int windowID)
    {
        GUILayout.Label("Real-time Gravity Status", EditorStyles.boldLabel);

        if (rb2d != null)
        {
            GUILayout.Label($"Velocity: {rb2d.linearVelocity}");
            GUILayout.Label($"RB2D Gravity Scale: {rb2d.gravityScale}");
        }

        if (gravityAffected != null)
        {
            GUILayout.Label($"Use Custom Gravity: {gravityAffected.useCustomGravity}");
            GUILayout.Label($"Gravity Scale: {gravityAffected.gravityScale}");
            GUILayout.Label($"Current Gravity: {gravityAffected.GetCurrentGravity()}");
            GUILayout.Label($"Maintain Inertia: {gravityAffected.maintainInertia}");
            GUILayout.Label($"Inertia Decay: {gravityAffected.inertiaDecay:F3}");
        }

        GUILayout.Label($"Physics2D Gravity: {Physics2D.gravity}");

        if (playerController != null)
        {
            GUILayout.Label($"Is Grounded: {playerController.IsGrounded()}");
            GUILayout.Label($"Player State: {playerController.currentState}");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Manual Fix (F9)"))
        {
            ApplyManualFix();
        }

        if (GUILayout.Button("Log Current State"))
        {
            LogCurrentState("MANUAL_LOG");
        }

        enableAutoFix = GUILayout.Toggle(enableAutoFix, "Auto Fix");
        enableRealTimeLogging = GUILayout.Toggle(enableRealTimeLogging, "Real-time Logging");

        GUI.DragWindow();
    }

    // Public API
    public void StartDebugging()
    {
        enableRealTimeLogging = true;
        showGUI = true;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(RealTimeLoggingCoroutine());
        }
    }

    public void StopDebugging()
    {
        enableRealTimeLogging = false;
        showGUI = false;
        StopAllCoroutines();
    }

    public void LogStateNow()
    {
        LogCurrentState("EXTERNAL_REQUEST");
    }
}

// EditorStylesの代替
public static class EditorStyles
{
    public static GUIStyle boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
}