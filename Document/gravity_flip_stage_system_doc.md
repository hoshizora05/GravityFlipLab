# Gravity Flip Lab - ステージ管理・ギミックシステム詳細ドキュメント

## 目次

1. [システム概要](#システム概要)
2. [アーキテクチャ設計](#アーキテクチャ設計)
3. [データ構造](#データ構造)
4. [コアシステム詳細](#コアシステム詳細)
5. [ギミックシステム](#ギミックシステム)
6. [パラレックス背景システム](#パラレックス背景システム)
7. [カメラシステム](#カメラシステム)
8. [ステージストリーミング](#ステージストリーミング)
9. [パフォーマンス最適化](#パフォーマンス最適化)
10. [実装ガイド](#実装ガイド)
11. [ベストプラクティス](#ベストプラクティス)
12. [トラブルシューティング](#トラブルシューティング)

---

## システム概要

Gravity Flip Lab のステージ管理・ギミックシステムは、プロシージャル生成された4096×1024ピクセルの大規模ステージを効率的に管理し、多様なギミックとインタラクティブ要素を統合したシステムです。このシステムは、パラレックス背景、動的ストリーミング、そして最適化されたパフォーマンス管理を通じて、滑らかで没入感のあるゲーム体験を提供します。

### 主要特徴

- **大規模ステージ管理**: 4096×1024ピクセルのマクロステージ
- **セグメント分割**: 256ピクセル単位の16セグメント構成
- **動的ストリーミング**: プレイヤー位置に基づく効率的なロード/アンロード
- **パラレックス背景**: 3層深度による視覚的奥行き表現
- **多様なギミック**: 8種類の障害物と4種類の環境要素
- **カメラシステム**: 先行追従とスムーズダンピング機能
- **パフォーマンス最適化**: カリング、プーリング、効果管理

### ステージ仕様

- **ステージサイズ**: 4096×1024ピクセル（約64×16 Unity units）
- **セグメント数**: 16セグメント（各256ピクセル幅）
- **最大アクティブセグメント**: 5セグメント同時
- **パラレックス層数**: 3層（遠景・中景・近景）
- **チェックポイント数**: ステージあたり最大10箇所
- **収集アイテム数**: ステージあたり3個のエナジーチップ

---

## アーキテクチャ設計

### システム構成図

```
┌─────────────────────────────────────────────────────────┐
│                  StageManager                           │
│                (中央制御システム)                        │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────┐ │
│ │ ParallaxBGMgr   │ │ CameraController│ │StageStreaming│ │
│ │ (背景管理)      │ │ (カメラ制御)    │ │(動的ロード)  │ │
│ └─────────────────┘ └─────────────────┘ └─────────────┘ │
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────┐ │
│ │ BaseObstacle    │ │ Collectible     │ │Environmental│ │
│ │ (ギミック基底)  │ │ (収集アイテム)  │ │(環境オブジェクト)│ │
│ └─────────────────┘ └─────────────────┘ └─────────────┘ │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────┐ │
│ │ StageOptimizer  │ │CheckpointManager│ │StageDataSO  │ │
│ │ (最適化)        │ │(チェックポイント)│ │(データ)     │ │
│ └─────────────────┘ └─────────────────┘ └─────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 設計原則

1. **Data-Driven Design**: ScriptableObjectベースの設定駆動アーキテクチャ
2. **Modular Component System**: 独立したコンポーネント設計
3. **Dynamic Loading**: 効率的なメモリ使用とパフォーマンス
4. **Event-Driven Communication**: 疎結合なシステム間通信
5. **Scalable Architecture**: 拡張性を考慮した設計

### 依存関係図

```
StageManager (Core)
    ├── StageDataSO (Data)
    │   ├── StageInfo (Basic Info)
    │   ├── BackgroundLayerData[] (Parallax)
    │   ├── ObstacleData[] (Gimmicks)
    │   ├── CollectibleData[] (Items)
    │   └── EnvironmentalData[] (Environment)
    ├── ParallaxBackgroundManager (Visual)
    ├── CameraController (Camera)
    ├── StageStreaming (Performance)
    ├── StageOptimizer (Performance)
    └── CheckpointManager (Gameplay)
```

---

## データ構造

### StageInfo（ステージ基本情報）

ステージの基本的な設定と構造を定義します。

```csharp
[System.Serializable]
public class StageInfo
{
    [Header("Basic Information")]
    public int worldNumber;                    // ワールド番号（1-5）
    public int stageNumber;                    // ステージ番号（1-10）
    public string stageName;                   // ステージ名称
    public float timeLimit = 300f;             // 制限時間（5分）
    public int energyChipCount = 3;            // エナジーチップ数
    
    [Header("Positioning")]
    public Vector3 playerStartPosition;        // プレイヤー開始位置
    public Vector3 goalPosition;               // ゴール位置
    public List<Vector3> checkpointPositions;  // チェックポイント位置リスト
    
    [Header("Visual Theme")]
    public StageTheme theme = StageTheme.Tech; // ステージテーマ
    
    [Header("Stage Layout")]
    public float stageLength = 4096f;          // ステージ幅（ピクセル）
    public float stageHeight = 1024f;          // ステージ高（ピクセル）
    public int segmentCount = 16;              // セグメント数
}
```

**フィールド詳細**:
- `worldNumber`: 所属ワールド（1=Tech、2=Industrial、3=Organic、4=Crystal、5=Void）
- `stageNumber`: ワールド内でのステージ番号
- `timeLimit`: ステージクリア制限時間（秒）
- `energyChipCount`: ステージ内に配置されるエナジーチップの総数
- `playerStartPosition`: プレイヤーのスポーン位置
- `goalPosition`: ステージのゴール地点
- `checkpointPositions`: 中間リスポーン地点のリスト
- `theme`: ステージの視覚テーマ（背景、色調等に影響）
- `stageLength`: ステージの水平方向の長さ
- `stageHeight`: ステージの垂直方向の高さ
- `segmentCount`: ステージを分割するセグメント数

### BackgroundLayerData（背景層データ）

パラレックス背景の各層を定義します。

```csharp
[System.Serializable]
public class BackgroundLayerData
{
    [Header("Layer Configuration")]
    public string layerName;                   // 層の名前
    public Sprite backgroundSprite;            // 背景スプライト
    public float parallaxFactor = 0.5f;        // パラレックス係数
    public Vector2 tileSize = new Vector2(512, 512); // タイルサイズ
    
    [Header("Rendering Options")]
    public bool enableVerticalLoop = false;    // 垂直ループ有効化
    public Color tintColor = Color.white;      // 色調補正
}
```

**パラレックス係数の目安**:
- `0.25f`: 遠景層（最も遅い移動）
- `0.5f`: 中景層（中程度の移動）
- `0.75f`: 近景層（最も速い移動）

### ObstacleData（障害物データ）

ステージ内の障害物の配置と設定を定義します。

```csharp
[System.Serializable]
public class ObstacleData
{
    [Header("Obstacle Configuration")]
    public ObstacleType type;                  // 障害物の種類
    public Vector3 position;                   // 配置位置
    public Vector3 rotation;                   // 回転角度
    public Vector3 scale = Vector3.one;        // スケール
    public Dictionary<string, object> parameters; // カスタムパラメータ
}

public enum ObstacleType
{
    Spike,          // スパイク（静的トラップ）
    ElectricFence,  // 電撃フェンス（周期的ダメージ）
    PistonCrusher,  // ピストンクラッシャー（可動式）
    RotatingSaw,    // 回転ノコギリ（動的障害物）
    HoverDrone,     // ホバードローン（AI敵）
    TimerGate,      // タイマーゲート（時限開閉）
    PhaseBlock,     // フェーズブロック（透明化）
    PressureSwitch  // 圧力スイッチ（トリガー式）
}
```

### CollectibleData（収集アイテムデータ）

ステージ内の収集可能なアイテムを定義します。

```csharp
[System.Serializable]
public class CollectibleData
{
    public CollectibleType type;               // アイテムの種類
    public Vector3 position;                   // 配置位置
    public int value = 1;                      // アイテムの価値
}

public enum CollectibleType
{
    EnergyChip,     // エナジーチップ（主要収集品）
    PowerUp,        // パワーアップアイテム
    ExtraLife       // 追加ライフ
}
```

### EnvironmentalData（環境要素データ）

ステージの環境的な要素を定義します。

```csharp
[System.Serializable]
public class EnvironmentalData
{
    public EnvironmentalType type;             // 環境要素の種類
    public Vector3 position;                   // 配置位置
    public Vector3 scale = Vector3.one;        // スケール
    public Dictionary<string, object> parameters; // カスタムパラメータ
}

public enum EnvironmentalType
{
    GravityWell,    // 重力井戸（重力場変更）
    WindTunnel,     // 風のトンネル（推進力）
    SpringPlatform, // スプリングプラットフォーム（跳躍補助）
    MovingPlatform  // 可動プラットフォーム（移動足場）
}
```

---

## コアシステム詳細

### StageManager（ステージ管理中枢）

ステージ全体の読み込み、初期化、管理を統括するシングルトンシステムです。

#### 主要責任

1. **ステージデータ管理**: ScriptableObjectからのデータ読み込み
2. **オブジェクト生成**: 障害物、収集品、環境要素の動的生成
3. **コンポーネント統合**: 各サブシステムとの連携
4. **ライフサイクル管理**: ステージの開始・終了処理
5. **イベント配信**: ステージ状態変更の通知

#### ステージロードプロセス

```csharp
private IEnumerator LoadStageCoroutine()
{
    // 1. 既存ステージクリア
    ClearStage();
    yield return new WaitForEndOfFrame();
    
    // 2. 背景システム設定
    yield return StartCoroutine(SetupBackground());
    
    // 3. カメラ境界設定
    SetupCamera();
    
    // 4. 障害物配置
    yield return StartCoroutine(LoadObstacles());
    
    // 5. 収集品配置
    yield return StartCoroutine(LoadCollectibles());
    
    // 6. 環境要素配置
    yield return StartCoroutine(LoadEnvironmental());
    
    // 7. ステージ初期化
    InitializeStage();
    
    // 8. 完了通知
    stageLoaded = true;
    OnStageLoaded?.Invoke();
}
```

#### パフォーマンス考慮

- **フレーム分散**: `yield return null`による処理分散
- **プログレッシブロード**: 段階的な要素配置
- **メモリ効率**: 不要オブジェクトの即座な破棄
- **イベント通知**: 他システムへの適切なタイミング通知

#### カメラ境界自動設定

```csharp
private void SetupCamera()
{
    if (cameraController != null && currentStageData != null)
    {
        cameraController.SetBoundaries(
            0f,                                              // 左境界
            currentStageData.stageInfo.stageLength,          // 右境界
            currentStageData.stageInfo.stageHeight * 0.5f,   // 上境界
            -currentStageData.stageInfo.stageHeight * 0.5f   // 下境界
        );
    }
}
```

### StageDataSO（ステージデータアセット）

ScriptableObjectベースのデータ駆動アーキテクチャを実現します。

#### データ階層構造

```
StageDataSO
├── StageInfo (基本情報)
├── BackgroundLayerData[3] (パラレックス背景)
├── ObstacleData[] (障害物配置)
├── CollectibleData[] (収集品配置)
└── EnvironmentalData[] (環境要素配置)
```

#### アセット作成方法

```csharp
[CreateAssetMenu(fileName = "StageData", menuName = "Gravity Flip Lab/Stage Data")]
public class StageDataSO : ScriptableObject
{
    // Unity エディタ上で Assets > Create > Gravity Flip Lab > Stage Data から作成
}
```

#### 使用例

```csharp
// リソースからの動的ロード
string resourcePath = $"StageData/World{world}/Stage{world}-{stage}";
StageDataSO stageData = Resources.Load<StageDataSO>(resourcePath);

// 直接参照によるロード
public StageDataSO predefinedStageData;
StageManager.Instance.LoadStage(predefinedStageData);
```

---

## ギミックシステム

### BaseObstacle（ギミック基底クラス）

全ての障害物・ギミックの共通インターフェースと機能を提供します。

#### 基本構造

```csharp
public abstract class BaseObstacle : MonoBehaviour
{
    [Header("Base Settings")]
    public ObstacleType obstacleType;      // 障害物タイプ
    public bool isActive = true;           // アクティブ状態
    public float damageAmount = 1f;        // ダメージ量
    public LayerMask targetLayers = 1;     // 対象レイヤー
    
    [Header("Effects")]
    public ParticleSystem activationEffect; // 起動エフェクト
    public ParticleSystem damageEffect;     // ダメージエフェクト
    public AudioClip activationSound;      // 起動音
    public AudioClip damageSound;          // ダメージ音
    
    // イベントシステム
    public System.Action<BaseObstacle> OnObstacleTriggered;
    public System.Action<BaseObstacle, GameObject> OnTargetDamaged;
}
```

#### ライフサイクル管理

```csharp
// 初期化フロー
public virtual void Initialize(ObstacleData data)
{
    obstacleData = data;
    obstacleType = data.type;
    ApplyParameters(data.parameters);  // カスタムパラメータ適用
    initialized = true;
}

// 起動フロー
public virtual void StartObstacle()
{
    if (!initialized) return;
    isActive = true;
    OnObstacleStart(); // 派生クラス固有の処理
}

// 停止フロー
public virtual void StopObstacle()
{
    isActive = false;
    OnObstacleStop(); // 派生クラス固有の処理
}
```

### 静的障害物

#### SpikeObstacle（スパイクトラップ）

固定または伸縮式のスパイクトラップです。

```csharp
public class SpikeObstacle : BaseObstacle
{
    [Header("Spike Configuration")]
    public bool pointsUp = true;           // 上向きスパイク
    public float spikeHeight = 1f;         // スパイク高さ
    public bool retractable = false;       // 伸縮機能
    public float retractDelay = 2f;        // 伸縮間隔
    
    private bool isExtended = true;        // 現在の状態
    private Coroutine retractCoroutine;    // 伸縮制御
}
```

**動作パターン**:
- **固定モード**: 常時展開されたスパイク
- **伸縮モード**: 一定間隔での伸縮動作
- **トリガーモード**: プレイヤー接近時の展開

**伸縮アニメーション**:
```csharp
private IEnumerator AnimateSpikes()
{
    float startScale = transform.localScale.y;
    float targetScale = isExtended ? 1f : 0.1f;
    float duration = 0.3f;
    
    for (float elapsedTime = 0f; elapsedTime < duration; elapsedTime += Time.deltaTime)
    {
        float t = elapsedTime / duration;
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Lerp(startScale, targetScale, t);
        transform.localScale = scale;
        yield return null;
    }
}
```

#### ElectricFence（電撃フェンス）

周期的に電流が流れる障害物です。

```csharp
public class ElectricFence : BaseObstacle
{
    [Header("Electric Configuration")]
    public float pulseInterval = 1f;      // パルス間隔
    public float pulseDuration = 0.5f;    // パルス持続時間
    public bool alwaysActive = false;     // 常時通電モード
    
    private bool isPulsing = false;       // 現在のパルス状態
}
```

**電撃パターン**:
- **間欠モード**: 一定間隔での通電・非通電
- **常時モード**: 継続的な通電状態
- **トリガーモード**: 外部信号による制御

### 可動障害物

#### PistonCrusher（ピストンクラッシャー）

垂直方向に動作する圧縮装置です。

```csharp
public class PistonCrusher : BaseObstacle
{
    [Header("Piston Configuration")]
    public float crushDistance = 3f;      // 圧縮距離
    public float crushSpeed = 5f;         // 動作速度
    public float waitTime = 2f;           // 待機時間
    public Transform pistonHead;          // ピストンヘッド
    
    private Vector3 startPosition;        // 初期位置
    private Vector3 crushPosition;        // 圧縮位置
}
```

**動作サイクル**:
1. **待機フェーズ**: 初期位置で待機
2. **圧縮フェーズ**: 下方向への高速移動
3. **停止フェーズ**: 圧縮位置で短時間停止
4. **復帰フェーズ**: 初期位置への復帰

#### RotatingSaw（回転ノコギリ）

回転しながら移動する動的障害物です。

```csharp
public class RotatingSaw : BaseObstacle
{
    [Header("Saw Configuration")]
    public float rotationSpeed = 360f;    // 回転速度（度/秒）
    public bool moveInPath = false;       // パス移動有効
    public Transform[] pathPoints;        // 移動パス
    public float moveSpeed = 2f;          // 移動速度
}
```

**移動パターン**:
- **固定回転**: その場での回転のみ
- **直線移動**: 指定方向への移動+回転
- **パス移動**: 複数ポイント間の循環移動

### AI駆動ギミック

#### HoverDrone（ホバードローン）

AI制御による敵対的障害物です。

```csharp
public class HoverDrone : BaseObstacle
{
    [Header("AI Configuration")]
    public float patrolSpeed = 2f;        // 巡回速度
    public float detectionRange = 5f;     // 探知範囲
    public float beamChargeTime = 1f;     // ビーム充電時間
    public float beamDuration = 2f;       // ビーム持続時間
    public Transform[] patrolPoints;      // 巡回ポイント
    
    [Header("Behavior")]
    public bool followPlayer = false;     // プレイヤー追跡モード
    public float attackCooldown = 3f;     // 攻撃間隔
}
```

**AI行動パターン**:

1. **巡回モード**:
```csharp
private IEnumerator PatrolBehavior()
{
    Vector3 targetPosition = patrolPoints[currentPatrolIndex].position;
    
    if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
    {
        transform.position = Vector3.MoveTowards(
            transform.position, targetPosition, patrolSpeed * Time.deltaTime);
    }
    else
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
}
```

2. **攻撃シーケンス**:
```csharp
private IEnumerator AttackSequence()
{
    isCharging = true;
    yield return new WaitForSeconds(beamChargeTime);  // チャージ
    
    isFiring = true;
    // ビーム発射とレイキャスト判定
    RaycastHit2D hit = Physics2D.Raycast(transform.position, 
        (targetPlayer.transform.position - transform.position).normalized, 
        detectionRange, targetLayers);
    
    if (hit.collider?.gameObject == targetPlayer)
        DealDamage(targetPlayer);
    
    yield return new WaitForSeconds(beamDuration);
    isFiring = false;
}
```

### インタラクティブ要素

#### TimerGate（タイマーゲート）

時間制限付きで開閉するゲートです。

```csharp
public class TimerGate : BaseObstacle
{
    [Header("Gate Configuration")]
    public float openDuration = 5f;       // 開放時間
    public bool startsOpen = false;       // 初期状態
    public Transform gateVisual;          // ゲート表示オブジェクト
    public Collider2D gateCollider;      // ゲート当たり判定
}
```

**動作フロー**:
1. **トリガー受信**: 外部からの開放指示
2. **ゲート開放**: 物理的通路の開通
3. **タイマー開始**: 指定時間のカウントダウン
4. **自動閉鎖**: タイマー終了時の閉鎖

#### PhaseBlock（フェーズブロック）

周期的に実体化・透明化するブロックです。

```csharp
public class PhaseBlock : MonoBehaviour
{
    [Header("Phase Configuration")]
    public float phaseInterval = 2f;      // フェーズ間隔
    public bool startsVisible = true;     // 初期表示状態
    public float transitionDuration = 0.5f; // 遷移時間
    
    private bool isVisible;               // 現在の表示状態
}
```

**フェーズサイクル**:
```csharp
private IEnumerator PhaseCycle()
{
    while (true)
    {
        yield return new WaitForSeconds(phaseInterval);
        isVisible = !isVisible;
        yield return StartCoroutine(TransitionPhase());
    }
}

private IEnumerator TransitionPhase()
{
    float targetAlpha = isVisible ? 1f : 0.3f;
    // アルファ値の滑らかな遷移
    // コライダーの有効/無効切り替え
}
```

---

## パラレックス背景システム

### ParallaxBackgroundManager（パラレックス管理）

多層背景による視覚的奥行き効果を実現します。

#### システム構成

```csharp
public class ParallaxBackgroundManager : MonoBehaviour
{
    [Header("Configuration")]
    public Transform cameraTransform;         // カメラ参照
    public ParallaxLayer[] parallaxLayers;    // パラレックス層配列
    public bool enableParallax = true;        // パラレックス有効化
    public float updateInterval = 0.016f;     // 更新間隔（60fps）
}
```

#### パラレックス層定義

```csharp
[System.Serializable]
public class ParallaxLayer
{
    public string layerName;                  // 層名称
    public SpriteRenderer spriteRenderer;     // スプライトレンダラー
    public float parallaxFactor = 0.5f;       // パラレックス係数
    public Vector2 textureSize = Vector2.one; // テクスチャサイズ
    public bool enableVerticalParallax = false; // 垂直パラレックス
    public bool enableLoop = true;            // ループ有効化
}
```

#### UVスクロール実装

```csharp
private void UpdateParallax()
{
    Vector3 cameraMovement = cameraTransform.position - lastCameraPosition;
    
    foreach (var layer in parallaxLayers)
    {
        Vector2 parallaxMovement = new Vector2(
            cameraMovement.x * layer.parallaxFactor,
            layer.enableVerticalParallax ? cameraMovement.y * layer.parallaxFactor : 0f
        );
        
        if (layer.enableLoop)
        {
            // UVオフセットによるシームレスループ
            Material material = layer.spriteRenderer.material;
            Vector2 newOffset = material.mainTextureOffset + 
                parallaxMovement / layer.textureSize;
            
            // UV座標の正規化（0-1範囲でラップ）
            newOffset.x = newOffset.x % 1f;
            newOffset.y = newOffset.y % 1f;
            material.mainTextureOffset = newOffset;
        }
    }
}
```

#### パフォーマンス最適化

- **更新頻度制御**: 60fps固定の更新間隔
- **変更差分計算**: カメラ移動量ベースの更新
- **条件分岐最適化**: 有効層のみの処理
- **UV正規化**: 効率的な座標ラップ処理

---

## カメラシステム

### CameraController（カメラ制御）

プレイヤー追従と先読み機能を持つカメラシステムです。

#### カメラ設定

```csharp
public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;              // 追従対象（プレイヤー）
    public float followSpeed = 2f;        // 追従速度
    public float lookAheadDistance = 3f;  // 先読み距離
    public Vector2 offset = Vector2.zero; // オフセット
    
    [Header("Boundaries")]
    public float leftBoundary = 0f;       // 左境界
    public float rightBoundary = 100f;    // 右境界
    public float topBoundary = 10f;       // 上境界
    public float bottomBoundary = -10f;   // 下境界
    
    [Header("Smooth Follow")]
    public bool enableSmoothFollow = true; // スムーズ追従
    public float horizontalDamping = 0.1f; // 水平ダンピング
    public float verticalDamping = 0.2f;   // 垂直ダンピング
}
```

#### 先読み追従アルゴリズム

```csharp
private void FollowTarget()
{
    Vector3 targetPosition = target.position + (Vector3)offset;
    
    // プレイヤーの移動速度に基づく先読み
    Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
    if (playerRb != null && playerRb.velocity.x > 0.1f)
    {
        targetPosition.x += lookAheadDistance;
    }
    
    Vector3 desiredPosition = new Vector3(
        targetPosition.x,
        targetPosition.y,
        transform.position.z
    );
    
    // 境界制限適用
    desiredPosition.x = Mathf.Clamp(desiredPosition.x, leftBoundary, rightBoundary);
    desiredPosition.y = Mathf.Clamp(desiredPosition.y, bottomBoundary, topBoundary);
    
    // スムーズダンピング適用
    if (enableSmoothFollow)
    {
        Vector3 smoothPosition = new Vector3(
            Mathf.SmoothDamp(transform.position.x, desiredPosition.x, 
                ref velocity.x, horizontalDamping),
            Mathf.SmoothDamp(transform.position.y, desiredPosition.y, 
                ref velocity.y, verticalDamping),
            transform.position.z
        );
        transform.position = smoothPosition;
    }
}
```

#### 動的境界設定

```csharp
public void SetBoundaries(float left, float right, float top, float bottom)
{
    leftBoundary = left;
    rightBoundary = right;
    topBoundary = top;
    bottomBoundary = bottom;
}

// ステージサイズに基づく自動設定
private void SetupCamera()
{
    if (cameraController != null && currentStageData != null)
    {
        float stageWidth = currentStageData.stageInfo.stageLength;
        float stageHeight = currentStageData.stageInfo.stageHeight;
        
        cameraController.SetBoundaries(
            0f,                    // 左境界（ステージ開始点）
            stageWidth,            // 右境界（ステージ終了点）
            stageHeight * 0.5f,    // 上境界（ステージ上端）
            -stageHeight * 0.5f    // 下境界（ステージ下端）
        );
    }
}
```

---

## ステージストリーミング

### StageStreaming（動的ロードシステム）

大規模ステージの効率的なメモリ管理を実現します。

#### セグメント管理

```csharp
public class StageStreaming : MonoBehaviour
{
    [Header("Streaming Configuration")]
    public float segmentWidth = 1024f;        // セグメント幅
    public int maxActiveSegments = 5;         // 最大アクティブセグメント数
    public float cullingDistance = 2048f;     // カリング距離
    
    private Queue<GameObject> segmentPool;    // セグメントプール
    private List<GameObject> activeSegments;  // アクティブセグメント
    private Dictionary<float, GameObject> segmentsByPosition; // 位置別セグメント
}
```

#### 動的ロード/アンロード

```csharp
private void UpdateSegmentStreaming()
{
    float playerX = playerTransform.position.x;
    
    // 必要セグメント位置の計算
    List<float> requiredPositions = new List<float>();
    for (int i = -1; i <= maxActiveSegments - 2; i++)
    {
        float segmentPosition = Mathf.Floor(playerX / segmentWidth) * segmentWidth + 
                               (i * segmentWidth);
        requiredPositions.Add(segmentPosition);
    }

    // セグメントの動的ロード
    foreach (float position in requiredPositions)
    {
        if (!segmentsByPosition.ContainsKey(position))
        {
            LoadSegmentAtPosition(position);
        }
    }

    // 遠距離セグメントのアンロード
    List<float> positionsToRemove = new List<float>();
    foreach (var kvp in segmentsByPosition)
    {
        float distance = Mathf.Abs(kvp.Key - playerX);
        if (distance > cullingDistance)
        {
            positionsToRemove.Add(kvp.Key);
        }
    }

    foreach (float position in positionsToRemove)
    {
        UnloadSegmentAtPosition(position);
    }
}
```

#### オブジェクトプール実装

```csharp
private GameObject GetSegmentFromPool()
{
    if (segmentPool.Count > 0)
    {
        return segmentPool.Dequeue();
    }
    else
    {
        // 新規セグメント作成
        GameObject newSegment = CreateNewSegment();
        return newSegment;
    }
}

private void ReturnSegmentToPool(GameObject segment)
{
    // セグメント内オブジェクトの無効化
    ClearSegmentObjects(segment);
    
    // プールに返却
    segment.SetActive(false);
    segmentPool.Enqueue(segment);
}
```

#### メモリ使用量の最適化

- **セグメント単位管理**: 1024ピクセル単位での効率的分割
- **距離ベースカリング**: プレイヤーから遠いセグメントの自動除去
- **オブジェクトプール**: メモリ断片化の防止
- **遅延破棄**: フレーム分散による負荷軽減

---

## パフォーマンス最適化

### StageOptimizer（最適化管理）

ステージ全体のパフォーマンス監視と最適化を行います。

#### 最適化設定

```csharp
public class StageOptimizer : MonoBehaviour
{
    [Header("Optimization Settings")]
    public bool enableObjectPooling = true;   // オブジェクトプール有効化
    public bool enableCulling = true;         // カリング有効化
    public float cullingDistance = 30f;       // カリング距離
    public int maxActiveEffects = 10;         // 最大アクティブエフェクト数
    
    private Camera mainCamera;                // メインカメラ参照
    private List<ParticleSystem> activeEffects; // アクティブエフェクトリスト
    private Queue<ParticleSystem> effectPool;   // エフェクトプール
}
```

#### 視野外カリング

```csharp
private void PerformCulling()
{
    Vector3 cameraPosition = mainCamera.transform.position;
    
    // 障害物のカリング処理
    BaseObstacle[] obstacles = FindObjectsOfType<BaseObstacle>();
    foreach (var obstacle in obstacles)
    {
        float distance = Vector3.Distance(obstacle.transform.position, cameraPosition);
        bool shouldBeActive = distance <= cullingDistance;
        
        // 状態変更が必要な場合のみ更新
        if (obstacle.gameObject.activeSelf != shouldBeActive)
        {
            obstacle.gameObject.SetActive(shouldBeActive);
        }
    }
    
    // 収集品のカリング処理
    Collectible[] collectibles = FindObjectsOfType<Collectible>();
    foreach (var collectible in collectibles)
    {
        float distance = Vector3.Distance(collectible.transform.position, cameraPosition);
        bool shouldBeActive = distance <= cullingDistance;
        
        if (collectible.gameObject.activeSelf != shouldBeActive)
        {
            collectible.gameObject.SetActive(shouldBeActive);
        }
    }
}
```

#### エフェクトプール管理

```csharp
private void ManageEffectPool()
{
    // アクティブエフェクト数の制限
    while (activeEffects.Count > maxActiveEffects)
    {
        ParticleSystem oldestEffect = activeEffects[0];
        activeEffects.RemoveAt(0);
        
        if (oldestEffect != null)
        {
            oldestEffect.Stop();
            ReturnEffectToPool(oldestEffect);
        }
    }
    
    // 完了したエフェクトの自動回収
    for (int i = activeEffects.Count - 1; i >= 0; i--)
    {
        ParticleSystem effect = activeEffects[i];
        if (effect != null && !effect.isPlaying)
        {
            activeEffects.RemoveAt(i);
            ReturnEffectToPool(effect);
        }
    }
}

public ParticleSystem GetPooledEffect()
{
    if (effectPool.Count > 0)
    {
        ParticleSystem effect = effectPool.Dequeue();
        activeEffects.Add(effect);
        return effect;
    }
    return null; // プールが空の場合
}
```

#### メモリ監視システム

```csharp
public class MemoryMonitor : MonoBehaviour
{
    [Header("Memory Monitoring")]
    public float monitorInterval = 5f;        // 監視間隔
    public long memoryThreshold = 100 * 1024 * 1024; // 100MB閾値
    
    private void Start()
    {
        InvokeRepeating(nameof(CheckMemoryUsage), 0f, monitorInterval);
    }
    
    private void CheckMemoryUsage()
    {
        long currentMemory = System.GC.GetTotalMemory(false);
        
        if (currentMemory > memoryThreshold)
        {
            Debug.LogWarning($"High memory usage detected: {currentMemory / 1024 / 1024}MB");
            PerformMemoryCleanup();
        }
        
        // 詳細情報をログ出力
        LogMemoryDetails();
    }
    
    private void PerformMemoryCleanup()
    {
        // ガベージコレクション強制実行
        System.GC.Collect();
        
        // 未使用アセットのアンロード
        Resources.UnloadUnusedAssets();
        
        // エフェクトプールのクリーンアップ
        StageOptimizer optimizer = FindObjectOfType<StageOptimizer>();
        optimizer?.CleanupEffectPool();
    }
}
```

---

## 収集品システム

### Collectible（収集アイテム）

ステージ内の収集可能なアイテムを管理します。

#### アイテム基本構成

```csharp
public class Collectible : MonoBehaviour
{
    [Header("Collectible Configuration")]
    public CollectibleType collectibleType = CollectibleType.EnergyChip;
    public int value = 1;                 // アイテムの価値
    public float rotationSpeed = 90f;     // 回転速度
    public float bobHeight = 0.5f;        // 浮遊高さ
    public float bobSpeed = 2f;           // 浮遊速度
    
    [Header("Visual Effects")]
    public ParticleSystem collectEffect;  // 収集エフェクト
    public AudioClip collectSound;        // 収集音
    
    private Vector3 startPosition;        // 初期位置
    private bool isCollected = false;     // 収集済みフラグ
}
```

#### アニメーション実装

```csharp
private void Update()
{
    if (isCollected) return;

    // Y軸回転アニメーション
    transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    
    // 浮遊アニメーション（サイン波）
    float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    Vector3 newPosition = startPosition;
    newPosition.y += bobOffset;
    transform.position = newPosition;
}
```

#### 収集処理

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (isCollected || !other.CompareTag("Player")) return;
    
    Collect();
}

private void Collect()
{
    isCollected = true;
    
    // エフェクト再生
    if (collectEffect != null)
        collectEffect.Play();
    
    // 音響効果
    if (collectSound != null)
        AudioManager.Instance?.PlaySE(collectSound);
    
    // ゲーム状態更新
    UpdateGameState();
    
    // 視覚的フィードバック
    StartCoroutine(CollectionSequence());
}

private IEnumerator CollectionSequence()
{
    // スケールアニメーション
    Vector3 originalScale = transform.localScale;
    float animationTime = 0.5f;
    
    for (float t = 0; t < animationTime; t += Time.deltaTime)
    {
        float scale = Mathf.Lerp(1f, 1.5f, t / animationTime);
        transform.localScale = originalScale * scale;
        
        // 透明度変化
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = Mathf.Lerp(1f, 0f, t / animationTime);
            sr.color = color;
        }
        
        yield return null;
    }
    
    // オブジェクト非表示
    gameObject.SetActive(false);
    
    // StageManagerに通知
    StageManager.Instance.CollectibleCollected(this);
    
    // 遅延破棄
    Destroy(gameObject, 1f);
}
```

### チェックポイントシステム

#### CheckpointTrigger（チェックポイントトリガー）

プレイヤーのリスポーン地点を管理します。

```csharp
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Configuration")]
    public Vector3 checkpointPosition;    // チェックポイント位置
    public bool saveOnTrigger = true;     // トリガー時自動保存
    public ParticleSystem activationEffect; // 起動エフェクト
    public AudioClip activationSound;     // 起動音
    
    private bool triggered = false;       // 起動済みフラグ
}
```

#### チェックポイント起動

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (triggered || !other.CompareTag("Player")) return;
    
    ActivateCheckpoint();
}

private void ActivateCheckpoint()
{
    triggered = true;
    
    // チェックポイント登録
    CheckpointManager.Instance.SetCheckpoint(checkpointPosition);
    
    // 視覚・音響フィードバック
    PlayCheckpointEffect();
    
    // セーブデータ更新（オプション）
    if (saveOnTrigger)
    {
        SaveManager.Instance.SaveProgress(GameManager.Instance.playerProgress);
    }
    
    Debug.Log($"Checkpoint activated at {checkpointPosition}");
}

private void PlayCheckpointEffect()
{
    if (activationEffect != null)
    {
        activationEffect.Play();
    }
    
    if (activationSound != null)
    {
        AudioManager.Instance?.PlaySE(activationSound);
    }
    
    // 追加的な視覚効果
    StartCoroutine(CheckpointGlowEffect());
}

private IEnumerator CheckpointGlowEffect()
{
    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    if (sr == null) yield break;
    
    Color originalColor = sr.color;
    Color glowColor = Color.white;
    
    // 発光効果
    for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
    {
        sr.color = Color.Lerp(originalColor, glowColor, 
            Mathf.PingPong(t, 0.5f) * 2f);
        yield return null;
    }
    
    sr.color = originalColor;
}
```

---

## 実装ガイド

### 基本セットアップ

#### 1. ステージマネージャーの設置

```csharp
// 空のGameObjectを作成
GameObject stageManagerObj = new GameObject("StageManager");

// StageManagerコンポーネントを追加
StageManager stageManager = stageManagerObj.AddComponent<StageManager>();

// 親オブジェクトの作成
stageManager.obstacleParent = new GameObject("Obstacles").transform;
stageManager.collectibleParent = new GameObject("Collectibles").transform;
stageManager.environmentalParent = new GameObject("Environmental").transform;

// 親オブジェクトの階層設定
stageManager.obstacleParent.SetParent(stageManagerObj.transform);
stageManager.collectibleParent.SetParent(stageManagerObj.transform);
stageManager.environmentalParent.SetParent(stageManagerObj.transform);
```

#### 2. ステージデータアセットの作成

```csharp
// Unity エディタでの作成手順：
// 1. Project ウィンドウで右クリック
// 2. Create > Gravity Flip Lab > Stage Data を選択
// 3. ファイル名を "Stage1-1" などに設定
// 4. Inspector でステージ情報を入力

// リソースフォルダ構造:
// Resources/
//   StageData/
//     World1/
//       Stage1-1.asset
//       Stage1-2.asset
//       ...
//     World2/
//       Stage2-1.asset
//       ...
```

#### 3. プレハブ配列の設定

```csharp
// Inspector での設定
// ObstaclePrefabs[8]:
// [0] - SpikePrefab
// [1] - ElectricFencePrefab
// [2] - PistonCrusherPrefab
// [3] - RotatingSawPrefab
// [4] - HoverDronePrefab
// [5] - TimerGatePrefab
// [6] - PhaseBlockPrefab
// [7] - PressureSwitchPrefab

// CollectiblePrefabs[3]:
// [0] - EnergyChipPrefab
// [1] - PowerUpPrefab
// [2] - ExtraLifePrefab

// EnvironmentalPrefabs[4]:
// [0] - GravityWellPrefab
// [1] - WindTunnelPrefab
// [2] - SpringPlatformPrefab
// [3] - MovingPlatformPrefab
```

### カスタムギミックの作成

#### 新しい障害物の実装例

```csharp
public class LaserBeam : BaseObstacle
{
    [Header("Laser Configuration")]
    public float beamLength = 10f;
    public float damageRate = 1f;
    public bool continuousBeam = true;
    public LineRenderer beamRenderer;
    
    private List<GameObject> targetsInBeam = new List<GameObject>();
    
    protected override void OnObstacleStart()
    {
        if (continuousBeam)
        {
            ActivateLaser();
        }
    }
    
    private void ActivateLaser()
    {
        // レーザービーム表示
        if (beamRenderer != null)
        {
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, transform.position + Vector3.right * beamLength);
        }
        
        // 継続ダメージ処理
        StartCoroutine(ContinuousDamage());
    }
    
    private IEnumerator ContinuousDamage()
    {
        while (isActive)
        {
            // レイキャストでターゲット検出
            RaycastHit2D[] hits = Physics2D.RaycastAll(
                transform.position, Vector2.right, beamLength, targetLayers);
            
            foreach (var hit in hits)
            {
                DealDamage(hit.collider.gameObject);
            }
            
            yield return new WaitForSeconds(1f / damageRate);
        }
    }
}
```

#### カスタムパラメータの処理

```csharp
protected override void ApplyParameters(Dictionary<string, object> parameters)
{
    if (parameters.ContainsKey("beamLength"))
        beamLength = (float)parameters["beamLength"];
    
    if (parameters.ContainsKey("damageRate"))
        damageRate = (float)parameters["damageRate"];
    
    if (parameters.ContainsKey("continuousBeam"))
        continuousBeam = (bool)parameters["continuousBeam"];
}
```

### パラレックス背景のセットアップ

#### 背景層の設定

```csharp
// ParallaxBackgroundManagerの設定
public void SetupParallaxLayers()
{
    // 3層構成の推奨設定
    ParallaxLayer[] layers = new ParallaxLayer[3];
    
    // 遠景層（最も遅い）
    layers[0] = new ParallaxLayer
    {
        layerName = "Far Background",
        parallaxFactor = 0.25f,
        enableVerticalParallax = false,
        enableLoop = true
    };
    
    // 中景層（中程度）
    layers[1] = new ParallaxLayer
    {
        layerName = "Mid Background",
        parallaxFactor = 0.5f,
        enableVerticalParallax = false,
        enableLoop = true
    };
    
    // 近景層（最も速い）
    layers[2] = new ParallaxLayer
    {
        layerName = "Near Background",
        parallaxFactor = 0.75f,
        enableVerticalParallax = true,
        enableLoop = true
    };
    
    parallaxManager.parallaxLayers = layers;
}
```

#### 背景スプライトの準備

```csharp
// テクスチャのインポート設定：
// - Texture Type: Sprite (2D and UI)
// - Sprite Mode: Single
// - Wrap Mode: Repeat
// - Filter Mode: Bilinear
// - Max Size: 1024 または 2048

// マテリアル設定：
// - Shader: Sprites/Default
// - Tiling: X=1, Y=1
// - Offset: X=0, Y=0
```

---

## ベストプラクティス

### コード組織化

#### 名前空間の活用

```csharp
namespace GravityFlipLab.Stage
{
    // ステージ関連クラス
    public class StageManager : MonoBehaviour { }
    public class StageStreaming : MonoBehaviour { }
}

namespace GravityFlipLab.Stage.Obstacles
{
    // ギミック関連クラス
    public class SpikeObstacle : BaseObstacle { }
    public class LaserBeam : BaseObstacle { }
}

namespace GravityFlipLab.Stage.Background
{
    // 背景関連クラス
    public class ParallaxBackgroundManager : MonoBehaviour { }
    public class ParallaxLayer { }
}
```

#### インターフェース設計

```csharp
public interface IActivatable
{
    void Activate();
    void Deactivate();
    bool IsActive { get; }
}

public interface IDamageable
{
    void TakeDamage(float amount);
    float CurrentHealth { get; }
}

public interface IPoolable
{
    void OnReturnToPool();
    void OnGetFromPool();
}

// 実装例
public class SmartObstacle : BaseObstacle, IActivatable, IPoolable
{
    public void Activate() { /* 実装 */ }
    public void Deactivate() { /* 実装 */ }
    public void OnReturnToPool() { /* 実装 */ }
    public void OnGetFromPool() { /* 実装 */ }
}
```

### パフォーマンス考慮

#### オブジェクトプールの実装

```csharp
public class ObjectPool<T> where T : MonoBehaviour, IPoolable
{
    private Queue<T> pool = new Queue<T>();
    private T prefab;
    private Transform parent;
    
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        
        for (int i = 0; i < initialSize; i++)
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }
    }
    
    public T Get()
    {
        T instance;
        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
        }
        else
        {
            instance = Object.Instantiate(prefab, parent);
        }
        
        instance.gameObject.SetActive(true);
        instance.OnGetFromPool();
        return instance;
    }
    
    public void Return(T instance)
    {
        instance.OnReturnToPool();
        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
    }
}

// 使用例
public class EffectManager : MonoBehaviour
{
    private ObjectPool<ParticleEffect> explosionPool;
    
    private void Start()
    {
        explosionPool = new ObjectPool<ParticleEffect>(
            explosionPrefab, 10, transform);
    }
    
    public void PlayExplosion(Vector3 position)
    {
        ParticleEffect explosion = explosionPool.Get();
        explosion.transform.position = position;
        explosion.Play();
        
        // 自動でプールに戻す
        StartCoroutine(ReturnToPoolAfterDelay(explosion, 2f));
    }
}
```

#### メモリ効率的なデータ構造

```csharp
// 配列ベースの効率的なコンポーネント管理
public class ComponentArray<T> where T : Component
{
    private T[] components;
    private bool[] activeFlags;
    private int capacity;
    private int count = 0;
    
    public ComponentArray(int capacity)
    {
        this.capacity = capacity;
        components = new T[capacity];
        activeFlags = new bool[capacity];
    }
    
    public int Add(T component)
    {
        if (count >= capacity) return -1;
        
        int index = count++;
        components[index] = component;
        activeFlags[index] = true;
        return index;
    }
    
    public void Remove(int index)
    {
        if (index < 0 || index >= count) return;
        
        activeFlags[index] = false;
        components[index] = null;
    }
    
    public void Update()
    {
        for (int i = 0; i < count; i++)
        {
            if (activeFlags[i] && components[i] != null)
            {
                // コンポーネント更新処理
                UpdateComponent(components[i]);
            }
        }
    }
    
    private void UpdateComponent(T component)
    {
        // 具体的な更新処理
    }
}
```

### エラーハンドリング

#### 堅牢なリソース管理

```csharp
public class SafeResourceLoader
{
    public static T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        try
        {
            T resource = Resources.Load<T>(path);
            if (resource == null)
            {
                Debug.LogError($"Failed to load resource: {path}");
                return null;
            }
            return resource;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception loading resource {path}: {e.Message}");
            return null;
        }
    }
    
    public static bool TryLoadResource<T>(string path, out T resource) where T : UnityEngine.Object
    {
        resource = LoadResource<T>(path);
        return resource != null;
    }
}

// 使用例
public void LoadStageData(int world, int stage)
{
    string path = $"StageData/World{world}/Stage{world}-{stage}";
    
    if (SafeResourceLoader.TryLoadResource<StageDataSO>(path, out StageDataSO stageData))
    {
        LoadStage(stageData);
    }
    else
    {
        // フォールバック処理
        LoadDefaultStage();
    }
}
```

#### 例外安全なコルーチン

```csharp
private IEnumerator SafeLoadStageCoroutine()
{
    try
    {
        yield return StartCoroutine(LoadStageCoroutine());
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Stage loading failed: {e.Message}");
        
        // エラー状態の復旧
        ClearStage();
        LoadDefaultStage();
        
        // エラー通知
        OnStageLoadError?.Invoke(e.Message);
    }
}

public class CoroutineRunner : MonoBehaviour
{
    public static void SafeStartCoroutine(IEnumerator coroutine, 
        System.Action<string> onError = null)
    {
        Instance.StartCoroutine(SafeCoroutineWrapper(coroutine, onError));
    }
    
    private static IEnumerator SafeCoroutineWrapper(IEnumerator coroutine, 
        System.Action<string> onError)
    {
        while (true)
        {
            try
            {
                if (!coroutine.MoveNext())
                    break;
                    
                yield return coroutine.Current;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Coroutine exception: {e.Message}");
                onError?.Invoke(e.Message);
                break;
            }
        }
    }
}
```

---

## トラブルシューティング

### よくある問題と解決策

#### 1. ステージが読み込まれない

**症状**: LoadStage を呼び出してもステージが表示されない

**原因**:
- StageDataSO が見つからない
- プレハブ配列の設定不備
- リソースパスの間違い

**解決策**:

```csharp
public class StageLoadingDebugger : MonoBehaviour
{
    [ContextMenu("Debug Stage Loading")]
    public void DebugStageLoading()
    {
        StageManager manager = StageManager.Instance;
        
        Debug.Log("=== Stage Loading Debug ===");
        Debug.Log($"Current Stage Data: {manager.currentStageData?.name ?? "NULL"}");
        Debug.Log($"Stage Loaded: {manager.stageLoaded}");
        
        // プレハブ配列の確認
        DebugPrefabArrays(manager);
        
        // リソースパスの確認
        DebugResourcePaths();
    }
    
    private void DebugPrefabArrays(StageManager manager)
    {
        Debug.Log("--- Prefab Arrays ---");
        
        for (int i = 0; i < manager.obstaclePrefabs.Length; i++)
        {
            string status = manager.obstaclePrefabs[i] != null ? "OK" : "NULL";
            Debug.Log($"Obstacle[{i}] ({(ObstacleType)i}): {status}");
        }
        
        for (int i = 0; i < manager.collectiblePrefabs.Length; i++)
        {
            string status = manager.collectiblePrefabs[i] != null ? "OK" : "NULL";
            Debug.Log($"Collectible[{i}] ({(CollectibleType)i}): {status}");
        }
    }
    
    private void DebugResourcePaths()
    {
        Debug.Log("--- Resource Paths ---");
        
        for (int world = 1; world <= 5; world++)
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                string path = $"StageData/World{world}/Stage{world}-{stage}";
                StageDataSO data = Resources.Load<StageDataSO>(path);
                
                if (data != null)
                    Debug.Log($"✓ Found: {path}");
                else
                    Debug.LogWarning($"✗ Missing: {path}");
            }
        }
    }
}
```

#### 2. パラレックス背景が動作しない

**症状**: 背景がカメラ移動に追従しない

**原因**:
- カメラ参照が設定されていない
- マテリアルの設定不備
- パラレックス係数が0

**解決策**:

```csharp
public class ParallaxDebugger : MonoBehaviour
{
    public ParallaxBackgroundManager parallaxManager;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugParallaxState();
        }
    }
    
    private void DebugParallaxState()
    {
        if (parallaxManager == null)
        {
            Debug.LogError("ParallaxBackgroundManager not assigned");
            return;
        }
        
        Debug.Log("=== Parallax Debug ===");
        Debug.Log($"Parallax Enabled: {parallaxManager.enableParallax}");
        Debug.Log($"Camera Transform: {parallaxManager.cameraTransform?.name ?? "NULL"}");
        
        for (int i = 0; i < parallaxManager.parallaxLayers.Length; i++)
        {
            var layer = parallaxManager.parallaxLayers[i];
            Debug.Log($"Layer {i}: {layer.layerName}");
            Debug.Log($"  SpriteRenderer: {layer.spriteRenderer?.name ?? "NULL"}");
            Debug.Log($"  Parallax Factor: {layer.parallaxFactor}");
            Debug.Log($"  Enable Loop: {layer.enableLoop}");
            
            if (layer.spriteRenderer != null)
            {
                Material mat = layer.spriteRenderer.material;
                Debug.Log($"  Material: {mat?.name ?? "NULL"}");
                if (mat != null)
                {
                    Debug.Log($"  Texture Offset: {mat.mainTextureOffset}");
                }
            }
        }
    }
    
    [ContextMenu("Fix Parallax Setup")]
    public void FixParallaxSetup()
    {
        if (parallaxManager.cameraTransform == null)
        {
            parallaxManager.cameraTransform = Camera.main.transform;
            Debug.Log("Fixed: Assigned main camera");
        }
        
        foreach (var layer in parallaxManager.parallaxLayers)
        {
            if (layer.spriteRenderer != null && layer.spriteRenderer.material != null)
            {
                Material mat = layer.spriteRenderer.material;
                if (mat.mainTexture != null)
                {
                    layer.textureSize = new Vector2(
                        mat.mainTexture.width, 
                        mat.mainTexture.height
                    );
                    Debug.Log($"Fixed: Updated texture size for {layer.layerName}");
                }
            }
        }
    }
}
```

#### 3. ギミックが正常に動作しない

**症状**: 障害物が期待通りに動作しない、ダメージを与えない

**原因**:
- レイヤーマスクの設定ミス
- コライダーの設定不備
- 初期化順序の問題

**解決策**:

```csharp
public class ObstacleDebugger : MonoBehaviour
{
    public BaseObstacle targetObstacle;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugObstacle();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ForceActivateObstacle();
        }
    }
    
    private void DebugObstacle()
    {
        if (targetObstacle == null)
        {
            Debug.LogError("Target obstacle not assigned");
            return;
        }
        
        Debug.Log("=== Obstacle Debug ===");
        Debug.Log($"Obstacle Type: {targetObstacle.obstacleType}");
        Debug.Log($"Is Active: {targetObstacle.isActive}");
        Debug.Log($"Initialized: {targetObstacle.GetType().GetField("initialized", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance)?.GetValue(targetObstacle)}");
        Debug.Log($"Target Layers: {targetObstacle.targetLayers.value}");
        
        // コライダー確認
        Collider2D[] colliders = targetObstacle.GetComponents<Collider2D>();
        Debug.Log($"Colliders: {colliders.Length}");
        foreach (var col in colliders)
        {
            Debug.Log($"  {col.GetType().Name}: IsTrigger={col.isTrigger}, Enabled={col.enabled}");
        }
        
        // プレイヤーとの距離確認
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(targetObstacle.transform.position, player.transform.position);
            Debug.Log($"Distance to Player: {distance:F2}");
        }
    }
    
    private void ForceActivateObstacle()
    {
        if (targetObstacle != null)
        {
            targetObstacle.StartObstacle();
            Debug.Log("Force activated obstacle");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (targetObstacle != null)
        {
            // ターゲットレイヤーマスクの可視化
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetObstacle.transform.position, 2f);
            
            // プレイヤーとの接続線
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(targetObstacle.transform.position, player.transform.position);
            }
        }
    }
}
```

#### 4. パフォーマンスの低下

**症状**: ステージ読み込み後にフレームレートが大幅に低下

**原因**:
- 過多なオブジェクト生成
- カリングの不備
- エフェクトの過剰生成

**解決策**:

```csharp
public class PerformanceProfiler : MonoBehaviour
{
    [Header("Profiling Settings")]
    public bool enableProfiling = true;
    public float profilingInterval = 1f;
    
