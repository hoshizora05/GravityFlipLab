# Gravity Flip Lab - 基盤システム詳細ドキュメント

## 目次

1. [システム概要](#システム概要)
2. [アーキテクチャ設計](#アーキテクチャ設計)
3. [データ構造](#データ構造)
4. [システム詳細](#システム詳細)
5. [クラス仕様](#クラス仕様)
6. [API リファレンス](#api-リファレンス)
7. [実装ガイド](#実装ガイド)
8. [ベストプラクティス](#ベストプラクティス)

---

## システム概要

Gravity Flip Lab の基盤システムは、ゲーム全体の動作を制御・管理する中核的なコンポーネント群で構成されています。これらのシステムは、Unityエンジン上でC#言語により実装され、堅牢で拡張性の高いアーキテクチャを提供します。

### 主要特徴

- **統一データ管理**: プレイヤーの進行状況、設定、統計情報の一元管理
- **暗号化セーブシステム**: AES-256による強固なセーブデータ保護
- **イベント駆動アーキテクチャ**: 疎結合なコンポーネント間通信
- **シーン管理**: 非同期ロードによるスムーズな画面遷移
- **設定管理**: 多言語対応とアクセシビリティ機能

---

## アーキテクチャ設計

### システム構成図

```
┌─────────────────────────────────────┐
│          Game Manager               │
│      (中央制御システム)              │
├─────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────────┐ │
│ │Save Manager │ │Config Manager   │ │
│ │(データ管理) │ │(設定管理)       │ │
│ └─────────────┘ └─────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │Scene Transition Manager         │ │
│ │(シーン遷移管理)                 │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### 設計原則

1. **Single Responsibility Principle**: 各クラスは単一の責任を持つ
2. **Singleton Pattern**: グローバルアクセスが必要なマネージャーに適用
3. **Event-Driven Architecture**: 疎結合なコンポーネント通信
4. **Data Encapsulation**: データの整合性とセキュリティを保証

---

## データ構造

### PlayerProgress（プレイヤー進行データ）

プレイヤーのゲーム進行状況を管理する中核データ構造です。

```csharp
[System.Serializable]
public class PlayerProgress
{
    public int currentWorld = 1;                                    // 現在のワールド（1-5）
    public int currentStage = 1;                                    // 現在のステージ（1-10）
    public Dictionary<string, StageData> stageProgress;             // ステージ進行状況
    public Dictionary<string, float> bestTimes;                     // ベストタイム記録
    public Dictionary<string, int> stageRanks;                      // ステージランク（0-4）
    public int totalEnergyChips = 0;                                // 総エナジーチップ数
    public PlayerSettings settings = new PlayerSettings();         // プレイヤー設定
}
```

**フィールド詳細**:
- `currentWorld`: プレイヤーが現在プレイしているワールド番号
- `currentStage`: プレイヤーが現在プレイしているステージ番号
- `stageProgress`: ステージキー（"ワールド-ステージ"形式）をキーとした進行状況
- `bestTimes`: 各ステージのベストクリアタイム
- `stageRanks`: ステージランク（0=未クリア, 1=C, 2=B, 3=A, 4=S）
- `totalEnergyChips`: 収集した総エナジーチップ数

### StageData（ステージデータ）

個別ステージの詳細情報を格納します。

```csharp
[System.Serializable]
public class StageData
{
    public bool isCleared = false;          // クリア済みフラグ
    public float bestTime = float.MaxValue; // ベストタイム
    public int deathCount = 0;              // 死亡回数
    public int energyChipsCollected = 0;    // 収集エナジーチップ数
    public int maxEnergyChips = 3;          // 最大エナジーチップ数
    public int rank = 0;                    // ランク評価
}
```

### PlayerSettings（プレイヤー設定）

ゲーム設定とユーザー環境設定を管理します。

```csharp
[System.Serializable]
public class PlayerSettings
{
    public float masterVolume = 1.0f;           // マスター音量
    public float bgmVolume = 0.8f;              // BGM音量
    public float seVolume = 1.0f;               // SE音量
    public int languageIndex = 0;               // 言語設定インデックス
    public bool assistModeEnabled = false;      // アシストモード有効化
    public int colorBlindMode = 0;              // 色覚サポートモード
    public bool highContrastMode = false;       // ハイコントラストモード
    public float uiScale = 1.0f;                // UI拡大率
    public KeyCode primaryInput = KeyCode.Space; // 主要入力キー
}
```

### GameState（ゲーム状態列挙型）

ゲームの現在状態を定義します。

```csharp
public enum GameState
{
    MainMenu,       // メインメニュー
    StageSelect,    // ステージ選択
    Gameplay,       // ゲームプレイ中
    Paused,         // 一時停止中
    GameOver,       // ゲームオーバー
    Loading,        // ロード中
    Options,        // オプション画面
    Leaderboard,    // リーダーボード
    Shop           // ショップ
}
```

---

## システム詳細

### GameManager（ゲーム管理システム）

ゲーム全体の状態と進行を統括する中央制御システムです。

#### 主要機能

1. **ゲーム状態管理**
   - 現在のゲーム状態の追跡と遷移制御
   - 状態変更イベントの発行

2. **セッション管理**
   - プレイセッションの開始・終了処理
   - セッション統計の記録

3. **ステージ管理**
   - 現在プレイ中のワールド・ステージ情報
   - ステージクリア処理とランク計算

#### 重要メソッド

```csharp
public void ChangeGameState(GameState newState)
```
ゲーム状態を変更し、関連するイベントを発行します。

```csharp
public void StartStage(int world, int stage)
```
指定されたステージを開始し、セッション情報を初期化します。

```csharp
public void CompleteStage(float clearTime, int deathCount, int energyChips)
```
ステージクリア処理を実行し、進行状況を更新します。

#### ランク計算アルゴリズム

```csharp
private int CalculateStageRank(float clearTime, int deathCount, int energyChips)
{
    int score = 0;
    
    // 時間スコア（例：30秒以内で40点）
    if (clearTime <= 30f) score += 40;
    else if (clearTime <= 60f) score += 30;
    else if (clearTime <= 90f) score += 20;
    else score += 10;
    
    // 死亡ペナルティ（1回につき5点減点）
    score -= deathCount * 5;
    
    // エナジーチップボーナス（1個につき10点）
    score += energyChips * 10;
    
    // ランク判定
    if (score >= 50) return 4; // S
    if (score >= 40) return 3; // A
    if (score >= 25) return 2; // B
    if (score >= 10) return 1; // C
    return 0; // 未ランク
}
```

### SaveManager（セーブデータ管理システム）

プレイヤーデータの永続化と暗号化を担当します。

#### セキュリティ機能

1. **AES-256暗号化**
   - セーブデータの完全暗号化
   - 固有暗号化キーによる保護

2. **データ整合性チェック**
   - ロード時のデータ検証
   - 破損データの検出と復旧

#### 暗号化プロセス

```csharp
private string EncryptString(string plainText, string key)
{
    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
    
    using (Aes aes = Aes.Create())
    {
        aes.Key = ResizeKey(keyBytes, 32); // AES-256
        aes.IV = new byte[16]; // ゼロIV（簡略化）
        
        using (ICryptoTransform encryptor = aes.CreateEncryptor())
        {
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
    }
}
```

#### セーブファイル構造

```
Application.persistentDataPath/
└── gravity_flip_save.dat（暗号化されたJSONデータ）
```

### ConfigManager（設定管理システム）

ゲーム設定とアンロック条件の管理を行います。

#### 主要機能

1. **設定値管理**
   - デフォルト値の定義
   - 動的設定変更の適用

2. **アンロック条件管理**
   - ステージアンロック状態の判定
   - 進行条件のチェック

#### アンロック判定ロジック

```csharp
public bool IsStageUnlocked(int world, int stage)
{
    if (world == 1 && stage == 1) return true; // 最初のステージは常にアンロック
    
    // 前のステージがクリア済みかチェック
    if (stage > 1)
    {
        string prevStageKey = $"{world}-{stage - 1}";
        return GameManager.Instance.playerProgress.stageProgress.ContainsKey(prevStageKey) &&
               GameManager.Instance.playerProgress.stageProgress[prevStageKey].isCleared;
    }
    
    // 前のワールドの最終ステージがクリア済みかチェック
    if (world > 1)
    {
        string lastStageKey = $"{world - 1}-{stagesPerWorld}";
        return GameManager.Instance.playerProgress.stageProgress.ContainsKey(lastStageKey) &&
               GameManager.Instance.playerProgress.stageProgress[lastStageKey].isCleared;
    }
    
    return false;
}
```

### SceneTransitionManager（シーン遷移管理システム）

ゲーム画面間の遷移を管理し、スムーズなユーザー体験を提供します。

#### 非同期ロード機能

```csharp
private IEnumerator LoadSceneCoroutine(string sceneName)
{
    isLoading = true;
    GameManager.Instance.ChangeGameState(GameState.Loading);
    
    OnSceneLoadStart?.Invoke(sceneName);

    // オプション：ローディング画面表示
    if (useLoadingScreen)
    {
        yield return new WaitForSeconds(transitionDuration);
    }

    // 非同期シーンロード
    AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
    loadOperation.allowSceneActivation = false;

    // ロード完了待機（90%まで）
    while (loadOperation.progress < 0.9f)
    {
        yield return null;
    }

    // 追加ロード時間
    yield return new WaitForSeconds(0.1f);

    // シーンアクティベート
    loadOperation.allowSceneActivation = true;

    // アクティベート完了待機
    while (!loadOperation.isDone)
    {
        yield return null;
    }

    currentSceneName = sceneName;
    isLoading = false;
    
    OnSceneLoadComplete?.Invoke(sceneName);
}
```

---

## クラス仕様

### GameManager

**継承**: MonoBehaviour  
**パターン**: Singleton  
**DontDestroyOnLoad**: 有効

#### フィールド

| フィールド名 | 型 | アクセス | 説明 |
|-------------|----|---------|----- |
| debugMode | bool | public | デバッグモード有効化 |
| gameSpeed | float | public | ゲームスピード（0.1-2.0） |
| currentState | GameState | public | 現在のゲーム状態 |
| currentWorld | int | public | 現在のワールド番号 |
| currentStage | int | public | 現在のステージ番号 |
| sessionStartTime | float | public | セッション開始時刻 |
| sessionDeathCount | int | public | セッション中の死亡回数 |
| sessionEnergyChips | int | public | セッション中のエナジーチップ数 |
| playerProgress | PlayerProgress | public | プレイヤー進行データ |

#### イベント

| イベント名 | 型 | 説明 |
|-----------|----|----- |
| OnGameStateChanged | System.Action\<GameState\> | ゲーム状態変更時 |
| OnStageChanged | System.Action\<int, int\> | ステージ変更時 |
| OnGameSpeedChanged | System.Action\<float\> | ゲームスピード変更時 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| ChangeGameState(GameState) | void | ゲーム状態変更 |
| SetCurrentStage(int, int) | void | 現在ステージ設定 |
| StartStage(int, int) | void | ステージ開始処理 |
| CompleteStage(float, int, int) | void | ステージクリア処理 |
| SetGameSpeed(float) | void | ゲームスピード設定 |
| PauseGame() | void | ゲーム一時停止 |
| ResumeGame() | void | ゲーム再開 |
| RestartStage() | void | ステージリスタート |

### SaveManager

**継承**: MonoBehaviour  
**パターン**: Singleton  
**DontDestroyOnLoad**: 有効

#### 定数

| 定数名 | 値 | 説明 |
|-------|----|----- |
| SAVE_FILE_NAME | "gravity_flip_save.dat" | セーブファイル名 |
| ENCRYPTION_KEY | "GravityFlipLab2024Key" | 暗号化キー |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| LoadProgress() | PlayerProgress | プレイヤーデータロード |
| SaveProgress(PlayerProgress) | void | プレイヤーデータセーブ |
| DeleteSaveData() | void | セーブデータ削除 |
| CreateNewProgress() | PlayerProgress | 新規プレイヤーデータ作成 |
| ValidateProgressData(PlayerProgress) | bool | データ整合性チェック |

#### 暗号化メソッド（プライベート）

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| EncryptString(string, string) | string | 文字列暗号化 |
| DecryptString(string, string) | string | 文字列復号化 |
| ResizeKey(byte[], int) | byte[] | キーサイズ調整 |

### ConfigManager

**継承**: MonoBehaviour  
**パターン**: Singleton  
**DontDestroyOnLoad**: 有効

#### 設定フィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| defaultGameSpeed | float | 1.0f | デフォルトゲームスピード |
| assistModeSpeedMultiplier | float | 0.8f | アシストモード速度倍率 |
| maxWorlds | int | 5 | 最大ワールド数 |
| stagesPerWorld | int | 10 | ワールドあたりステージ数 |
| baseGravity | float | -9.81f | 基本重力値 |
| gravityFlipDuration | float | 0.1f | 重力反転時間 |
| invincibilityDuration | float | 0.1f | 無敵時間 |
| maxEnergyChipsPerStage | int | 3 | ステージあたり最大エナジーチップ数 |
| assistModeBarrierCount | int | 3 | アシストモードバリア数 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| Initialize() | void | 設定システム初期化 |
| ApplySettings(PlayerSettings) | void | 設定適用 |
| IsStageUnlocked(int, int) | bool | ステージアンロック判定 |
| GetStageKey(int, int) | string | ステージキー生成 |
| ValidateWorldStage(ref int, ref int) | void | ワールド・ステージ値検証 |

### SceneTransitionManager

**継承**: MonoBehaviour  
**パターン**: Singleton  
**DontDestroyOnLoad**: 有効

#### シーン名設定

| フィールド名 | デフォルト値 | 説明 |
|-------------|-------------|----- |
| mainMenuSceneName | "MainMenu" | メインメニューシーン名 |
| stageSelectSceneName | "StageSelect" | ステージ選択シーン名 |
| gameplaySceneName | "Gameplay" | ゲームプレイシーン名 |
| leaderboardSceneName | "Leaderboard" | リーダーボードシーン名 |
| optionsSceneName | "Options" | オプションシーン名 |
| shopSceneName | "Shop" | ショップシーン名 |

#### 遷移設定

| フィールド名 | 型 | デフォルト値 | 説明 |
|-------------|----|-----------|----- |
| transitionDuration | float | 0.5f | 遷移時間 |
| useLoadingScreen | bool | true | ローディング画面使用 |

#### イベント

| イベント名 | 型 | 説明 |
|-----------|----|----- |
| OnSceneLoadStart | System.Action\<string\> | シーンロード開始時 |
| OnSceneLoadComplete | System.Action\<string\> | シーンロード完了時 |

#### メソッド

| メソッド名 | 戻り値 | 説明 |
|-----------|--------|----- |
| LoadScene(SceneType) | void | シーンタイプ指定ロード |
| LoadScene(string) | void | シーン名指定ロード |
| LoadGameplayScene(int, int) | void | ゲームプレイシーンロード |
| ReloadCurrentScene() | void | 現在シーン再ロード |

---

## API リファレンス

### GameManager API

#### インスタンスアクセス

```csharp
GameManager.Instance
```

#### 状態管理

```csharp
// ゲーム状態変更
GameManager.Instance.ChangeGameState(GameState.Gameplay);

// 現在状態取得
GameState currentState = GameManager.Instance.currentState;

// ステージ開始
GameManager.Instance.StartStage(1, 1);

// ステージクリア
GameManager.Instance.CompleteStage(45.5f, 2, 3);
```

#### イベント購読

```csharp
private void OnEnable()
{
    GameManager.OnGameStateChanged += OnGameStateChanged;
    GameManager.OnStageChanged += OnStageChanged;
}

private void OnDisable()
{
    GameManager.OnGameStateChanged -= OnGameStateChanged;
    GameManager.OnStageChanged -= OnStageChanged;
}

private void OnGameStateChanged(GameState newState)
{
    Debug.Log($"Game state changed to: {newState}");
}
```

### SaveManager API

#### データ操作

```csharp
// プレイヤーデータロード
PlayerProgress progress = SaveManager.Instance.LoadProgress();

// プレイヤーデータセーブ
SaveManager.Instance.SaveProgress(progress);

// セーブデータ削除
SaveManager.Instance.DeleteSaveData();
```

### ConfigManager API

#### 設定管理

```csharp
// ステージアンロック判定
bool isUnlocked = ConfigManager.Instance.IsStageUnlocked(2, 5);

// ステージキー取得
string stageKey = ConfigManager.Instance.GetStageKey(1, 3); // "1-3"

// 設定適用
ConfigManager.Instance.ApplySettings(playerSettings);
```

### SceneTransitionManager API

#### シーン遷移

```csharp
// シーンタイプ指定で遷移
SceneTransitionManager.Instance.LoadScene(SceneType.StageSelect);

// シーン名指定で遷移
SceneTransitionManager.Instance.LoadScene("MainMenu");

// ゲームプレイシーンに遷移（ワールド・ステージ指定）
SceneTransitionManager.Instance.LoadGameplayScene(2, 3);

// 現在シーン再ロード
SceneTransitionManager.Instance.ReloadCurrentScene();
```

#### イベント購読

```csharp
private void OnEnable()
{
    SceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
    SceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
}

private void OnSceneLoadStart(string sceneName)
{
    Debug.Log($"Loading scene: {sceneName}");
}
```

---

## 実装ガイド

### 初期セットアップ

1. **空のGameObjectにスクリプトをアタッチ**
   ```csharp
   // GameManager, SaveManager, ConfigManager, SceneTransitionManager
   // を個別のGameObjectに配置、またはすべて一つのオブジェクトに配置
   ```

2. **DontDestroyOnLoadの設定**
   ```csharp
   // 各マネージャーは自動的にDontDestroyOnLoadが適用される
   ```

3. **シーン名の設定**
   ```csharp
   // SceneTransitionManagerでシーン名を適切に設定
   [Header("Scene Configuration")]
   public string mainMenuSceneName = "MainMenu";
   public string stageSelectSceneName = "StageSelect";
   // ... 他のシーン名
   ```

### カスタムイベント実装

```csharp
public class CustomGameEventListener : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleGameStateChange;
        GameManager.OnStageChanged += HandleStageChange;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleGameStateChange;
        GameManager.OnStageChanged -= HandleStageChange;
    }

    private void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Gameplay:
                // ゲームプレイ開始処理
                break;
            case GameState.Paused:
                // 一時停止処理
                break;
            // ... 他の状態
        }
    }

    private void HandleStageChange(int world, int stage)
    {
        Debug.Log($"Stage changed to World {world}, Stage {stage}");
    }
}
```

### カスタム設定の追加

```csharp
// PlayerSettingsクラスに新しい設定を追加
[System.Serializable]
public class PlayerSettings
{
    // 既存の設定...
    
    // カスタム設定の追加例
    public bool customFeatureEnabled = false;
    public float customValue = 1.0f;
    public Color customColor = Color.white;
}

// ConfigManagerで新しい設定を処理
public void ApplyCustomSettings(PlayerSettings settings)
{
    if (settings.customFeatureEnabled)
    {
        // カスタム機能の有効化処理
    }
    
    // カスタム値の適用
    SomeSystem.SetCustomValue(settings.customValue);
}
```

### デバッグ機能の活用

```csharp
// GameManagerのデバッグモードを活用
if (GameManager.Instance.debugMode)
{
    Debug.Log("Debug information");
    OnScreenDebugGUI();
}

private void OnScreenDebugGUI()
{
    GUILayout.Label($"Current State: {GameManager.Instance.currentState}");
    GUILayout.Label($"Current Stage: {GameManager.Instance.currentWorld}-{GameManager.Instance.currentStage}");
    GUILayout.Label($"Session Deaths: {GameManager.Instance.sessionDeathCount}");
}
```

---

## ベストプラクティス

### データ整合性の確保

1. **必須フィールドの初期化**
   ```csharp
   public PlayerProgress CreateNewProgress()
   {
       var progress = new PlayerProgress();
       progress.stageProgress = new Dictionary<string, StageData>();
       progress.bestTimes = new Dictionary<string, float>();
       progress.stageRanks = new Dictionary<string, int>();
       return progress;
   }
   ```

2. **null チェックの実装**
   ```csharp
   public void SafeAccessProgress()
   {
       if (GameManager.Instance?.playerProgress != null)
       {
           // 安全なアクセス
       }
   }
   ```

### パフォーマンス最適化

1. **イベントの適切な登録・解除**
   ```csharp
   private void OnEnable()
   {
       if (GameManager.Instance != null)
           GameManager.OnGameStateChanged += HandleStateChange;
   }

   private void OnDisable()
   {
       if (GameManager.Instance != null)
           GameManager.OnGameStateChanged -= HandleStateChange;
   }
   ```

2. **頻繁な辞書アクセスの最適化**
   ```csharp
   // 悪い例：毎フレーム辞書にアクセス
   if (progress.stageProgress.ContainsKey(stageKey))
   {
       // 処理
   }

   // 良い例：一度取得してキャッシュ
   if (progress.stageProgress.TryGetValue(stageKey, out StageData stageData))
   {
       // 処理
   }
   ```

### エラーハンドリング

1. **セーブ・ロード処理**
   ```csharp
   public PlayerProgress LoadProgressSafely()
   {
       try
       {
           return LoadProgress();
       }
       catch (System.Exception e)
       {
           Debug.LogError($"Failed to load progress: {e.Message}");
           return CreateNewProgress();
       }
   }
   ```

2. **シーン遷移エラー**
   ```csharp
   public void LoadSceneSafely(string sceneName)
   {
       if (string.IsNullOrEmpty(sceneName))
       {
           Debug.LogError("Scene name is null or empty");
           return;
       }

       if (Application.CanStreamedLevelBeLoaded(sceneName))
       {
           LoadScene(sceneName);
       }
       else
       {
           Debug.LogError($"Scene {sceneName} cannot be loaded");
       }
   }
   ```

### セキュリティ対策

1. **セーブデータの暗号化強化**
   ```csharp
   // より強固な暗号化のためのソルト追加
   private string EncryptStringWithSalt(string plainText, string key)
   {
       byte[] salt = new byte[16];
       using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
       {
           rng.GetBytes(salt);
       }
       
       byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
       byte[] keyBytes = Encoding.UTF8.GetBytes(key);
       
       using (Aes aes = Aes.Create())
       {
           aes.Key = DeriveKeyFromPassword(keyBytes, salt, 32);
           aes.IV = salt;
           
           using (ICryptoTransform encryptor = aes.CreateEncryptor())
           {
               byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
               byte[] result = new byte[salt.Length + encryptedBytes.Length];
               Array.Copy(salt, 0, result, 0, salt.Length);
               Array.Copy(encryptedBytes, 0, result, salt.Length, encryptedBytes.Length);
               return Convert.ToBase64String(result);
           }
       }
   }
   ```

2. **データ改ざん検出**
   ```csharp
   public bool ValidateDataIntegrity(PlayerProgress progress)
   {
       // チェックサム計算による整合性確認
       string dataHash = CalculateDataHash(progress);
       return dataHash == progress.dataHash;
   }
   
   private string CalculateDataHash(PlayerProgress progress)
   {
       string dataString = JsonUtility.ToJson(progress);
       using (SHA256 sha256 = SHA256.Create())
       {
           byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataString));
           return Convert.ToBase64String(hashBytes);
       }
   }
   ```

### メモリ管理

1. **イベントリスナーの適切な管理**
   ```csharp
   public class ProperEventManagement : MonoBehaviour
   {
       private System.Action<GameState> stateChangeHandler;
       
       private void Awake()
       {
           stateChangeHandler = OnGameStateChanged;
       }
       
       private void OnEnable()
       {
           GameManager.OnGameStateChanged += stateChangeHandler;
       }
       
       private void OnDisable()
       {
           GameManager.OnGameStateChanged -= stateChangeHandler;
       }
       
       private void OnGameStateChanged(GameState newState)
       {
           // 処理
       }
   }
   ```

2. **一時オブジェクトの管理**
   ```csharp
   // 文字列連結の最適化
   private readonly StringBuilder stringBuilder = new StringBuilder();
   
   public string CreateStageKey(int world, int stage)
   {
       stringBuilder.Clear();
       stringBuilder.Append(world);
       stringBuilder.Append('-');
       stringBuilder.Append(stage);
       return stringBuilder.ToString();
   }
   ```

---

## トラブルシューティング

### よくある問題と解決策

#### 1. セーブデータが読み込めない

**症状**: ゲーム起動時にセーブデータのロードに失敗する

**原因**:
- セーブファイルの破損
- 暗号化キーの不整合
- プラットフォーム間でのデータ形式の違い

**解決策**:
```csharp
public PlayerProgress LoadProgressWithFallback()
{
    try
    {
        // 通常のロード処理
        PlayerProgress progress = LoadProgress();
        
        // データ検証
        if (ValidateProgressData(progress))
        {
            return progress;
        }
        else
        {
            Debug.LogWarning("Save data validation failed, attempting recovery");
            return AttemptDataRecovery();
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Save load failed: {e.Message}");
        return CreateBackupProgress();
    }
}

private PlayerProgress AttemptDataRecovery()
{
    // バックアップファイルからの復旧試行
    string backupPath = SaveFilePath + ".backup";
    if (File.Exists(backupPath))
    {
        try
        {
            string backupData = File.ReadAllText(backupPath);
            string jsonData = DecryptString(backupData, ENCRYPTION_KEY);
            return JsonUtility.FromJson<PlayerProgress>(jsonData);
        }
        catch
        {
            return CreateNewProgress();
        }
    }
    return CreateNewProgress();
}
```

#### 2. シーン遷移が完了しない

**症状**: LoadScene呼び出し後、シーンが切り替わらない

**原因**:
- 存在しないシーン名の指定
- Build Settingsにシーンが追加されていない
- 非同期ロード中の例外

**解決策**:
```csharp
public void LoadSceneWithValidation(string sceneName)
{
    // シーン存在チェック
    if (!IsSceneInBuildSettings(sceneName))
    {
        Debug.LogError($"Scene '{sceneName}' is not in Build Settings");
        return;
    }
    
    // 既にロード中でないかチェック
    if (isLoading)
    {
        Debug.LogWarning("Scene loading is already in progress");
        return;
    }
    
    StartCoroutine(LoadSceneWithErrorHandling(sceneName));
}

private bool IsSceneInBuildSettings(string sceneName)
{
    for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
        string sceneNameFromPath = Path.GetFileNameWithoutExtension(scenePath);
        if (sceneNameFromPath == sceneName)
        {
            return true;
        }
    }
    return false;
}

private IEnumerator LoadSceneWithErrorHandling(string sceneName)
{
    try
    {
        yield return LoadSceneCoroutine(sceneName);
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Scene loading failed: {e.Message}");
        isLoading = false;
        
        // フォールバック処理
        if (sceneName != mainMenuSceneName)
        {
            Debug.Log("Falling back to main menu");
            yield return LoadSceneCoroutine(mainMenuSceneName);
        }
    }
}
```

#### 3. Singletonインスタンスが重複する

**症状**: 複数のマネージャーインスタンスが生成される

**原因**:
- シーン遷移時の初期化タイミング
- DontDestroyOnLoadの設定ミス

**解決策**:
```csharp
public class ImprovedSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (FindObjectsOfType<T>().Length > 1)
                    {
                        Debug.LogError($"[Singleton] Multiple instances of {typeof(T)} found!");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        _instance = singleton.AddComponent<T>();
                        singleton.name = $"(singleton) {typeof(T)}";

                        DontDestroyOnLoad(singleton);
                        Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created.");
                    }
                    else
                    {
                        Debug.Log($"[Singleton] Using instance already created: {_instance.gameObject.name}");
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"[Singleton] Another instance of {typeof(T)} already exists. Destroying this one.");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }
}
```

### デバッグ支援機能

#### 1. 統合デバッグシステム

```csharp
public class GravityFlipLabDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugGUI = true;
    public bool logSystemEvents = true;
    public KeyCode debugToggleKey = KeyCode.F12;
    
    private bool debugPanelVisible = false;
    private Vector2 scrollPosition = Vector2.zero;
    
    private void Update()
    {
        if (Input.GetKeyDown(debugToggleKey))
        {
            debugPanelVisible = !debugPanelVisible;
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugGUI || !debugPanelVisible) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== Gravity Flip Lab Debug ===", EditorStyles.boldLabel);
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        // GameManager情報
        DrawGameManagerInfo();
        
        // SaveManager情報
        DrawSaveManagerInfo();
        
        // パフォーマンス情報
        DrawPerformanceInfo();
        
        // デバッグアクション
        DrawDebugActions();
        
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private void DrawGameManagerInfo()
    {
        GUILayout.Label("--- Game Manager ---", EditorStyles.boldLabel);
        
        if (GameManager.Instance != null)
        {
            GUILayout.Label($"State: {GameManager.Instance.currentState}");
            GUILayout.Label($"World-Stage: {GameManager.Instance.currentWorld}-{GameManager.Instance.currentStage}");
            GUILayout.Label($"Session Deaths: {GameManager.Instance.sessionDeathCount}");
            GUILayout.Label($"Session Chips: {GameManager.Instance.sessionEnergyChips}");
            GUILayout.Label($"Game Speed: {GameManager.Instance.gameSpeed:F2}");
        }
        else
        {
            GUILayout.Label("GameManager: Not Found", EditorStyles.helpBox);
        }
        
        GUILayout.Space(10);
    }
    
    private void DrawSaveManagerInfo()
    {
        GUILayout.Label("--- Save Manager ---", EditorStyles.boldLabel);
        
        if (SaveManager.Instance != null)
        {
            string savePath = Path.Combine(Application.persistentDataPath, "gravity_flip_save.dat");
            bool saveExists = File.Exists(savePath);
            
            GUILayout.Label($"Save File Exists: {saveExists}");
            if (saveExists)
            {
                FileInfo fileInfo = new FileInfo(savePath);
                GUILayout.Label($"Save Size: {fileInfo.Length} bytes");
                GUILayout.Label($"Last Modified: {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}");
            }
        }
        
        GUILayout.Space(10);
    }
    
    private void DrawPerformanceInfo()
    {
        GUILayout.Label("--- Performance ---", EditorStyles.boldLabel);
        
        GUILayout.Label($"FPS: {1f / Time.unscaledDeltaTime:F1}");
        GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024:F1} MB");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
        
        GUILayout.Space(10);
    }
    
    private void DrawDebugActions()
    {
        GUILayout.Label("--- Debug Actions ---", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Complete Current Stage"))
        {
            GameManager.Instance?.CompleteStage(30f, 0, 3);
        }
        
        if (GUILayout.Button("Reset Save Data"))
        {
            SaveManager.Instance?.DeleteSaveData();
            Debug.Log("Save data deleted");
        }
        
        if (GUILayout.Button("Unlock All Stages"))
        {
            UnlockAllStagesDebug();
        }
        
        if (GUILayout.Button("Add 100 Energy Chips"))
        {
            if (GameManager.Instance?.playerProgress != null)
            {
                GameManager.Instance.playerProgress.totalEnergyChips += 100;
                SaveManager.Instance?.SaveProgress(GameManager.Instance.playerProgress);
            }
        }
    }
    
    private void UnlockAllStagesDebug()
    {
        if (GameManager.Instance?.playerProgress == null) return;
        
        var progress = GameManager.Instance.playerProgress;
        
        for (int world = 1; world <= 5; world++)
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                string stageKey = $"{world}-{stage}";
                if (!progress.stageProgress.ContainsKey(stageKey))
                {
                    progress.stageProgress[stageKey] = new StageData();
                }
                progress.stageProgress[stageKey].isCleared = true;
            }
        }
        
        SaveManager.Instance?.SaveProgress(progress);
        Debug.Log("All stages unlocked");
    }
}
```

#### 2. システムヘルスチェック

```csharp
public class SystemHealthMonitor : MonoBehaviour
{
    [Header("Health Check Settings")]
    public float checkInterval = 5f;
    public bool autoRepair = true;
    
