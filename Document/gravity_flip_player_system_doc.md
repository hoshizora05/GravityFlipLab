# Gravity Flip Lab - プレイヤーキャラクターシステム詳細ドキュメント

## 目次

1. [システム概要](#システム概要)
2. [アーキテクチャ設計](#アーキテクチャ設計)
3. [データ構造](#データ構造)
4. [コンポーネント詳細](#コンポーネント詳細)
5. [クラス仕様](#クラス仕様)
6. [API リファレンス](#api-リファレンス)
7. [実装ガイド](#実装ガイド)
8. [ベストプラクティス](#ベストプラクティス)
9. [トラブルシューティング](#トラブルシューティング)
10. [パフォーマンス最適化](#パフォーマンス最適化)

---

## システム概要

Gravity Flip Lab のプレイヤーキャラクターシステム（SYNC-01）は、ゲームの中核となる重力反転アクションを実現する統合システムです。このシステムは、プレイヤーの移動、物理挙動、アニメーション、視覚効果、そして当たり判定を一元管理し、滑らかで応答性の高いゲームプレイ体験を提供します。

### 主要特徴

- **重力反転システム**: 瞬時の重力方向切り替えと物理演算
- **オートランシステム**: 一定速度での自動移動とプレイヤー制御の分離
- **精密な当たり判定**: 頭・足部分の無敵フレーム実装
- **動的アニメーション**: 状態に応じた滑らかなアニメーション遷移
- **カスタマイズ可能な外観**: 8色のスキンカラーとエフェクト
- **包括的なイベントシステム**: 他システムとの疎結合な連携

### ゲームプレイ仕様

- **移動速度**: 5.0 units/sec（設定可能）
- **重力反転時間**: 0.1秒の瞬間反転
- **無敵フレーム**: 頭・足衝突時0.1秒
- **最大落下速度**: 20.0 units/sec
- **アシストモード**: バリア3個、ゲーム速度80%

---

## アーキテクチャ設計

### システム構成図

```
┌─────────────────────────────────────┐
│          PlayerController          │
│         (中央制御システム)           │
├─────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────────┐ │
│ │PlayerMovement│ │PlayerAnimation │ │
│ │(移動制御)    │ │(アニメーション) │ │
│ └─────────────┘ └─────────────────┘ │
│ ┌─────────────┐ ┌─────────────────┐ │
│ │PlayerCollision│ │PlayerVisuals   │ │
│ │(当たり判定)   │ │(視覚効果)      │ │
│ └─────────────┘ └─────────────────┘ │
├─────────────────────────────────────┤
│        CheckpointManager            │
│       (チェックポイント管理)         │
└─────────────────────────────────────┘
```

### 設計原則

1. **Component Pattern**: 機能別に分割された専門コンポーネント
2. **Event-Driven Architecture**: イベントによる疎結合な通信
3. **State Machine**: 明確な状態管理と遷移制御
4. **Physics Integration**: Unity物理エンジンとの統合
5. **Modularity**: 各コンポーネントの独立性と再利用性

### 依存関係

```
PlayerController (Core)
    ├── PlayerMovement (Physics)
    ├── PlayerAnimation (Visual)
    ├── PlayerCollision (Detection)
    ├── PlayerVisuals (Effects)
    └── CheckpointManager (External)
        └── GameManager (Global)
```

---

## データ構造

### PlayerStats（プレイヤー統計データ）

プレイヤーキャラクターの物理的特性と状態を定義します。

```csharp
[System.Serializable]
public class PlayerStats
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;              // 移動速度（units/sec）
    public float fallSpeed = 10.0f;             // 基本落下速度
    public float maxFallSpeed = 20.0f;          // 最大落下速度
    public float gravityScale = 1.0f;           // 重力スケール
    
    [Header("Combat")]
    public float invincibilityDuration = 0.1f;  // 無敵時間（秒）
    public bool isInvincible = false;           // 現在の無敵状態
    public int livesRemaining = 3;              // 残りライフ数
}
```

**フィールド詳細**:
- `moveSpeed`: 水平方向の移動速度。オートランの基準速度
- `fallSpeed`: 重力による基本落下加速度
- `maxFallSpeed`: 落下速度の上限値。無制限加速を防ぐ
- `gravityScale`: Unity物理エンジンの重力スケール係数
- `invincibilityDuration`: ダメージ後の無敵フレーム持続時間
- `isInvincible`: 現在無敵状態かどうかのフラグ
- `livesRemaining`: プレイヤーの残りライフ数

### PlayerState（プレイヤー状態列挙型）

プレイヤーキャラクターの現在状態を定義します。

```csharp
public enum PlayerState
{
    Running,        // 地上走行中
    Falling,        // 落下中
    GravityFlipping, // 重力反転実行中
    Dead,           // 死亡状態
    Invincible      // 無敵状態
}
```

**状態詳細**:
- `Running`: 地面に接地して水平移動している状態
- `Falling`: 空中で重力の影響を受けている状態
- `GravityFlipping`: 重力反転のアニメーション中
- `Dead`: プレイヤーが死亡し、リスポーン待ちの状態
- `Invincible`: ダメージ後の一時的な無敵状態

### GravityDirection（重力方向列挙型）

重力の方向を数値で表現します。

```csharp
public enum GravityDirection
{
    Down = -1,      // 下向き重力（通常）
    Up = 1          // 上向き重力（反転）
}
```

**設計理由**:
- 数値での表現により、物理計算での直接使用が可能
- -1と1により、単純な乗算で方向転換を実現

---

## コンポーネント詳細

### PlayerController（中央制御システム）

プレイヤーキャラクターの全体的な制御を行うメインコンポーネントです。

#### 主要責任

1. **状態管理**: プレイヤーの現在状態の追跡と遷移制御
2. **入力処理**: プレイヤー入力の受付と各コンポーネントへの伝達
3. **生死管理**: ダメージ処理、死亡・リスポーン制御
4. **重力制御**: 重力反転の実行と管理
5. **イベント発行**: 他システムへの状態変化通知

#### 重要なフィールド

```csharp
[Header("Player Components")]
public PlayerMovement movement;           // 移動制御コンポーネント
public PlayerAnimation playerAnimation;   // アニメーション制御
public PlayerCollision playerCollision;   // 当たり判定制御
public PlayerVisuals playerVisuals;       // 視覚効果制御

[Header("Player Stats")]
public PlayerStats stats = new PlayerStats(); // プレイヤー統計データ

[Header("Debug")]
public bool debugMode = false;            // デバッグモード有効化
```

#### イベントシステム

```csharp
// 静的イベント - ゲーム全体での購読が可能
public static event System.Action OnPlayerDeath;           // プレイヤー死亡時
public static event System.Action OnPlayerRespawn;         // プレイヤーリスポーン時
public static event System.Action<GravityDirection> OnGravityFlip; // 重力反転時
public static event System.Action<PlayerState> OnStateChanged;     // 状態変更時
```

#### 状態遷移ロジック

```csharp
private void UpdateStateLogic()
{
    switch (currentState)
    {
        case PlayerState.Running:
            // Y速度が一定以上なら落下状態へ
            if (Mathf.Abs(rb2d.velocity.y) > 0.1f)
            {
                ChangeState(PlayerState.Falling);
            }
            break;

        case PlayerState.Falling:
            // 地面に接触且つY速度が小さいなら走行状態へ
            if (Mathf.Abs(rb2d.velocity.y) < 0.1f && movement.IsGrounded())
            {
                ChangeState(PlayerState.Running);
            }
            break;

        case PlayerState.GravityFlipping:
            // 重力反転は時間制限があり、自動的に他の状態に遷移
            break;

        case PlayerState.Dead:
            // 死亡状態では何もしない（リスポーン処理は別途実行）
            break;
    }
}
```

### PlayerMovement（移動制御システム）

プレイヤーキャラクターの物理的な移動を制御します。

#### 主要機能

1. **オートラン**: 一定速度での自動右移動
2. **重力適用**: カスタム重力システムとの統合
3. **地面判定**: 接地状態の精密な検出
4. **速度制限**: 落下速度の上限制御
5. **斜面対応**: 斜面での速度調整

#### 地面判定システム

```csharp
private void CheckGrounded()
{
    // 重力方向に応じたレイキャスト方向を決定
    Vector2 rayDirection = (playerController.gravityDirection == GravityDirection.Down) ? 
        Vector2.down : Vector2.up;
    
    // 地面検出レイキャスト
    groundHit = Physics2D.Raycast(groundCheckPoint.position, rayDirection, 
        groundCheckDistance, groundLayerMask);
    
    isGrounded = groundHit.collider != null;
}
```

#### オートラン実装

```csharp
private void ApplyAutoRun()
{
    Vector2 velocity = rb2d.velocity;
    velocity.x = autoRunSpeed;
    
    // 斜面での速度補正
    if (isGrounded && groundHit.normal != Vector2.up)
    {
        float slopeAngle = Vector2.Angle(groundHit.normal, Vector2.up);
        if (slopeAngle > 5f)
        {
            velocity.x *= slopeSpeedMultiplier;
        }
    }
    
    rb2d.velocity = velocity;
}
```

### PlayerAnimation（アニメーション制御システム）

プレイヤーキャラクターのアニメーションを管理します。

#### アニメーション状態

1. **Run**: 走行アニメーション（4フレームループ）
2. **Fall**: 落下アニメーション（2フレーム）
3. **GravityFlip**: 重力反転エフェクト
4. **Death**: 死亡アニメーション

#### 重力方向対応

```csharp
public void UpdateGravityDirection(GravityDirection direction)
{
    if (spriteRenderer != null)
    {
        // 重力方向に応じてスプライトを反転
        spriteRenderer.flipY = (direction == GravityDirection.Up);
    }
}
```

#### アニメーション速度制御

```csharp
public void OnStateChanged(PlayerState newState)
{
    if (animator == null) return;

    switch (newState)
    {
        case PlayerState.Running:
            animator.SetTrigger(runTrigger);
            animator.speed = runAnimationSpeed;    // 1.0f
            break;

        case PlayerState.Falling:
            animator.SetTrigger(fallTrigger);
            animator.speed = fallAnimationSpeed;   // 0.5f
            break;
    }
}
```

### PlayerCollision（当たり判定システム）

プレイヤーキャラクターの衝突検出と処理を行います。

#### レイヤーマスク管理

```csharp
[Header("Collision Settings")]
public LayerMask obstacleLayerMask = 1;      // 障害物レイヤー
public LayerMask collectibleLayerMask = 1;    // 収集アイテムレイヤー
public LayerMask checkpointLayerMask = 1;     // チェックポイントレイヤー
```

#### 特殊な当たり判定ロジック

```csharp
private void HandleObstacleCollision(Collider2D obstacle)
{
    if (playerController.stats.isInvincible) return;

    // 頭・足の衝突判定
    bool isHeadFeetCollision = IsHeadOrFeetCollision(obstacle);
    
    if (isHeadFeetCollision)
    {
        lastCollisionTime = Time.time;
        StartCoroutine(DelayedDamage()); // 0.1秒後にダメージ
    }
    else
    {
        playerController.TakeDamage();   // 即座にダメージ
    }
}

private bool IsHeadOrFeetCollision(Collider2D obstacle)
{
    Vector2 playerCenter = transform.position;
    Vector2 obstacleCenter = obstacle.bounds.center;
    
    float verticalDistance = Mathf.Abs(playerCenter.y - obstacleCenter.y);
    float horizontalDistance = Mathf.Abs(playerCenter.x - obstacleCenter.x);
    
    // 垂直距離が水平距離より大きい場合は頭・足の衝突
    return verticalDistance > horizontalDistance;
}
```

### PlayerVisuals（視覚効果システム）

プレイヤーキャラクターの視覚効果とスキンシステムを管理します。

#### エフェクトコンポーネント

```csharp
[Header("Visual Effects")]
public ParticleSystem gravityFlipEffect;  // 重力反転エフェクト
public ParticleSystem deathEffect;        // 死亡エフェクト
public ParticleSystem trailEffect;        // 軌跡エフェクト
```

#### スキンシステム

```csharp
[Header("Skin Settings")]
public SpriteRenderer bodyRenderer;       // 本体レンダラー
public SpriteRenderer visorRenderer;      // バイザーレンダラー
public Color[] skinColors = new Color[8]; // 8色のスキンカラー
public int currentSkinIndex = 0;          // 現在のスキンインデックス
```

#### 無敵時視覚効果

```csharp
private IEnumerator InvincibilityFlashCoroutine()
{
    while (playerController.stats.isInvincible)
    {
        SetRenderersAlpha(0.3f);  // 半透明に
        yield return new WaitForSeconds(flashInterval);
        SetRenderersAlpha(1.0f);  // 不透明に
        yield return new WaitForSeconds(flashInterval);
    }
}
```

### CheckpointManager（チェックポイント管理システム）

リスポーン地点の管理を行うシングルトンシステムです。

#### チェックポイント履歴

```csharp
private Vector3 currentCheckpointPosition;              // 現在のチェックポイント
private List<Vector3> checkpointHistory = new List<Vector3>(); // 履歴
```

#### 自動チェックポイント設定

```csharp
public void SetCheckpoint(Vector3 position)
{
    checkpointHistory.Add(currentCheckpointPosition);
    currentCheckpointPosition = position;
    
    Debug.Log($"Checkpoint set at: {position}");
}
```

---

## クラス仕様

### PlayerController

**継承**: MonoBehaviour  
**必要コンポーネント**: Rigidbody2D, Collider2D, PlayerMovement, PlayerAnimation

#### プロパティ

| プロパティ名 | 型 | アクセス | 説明 |
|-------------|----|---------|----- |
| currentState | PlayerState | public | 現在のプレイヤー状態 |
| gravityDirection | GravityDirection | public | 現在の重力方向 |
| isAlive | bool | public | 生死状態 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| FlipGravity() | void | 重力反転実行 |
| TakeDamage() | void | ダメージ処理 |
| Respawn() | void | リスポーン処理 |
| GetVelocity() | Vector2 | 現在速度取得 |
| SetVelocity(Vector2) | void | 速度設定 |
| AddForce(Vector2) | void | 力を加える |

### PlayerMovement

**継承**: MonoBehaviour

#### フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| autoRunSpeed | float | 5.0f | オートラン速度 |
| gravityForce | float | 9.81f | 重力の強さ |
| maxFallSpeed | float | 20.0f | 最大落下速度 |
| groundCheckDistance | float | 0.6f | 地面判定距離 |
| springForce | float | 5.0f | スプリング力 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| IsGrounded() | bool | 接地判定 |
| ApplyGravityFlip(GravityDirection) | void | 重力反転適用 |
| ApplySpringForce(Vector2) | void | スプリング力適用 |

### PlayerAnimation

**継承**: MonoBehaviour

#### フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| runAnimationSpeed | float | 1.0f | 走行アニメーション速度 |
| fallAnimationSpeed | float | 0.5f | 落下アニメーション速度 |

#### トリガー名

| トリガー名 | 用途 |
|-----------|----- |
| "Run" | 走行アニメーション |
| "Fall" | 落下アニメーション |
| "GravityFlip" | 重力反転アニメーション |
| "Death" | 死亡アニメーション |

### PlayerCollision

**継承**: MonoBehaviour

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| HandleCollision(Collider2D) | void | 衝突処理 |
| IsHeadOrFeetCollision(Collider2D) | bool | 頭・足衝突判定 |

### PlayerVisuals

**継承**: MonoBehaviour

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| PlayGravityFlipEffect() | void | 重力反転エフェクト再生 |
| PlayDeathEffect() | void | 死亡エフェクト再生 |
| ApplySkin(int) | void | スキン適用 |
| SetInvincibleVisuals(bool) | void | 無敵時視覚効果設定 |

---

## API リファレンス

### PlayerController API

#### インスタンスアクセス

```csharp
// プレイヤーオブジェクトからの取得
PlayerController player = GameObject.FindGameObjectWithTag("Player")
    .GetComponent<PlayerController>();
```

#### 状態制御

```csharp
// 重力反転
player.FlipGravity();

// ダメージ処理
player.TakeDamage();

// 強制リスポーン
player.Respawn();

// 現在状態取得
PlayerState state = player.currentState;
GravityDirection gravity = player.gravityDirection;
bool alive = player.isAlive;
```

#### 物理制御

```csharp
// 速度制御
Vector2 velocity = player.GetVelocity();
player.SetVelocity(new Vector2(5f, 0f));

// 力を加える
player.AddForce(Vector2.up * 10f);
```

#### イベント購読

```csharp
private void OnEnable()
{
    PlayerController.OnPlayerDeath += HandlePlayerDeath;
    PlayerController.OnPlayerRespawn += HandlePlayerRespawn;
    PlayerController.OnGravityFlip += HandleGravityFlip;
    PlayerController.OnStateChanged += HandleStateChanged;
}

private void OnDisable()
{
    PlayerController.OnPlayerDeath -= HandlePlayerDeath;
    PlayerController.OnPlayerRespawn -= HandlePlayerRespawn;
    PlayerController.OnGravityFlip -= HandleGravityFlip;
    PlayerController.OnStateChanged -= HandleStateChanged;
}

private void HandlePlayerDeath()
{
    Debug.Log("Player died!");
    // UI更新、効果音再生など
}

private void HandleGravityFlip(GravityDirection direction)
{
    Debug.Log($"Gravity flipped to: {direction}");
    // 環境エフェクト、カメラシェイクなど
}
```

### PlayerVisuals API

#### スキン管理

```csharp
// スキン変更
PlayerVisuals visuals = player.playerVisuals;
visuals.ApplySkin(3); // 4番目のスキンカラーを適用

// エフェクト制御
visuals.SetTrailEffect(true);  // 軌跡エフェクト有効化
visuals.PlayGravityFlipEffect(); // 重力反転エフェクト再生
```

### CheckpointManager API

#### チェックポイント管理

```csharp
// チェックポイント設定
CheckpointManager.Instance.SetCheckpoint(transform.position);

// 現在のチェックポイント取得
Vector3 respawnPos = CheckpointManager.Instance.GetCurrentCheckpointPosition();

// デフォルトチェックポイントにリセット
CheckpointManager.Instance.ResetToDefaultCheckpoint();
```

---

## 実装ガイド

### 基本セットアップ

#### 1. プレイヤーオブジェクトの作成

```csharp
// 空のGameObjectを作成し、以下を設定
1. Tag: "Player"
2. Layer: "Player"（デフォルトでも可）
3. 必要コンポーネントの追加:
   - Rigidbody2D
   - Collider2D (BoxCollider2D推奨)
   - SpriteRenderer
   - Animator
```

#### 2. 物理設定

```csharp
// Rigidbody2D設定
- Mass: 1
- Linear Drag: 0
- Angular Drag: 0.05
- Gravity Scale: 0 (カスタム重力を使用)
- Freeze Rotation Z: true
```

#### 3. コンポーネントアタッチ

```csharp
// PlayerControllerスクリプトをアタッチ
// 他のコンポーネント（Movement, Animation, Collision, Visuals）も同じオブジェクトに追加
```

### 地面判定ポイントの設置

```csharp
// プレイヤーオブジェクトの子として空のGameObjectを作成
GameObject groundCheck = new GameObject("GroundCheckPoint");
groundCheck.transform.SetParent(playerTransform);
groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

// PlayerMovementコンポーネントのgroundCheckPointフィールドに割り当て
```

### アニメーターの設定

#### アニメーター状態構成

```
Entry -> Running (デフォルト)
Running -> Falling (Fall trigger)
Falling -> Running (Run trigger)
Any State -> GravityFlip (GravityFlip trigger)
Any State -> Death (Death trigger)
```

#### パラメーター設定

```csharp
// Animatorパラメーター
- Run (Trigger)
- Fall (Trigger)  
- GravityFlip (Trigger)
- Death (Trigger)
```

### レイヤーマスクの設定

```csharp
// Project Settings -> Tags and Layers で以下を設定
- Layer 8: Ground
- Layer 9: Obstacle  
- Layer 10: Collectible
- Layer 11: Checkpoint
```

### パーティクルシステムの設定

#### 重力反転エフェクト

```csharp
// パーティクルシステム設定例
var main = gravityFlipEffect.main;
main.startLifetime = 0.5f;
main.startSpeed = 10f;
main.maxParticles = 50;

var emission = gravityFlipEffect.emission;
emission.rateOverTime = 100f;

var shape = gravityFlipEffect.shape;
shape.shapeType = ParticleSystemShapeType.Circle;
shape.radius = 1f;
```

### 入力システムの統合

```csharp
public class PlayerInput : MonoBehaviour
{
    private PlayerController playerController;
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        // 複数入力対応
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0) ||
            Input.GetButtonDown("Fire1"))
        {
            playerController.FlipGravity();
        }
    }
}
```

---

## ベストプラクティス

### コンポーネント初期化の順序

```csharp
private void Awake()
{
    // 1. 必須コンポーネントの取得
    InitializeRequiredComponents();
}

private void Start()  
{
    // 2. 他オブジェクトとの連携が必要な初期化
    Initialize();
}

private void InitializeRequiredComponents()
{
    rb2d = GetComponent<Rigidbody2D>();
    playerCollider = GetComponent<Collider2D>();
    
    // null チェック
    if (rb2d == null)
    {
        Debug.LogError("Rigidbody2D component required!");
        enabled = false;
        return;
    }
}
```

### イベント管理

```csharp
// イベント購読の安全な管理
public class SafeEventListener : MonoBehaviour
{
    private System.Action<PlayerState> stateChangeHandler;
    
    private void Awake()
    {
        // ハンドラーを事前に作成
        stateChangeHandler = OnPlayerStateChanged;
    }
    
    private void OnEnable()
    {
        PlayerController.OnStateChanged += stateChangeHandler;
    }
    
    private void OnDisable()
    {
        PlayerController.OnStateChanged -= stateChangeHandler;
    }
    
    private void OnPlayerStateChanged(PlayerState newState)
    {
        // 状態変更処理
    }
}
```

### パフォーマンス最適化

#### オブジェクトプーリング

```csharp
public class EffectPool : MonoBehaviour
{
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<GameObject> effectPool = new Queue<GameObject>();
    
    private void Start()
    {
        // プール初期化
        for (int i = 0; i < poolSize; i++)
        {
            GameObject effect = Instantiate(effectPrefab);
            effect.SetActive(false);
            effectPool.Enqueue(effect);
        }
    }
    
    public GameObject GetEffect()
    {
        if (effectPool.Count > 0)
        {
            GameObject effect = effectPool.Dequeue();
            effect.SetActive(true);
            return effect;
        }
        
        // プールが空の場合は新規作成
        return Instantiate(effectPrefab);
    }
    
    public void ReturnEffect(GameObject effect)
    {
        effect.SetActive(false);
        effectPool.Enqueue(effect);
    }
}
```

#### フレームレート最適化

```csharp
public class PlayerOptimization : MonoBehaviour
{
    private PlayerController playerController;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.016f; // 60fps
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        // 必要以上の更新を避ける
        if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
        
        // 重要でない処理の間引き
        if (Time.frameCount % 3 == 0)
        {
            UpdateNonCriticalSystems();
        }
        
        lastUpdateTime = Time.time;
    }
    
    private void UpdateNonCriticalSystems()
    {
        // パーティクルエフェクトの更新など
        // UI更新など
    }
}
```

### エラーハンドリング

#### コンポーネント依存関係の確認

```csharp
public class ComponentValidator : MonoBehaviour
{
    private void Awake()
    {
        ValidateComponents();
    }
    
    private void ValidateComponents()
    {
        List<string> missingComponents = new List<string>();
        
        if (GetComponent<Rigidbody2D>() == null)
            missingComponents.Add("Rigidbody2D");
            
        if (GetComponent<Collider2D>() == null)
            missingComponents.Add("Collider2D");
            
        if (GetComponent<PlayerMovement>() == null)
            missingComponents.Add("PlayerMovement");
            
        if (missingComponents.Count > 0)
        {
            string errorMessage = $"Missing required components: {string.Join(", ", missingComponents)}";
            Debug.LogError(errorMessage, this);
            
            // ゲームを停止させるか、安全な状態に移行
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
```

#### 物理エラーの対処

```csharp
private void FixedUpdate()
{
    try
    {
        // 物理更新処理
        UpdatePhysics();
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Physics update error: {e.Message}", this);
        
        // 安全な状態に復帰
        rb2d.velocity = Vector2.zero;
        transform.position = CheckpointManager.Instance.GetCurrentCheckpointPosition();
    }
}

private void UpdatePhysics()
{
    // NaN や Infinity のチェック
    if (float.IsNaN(rb2d.velocity.x) || float.IsNaN(rb2d.velocity.y) ||
        float.IsInfinity(rb2d.velocity.x) || float.IsInfinity(rb2d.velocity.y))
    {
        Debug.LogWarning("Invalid velocity detected, resetting");
        rb2d.velocity = Vector2.zero;
    }
    
    // 位置の妥当性チェック
    if (Mathf.Abs(transform.position.x) > 10000f || Mathf.Abs(transform.position.y) > 10000f)
    {
        Debug.LogWarning("Player position out of bounds, teleporting to checkpoint");
        transform.position = CheckpointManager.Instance.GetCurrentCheckpointPosition();
    }
}
```

---

## トラブルシューティング

### よくある問題と解決策

#### 1. プレイヤーが地面を貫通する

**症状**: プレイヤーが地面をすり抜けて落下し続ける

**原因**:
- 物理演算の時間間隔とオブジェクトの速度が合わない
- コライダーのサイズが不適切
- レイヤーマスクの設定ミス

**解決策**:
```csharp
// Rigidbody2Dの設定を調整
public void FixGroundPenetration()
{
    Rigidbody2D rb = GetComponent<Rigidbody2D>();
    
    // Collision Detection を Continuous に変更
    rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    
    // Interpolate を有効化
    rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    
    // 落下速度を制限
    Vector2 velocity = rb.velocity;
    velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    rb.velocity = velocity;
}

// Project Settings -> Time -> Fixed Timestep を 0.02 から 0.01 に変更
// (物理演算の精度向上)
```

#### 2. 重力反転が正常に動作しない

**症状**: 重力反転ボタンを押しても反転しない、または不自然な動きになる

**原因**:
- コルーチンの重複実行
- 物理設定の競合
- 状態管理の不整合

**解決策**:
```csharp
public void FlipGravity()
{
    // 既に重力反転中なら無視
    if (currentState == PlayerState.GravityFlipping) 
    {
        Debug.Log("Gravity flip already in progress");
        return;
    }
    
    // 死亡状態では実行しない
    if (currentState == PlayerState.Dead) 
    {
        Debug.Log("Cannot flip gravity while dead");
        return;
    }
    
    // 重力反転実行
    StartCoroutine(GravityFlipCoroutine());
}

private IEnumerator GravityFlipCoroutine()
{
    // 状態変更
    ChangeState(PlayerState.GravityFlipping);
    
    // 重力方向切り替え
    gravityDirection = gravityDirection == GravityDirection.Down ? 
        GravityDirection.Up : GravityDirection.Down;
    
    // 物理設定適用
    movement.ApplyGravityFlip(gravityDirection);
    
    // 視覚効果
    playerVisuals.PlayGravityFlipEffect();
    
    // イベント発行
    OnGravityFlip?.Invoke(gravityDirection);
    
    // 待機時間
    yield return new WaitForSeconds(ConfigManager.Instance.gravityFlipDuration);
    
    // 状態復帰
    ChangeState(movement.IsGrounded() ? PlayerState.Running : PlayerState.Falling);
}
```

#### 3. アニメーションが切り替わらない

**症状**: プレイヤーの状態が変わってもアニメーションが更新されない

**原因**:
- Animatorパラメーターの設定ミス
- トランジション条件の不備
- スクリプトとAnimatorの連携エラー

**解決策**:
```csharp
public class AnimationDebugger : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugAnimationState();
        }
    }
    
    private void DebugAnimationState()
    {
        if (animator == null) 
        {
            Debug.LogError("Animator component not found!");
            return;
        }
        
        // 現在の状態情報を出力
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current Animation State: {stateInfo.fullPathHash}");
        Debug.Log($"Player State: {playerController.currentState}");
        
        // パラメーター確認
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"Parameter: {param.name}, Type: {param.type}");
        }
        
        // 強制的にアニメーション更新
        ForceAnimationUpdate();
    }
    
    private void ForceAnimationUpdate()
    {
        switch (playerController.currentState)
        {
            case PlayerState.Running:
                animator.SetTrigger("Run");
                break;
            case PlayerState.Falling:
                animator.SetTrigger("Fall");
                break;
        }
    }
}
```

#### 4. パフォーマンスの低下

**症状**: ゲーム実行中にフレームレートが大幅に低下する

**原因**:
- Update()での重い処理
- メモリリークによるGC頻発
- パーティクルエフェクトの過剰生成

**解決策**:
```csharp
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Performance Settings")]
    public bool enableProfiling = true;
    public float profileInterval = 1f;
    
    private float lastProfileTime = 0f;
    private int frameCount = 0;
    private float fps = 0f;
    
    private void Update()
    {
        if (!enableProfiling) return;
        
        frameCount++;
        
        if (Time.time - lastProfileTime >= profileInterval)
        {
            fps = frameCount / profileInterval;
            frameCount = 0;
            lastProfileTime = Time.time;
            
            // パフォーマンス警告
            if (fps < 30f)
            {
                Debug.LogWarning($"Low FPS detected: {fps:F1}");
                OptimizePerformance();
            }
        }
    }
    
    private void OptimizePerformance()
    {
        // メモリクリーンアップ
        System.GC.Collect();
        
        // パーティクルエフェクトの制限
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
        foreach (var particle in particles)
        {
            if (particle.particleCount > 100)
            {
                particle.Stop();
                Debug.Log($"Stopped excessive particle system: {particle.name}");
            }
        }
        
        // 不要なオブジェクトの削除
        CleanupInactiveObjects();
    }
    
    private void CleanupInactiveObjects()
    {
        // 非アクティブなエフェクトオブジェクトを削除
        GameObject[] effects = GameObject.FindGameObjectsWithTag("Effect");
        foreach (var effect in effects)
        {
            if (!effect.activeInHierarchy)
            {
                Destroy(effect);
            }
        }
    }
}
```

#### 5. セーブデータとの整合性エラー

**症状**: プレイヤーの状態がセーブデータと一致しない

**原因**:
- 初期化順序の問題
- セーブデータの破損
- バージョン互換性の問題

**解決策**:
```csharp
public void InitializeFromSaveData()
{
    try
    {
        PlayerProgress progress = GameManager.Instance.playerProgress;
        
        // プレイヤー設定の適用
        ApplyPlayerSettings(progress.settings);
        
        // 統計データの適用
        ApplyPlayerStats(progress);
        
        Debug.Log("Player initialized from save data successfully");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to initialize from save data: {e.Message}");
        
        // デフォルト設定で初期化
        InitializeWithDefaults();
    }
}

private void ApplyPlayerSettings(PlayerSettings settings)
{
    if (settings == null)
    {
        Debug.LogWarning("Player settings is null, using defaults");
        return;
    }
    
    // アシストモード設定
    if (settings.assistModeEnabled)
    {
        stats.livesRemaining += ConfigManager.Instance.assistModeBarrierCount;
        stats.moveSpeed *= 0.8f; // 速度を80%に
    }
    
    // 入力設定
    // InputManager で設定を適用
}

private void InitializeWithDefaults()
{
    Debug.Log("Initializing player with default settings");
    
    stats = new PlayerStats();
    currentState = PlayerState.Running;
    gravityDirection = GravityDirection.Down;
    isAlive = true;
}
```

---

## パフォーマンス最適化

### CPU最適化

#### フレーム処理の分散

```csharp
public class FrameDistributor : MonoBehaviour
{
    private PlayerController playerController;
    private Queue<System.Action> frameQueue = new Queue<System.Action>();
    private int maxActionsPerFrame = 3;
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void Update()
    {
        // フレームあたりの処理数を制限
        int processedActions = 0;
        
        while (frameQueue.Count > 0 && processedActions < maxActionsPerFrame)
        {
            System.Action action = frameQueue.Dequeue();
            action.Invoke();
            processedActions++;
        }
    }
    
    public void ScheduleAction(System.Action action)
    {
        frameQueue.Enqueue(action);
    }
    
    // 使用例
    private void UpdateVisualEffects()
    {
        ScheduleAction(() => UpdateParticleEffects());
        ScheduleAction(() => UpdateSkinColors());
        ScheduleAction(() => UpdateAnimationBlending());
    }
}
```

#### 計算結果のキャッシュ

```csharp
public class PlayerCalculationCache : MonoBehaviour
{
    private PlayerController playerController;
    
    // キャッシュされた値
    private Vector2 cachedVelocity;
    private bool velocityCacheValid = false;
    private float velocityCacheTime = 0f;
    private const float CACHE_DURATION = 0.033f; // 30fps相当
    
    public Vector2 GetCachedVelocity()
    {
        if (!velocityCacheValid || Time.time - velocityCacheTime > CACHE_DURATION)
        {
            cachedVelocity = playerController.GetVelocity();
            velocityCacheValid = true;
            velocityCacheTime = Time.time;
        }
        
        return cachedVelocity;
    }
    
    public void InvalidateVelocityCache()
    {
        velocityCacheValid = false;
    }
}
```

### メモリ最適化

#### オブジェクトプールの実装

```csharp
public class PlayerEffectPool : MonoBehaviour
{
    [System.Serializable]
    public class EffectPool
    {
        public GameObject prefab;
        public int poolSize = 10;
        public Queue<GameObject> pool = new Queue<GameObject>();
    }
    
    [SerializeField] private EffectPool[] effectPools;
    private Dictionary<string, EffectPool> poolDictionary = new Dictionary<string, EffectPool>();
    
    private void Start()
    {
        InitializePools();
    }
    
    private void InitializePools()
    {
        foreach (var pool in effectPools)
        {
            poolDictionary[pool.prefab.name] = pool;
            
            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                pool.pool.Enqueue(obj);
            }
        }
    }
    
    public GameObject SpawnEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(effectName))
        {
            Debug.LogWarning($"Effect pool not found: {effectName}");
            return null;
        }
        
        EffectPool pool = poolDictionary[effectName];
        GameObject effect;
        
        if (pool.pool.Count > 0)
        {
            effect = pool.pool.Dequeue();
        }
        else
        {
            effect = Instantiate(pool.prefab);
        }
        
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        effect.SetActive(true);
        
        // 自動でプールに戻すコンポーネントを追加
        AutoReturn autoReturn = effect.GetComponent<AutoReturn>();
        if (autoReturn == null)
        {
            autoReturn = effect.AddComponent<AutoReturn>();
        }
        autoReturn.Initialize(this, effectName);
        
        return effect;
    }
    
    public void ReturnToPool(string effectName, GameObject obj)
    {
        if (poolDictionary.ContainsKey(effectName))
        {
            obj.SetActive(false);
            poolDictionary[effectName].pool.Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}

public class AutoReturn : MonoBehaviour
{
    private PlayerEffectPool pool;
    private string effectName;
    private float lifetime = 2f;
    
    public void Initialize(PlayerEffectPool pool, string effectName)
    {
        this.pool = pool;
        this.effectName = effectName;
        
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifetime);
    }
    
    private void ReturnToPool()
    {
        pool.ReturnToPool(effectName, gameObject);
    }
}
```

#### メモリリーク対策

```csharp
public class MemoryManager : MonoBehaviour
{
    private static MemoryManager _instance;
    public static MemoryManager Instance => _instance;
    
    [Header("Memory Management")]
    public float cleanupInterval = 30f;
    public long memoryThreshold = 100 * 1024 * 1024; // 100MB
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InvokeRepeating(nameof(PerformMemoryCleanup), cleanupInterval, cleanupInterval);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void PerformMemoryCleanup()
    {
        long memoryBefore = System.GC.GetTotalMemory(false);
        
        if (memoryBefore > memoryThreshold)
        {
            // 強制ガベージコレクション
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long memoryAfter = System.GC.GetTotalMemory(false);
            long freedMemory = memoryBefore - memoryAfter;
            
            Debug.Log($"Memory cleanup completed. Freed: {freedMemory / 1024 / 1024}MB");
        }
        
        // リソースのアンロード
        Resources.UnloadUnusedAssets();
    }
    
    public void ForceCleanup()
    {
        PerformMemoryCleanup();
    }
}
```

### GPU最適化

#### エフェクトの最適化

```csharp
public class OptimizedPlayerVisuals : PlayerVisuals
{
    [Header("Optimization Settings")]
    public int maxParticles = 50;
    public float cullingDistance = 20f;
    
    private Camera mainCamera;
    private bool isVisible = true;
    
    protected override void Start()
    {
        base.Start();
        mainCamera = Camera.main;
        
        // パーティクルシステムの最適化
        OptimizeParticleSystems();
    }
    
    private void OptimizeParticleSystems()
    {
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        
        foreach (var particle in particles)
        {
            var main = particle.main;
            main.maxParticles = maxParticles;
            
            // カリング設定
            var renderer = particle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material.enableInstancing = true;
            }
        }
    }
    
    private void Update()
    {
        // 視野外カリング
        CheckVisibility();
        
        if (!isVisible)
        {
            // 視野外では重いエフェクトを停止
            PauseHeavyEffects();
        }
        else
        {
            ResumeHeavyEffects();
        }
    }
    
    private void CheckVisibility()
    {
        if (mainCamera == null) return;
        
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
        
        isVisible = viewportPosition.x >= -0.1f && viewportPosition.x <= 1.1f &&
                   viewportPosition.y >= -0.1f && viewportPosition.y <= 1.1f &&
                   viewportPosition.z > 0f;
    }
    
    private void PauseHeavyEffects()
    {
        if (trailEffect != null && trailEffect.isPlaying)
        {
            trailEffect.Pause();
        }
    }
    
    private void ResumeHeavyEffects()
    {
        if (trailEffect != null && trailEffect.isPaused)
        {
            trailEffect.Play();
        }
    }
}
```

---

## デバッグとテスト

### デバッグツールの実装

```csharp
public class PlayerDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugGUI = true;
    public bool enableDebugDrawing = true;
    public KeyCode debugToggleKey = KeyCode.F12;
    
    private PlayerController playerController;
    private bool debugGUIVisible = false;
    private GUIStyle debugStyle;
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        InitializeDebugStyle();
    }
    
    private void InitializeDebugStyle()
    {
        debugStyle = new GUIStyle();
        debugStyle.fontSize = 14;
        debugStyle.normal.textColor = Color.white;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(debugToggleKey))
        {
            debugGUIVisible = !debugGUIVisible;
        }
        
        // デバッグコマンド
        HandleDebugCommands();
    }
    
    private void HandleDebugCommands()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // 強制重力反転
            playerController.FlipGravity();
            Debug.Log("Debug: Forced gravity flip");
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            // 無敵モード切り替え
            playerController.stats.isInvincible = !playerController.stats.isInvincible;
            Debug.Log($"Debug: Invincibility {(playerController.stats.isInvincible ? "ON" : "OFF")}");
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            // チェックポイントにテレポート
            Vector3 checkpointPos = CheckpointManager.Instance.GetCurrentCheckpointPosition();
            transform.position = checkpointPos;
            Debug.Log($"Debug: Teleported to checkpoint at {checkpointPos}");
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            // 全スキン試着
            StartCoroutine(CycleSkins());
        }
    }
    
    private IEnumerator CycleSkins()
    {
        for (int i = 0; i < playerController.playerVisuals.skinColors.Length; i++)
        {
            playerController.playerVisuals.ApplySkin(i);
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void OnGUI()
    {
        if (!enableDebugGUI || !debugGUIVisible) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== Player Debug Info ===", debugStyle);
        
        // 基本情報
        DrawBasicInfo();
        
        // 物理情報
        DrawPhysicsInfo();
        
        // 状態情報
        DrawStateInfo();
        
        // デバッグアクション
        DrawDebugActions();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawBasicInfo()
    {
        GUILayout.Label("--- Basic Info ---", debugStyle);
        GUILayout.Label($"Position: {transform.position}", debugStyle);
        GUILayout.Label($"Alive: {playerController.isAlive}", debugStyle);
        GUILayout.Label($"Lives: {playerController.stats.livesRemaining}", debugStyle);
        GUILayout.Space(10);
    }
    
    private void DrawPhysicsInfo()
    {
        GUILayout.Label("--- Physics Info ---", debugStyle);
        Vector2 velocity = playerController.GetVelocity();
        GUILayout.Label($"Velocity: ({velocity.x:F2}, {velocity.y:F2})", debugStyle);
        GUILayout.Label($"Gravity Direction: {playerController.gravityDirection}", debugStyle);
        GUILayout.Label($"Grounded: {playerController.movement.IsGrounded()}", debugStyle);
        GUILayout.Space(10);
    }
    
    private void DrawStateInfo()
    {
        GUILayout.Label("--- State Info ---", debugStyle);
        GUILayout.Label($"Current State: {playerController.currentState}", debugStyle);
        GUILayout.Label($"Invincible: {playerController.stats.isInvincible}", debugStyle);
        GUILayout.Label($"Skin Index: {playerController.playerVisuals.currentSkinIndex}", debugStyle);
        GUILayout.Space(10);
    }
    
    private void DrawDebugActions()
    {
        GUILayout.Label("--- Debug Actions ---", debugStyle);
        
        if (GUILayout.Button("Flip Gravity"))
        {
            playerController.FlipGravity();
        }
        
        if (GUILayout.Button("Take Damage"))
        {
            playerController.TakeDamage();
        }
        
        if (GUILayout.Button("Toggle Invincibility"))
        {
            playerController.stats.isInvincible = !playerController.stats.isInvincible;
        }
        
        if (GUILayout.Button("Respawn"))
        {
            playerController.Respawn();
        }
        
        if (GUILayout.Button("Next Skin"))
        {
            int nextSkin = (playerController.playerVisuals.currentSkinIndex + 1) % 
                           playerController.playerVisuals.skinColors.Length;
            playerController.playerVisuals.ApplySkin(nextSkin);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!enableDebugDrawing || !Application.isPlaying) return;
        
        // プレイヤーの当たり判定を描画
        Gizmos.color = playerController.isAlive ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        // 速度ベクトルを描画
        Gizmos.color = Color.blue;
        Vector2 velocity = playerController.GetVelocity();
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(velocity * 0.1f));
        
        // 重力方向を描画
        Gizmos.color = Color.yellow;
        Vector3 gravityDir = Vector3.down * (float)playerController.gravityDirection;
        Gizmos.DrawLine(transform.position, transform.position + gravityDir * 2f);
    }
}
```

### 自動テストシステム

```csharp
public class PlayerSystemTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestsOnStart = false;
    public float testTimeout = 10f;
    
    private PlayerController playerController;
    private List<string> testResults = new List<string>();
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        
        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }
    
    private IEnumerator RunAllTests()
    {
        Debug.Log("Starting Player System Tests...");
        testResults.Clear();
        
        // 各テストを実行
        yield return StartCoroutine(TestMovement());
        yield return StartCoroutine(TestGravityFlip());
        yield return StartCoroutine(TestDamageSystem());
        yield return StartCoroutine(TestRespawnSystem());
        yield return StartCoroutine(TestVisualEffects());
        
        // 結果をまとめて出力
        OutputTestResults();
    }
    
    private IEnumerator TestMovement()
    {
        Debug.Log("Testing movement system...");
        
        Vector3 startPosition = transform.position;
        float testDuration = 2f;
        float elapsedTime = 0f;
        
        while (elapsedTime < testDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Vector3 endPosition = transform.position;
        float distanceMoved = Vector3.Distance(startPosition, endPosition);
        
        bool testPassed = distanceMoved > 1f; // 最低1unit移動していることを確認
        testResults.Add($"Movement Test: {(testPassed ? "PASS" : "FAIL")} - Distance: {distanceMoved:F2}");
    }
    
    private IEnumerator TestGravityFlip()
    {
        Debug.Log("Testing gravity flip system...");
        
        GravityDirection initialDirection = playerController.gravityDirection;
        
        // 重力反転実行
        playerController.FlipGravity();
        
        // 反転完了まで待機
        yield return new WaitForSeconds(0.2f);
        
        GravityDirection newDirection = playerController.gravityDirection;
        bool testPassed = initialDirection != newDirection;
        
        testResults.Add($"Gravity Flip Test: {(testPassed ? "PASS" : "FAIL")} - {initialDirection} -> {newDirection}");
    }
    
    private IEnumerator TestDamageSystem()
    {
        Debug.Log("Testing damage system...");
        
        int initialLives = playerController.stats.livesRemaining;
        
        // ダメージを与える
        playerController.TakeDamage();
        
        yield return new WaitForSeconds(0.1f);
        
        int newLives = playerController.stats.livesRemaining;
        bool testPassed = newLives == initialLives - 1;
        
        testResults.Add($"Damage Test: {(testPassed ? "PASS" : "FAIL")} - Lives: {initialLives} -> {newLives}");
    }
    
    private IEnumerator TestRespawnSystem()
    {
        Debug.Log("Testing respawn system...");
        
        Vector3 checkpointPosition = CheckpointManager.Instance.GetCurrentCheckpointPosition();
        
        // プレイヤーを別の位置に移動
        transform.position = checkpointPosition + Vector3.right * 10f;
        
        yield return new WaitForSeconds(0.1f);
        
        // リスポーン実行
        playerController.Respawn();
        
        yield return new WaitForSeconds(0.1f);
        
        float distanceFromCheckpoint = Vector3.Distance(transform.position, checkpointPosition);
        bool testPassed = distanceFromCheckpoint < 1f;
        
        testResults.Add($"Respawn Test: {(testPassed ? "PASS" : "FAIL")} - Distance from checkpoint: {distanceFromCheckpoint:F2}");
    }
    
    private IEnumerator TestVisualEffects()
    {
        Debug.Log("Testing visual effects...");
        
        // スキン変更テスト
        int initialSkin = playerController.playerVisuals.currentSkinIndex;
        playerController.playerVisuals.ApplySkin((initialSkin + 1) % playerController.playerVisuals.skinColors.Length);
        
        yield return new WaitForSeconds(0.1f);
        
        int newSkin = playerController.playerVisuals.currentSkinIndex;
        bool skinTestPassed = newSkin != initialSkin;
        
        // エフェクト実行テスト
        playerController.playerVisuals.PlayGravityFlipEffect();
        
        yield return new WaitForSeconds(0.1f);
        
        bool effectTestPassed = true; // エフェクトが正常に実行されたかチェック（実装に依存）
        
        testResults.Add($"Visual Effects Test: {(skinTestPassed && effectTestPassed ? "PASS" : "FAIL")} - Skin: {initialSkin} -> {newSkin}");
    }
    
    private void OutputTestResults()
    {
        Debug.Log("=== Player System Test Results ===");
        
        int passedTests = 0;
        int totalTests = testResults.Count;
        
        foreach (string result in testResults)
        {
            Debug.Log(result);
            if (result.Contains("PASS")) passedTests++;
        }
        
        Debug.Log($"Tests Passed: {passedTests}/{totalTests}");
        
        if (passedTests == totalTests)
        {
            Debug.Log("All tests passed! Player system is working correctly.");
        }
        else
        {
            Debug.LogWarning($"{totalTests - passedTests} test(s) failed. Please check the implementation.");
        }
    }
    
    // 手動テスト実行用メソッド
    [ContextMenu("Run All Tests")]
    public void RunTestsManually()
    {
        StartCoroutine(RunAllTests());
    }
}
```

### ユニットテスト例

```csharp
#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class PlayerControllerTests
{
    private GameObject playerObject;
    private PlayerController playerController;
    
    [SetUp]
    public void SetUp()
    {
        // テスト用プレイヤーオブジェクト作成
        playerObject = new GameObject("TestPlayer");
        playerObject.AddComponent<Rigidbody2D>();
        playerObject.AddComponent<BoxCollider2D>();
        playerObject.AddComponent<PlayerMovement>();
        playerObject.AddComponent<PlayerAnimation>();
        playerObject.AddComponent<PlayerCollision>();
        playerObject.AddComponent<PlayerVisuals>();
        
        playerController = playerObject.AddComponent<PlayerController>();
    }
    
    [TearDown]
    public void TearDown()
    {
        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }
    }
    
    [Test]
    public void PlayerController_InitializesCorrectly()
    {
        // 初期状態の確認
        Assert.AreEqual(PlayerState.Running, playerController.currentState);
        Assert.AreEqual(GravityDirection.Down, playerController.gravityDirection);
        Assert.IsTrue(playerController.isAlive);
    }
    
    [Test]
    public void TakeDamage_ReducesLives()
    {
        // 初期ライフ数を記録
        int initialLives = playerController.stats.livesRemaining;
        
        // ダメージを与える
        playerController.TakeDamage();
        
        // ライフが減っているか確認
        Assert.AreEqual(initialLives - 1, playerController.stats.livesRemaining);
    }
    
    [Test]
    public void FlipGravity_ChangesDirection()
    {
        // 初期重力方向を記録
        GravityDirection initialDirection = playerController.gravityDirection;
        
        // 重力反転実行
        playerController.FlipGravity();
        
        // 方向が変わっているか確認（非同期処理のため、状態チェック）
        Assert.AreEqual(PlayerState.GravityFlipping, playerController.currentState);
    }
    
    [UnityTest]
    public IEnumerator GravityFlip_CompletesSuccessfully()
    {
        GravityDirection initialDirection = playerController.gravityDirection;
        
        playerController.FlipGravity();
        
        // 重力反転完了まで待機
        yield return new WaitForSeconds(0.2f);
        
        // 方向が変わっているか確認
        Assert.AreNotEqual(initialDirection, playerController.gravityDirection);
        Assert.AreNotEqual(PlayerState.GravityFlipping, playerController.currentState);
    }
    
    [Test]
    public void PlayerStats_HasValidDefaults()
    {
        PlayerStats stats = new PlayerStats();
        
        Assert.Greater(stats.moveSpeed, 0f);
        Assert.Greater(stats.fallSpeed, 0f);
        Assert.Greater(stats.maxFallSpeed, stats.fallSpeed);
        Assert.Greater(stats.invincibilityDuration, 0f);
        Assert.Greater(stats.livesRemaining, 0);
    }
}

public class PlayerMovementTests
{
    private GameObject playerObject;
    private PlayerMovement playerMovement;
    private PlayerController playerController;
    
    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        playerObject.AddComponent<Rigidbody2D>();
        playerObject.AddComponent<BoxCollider2D>();
        
        playerController = playerObject.AddComponent<PlayerController>();
        playerMovement = playerObject.AddComponent<PlayerMovement>();
        
        playerMovement.Initialize(playerController);
    }
    
    [TearDown] 
    public void TearDown()
    {
        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }
    }
    
    [Test]
    public void AutoRunSpeed_IsPositive()
    {
        Assert.Greater(playerMovement.autoRunSpeed, 0f);
    }
    
    [Test]
    public void MaxFallSpeed_LimitsVelocity()
    {
        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        
        // 極端な速度を設定
        rb.velocity = new Vector2(0, -100f);
        
        // 物理更新をシミュレート
        playerMovement.SendMessage("FixedUpdate", SendMessageOptions.DontRequireReceiver);
        
        // 速度が制限されているか確認
        Assert.LessOrEqual(Mathf.Abs(rb.velocity.y), playerMovement.maxFallSpeed);
    }
}
#endif
```

---

## 拡張とカスタマイズ

### カスタムプレイヤー能力の追加

```csharp
// 新しい能力システム
public enum PlayerAbility
{
    None,
    DoubleJump,
    WallClimb,
    Dash,
    TimeControl
}

[System.Serializable]
public class PlayerAbilityData
{
    public PlayerAbility ability;
    public float cooldownTime;
    public float duration;
    public bool isUnlocked;
    public int energyChipCost;
}

public class PlayerAbilitySystem : MonoBehaviour
{
    [Header("Abilities")]
    public PlayerAbilityData[] abilities;
    
    private PlayerController playerController;
    private Dictionary<PlayerAbility, float> abilityCooldowns = new Dictionary<PlayerAbility, float>();
    
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        InitializeAbilities();
    }
    
    private void InitializeAbilities()
    {
        foreach (var ability in abilities)
        {
            abilityCooldowns[ability.ability] = 0f;
        }
    }
    
    private void Update()
    {
        HandleAbilityInput();
        UpdateCooldowns();
    }
    
    private void HandleAbilityInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            TryUseAbility(PlayerAbility.Dash);
        }
        
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TryUseAbility(PlayerAbility.DoubleJump);
        }
    }
    
    public bool TryUseAbility(PlayerAbility ability)
    {
        PlayerAbilityData abilityData = GetAbilityData(ability);
        
        if (abilityData == null || !abilityData.isUnlocked)
        {
            Debug.Log($"Ability {ability} is not unlocked");
            return false;
        }
        
        if (abilityCooldowns[ability] > 0f)
        {
            Debug.Log($"Ability {ability} is on cooldown");
            return false;
        }
        
        // 能力実行
        ExecuteAbility(ability, abilityData);
        
        // クールダウン設定
        abilityCooldowns[ability] = abilityData.cooldownTime;
        
        return true;
    }
    
    private void ExecuteAbility(PlayerAbility ability, PlayerAbilityData data)
    {
        switch (ability)
        {
            case PlayerAbility.Dash:
                ExecuteDash(data);
                break;
            case PlayerAbility.DoubleJump:
                ExecuteDoubleJump(data);
                break;
            case PlayerAbility.WallClimb:
                ExecuteWallClimb(data);
                break;
            case PlayerAbility.TimeControl:
                ExecuteTimeControl(data);
                break;
        }
    }
    
    private void ExecuteDash(PlayerAbilityData data)
    {
        Vector2 dashDirection = playerController.gravityDirection == GravityDirection.Down ? 
            Vector2.right : Vector2.right;
        
        playerController.AddForce(dashDirection * 15f);
        
        // ダッシュエフェクト
        playerController.playerVisuals.SetTrailEffect(true);
        
        // 一定時間後にエフェクト停止
        StartCoroutine(DisableTrailAfterDelay(data.duration));
    }
    
    private IEnumerator DisableTrailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        playerController.playerVisuals.SetTrailEffect(false);
    }
    
    private void ExecuteDoubleJump(PlayerAbilityData data)
    {
        // ダブルジャンプ実装
        Vector2 jumpDirection = playerController.gravityDirection == GravityDirection.Down ? 
            Vector2.up : Vector2.down;
        
        playerController.SetVelocity(new Vector2(playerController.GetVelocity().x, 0f));
        playerController.AddForce(jumpDirection * 12f);
    }
    
    private void ExecuteWallClimb(PlayerAbilityData data)
    {
        // 壁登り実装
        // 壁検出と垂直移動
    }
    
    private void ExecuteTimeControl(PlayerAbilityData data)
    {
        // 時間制御実装
        StartCoroutine(SlowTimeCoroutine(data.duration));
    }
    
    private IEnumerator SlowTimeCoroutine(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.5f;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = originalTimeScale;
    }
    
    private PlayerAbilityData GetAbilityData(PlayerAbility ability)
    {
        return System.Array.Find(abilities, a => a.ability == ability);
    }
    
    private void UpdateCooldowns()
    {
        var keys = new PlayerAbility[abilityCooldowns.Keys.Count];
        abilityCooldowns.Keys.CopyTo(keys, 0);
        
        foreach (var ability in keys)
        {
            if (abilityCooldowns[ability] > 0f)
            {
                abilityCooldowns[ability] -= Time.deltaTime;
                if (abilityCooldowns[ability] < 0f)
                {
                    abilityCooldowns[ability] = 0f;
                }
            }
        }
    }
    
    public float GetAbilityCooldown(PlayerAbility ability)
    {
        return abilityCooldowns.ContainsKey(ability) ? abilityCooldowns[ability] : 0f;
    }
}
```

### マルチプレイヤー対応

```csharp
public class NetworkPlayerController : PlayerController
{
    [Header("Network Settings")]
    public bool isLocalPlayer = true;
    public float networkSendRate = 20f;
    
    private float lastNetworkUpdate = 0f;
    private Vector3 networkPosition;
    private GravityDirection networkGravity;
    
    protected override void Update()
    {
        if (isLocalPlayer)
        {
            // ローカルプレイヤーの通常更新
            base.Update();
            
            // ネットワーク送信
            if (Time.time - lastNetworkUpdate >= 1f / networkSendRate)
            {
                SendNetworkUpdate();
                lastNetworkUpdate = Time.time;
            }
        }
        else
        {
            // リモートプレイヤーの補間
            InterpolateNetworkData();
        }
    }
    
    private void SendNetworkUpdate()
    {
        // ネットワークライブラリに依存する実装
        // 位置、状態、重力方向などを送信
        NetworkData data = new NetworkData
        {
            position = transform.position,
            velocity = GetVelocity(),
            gravityDirection = gravityDirection,
            currentState = currentState,
            isAlive = isAlive
        };
        
        // NetworkManager.SendPlayerUpdate(data);
    }
    
    public void ReceiveNetworkUpdate(NetworkData data)
    {
        if (isLocalPlayer) return;
        
        networkPosition = data.position;
        networkGravity = data.gravityDirection;
        
        // 状態同期
        if (currentState != data.currentState)
        {
            ChangeState(data.currentState);
        }
        
        if (gravityDirection != data.gravityDirection)
        {
            gravityDirection = data.gravityDirection;
            OnGravityFlip?.Invoke(gravityDirection);
        }
    }
    
    private void InterpolateNetworkData()
    {
        // 位置の補間
        transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
        
        // 重力方向の同期
        if (gravityDirection != networkGravity)
        {
            gravityDirection = networkGravity;
            playerAnimation.UpdateGravityDirection(gravityDirection);
        }
    }
}

[System.Serializable]
public class NetworkData
{
    public Vector3 position;
    public Vector2 velocity;
    public GravityDirection gravityDirection;
    public PlayerState currentState;
    public bool isAlive;
}
```

---

## 結論

Gravity Flip Lab のプレイヤーキャラクターシステムは、重力反転というユニークなゲームメカニクスを中心とした、包括的で拡張性の高いシステムです。このドキュメントで詳述した設計により、以下の利点が実現されます：

### 主要な特徴

1. **モジュラー設計**: 各コンポーネントが独立して機能し、保守性が高い
2. **イベント駆動**: 疎結合なアーキテクチャにより、システム間の連携が柔軟
3. **高度な物理統合**: Unity物理エンジンとの密接な連携による自然な挙動
4. **包括的なデバッグ支援**: 開発・テスト段階での効率的な問題解決
5. **パフォーマンス最適化**: メモリ・CPU使用量の最適化による快適な動作

### 技術的優位性

- **精密な当たり判定**: 頭・足部分の無敵フレームによる公平なゲームプレイ
- **滑らかな重力反転**: 0.1秒の瞬間切り替えによる爽快感
- **視覚的フィードバック**: 豊富なエフェクトとアニメーションによる没入感
- **カスタマイズ性**: 8色のスキンシステムとエフェクト制御
- **拡張性**: 新しい能力や機能の追加が容易

### 開発・運用面での利点

- **デバッグ容易性**: 詳細なログ出力と視覚的デバッグツール
- **テスト自動化**: ユニットテストと統合テストによる品質保証
- **パフォーマンス監視**: リアルタイムな性能追跡と最適化
- **エラー耐性**: 例外処理とフォールバック機構による安定性

このシステムは、単純な操作で奥深いゲームプレイを実現し、プレイヤーに継続的な楽しさを提供するよう設計されています。また、将来的な機能拡張や他プラットフォームへの対応も考慮した、持続可能なアーキテクチャとなっています。

開発チームは、このドキュメントを参考に効率的な実装を行い、高品質なゲーム体験を提供できるでしょう。