    private int lastFrameCount;
    private float lastProfilingTime;
    
    private void Update()
    {
        if (!enableProfiling) return;
        
        if (Time.time - lastProfilingTime >= profilingInterval)
        {
            PerformProfiling();
            lastProfilingTime = Time.time;
        }
    }
    
    private void PerformProfiling()
    {
        // FPS計算
        int currentFrameCount = Time.frameCount;
        float fps = (currentFrameCount - lastFrameCount) / profilingInterval;
        lastFrameCount = currentFrameCount;
        
        // オブジェクト数カウント
        int obstacleCount = FindObjectsOfType<BaseObstacle>().Length;
        int collectibleCount = FindObjectsOfType<Collectible>().Length;
        int particleCount = FindObjectsOfType<ParticleSystem>().Length;
        
        // メモリ使用量
        long memoryUsage = System.GC.GetTotalMemory(false);
        
        Debug.Log($"=== Performance Report ===");
        Debug.Log($"FPS: {fps:F1}");
        Debug.Log($"Obstacles: {obstacleCount}");
        Debug.Log($"Collectibles: {collectibleCount}");
        Debug.Log($"Particle Systems: {particleCount}");
        Debug.Log($"Memory: {memoryUsage / 1024 / 1024}MB");
        
        // パフォーマンス警告
        if (fps < 30f)
        {
            Debug.LogWarning("Low FPS detected!");
            SuggestOptimizations(obstacleCount, collectibleCount, particleCount);
        }
    }
    