    private float lastCheckTime = 0f;
    
    private void Update()
    {
        if (Time.time - lastCheckTime >= checkInterval)
        {
            PerformHealthCheck();
            lastCheckTime = Time.time;
        }
    }
    
    private void PerformHealthCheck()
    {
        List<string> issues = new List<string>();
        
        // GameManager チェック
        if (GameManager.Instance == null)
        {
            issues.Add("GameManager instance is null");
        }
        
        // SaveManager チェック
        if (SaveManager.Instance == null)
        {
            issues.Add("SaveManager instance is null");
        }
        
        // ConfigManager チェック
        if (ConfigManager.Instance == null)
        {
            issues.Add("ConfigManager instance is null");
        }
        
        // SceneTransitionManager チェック
        if (SceneTransitionManager.Instance == null)
        {
            issues.Add("SceneTransitionManager instance is null");
        }
        
        // メモリ使用量チェック
        long memoryUsage = System.GC.GetTotalMemory(false);
        if (memoryUsage > 500 * 1024 * 1024) // 500MB以上
        {
            issues.Add($"High memory usage: {memoryUsage / 1024 / 1024} MB");
            
            if (autoRepair)
            {
                System.GC.Collect();
                Debug.Log("Performed garbage collection");
            }
        }
        
        // FPSチェック
        float fps = 1f / Time.unscaledDeltaTime;
        if (fps < 30f)
        {
            issues.Add($"Low FPS detected: {fps:F1}");
        }
        
        // 問題がある場合のログ出力
        if (issues.Count > 0)
        {
            Debug.LogWarning($"System Health Issues Detected:\n{string.Join("\n", issues)}");
        }
    }
}
```

---

## 拡張性とカスタマイズ

### カスタムゲーム状態の追加

```csharp
// GameState enumを拡張
public enum GameState
{
    MainMenu,
    StageSelect,
    Gameplay,
    Paused,
    GameOver,
    Loading,
    Options,
    Leaderboard,
    Shop,
    
    // カスタム状態の追加
    Tutorial,
    CutScene,
    Multiplayer,
    Achievement
}

// GameManagerでカスタム状態の処理を追加
public class ExtendedGameManager : GameManager
{
    [Header("Extended Features")]
    public bool tutorialEnabled = true;
    public bool multiplayerEnabled = false;
    
    protected override void HandleStateChange(GameState newState)
    {
        base.HandleStateChange(newState);
        
        switch (newState)
        {
            case GameState.Tutorial:
                StartTutorial();
                break;
            case GameState.CutScene:
                StartCutScene();
                break;
            case GameState.Multiplayer:
                InitializeMultiplayer();
                break;
            case GameState.Achievement:
                ShowAchievements();
                break;
        }
    }
    
    private void StartTutorial()
    {
        // チュートリアル開始処理
    }
    
    private void StartCutScene()
    {
        // カットシーン開始処理
    }
    
    private void InitializeMultiplayer()
    {
        // マルチプレイヤー初期化処理
    }
    
    private void ShowAchievements()
    {
        // 実績表示処理
    }
}
```

### カスタムデータフィールドの追加

```csharp
// PlayerProgressを拡張
[System.Serializable]
public class ExtendedPlayerProgress : PlayerProgress
{
    [Header("Extended Progress")]
    public Dictionary<string, bool> achievements = new Dictionary<string, bool>();
    public Dictionary<string, int> statistics = new Dictionary<string, int>();
    public List<string> unlockedSkins = new List<string>();
    public DateTime lastPlayTime = DateTime.Now;
    public int totalPlayTimeSeconds = 0;
    