    private void SuggestOptimizations(int obstacles, int collectibles, int particles)
    {
        List<string> suggestions = new List<string>();
        
        if (obstacles > 50)
            suggestions.Add("Consider obstacle culling or pooling");
        
        if (collectibles > 20)
            suggestions.Add("Implement collectible distance culling");
        
        if (particles > 10)
            suggestions.Add("Limit active particle systems");
        
        if (suggestions.Count > 0)
        {
            Debug.LogWarning("Optimization suggestions:\n" + string.Join("\n", suggestions));
        }
    }
    
    [ContextMenu("Force Garbage Collection")]
    public void ForceGC()
    {
        long beforeGC = System.GC.GetTotalMemory(false);
        System.GC.Collect();
        long afterGC = System.GC.GetTotalMemory(false);
        
        Debug.Log($"GC performed: {(beforeGC - afterGC) / 1024 / 1024}MB freed");
    }
    
    [ContextMenu("Optimize Current Stage")]
    public void OptimizeCurrentStage()
    {
        // 距離ベースのオブジェクト無効化
        Camera mainCamera = Camera.main;
        Vector3 cameraPos = mainCamera.transform.position;
        
        BaseObstacle[] obstacles = FindObjectsOfType<BaseObstacle>();
        int disabledCount = 0;
        
        foreach (var obstacle in obstacles)
        {
            float distance = Vector3.Distance(obstacle.transform.position, cameraPos);
            if (distance > 30f)
            {
                obstacle.gameObject.SetActive(false);
                disabledCount++;
            }
        }
        
        Debug.Log($"Optimization complete: {disabledCount} objects disabled");
    }
}
```

#### 5. セーブデータの不整合

**症状**: ステージ進行状況が正しく保存・復元されない

**原因**:
- イベントの発火タイミング
- データの同期不備
- セーブポイントの設定ミス

**解決策**:

```csharp
public class SaveDataValidator : MonoBehaviour
{
    [ContextMenu("Validate Save Data")]
    public void ValidateSaveData()
    {
        PlayerProgress progress = GameManager.Instance.playerProgress;
        
        Debug.Log("=== Save Data Validation ===");
        Debug.Log($"Current World: {progress.currentWorld}");
        Debug.Log($"Current Stage: {progress.currentStage}");
        Debug.Log($"Total Energy Chips: {progress.totalEnergyChips}");
        Debug.Log($"Stage Progress Count: {progress.stageProgress.Count}");
        
        // ステージデータの整合性確認
        foreach (var kvp in progress.stageProgress)
        {
            string stageKey = kvp.Key;
            StageData stageData = kvp.Value;
            
            Debug.Log($"Stage {stageKey}:");
            Debug.Log($"  Cleared: {stageData.isCleared}");
            Debug.Log($"  Best Time: {stageData.bestTime:F2}s");
            Debug.Log($"  Energy Chips: {stageData.energyChipsCollected}/{stageData.maxEnergyChips}");
            Debug.Log($"  Rank: {GetRankString(stageData.rank)}");
            
            // データの妥当性チェック
            if (stageData.bestTime < 0 || stageData.bestTime > 3600)
            {
                Debug.LogWarning($"Invalid best time for {stageKey}: {stageData.bestTime}");
            }
            
            if (stageData.energyChipsCollected > stageData.maxEnergyChips)
            {
                Debug.LogWarning($"Invalid energy chip count for {stageKey}");
            }
        }
    }
    
    private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 4: return "S";
            case 3: return "A";
            case 2: return "B";
            case 1: return "C";
            default: return "None";
        }
    }
    
    [ContextMenu("Fix Save Data")]
    public void FixSaveData()
    {
        PlayerProgress progress = GameManager.Instance.playerProgress;
        bool fixedData = false;
        
        // 範囲外の値を修正
        if (progress.currentWorld < 1 || progress.currentWorld > 5)
        {
            progress.currentWorld = 1;
            fixedData = true;
            Debug.Log("Fixed: Current world reset to 1");
        }
        
        if (progress.currentStage < 1 || progress.currentStage > 10)
        {
            progress.currentStage = 1;
            fixedData = true;
            Debug.Log("Fixed: Current stage reset to 1");
        }
        
        // ステージデータの修正
        List<string> keysToFix = new List<string>();
        foreach (var kvp in progress.stageProgress)
        {
            StageData stageData = kvp.Value;
            
            if (stageData.bestTime < 0 || stageData.bestTime > 3600)
            {
                stageData.bestTime = float.MaxValue;
                keysToFix.Add(kvp.Key);
            }
            
            if (stageData.energyChipsCollected > stageData.maxEnergyChips)
            {
                stageData.energyChipsCollected = stageData.maxEnergyChips;
                keysToFix.Add(kvp.Key);
            }
        }
        
        if (keysToFix.Count > 0)
        {
            fixedData = true;
            Debug.Log($"Fixed: {keysToFix.Count} stage data entries");
        }
        
        if (fixedData)
        {
            SaveManager.Instance.SaveProgress(progress);
            Debug.Log("Save data has been fixed and saved");
        }
        else
        {
            Debug.Log("No issues found in save data");
        }
    }
}
```

---

## 高度な機能と拡張

### プロシージャル生成システム

```csharp
public class ProceduralStageGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int segmentCount = 16;
    public float difficultyProgression = 1.2f;
    public ObstacleType[] availableObstacles;
    public AnimationCurve difficultyS urve;
    
    [Header("Obstacle Density")]
    public int minObstaclesPerSegment = 1;
    public int maxObstaclesPerSegment = 4;
    
    private System.Random random;
    
    public StageDataSO GenerateStage(int seed, int difficulty)
    {
        random = new System.Random(seed);
        
        StageDataSO generatedStage = CreateInstance<StageDataSO>();
        generatedStage.stageInfo = CreateStageInfo(difficulty);
        generatedStage.obstacles = GenerateObstacles(difficulty);
        generatedStage.collectibles = GenerateCollectibles();
        generatedStage.environmental = GenerateEnvironmental(difficulty);
        
        return generatedStage;
    }
    
    private StageInfo CreateStageInfo(int difficulty)
    {
        return new StageInfo
        {
            worldNumber = (difficulty - 1) / 10 + 1,
            stageNumber = ((difficulty - 1) % 10) + 1,
            stageName = $"Generated Stage {difficulty}",
            timeLimit = 300f - (difficulty * 10f),
            energyChipCount = 3,
            playerStartPosition = Vector3.zero,
            goalPosition = new Vector3(4096f, 0f, 0f),
            stageLength = 4096f,
            stageHeight = 1024f,
            segmentCount = segmentCount
        };
    }
    
    private List<ObstacleData> GenerateObstacles(int difficulty)
    {
        List<ObstacleData> obstacles = new List<ObstacleData>();
        float difficultyMultiplier = difficultyS urve.Evaluate(difficulty / 50f);
        
        for (int segment = 0; segment < segmentCount; segment++)
        {
            float segmentX = segment * (4096f / segmentCount);
            int obstacleCount = random.Next(minObstaclesPerSegment, 
                Mathf.RoundToInt(maxObstaclesPerSegment * difficultyMultiplier) + 1);
            
            for (int i = 0; i < obstacleCount; i++)
            {
                ObstacleData obstacle = new ObstacleData
                {
                    type = availableObstacles[random.Next(availableObstacles.Length)],
                    position = new Vector3(
                        segmentX + random.Next(0, (int)(4096f / segmentCount)),
                        random.Next(-400, 400),
                        0f
                    ),
                    rotation = Vector3.zero,
                    scale = Vector3.one
                };
                
                // 難易度に応じたパラメータ調整
                AdjustObstacleForDifficulty(obstacle, difficultyMultiplier);
                obstacles.Add(obstacle);
            }
        }
        
        return obstacles;
    }
    
    private void AdjustObstacleForDifficulty(ObstacleData obstacle, float difficultyMultiplier)
    {
        obstacle.parameters = new Dictionary<string, object>();
        
        switch (obstacle.type)
        {
            case ObstacleType.PistonCrusher:
                obstacle.parameters["crushSpeed"] = 5f * difficultyMultiplier;
                obstacle.parameters["waitTime"] = Mathf.Max(0.5f, 2f / difficultyMultiplier);
                break;
                
            case ObstacleType.RotatingSaw:
                obstacle.parameters["rotationSpeed"] = 360f * difficultyMultiplier;
                obstacle.parameters["moveSpeed"] = 2f * difficultyMultiplier;
                break;
                
            case ObstacleType.HoverDrone:
                obstacle.parameters["detectionRange"] = 5f * difficultyMultiplier;
                obstacle.parameters["attackCooldown"] = Mathf.Max(1f, 3f / difficultyMultiplier);
                break;
        }
    }
    
    private List<CollectibleData> GenerateCollectibles()
    {
        List<CollectibleData> collectibles = new List<CollectibleData>();
        
        for (int i = 0; i < 3; i++)
        {
            float x = (i + 1) * (4096f / 4f);
            float y = random.Next(-200, 200);
            
            collectibles.Add(new CollectibleData
            {
                type = CollectibleType.EnergyChip,
                position = new Vector3(x, y, 0f),
                value = 1
            });
        }
        
        return collectibles;
    }
    
    private List<EnvironmentalData> GenerateEnvironmental(int difficulty)
    {
        List<EnvironmentalData> environmental = new List<EnvironmentalData>();
        
        // 難易度に応じて環境要素を配置
        if (difficulty > 10)
        {
            // 重力井戸の配置
            environmental.Add(new EnvironmentalData
            {
                type = EnvironmentalType.GravityWell,
                position = new Vector3(2048f, 0f, 0f),
                scale = Vector3.one,
                parameters = new Dictionary<string, object>
                {
                    ["strength"] = 15f,
                    ["radius"] = 10f
                }
            });
        }
        
        return environmental;
    }
}
```

### AIナビゲーションシステム

```csharp
public class StageAI : MonoBehaviour
{
    [Header("AI Configuration")]
    public float analysisRadius = 20f;
    public float pathfindingAccuracy = 0.5f;
    public LayerMask obstacleLayerMask;
    