    // カスタムメソッド
    public void UnlockAchievement(string achievementId)
    {
        if (!achievements.ContainsKey(achievementId))
        {
            achievements[achievementId] = true;
            Debug.Log($"Achievement unlocked: {achievementId}");
        }
    }
    
    public void IncrementStatistic(string statName, int amount = 1)
    {
        if (!statistics.ContainsKey(statName))
        {
            statistics[statName] = 0;
        }
        statistics[statName] += amount;
    }
    
    public bool HasAchievement(string achievementId)
    {
        return achievements.ContainsKey(achievementId) && achievements[achievementId];
    }
}
```

### プラグインシステムの実装

```csharp
// プラグインインターフェース
public interface IGamePlugin
{
    string PluginName { get; }
    string Version { get; }
    void Initialize();
    void OnGameStateChanged(GameState newState);
    void OnStageCompleted(int world, int stage, float time);
    void Shutdown();
}

// プラグインマネージャー
public class PluginManager : MonoBehaviour
{
    private static PluginManager _instance;
    public static PluginManager Instance => _instance;
    
    private List<IGamePlugin> loadedPlugins = new List<IGamePlugin>();
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlugins();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadPlugins()
    {
        // プラグインの動的ロード（アセンブリスキャンなど）
        var pluginTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IGamePlugin).IsAssignableFrom(type) && !type.IsInterface)
            .ToArray();
        
        foreach (var pluginType in pluginTypes)
        {
            try
            {
                var plugin = Activator.CreateInstance(pluginType) as IGamePlugin;
                plugin?.Initialize();
                loadedPlugins.Add(plugin);
                Debug.Log($"Plugin loaded: {plugin.PluginName} v{plugin.Version}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load plugin {pluginType.Name}: {e.Message}");
            }
        }
    }
    
    public void NotifyGameStateChanged(GameState newState)
    {
        foreach (var plugin in loadedPlugins)
        {
            try
            {
                plugin.OnGameStateChanged(newState);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Plugin {plugin.PluginName} error in OnGameStateChanged: {e.Message}");
            }
        }
    }
    
    public void NotifyStageCompleted(int world, int stage, float time)
    {
        foreach (var plugin in loadedPlugins)
        {
            try
            {
                plugin.OnStageCompleted(world, stage, time);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Plugin {plugin.PluginName} error in OnStageCompleted: {e.Message}");
            }
        }
    }
}