    private Dictionary<Vector2Int, NodeData> navigationGrid;
    private int gridResolution = 64;
    
    private struct NodeData
    {
        public bool isWalkable;
        public float cost;
        public Vector2 worldPosition;
    }
    
    private void Start()
    {
        BuildNavigationGrid();
    }
    
    private void BuildNavigationGrid()
    {
        navigationGrid = new Dictionary<Vector2Int, NodeData>();
        
        StageDataSO stageData = StageManager.Instance.currentStageData;
        if (stageData == null) return;
        
        float cellSize = stageData.stageInfo.stageLength / gridResolution;
        
        for (int x = 0; x < gridResolution; x++)
        {
            for (int y = 0; y < gridResolution; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector2 worldPos = new Vector2(
                    x * cellSize,
                    (y - gridResolution / 2) * cellSize
                );
                
                bool isWalkable = !Physics2D.OverlapCircle(worldPos, cellSize / 2, obstacleLayerMask);
                
                navigationGrid[gridPos] = new NodeData
                {
                    isWalkable = isWalkable,
                    cost = isWalkable ? 1f : float.MaxValue,
                    worldPosition = worldPos
                };
            }
        }
    }
    
    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        Vector2Int startGrid = WorldToGrid(start);
        Vector2Int endGrid = WorldToGrid(end);
        
        return AStar(startGrid, endGrid);
    }
    
    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        StageDataSO stageData = StageManager.Instance.currentStageData;
        float cellSize = stageData.stageInfo.stageLength / gridResolution;
        
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize) + gridResolution / 2
        );
    }
    
    private List<Vector2> AStar(Vector2Int start, Vector2Int end)
    {
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        
        List<Vector2Int> openSet = new List<Vector2Int> { start };
        gScore[start] = 0f;
        fScore[start] = Vector2Int.Distance(start, end);
        
        while (openSet.Count > 0)
        {
            Vector2Int current = GetLowestFScore(openSet, fScore);
            
            if (current == end)
            {
                return ReconstructPath(cameFrom, current);
            }
            
            openSet.Remove(current);
            
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (!navigationGrid.ContainsKey(neighbor) || !navigationGrid[neighbor].isWalkable)
                    continue;
                
                float tentativeGScore = gScore[current] + navigationGrid[neighbor].cost;
                
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector2Int.Distance(neighbor, end);
                    
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        
        return new List<Vector2>(); // パスが見つからない場合
    }
    
    private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
    {
        Vector2Int lowest = openSet[0];
        float lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;
        
        foreach (Vector2Int node in openSet)
        {
            float score = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
            if (score < lowestScore)
            {
                lowest = node;
                lowestScore = score;
            }
        }
        
        return lowest;
    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int node)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                Vector2Int neighbor = new Vector2Int(node.x + dx, node.y + dy);
                neighbors.Add(neighbor);
            }
        }
        
        return neighbors;
    }
    
    private List<Vector2> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2> path = new List<Vector2>();
        
        while (cameFrom.ContainsKey(current))
        {
            if (navigationGrid.ContainsKey(current))
            {
                path.Insert(0, navigationGrid[current].worldPosition);
            }
            current = cameFrom[current];
        }
        
        return path;
    }
}
```

---

## 結論

Gravity Flip Lab のステージ管理・ギミックシステムは、大規模で複雑なゲームステージを効率的に管理し、多様なインタラクティブ要素を統合した包括的なシステムです。このドキュメントで詳述した設計により、以下の優れた特徴が実現されます：

### 技術的優位性

1. **スケーラブルアーキテクチャ**: 4096×1024ピクセルの大規模ステージを効率的に管理
2. **モジュラー設計**: 独立したコンポーネント設計による高い保守性
3. **データ駆動開発**: ScriptableObjectベースの柔軟な設定システム
4. **動的ストリーミング**: メモリ効率的なセグメント管理
5. **高度な最適化**: カリング、プーリング、効果管理による安定したパフォーマンス

### ゲームプレイへの貢献

- **多様なギミック**: 8種類の障害物による豊富なゲームプレイバリエーション
- **視覚的奥行き**: 3層パラレックス背景による没入感の向上
- **スムーズなカメラワーク**: 先読み追従による快適な視覚体験
- **インタラクティブ要素**: プレイヤーの行動に応じた動的な環境変化
- **収集要素**: エナジーチップによる探索インセンティブ

### 開発・運用面の利点

- **効率的な開発フロー**: ビジュアルエディタによる直感的なステージ作成
- **デバッグ支援**: 包括的なデバッグツールと診断機能
- **パフォーマンス監視**: リアルタイムな性能追跡と最適化提案
- **エラー耐性**: 例外処理とフォールバック機能による安定性
- **拡張性**: 新しいギミックや機能の追加が容易

### 実装の特徴

このシステムは以下の革新的な技術を採用しています：

#### 1. ハイブリッドストリーミング
- **セグメント分割**: 256ピクセル単位での効率的な管理
- **動的ロード/アンロード**: プレイヤー位置に基づく最適化
- **オブジェクトプール**: メモリ断片化の防止
- **距離ベースカリング**: 視野外オブジェクトの自動無効化

#### 2. 高度なギミックシステム
- **基底クラス設計**: 統一されたインターフェースによる一貫性
- **パラメータ駆動**: 辞書ベースの柔軟な設定システム
- **AI統合**: 知的な敵対ギミックによる動的な挑戦
- **物理統合**: Unity Physics2Dとの密接な連携

#### 3. 視覚効果システム
- **UVスクロール**: シームレスなパラレックス背景
- **エフェクト管理**: 制限付きパーティクルシステム
- **カメラ制御**: スムーズダンピングによる自然な追従
- **テーマ統合**: ワールドテーマに応じた視覚調整

### 今後の発展可能性

このシステムは以下のような拡張が可能です：

#### 技術的拡張
- **プロシージャル生成**: AI駆動による無限ステージ生成
- **マルチプレイヤー対応**: 協力・対戦プレイの実装
- **VR/AR対応**: 没入型インターフェースへの拡張
- **クラウド統合**: オンラインステージ共有システム

#### ゲームプレイ拡張
- **カスタムギミック**: ユーザー作成ギミックシステム
- **動的難易度**: プレイヤーのスキルに応じた自動調整
- **ナラティブ統合**: ストーリー要素の段階的展開
- **コミュニティ機能**: ステージ共有とレーティングシステム

#### AI・機械学習統合
- **適応型AI**: プレイヤー行動の学習による最適化
- **予測システム**: 機械学習による障害物配置最適化
- **行動分析**: プレイパターンの詳細分析
- **自動バランシング**: AI による難易度の動的調整

### 実装時の重要なポイント

#### 1. メモリ管理の最適化
```csharp
// 推奨メモリ使用量の目安
- セグメントあたり: 最大 5MB
- アクティブオブジェクト: 最大 100個
- パーティクルエフェクト: 最大 10個同時
- テクスチャメモリ: 最大 50MB
```

#### 2. パフォーマンス目標
```csharp
// ターゲットパフォーマンス
- FPS: 60fps (Nintendo Switch)
- ロード時間: 3秒以内
- メモリ使用量: 100MB以下
- ガベージコレクション: 16ms以下
```

#### 3. エラーハンドリング
- **段階的復旧**: 部分的な機能低下による継続動作
- **自動診断**: 問題の自動検出と報告
- **フォールバック**: 代替手段による安定動作
- **ログ記録**: 詳細なデバッグ情報の保存

### 品質保証

このシステムは以下の品質保証手法を採用しています：

#### 1. テスト戦略
- **ユニットテスト**: 個別コンポーネントの動作検証
- **統合テスト**: システム間連携の確認
- **パフォーマンステスト**: 負荷状態での安定性検証
- **ユーザビリティテスト**: プレイヤー体験の評価

#### 2. 継続的改善
- **メトリクス収集**: プレイヤー行動データの分析
- **A/Bテスト**: 異なる設定による効果測定
- **フィードバック統合**: コミュニティからの意見反映
- **反復的改善**: 定期的なシステム最適化

#### 3. 文書化
- **API リファレンス**: 完全な関数・クラス仕様
- **実装ガイド**: 段階的な導入手順
- **ベストプラクティス**: 効率的な開発方法
- **トラブルシューティング**: 問題解決のための診断手順

---

## 付録

### A. パフォーマンス最適化チェックリスト

#### 基本最適化
- [ ] オブジェクトプールの実装
- [ ] 距離ベースカリングの設定
- [ ] エフェクト数の制限
- [ ] テクスチャサイズの最適化
- [ ] オーディオ圧縮の適用

#### 高度な最適化
- [ ] 空間分割システムの実装
- [ ] LOD（Level of Detail）システム
- [ ] 非同期ロードの実装
- [ ] メモリプールの管理
- [ ] ガベージコレクション最適化

#### プラットフォーム固有
- [ ] Nintendo Switch 最適化
- [ ] Steam 対応
- [ ] モバイル プラットフォーム調整
- [ ] VR/AR 対応準備

### B. デバッグコマンド一覧

| コマンド | 機能 | 使用例 |
|---------|------|--------|
| `F1` | ステージ情報表示 | デバッグ情報の確認 |
| `F2` | ギミック状態表示 | 障害物の動作確認 |
| `F3` | パフォーマンス情報 | FPS・メモリ使用量 |
| `F4` | 強制ガベージコレクション | メモリクリーンアップ |
| `F5` | ステージリロード | 即座の再読み込み |

### C. 設定ファイル例

#### ステージ設定テンプレート

```json
{
  "stageInfo": {
    "worldNumber": 1,
    "stageNumber": 1,
    "stageName": "Tutorial Stage",
    "timeLimit": 300.0,
    "energyChipCount": 3,
    "playerStartPosition": {"x": 0, "y": 0, "z": 0},
    "goalPosition": {"x": 4096, "y": 0, "z": 0},
    "theme": "Tech",
    "stageLength": 4096.0,
    "stageHeight": 1024.0,
    "segmentCount": 16
  },
  "backgroundLayers": [
    {
      "layerName": "Far Background",
      "parallaxFactor": 0.25,
      "tileSize": {"x": 512, "y": 512},
      "enableVerticalLoop": false,
      "tintColor": {"r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0}
    }
  ]
}
```

### D. パフォーマンス基準値

#### Nintendo Switch 目標値
- **FPS**: 60fps (常時)
- **メモリ使用量**: 100MB以下
- **ロード時間**: 3秒以内
- **ガベージコレクション**: 16ms以下

#### PC 目標値
- **FPS**: 120fps以上
- **メモリ使用量**: 200MB以下
- **ロード時間**: 1秒以内
- **4K解像度対応**: 可能

#### モバイル 目標値
- **FPS**: 30fps以上
- **メモリ使用量**: 50MB以下
- **バッテリー消費**: 標準的なゲーム並み
- **発熱制御**: 適切な温度管理

---

## 終わりに

Gravity Flip Lab のステージ管理・ギミックシステムは、大規模で複雑なゲーム環境を効率的に管理し、プレイヤーに豊富で没入感のある体験を提供する包括的なシステムです。このドキュメントを参考に、開発チームは効率的で高品質な実装を実現し、プレイヤーに画期的なゲーム体験を提供できるでしょう。

システムの各コンポーネントは独立性を保ちながらも密接に連携し、拡張性と保守性を両立した設計となっています。また、パフォーマンス最適化と品質保証の仕組みにより、商業レベルの製品として十分な品質を確保しています。

このシステムが、Gravity Flip Lab の成功と、ゲーム業界における新しいステージ管理技術の発展に貢献することを期待しています。プレイヤーが重力反転の爽快感と共に、美しく設計されたステージ環境で最高のゲーム体験を楽しめることを願っています。

### 参考資料

#### 技術資料
- Unity Performance Optimization Guide
- Unity 2D Best Practices
- C# Coding Standards for Unity
- Memory Management in Unity

#### ゲームデザイン資料
- Level Design Principles
- Player Engagement Metrics
- Accessibility Guidelines
- User Experience Design

#### コミュニティリソース
- Unity Community Forums
- Game Development Blogs
- Open Source Projects
- Developer Conference Presentations

**最終更新**: 2024年12月
**バージョン**: 1.0
**作成者**: Gravity Flip Lab Development Team