// サンプルプラグイン
public class AchievementPlugin : IGamePlugin
{
    public string PluginName => "Achievement System";
    public string Version => "1.0.0";
    
    public void Initialize()
    {
        Debug.Log("Achievement Plugin initialized");
    }
    
    public void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Gameplay)
        {
            // ゲームプレイ開始時の実績チェック
        }
    }
    
    public void OnStageCompleted(int world, int stage, float time)
    {
        // ステージクリア実績のチェック
        CheckSpeedRunAchievements(world, stage, time);
        CheckProgressAchievements(world, stage);
    }
    
    public void Shutdown()
    {
        Debug.Log("Achievement Plugin shutdown");
    }
    
    private void CheckSpeedRunAchievements(int world, int stage, float time)
    {
        if (time <= 30f)
        {
            // スピードラン実績
        }
    }
    
    private void CheckProgressAchievements(int world, int stage)
    {
        if (world == 1 && stage == 1)
        {
            // 初回クリア実績
        }
    }
}
```

---

## 結論

Gravity Flip Lab の基盤システムは、ゲーム開発における重要な要素を包括的にカバーする設計となっています。各システムは独立性を保ちながらも、イベントドリブンなアーキテクチャにより密接に連携し、堅牢で拡張性の高いゲーム基盤を提供します。

### 主要な利点

1. **モジュラー設計**: 各システムが独立しており、個別の修正・拡張が容易
2. **データ保護**: AES暗号化によるセーブデータの安全性確保
3. **パフォーマンス**: 非同期処理による快適なユーザー体験
4. **拡張性**: プラグインシステムや継承による機能拡張が可能
5. **デバッグ支援**: 包括的なデバッグ・監視機能

### 今後の発展

この基盤システムは、以下のような拡張が可能です：

- **クラウド連携**: オンラインセーブデータ同期
- **分析システム**: プレイヤー行動の分析・収集
- **A/Bテスト**: ゲームバランス調整の支援
- **モッディング**: ユーザー作成コンテンツの対応
- **マルチプラットフォーム**: 異なるプラットフォーム間でのデータ共有

このドキュメントを参考に、Gravity Flip Lab の開発を効率的かつ確実に進めることができるでしょう。