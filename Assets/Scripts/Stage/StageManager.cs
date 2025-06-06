using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;
using AddressableManagementSystem;

namespace GravityFlipLab.Stage
{
    #region Stage Data Structures

    #endregion

    #region Stage Manager

    public class StageManager : MonoBehaviour
    {
        private static StageManager _instance;
        public static StageManager Instance
        {
            get
            {
                // OnDestroy中やアプリケーション終了中は新しいインスタンスを作成しない
                if (_instance == null && !isApplicationQuitting)
                {
                    _instance = FindFirstObjectByType<StageManager>();
                    // シーン固有なので、見つからない場合は null を返す（動的生成しない）
                }
                return _instance;
            }
        }

        [Header("Stage Configuration")]
        public StageDataSO currentStageData;
        public Transform obstacleParent;
        public Transform collectibleParent;
        public Transform environmentalParent;

        [Header("Background System")]
        public ParallaxBackgroundManager parallaxManager;
        public CameraController cameraController;

        [Header("Foreground Tilemap")]
        public Tilemap foregroundTilemap;
        public TilemapCollider2D foregroundCollider;

        [Header("Goal Settings")]
        public GameObject goalPrefab; // ゴールプレハブの参照
        private GameObject currentGoal; // 現在のゴールオブジェクト

        [Header("Player Settings")]
        public GameObject playerPrefab; // プレイヤープレハブの参照
        private GameObject currentPlayer; // 現在のプレイヤーオブジェクト

        [Header("Checkpoint Integration")]
        public bool useEnhancedCheckpointSystem = true;
        public GameObject checkpointPrefab;
        public float checkpointSpacing = 512f; // Distance between auto-generated checkpoints

        [Header("Prefab References")]
        public GameObject[] obstaclePrefabs = new GameObject[8]; // One for each ObstacleType
        public GameObject[] collectiblePrefabs = new GameObject[3]; // One for each CollectibleType
        public GameObject[] environmentalPrefabs = new GameObject[4]; // One for each EnvironmentalType

        [Header("Stage State")]
        public bool stageLoaded = false;
        public float stageStartTime;
        public int collectiblesRemaining;

        // Current stage objects
        private List<BaseObstacle> activeObstacles = new List<BaseObstacle>();
        private List<Collectible> activeCollectibles = new List<Collectible>();
        private List<GameObject> environmentalObjects = new List<GameObject>();

        // Stage streaming
        private Queue<GameObject> stageSegmentPool = new Queue<GameObject>();
        private List<GameObject> activeSegments = new List<GameObject>();
        private float segmentWidth = 1024f; // 1024px per segment
        private int maxActiveSegments = 5;

        // Events
        public static event System.Action OnStageLoaded;
        public static event System.Action OnStageCompleted;
        public static event System.Action<int> OnCollectibleCollected;

        [Header("Tilemap System")]
        public TilemapGroundManager tilemapGroundManager;
        public bool useTilemapTerrain = true;
        public LayerMask groundLayerMask = 1;

        // シーン初期化状態の管理
        private bool sceneInitialized = false;
        private bool isInitializing = false;
        private static bool isApplicationQuitting = false;

        [Header("Slope System")]
        public Transform slopeParent;
        public GameObject[] slopePrefabs = new GameObject[8]; // 各SlopeType用

        // 現在アクティブな傾斜オブジェクト
        private List<SlopeObject> activeSlopes = new List<SlopeObject>();

        [Header("Ground Level Integration")]
        private float groundLevelOffset = 0f;


        private void Awake()
        {
            // StageManagerはシーン固有のため、重複チェックのみ行う
            if (_instance != null && _instance != this)
            {
                Debug.Log("StageManager: Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            sceneInitialized = false;
            isInitializing = false;

            Debug.Log("StageManager: Awake called - Scene-specific instance created");
        }

        private void OnDestroy()
        {
            Debug.Log("StageManager: OnDestroy called");

            // OnDestroy中のフラグを設定
            isApplicationQuitting = true;

            // インスタンスのクリア（このインスタンスが破棄される場合のみ）
            if (_instance == this)
            {
                _instance = null;
                Debug.Log("StageManager: Instance reference cleared");
            }

            // StageManagerイベントの登録解除
            OnStageLoaded = null;
            OnStageCompleted = null;
            OnCollectibleCollected = null;

            // 進行中のコルーチンを停止
            StopAllCoroutines();
        }

        private void OnApplicationQuit()
        {
            Debug.Log("StageManager: OnApplicationQuit called");
            isApplicationQuitting = true;
        }

#if UNITY_EDITOR
        // エディター専用：プレイモード終了検知
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("StageManager: Editor play mode exiting");
                isApplicationQuitting = true;

                // インスタンス参照をクリア
                _instance = null;

                // 静的イベントをクリア
                OnStageLoaded = null;
                OnStageCompleted = null;
                OnCollectibleCollected = null;
            }
        }
#endif

        /// <summary>
        /// 手動でのクリーンアップメソッド
        /// </summary>
        public static void ManualCleanup()
        {
            Debug.Log("StageManager: Manual cleanup requested");

            isApplicationQuitting = true;

            // 静的イベントのクリーンアップ
            OnStageLoaded = null;
            OnStageCompleted = null;
            OnCollectibleCollected = null;

            // インスタンス参照のクリア
            _instance = null;
        }

        private void Start()
        {
            Debug.Log("StageManager: Start called");

            // 既に初期化中の場合は重複実行を防ぐ
            if (isInitializing)
            {
                Debug.Log("StageManager: Already initializing, skipping Start");
                return;
            }

            StartCoroutine(InitializeStageManager());
        }

        private void OnEnable()
        {
            Debug.Log("StageManager: OnEnable called");

            // シーン切り替え後の再有効化時にも初期化をチェック
            if (!sceneInitialized && !isInitializing)
            {
                StartCoroutine(InitializeStageManager());
            }
        }

        /// <summary>
        /// StageManagerの完全な初期化プロセス
        /// </summary>
        private IEnumerator InitializeStageManager()
        {
            if (isInitializing)
            {
                Debug.Log("StageManager: Initialization already in progress");
                yield break;
            }

            isInitializing = true;
            Debug.Log("StageManager: Starting initialization process");

            // フレームを待って他のシステムの初期化を待つ
            yield return new WaitForEndOfFrame();

            bool initializationSuccessful = false;

            // 1. 基本コンポーネントの作成・初期化
            try
            {
                CreateParentObjects();
                InitializeComponents();
                Debug.Log("StageManager: Basic components initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StageManager: Failed to initialize basic components - {e.Message}");
                isInitializing = false;
                yield break;
            }

            Debug.Log("StageManager: Waiting for AddressableResourceManager initialization...");
            yield return StartCoroutine(WaitForAddressableInitialization());

            // 2. GameManagerの準備を待つ
            yield return new WaitUntil(() => GameManager.Instance != null);
            Debug.Log("StageManager: GameManager ready");

            // 3. 現在のステージをロード
            try
            {
                LoadCurrentStage();
                initializationSuccessful = true;
                Debug.Log("StageManager: Stage loading initiated");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StageManager: Failed to load stage - {e.Message}");
                initializationSuccessful = false;
            }

            // 4. 初期化完了処理
            if (initializationSuccessful)
            {
                sceneInitialized = true;
                Debug.Log("StageManager: Initialization completed successfully");
            }
            else
            {
                Debug.LogError("StageManager: Initialization failed");
                sceneInitialized = false;
            }

            isInitializing = false;
        }
        /// <summary>
        /// AddressableResourceManagerの初期化を待つ
        /// </summary>
        private IEnumerator WaitForAddressableInitialization()
        {
            var resourceManager = AddressableResourceManager.Instance;

            // 初期化されるまで待機
            float timeout = 10f; // 10秒でタイムアウト
            float elapsedTime = 0f;

            while (!resourceManager.IsInitialized && elapsedTime < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            if (!resourceManager.IsInitialized)
            {
                Debug.LogError("StageManager: AddressableResourceManager initialization timed out!");

                // 手動で初期化を試行
                Debug.Log("StageManager: Attempting manual initialization of AddressableResourceManager...");
                yield return StartCoroutine(InitializeAddressableResourceManager());
            }
            else
            {
                Debug.Log("StageManager: AddressableResourceManager is ready");
            }
        }

        /// <summary>
        /// AddressableResourceManagerを手動で初期化
        /// </summary>
        private IEnumerator InitializeAddressableResourceManager()
        {
            var resourceManager = AddressableResourceManager.Instance;

            // 非同期初期化をコルーチンで実行
            bool initializationComplete = false;
            System.Exception initializationException = null;

            // 初期化タスクを開始
            var initTask = resourceManager.Initialize();

            // タスクの完了を待つ
            while (!initTask.IsCompleted)
            {
                yield return null;
            }

            if (initTask.Exception != null)
            {
                initializationException = initTask.Exception;
                Debug.LogError($"StageManager: Failed to initialize AddressableResourceManager: {initializationException}");
            }
            else
            {
                initializationComplete = true;
                Debug.Log("StageManager: AddressableResourceManager manually initialized successfully");
            }

            if (!initializationComplete)
            {
                Debug.LogError("StageManager: Cannot proceed without AddressableResourceManager");
            }
        }

        /// <summary>
        /// 外部からの強制再初期化メソッド
        /// </summary>
        public void ForceReinitialize()
        {
            Debug.Log("StageManager: Force reinitialization requested");

            // 現在のステージをクリア
            ClearStage();

            // 初期化状態をリセット
            sceneInitialized = false;
            isInitializing = false;

            // 再初期化を開始
            StartCoroutine(InitializeStageManager());
        }

        /// <summary>
        /// シーン読み込み完了時に呼び出されるメソッド
        /// </summary>
        public void OnSceneLoaded()
        {
            Debug.Log("StageManager: Scene loaded notification received");

            if (!sceneInitialized && !isInitializing)
            {
                StartCoroutine(InitializeStageManager());
            }
        }

        private void CreateParentObjects()
        {
            if (obstacleParent == null)
            {
                obstacleParent = new GameObject("Obstacles").transform;
                obstacleParent.SetParent(transform);
            }

            if (collectibleParent == null)
            {
                collectibleParent = new GameObject("Collectibles").transform;
                collectibleParent.SetParent(transform);
            }

            if (environmentalParent == null)
            {
                environmentalParent = new GameObject("Environmental").transform;
                environmentalParent.SetParent(transform);
            }

            if (slopeParent == null)
            {
                slopeParent = new GameObject("Slopes").transform;
                slopeParent.SetParent(transform);
            }

            Debug.Log("StageManager: Parent objects created/verified");
        }

        private void InitializeComponents()
        {
            // Initialize parallax manager
            if (parallaxManager == null)
                parallaxManager = FindFirstObjectByType<ParallaxBackgroundManager>();

            // Initialize camera controller
            if (cameraController == null)
                cameraController = Camera.main?.GetComponent<CameraController>();

            // Initialize foreground tilemap
            if (foregroundTilemap == null)
                foregroundTilemap = GetComponentInChildren<Tilemap>();

            if (foregroundCollider == null)
                foregroundCollider = GetComponentInChildren<TilemapCollider2D>();

            Debug.Log("StageManager: Components initialized");
        }

        public async void LoadStage(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("StageManager: Cannot load null stage data");
                return;
            }

            Debug.Log($"StageManager: Loading stage data: {stageData.name}");

            // StageInfoの整合性チェック（非同期）
            bool isValid = await ValidateStageInfoAsync(stageData);
            if (!isValid)
            {
                Debug.LogError($"StageManager: Stage validation failed for {stageData.name}");
                return;
            }

            currentStageData = stageData;
            StartCoroutine(LoadStageCoroutine());
        }

        /// <summary>
        /// ステージに関連するアセットをプリロード
        /// </summary>
        public async System.Threading.Tasks.Task PreloadStageAssetsAsync(int world, int stage)
        {
            string stageKey = $"Stage{world}-{stage}";

            try
            {
                Debug.Log($"StageManager: Preloading assets for {stageKey}");

                // メインのステージデータをプリロード
                var resourceManager = AddressableResourceManager.Instance;
                var stageKeys = new List<string> { stageKey };
                await resourceManager.PreloadAssetsAsync<StageDataSO>(stageKeys, AddressableResourceManager.LoadPriority.High);

                // ステージ関連のラベルでアセットをプリロード（例：World1, Stage関連）
                string worldLabel = $"World{world}";
                var worldAssets = await AddressableHelper.GetKeysWithLabel(worldLabel);

                if (worldAssets.Count > 0)
                {
                    await resourceManager.PreloadAssetsAsync<UnityEngine.Object>(worldAssets, AddressableResourceManager.LoadPriority.Normal);
                    Debug.Log($"StageManager: Preloaded {worldAssets.Count} world assets for {worldLabel}");
                }

                Debug.Log($"StageManager: Asset preloading completed for {stageKey}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StageManager: Error preloading assets for {stageKey}: {e.Message}");
            }
        }

        /// <summary>
        /// 現在のステージアセットをリリース
        /// </summary>
        public void ReleaseCurrentStageAssets()
        {
            if (currentStageData?.stageInfo == null) return;

            string stageKey = GetAddressableKeyFromStageInfo(currentStageData.stageInfo);

            if (!string.IsNullOrEmpty(stageKey))
            {
                AddressableResourceManager.Instance.ReleaseAsset(stageKey);
                Debug.Log($"StageManager: Released stage assets for {stageKey}");
            }
        }


        /// <summary>
        /// StageInfoの整合性をチェック
        /// </summary>
        private bool ValidateStageInfo(StageDataSO stageData)
        {
            if (stageData?.stageInfo == null)
            {
                Debug.LogError("StageManager: StageInfo is null");
                return false;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("StageManager: GameManager is not available for validation");
                return false;
            }

            var stageInfo = stageData.stageInfo;
            int expectedWorld = GameManager.Instance.currentWorld;
            int expectedStage = GameManager.Instance.currentStage;

            // worldNumberとstageNumberが設定されているかチェック
            if (stageInfo.worldNumber <= 0 || stageInfo.stageNumber <= 0)
            {
                Debug.LogError($"StageManager: Invalid stage numbers in StageInfo - World: {stageInfo.worldNumber}, Stage: {stageInfo.stageNumber}");
                return false;
            }

            // GameManagerの期待値と一致するかチェック
            if (stageInfo.worldNumber != expectedWorld || stageInfo.stageNumber != expectedStage)
            {
                Debug.LogError($"StageManager: Stage number mismatch! " +
                              $"Expected: World {expectedWorld}, Stage {expectedStage}, " +
                              $"Got: World {stageInfo.worldNumber}, Stage {stageInfo.stageNumber}");
                return false;
            }

            Debug.Log($"StageManager: Stage validation passed - World {stageInfo.worldNumber}, Stage {stageInfo.stageNumber}");
            return true;
        }

        public void LoadCurrentStage()
        {
            Debug.Log("StageManager: LoadCurrentStage called");

            if (currentStageData != null)
            {
                Debug.Log($"StageManager: Loading existing stage data: {currentStageData.name}");
                LoadStage(currentStageData);
            }
            else if (GameManager.Instance != null)
            {
                // GameManagerから現在のステージ情報を取得
                Debug.Log($"StageManager: Loading stage from GameManager: World {GameManager.Instance.currentWorld}, Stage {GameManager.Instance.currentStage}");
                LoadStageByNumber(GameManager.Instance.currentWorld, GameManager.Instance.currentStage);
            }
            else
            {
                Debug.LogWarning("StageManager: No stage data available and GameManager is null");
                // デフォルトステージをロード
                LoadStageByNumber(1, 1);
            }
        }

        /// <summary>
        /// StageInfoの値を使用してステージをロード（Addressable対応）
        /// </summary>
        public async void LoadStageByNumber(int world, int stage)
        {
            Debug.Log($"StageManager: Loading stage World {world}, Stage {stage}");

            // StageInfoの値を使用してAddressableキーを構築
            string addressableKey = $"Stage{world}-{stage}";

            try
            {
                // Addressableでキーの存在確認
                bool keyExists = await AddressableHelper.ValidateKeyExists(addressableKey);

                if (!keyExists)
                {
                    Debug.LogError($"StageManager: Addressable key not found: {addressableKey}");
                    Debug.LogError("StageManager: Critical error - Cannot continue without valid stage data");
                    return;
                }

                // AddressableResourceManagerを使用してステージデータをロード
                var resourceManager = AddressableResourceManager.Instance;
                // 初期化されていない場合は初期化を待つ
                if (!resourceManager.IsInitialized)
                {
                    Debug.Log("StageManager: AddressableResourceManager not initialized, waiting for initialization...");
                    await resourceManager.Initialize();
                    Debug.Log("StageManager: AddressableResourceManager initialization completed");
                }

                var stageDataHandle = resourceManager.LoadAssetAsync<StageDataSO>(addressableKey);

                // ハンドルが有効かチェック
                if (!stageDataHandle.IsValid())
                {
                    Debug.LogError($"StageManager: Invalid handle returned for {addressableKey}");
                    return;
                }

                Debug.Log($"StageManager: Handle created, waiting for completion...");

                // AsyncOperationHandle<T>のTaskを待機
                await stageDataHandle.Task;

                if (stageDataHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    StageDataSO stageData = stageDataHandle.Result;
                    Debug.Log($"StageManager: Loaded stage data from Addressables: {addressableKey}");

                    // ロードしたステージデータのStageInfoを検証
                    if (stageData.stageInfo != null)
                    {
                        if (stageData.stageInfo.worldNumber != world || stageData.stageInfo.stageNumber != stage)
                        {
                            Debug.LogError($"StageManager: Critical error - Addressable key and StageInfo mismatch! " +
                                          $"Addressable key: {addressableKey}, " +
                                          $"StageInfo: World {stageData.stageInfo.worldNumber}, Stage {stageData.stageInfo.stageNumber}");
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogError($"StageManager: StageInfo is null in loaded stage data: {addressableKey}");
                        return;
                    }

                    LoadStage(stageData);
                }
                else
                {
                    Debug.LogError($"StageManager: Failed to load stage data from Addressables: {addressableKey}");
                    Debug.LogError("StageManager: Critical error - Cannot continue without valid stage data");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StageManager: Exception while loading stage data: {e.Message}");
                Debug.LogError("StageManager: Critical error - Cannot continue without valid stage data");
            }
        }
        /// <summary>
        /// StageInfoから適切なAddressableキーを生成
        /// </summary>
        public static string GetAddressableKeyFromStageInfo(StageInfo stageInfo)
        {
            if (stageInfo == null) return null;
            return $"Stage{stageInfo.worldNumber}-{stageInfo.stageNumber}";
        }

        /// <summary>
        /// StageInfoの整合性をチェック（Addressable対応）
        /// </summary>
        private async System.Threading.Tasks.Task<bool> ValidateStageInfoAsync(StageDataSO stageData)
        {
            if (stageData?.stageInfo == null)
            {
                Debug.LogError("StageManager: StageInfo is null");
                return false;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("StageManager: GameManager is not available for validation");
                return false;
            }

            var stageInfo = stageData.stageInfo;
            int expectedWorld = GameManager.Instance.currentWorld;
            int expectedStage = GameManager.Instance.currentStage;

            // worldNumberとstageNumberが設定されているかチェック
            if (stageInfo.worldNumber <= 0 || stageInfo.stageNumber <= 0)
            {
                Debug.LogError($"StageManager: Invalid stage numbers in StageInfo - World: {stageInfo.worldNumber}, Stage: {stageInfo.stageNumber}");
                return false;
            }

            // GameManagerの期待値と一致するかチェック
            if (stageInfo.worldNumber != expectedWorld || stageInfo.stageNumber != expectedStage)
            {
                Debug.LogError($"StageManager: Stage number mismatch! " +
                              $"Expected: World {expectedWorld}, Stage {expectedStage}, " +
                              $"Got: World {stageInfo.worldNumber}, Stage {stageInfo.stageNumber}");
                return false;
            }

            // Addressableキーの存在確認
            string expectedAddressableKey = GetAddressableKeyFromStageInfo(stageInfo);
            bool keyExists = await AddressableHelper.ValidateKeyExists(expectedAddressableKey);

            if (!keyExists)
            {
                Debug.LogError($"StageManager: Addressable key does not exist: {expectedAddressableKey}");
                return false;
            }

            Debug.Log($"StageManager: Stage validation passed - World {stageInfo.worldNumber}, Stage {stageInfo.stageNumber}, Key: {expectedAddressableKey}");
            return true;
        }

        /// <summary>
        /// 次のステージをプリロード（パフォーマンス向上のため）
        /// </summary>
        public async void PreloadNextStage()
        {
            if (GameManager.Instance == null || currentStageData?.stageInfo == null) return;

            int currentWorld = currentStageData.stageInfo.worldNumber;
            int currentStage = currentStageData.stageInfo.stageNumber;

            // 次のステージを計算
            int nextWorld = currentWorld;
            int nextStage = currentStage + 1;

            // ワールドの境界チェック
            if (nextStage > ConfigManager.Instance.stagesPerWorld)
            {
                nextWorld++;
                nextStage = 1;

                if (nextWorld > ConfigManager.Instance.maxWorlds)
                {
                    Debug.Log("StageManager: No next stage to preload (reached end)");
                    return;
                }
            }

            // 次のステージが解放されているかチェック
            if (ConfigManager.Instance.IsStageUnlocked(nextWorld, nextStage))
            {
                await PreloadStageAssetsAsync(nextWorld, nextStage);
            }
        }

        /// <summary>
        /// 現在ロードされているステージの情報を取得
        /// </summary>
        public StageInfo GetCurrentStageInfo()
        {
            return currentStageData?.stageInfo;
        }

        /// <summary>
        /// 現在ロードされているステージのワールド番号を取得
        /// </summary>
        public int GetCurrentWorldNumber()
        {
            return currentStageData?.stageInfo?.worldNumber ?? 0;
        }

        /// <summary>
        /// 現在ロードされているステージのステージ番号を取得
        /// </summary>
        public int GetCurrentStageNumber()
        {
            return currentStageData?.stageInfo?.stageNumber ?? 0;
        }

        /// <summary>
        /// デフォルトのステージデータを作成（リソースが見つからない場合のフォールバック）
        /// </summary>
        private void CreateDefaultStageData(int world, int stage)
        {
            Debug.Log($"StageManager: Creating default stage data for World {world}, Stage {stage}");

            // 基本的なStageDataSOをコードで作成
            var defaultStageData = ScriptableObject.CreateInstance<StageDataSO>();

            // StageInfo初期化
            defaultStageData.stageInfo = new StageInfo
            {
                worldNumber = world,
                stageNumber = stage,
                stageName = $"World {world} - Stage {stage}",
                timeLimit = 300f,
                energyChipCount = 3,
                playerStartPosition = new Vector3(0f, 0f, 0f),
                goalPosition = new Vector3(50f, 0f, 0f),
                checkpointPositions = new List<Vector3>(),
                theme = StageTheme.Tech,
                stageLength = 4096f,
                stageHeight = 1024f,
                segmentCount = 16
            };

            // 基本的な背景レイヤー設定
            defaultStageData.backgroundLayers = new BackgroundLayerData[3];
            for (int i = 0; i < 3; i++)
            {
                defaultStageData.backgroundLayers[i] = new BackgroundLayerData
                {
                    layerName = $"Background Layer {i}",
                    parallaxFactor = 0.25f + (i * 0.25f),
                    tileSize = new Vector2(512, 512),
                    enableVerticalLoop = false,
                    tintColor = Color.white
                };
            }

            // 空のオブスタクル・コレクティブルリスト
            defaultStageData.obstacles = new List<ObstacleData>();
            defaultStageData.collectibles = new List<CollectibleData>();
            defaultStageData.environmental = new List<EnvironmentalData>();

            currentStageData = defaultStageData;
            StartCoroutine(LoadStageCoroutine());
        }
        private void CalculateGroundLevelOffset()
        {
            groundLevelOffset = 0f;

            if (useTilemapTerrain && tilemapGroundManager != null)
            {
                float groundLevel = tilemapGroundManager.groundLevel;
                float thickness = tilemapGroundManager.groundThickness;

                // 実測値に基づく計算式
                // groundLevel=-10, thickness=4 で着地位置=-6.4 の関係から
                // 着地位置 = groundLevel + thickness + 0.6
                float estimatedLandingY = groundLevel + thickness - 0.4f;

                // y座標0を地面表面（着地位置）に合わせるためのオフセット
                groundLevelOffset = estimatedLandingY;

                Debug.Log($"StageManager: Ground level={groundLevel}, thickness={thickness}");
                Debug.Log($"StageManager: Estimated landing position={estimatedLandingY}, offset={groundLevelOffset}");
            }
            else
            {
                Debug.Log("StageManager: No tilemap terrain system, using default offset (0)");
            }
        }
        /// <summary>
        /// 位置にグラウンドレベルオフセットを適用
        /// </summary>
        private Vector3 ApplyGroundLevelOffset(Vector3 originalPosition)
        {
            return new Vector3(originalPosition.x, originalPosition.y + groundLevelOffset, originalPosition.z);
        }
        private IEnumerator LoadStageCoroutine()
        {
            Debug.Log("StageManager: Starting stage load coroutine");

            // Clear existing stage
            ClearStage();

            yield return new WaitForEndOfFrame();

            CalculateGroundLevelOffset();

            // Setup background layers
            yield return StartCoroutine(SetupBackground());

            // Setup camera boundaries
            SetupCamera();

            // Load obstacles
            yield return StartCoroutine(LoadObstacles());

            // Load collectibles
            yield return StartCoroutine(LoadCollectibles());

            // Load environmental objects
            yield return StartCoroutine(LoadEnvironmental());

            yield return StartCoroutine(LoadSlopes());

            // Setup goal
            SetupGoal();

            // Setup player
            SetupPlayer();

            //yield return StartCoroutine(SetupTerrain());

            // Initialize stage
            InitializeStage();

            stageLoaded = true;
            OnStageLoaded?.Invoke();

            Debug.Log($"StageManager: Stage load completed successfully - {currentStageData.stageInfo.stageName}");
        }

        // ゴールセットアップメソッド
        private void SetupGoal()
        {
            if (currentStageData?.stageInfo == null)
            {
                Debug.LogWarning("StageManager: Cannot setup goal - no stage data or stage info");
                return;
            }

            Vector3 goalPosition = ApplyGroundLevelOffset(currentStageData.stageInfo.goalPosition);
            Debug.Log($"StageManager: Setting up goal at position: {goalPosition}");

            // ゴールプレハブが設定されている場合
            if (goalPrefab != null)
            {
                currentGoal = Instantiate(goalPrefab);
                currentGoal.transform.position = goalPosition;
                currentGoal.name = "StageGoal";

                // GoalTriggerコンポーネントの取得と設定
                GoalTrigger goalTrigger = currentGoal.GetComponent<GoalTrigger>();
                if (goalTrigger != null)
                {
                    goalTrigger.SetGoalPosition(goalPosition);
                }

                Debug.Log($"StageManager: Goal created from prefab at position: {goalPosition}");
            }
            else
            {
                // プレハブが設定されていない場合、シンプルなゴールを作成
                CreateDefaultGoal(goalPosition);
            }
        }

        // デフォルトゴール作成メソッド
        private void CreateDefaultGoal(Vector3 position)
        {
            Debug.Log($"StageManager: Creating default goal at position: {position}");

            currentGoal = new GameObject("StageGoal");
            currentGoal.transform.position = position;

            // ビジュアルコンポーネントの追加
            SpriteRenderer renderer = currentGoal.AddComponent<SpriteRenderer>();
            renderer.color = Color.yellow;
            renderer.sprite = CreateDefaultGoalSprite();

            // コライダーの追加
            BoxCollider2D collider = currentGoal.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2f, 3f);

            // ライトエフェクトの追加
            Light goalLight = currentGoal.AddComponent<Light>();
            goalLight.type = LightType.Point;
            goalLight.color = Color.yellow;
            goalLight.intensity = 2f;
            goalLight.range = 5f;

            // GoalTriggerコンポーネントの追加
            GoalTrigger goalTrigger = currentGoal.AddComponent<GoalTrigger>();
            goalTrigger.goalRenderer = renderer;
            goalTrigger.goalLight = goalLight;
            goalTrigger.SetGoalPosition(position);

            Debug.Log($"StageManager: Default goal created successfully at position: {position}");
        }

        // プレイヤーセットアップメソッド
        private void SetupPlayer()
        {
            if (currentStageData?.stageInfo == null)
            {
                Debug.LogWarning("StageManager: Cannot setup player - no stage data or stage info");
                return;
            }

            Vector3 playerStartPosition = ApplyGroundLevelOffset(currentStageData.stageInfo.playerStartPosition);
            Debug.Log($"StageManager: Setting up player at position: {playerStartPosition}");

            // 既存のプレイヤーがあれば削除
            if (currentPlayer != null)
            {
                DestroyImmediate(currentPlayer);
                currentPlayer = null;
            }

            // シーン内の既存プレイヤーもクリア
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                DestroyImmediate(existingPlayer);
            }

            // プレイヤープレハブが設定されている場合
            if (playerPrefab != null)
            {
                currentPlayer = Instantiate(playerPrefab);
                currentPlayer.transform.position = playerStartPosition;
                currentPlayer.name = "Player";
                currentPlayer.tag = "Player";

                // プレイヤーコンポーネントの初期化
                InitializePlayerComponents(currentPlayer);

                Debug.Log($"StageManager: Player created from prefab at position: {playerStartPosition}");
            }
            else
            {
                // プレハブが設定されていない場合、シンプルなプレイヤーを作成
                CreateDefaultPlayer(playerStartPosition);
            }
        }

        /// <summary>
        /// プレイヤーコンポーネントの適切な初期化
        /// </summary>
        private void InitializePlayerComponents(GameObject player)
        {
            if (player == null) return;

            Debug.Log("StageManager: Initializing player components");

            // PlayerControllerの取得
            var playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("StageManager: Player prefab does not have PlayerController component");
                return;
            }

            // PlayerMovementの初期化
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.Initialize(playerController);
                Debug.Log("StageManager: PlayerMovement initialized");
            }

            // RespawnIntegrationの追加と初期化
            var respawnIntegration = player.GetComponent<RespawnIntegration>();
            if (respawnIntegration == null)
            {
                respawnIntegration = player.AddComponent<RespawnIntegration>();
                Debug.Log("StageManager: RespawnIntegration component added");
            }

            // AdvancedGroundDetectorの初期化
            var groundDetector = player.GetComponent<AdvancedGroundDetector>();
            if (groundDetector != null)
            {
                groundDetector.ForceDetection();
            }

            // その他のコンポーネントの検証
            ValidatePlayerComponents(player);
        }

        /// <summary>
        /// プレイヤーコンポーネントの検証
        /// </summary>
        private void ValidatePlayerComponents(GameObject player)
        {
            var requiredComponents = new System.Type[]
            {
                typeof(PlayerController),
                typeof(Rigidbody2D),
                typeof(Collider2D),
                typeof(GravityFlipLab.Physics.GravityAffectedObject)
            };

            foreach (var componentType in requiredComponents)
            {
                if (player.GetComponent(componentType) == null)
                {
                    Debug.LogWarning($"StageManager: Player is missing required component: {componentType.Name}");
                }
            }

            // PlayerMovementの状態検証
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                if (!playerMovement.ValidateComponentState())
                {
                    Debug.LogWarning("StageManager: PlayerMovement component validation failed, attempting to fix...");
                    var playerController = player.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerMovement.SafeReinitialize(playerController);
                    }
                }
            }
        }

        // デフォルトプレイヤー作成メソッド
        private void CreateDefaultPlayer(Vector3 position)
        {
            Debug.Log($"StageManager: Creating default player at position: {position}");

            currentPlayer = new GameObject("Player");
            currentPlayer.transform.position = position;
            currentPlayer.tag = "Player";

            // 基本的なビジュアルコンポーネント
            SpriteRenderer renderer = currentPlayer.AddComponent<SpriteRenderer>();
            renderer.color = Color.blue;
            renderer.sprite = CreateDefaultPlayerSprite();

            // 物理コンポーネント
            Rigidbody2D rb = currentPlayer.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 1f;

            // コライダー
            BoxCollider2D collider = currentPlayer.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);

            // GravityAffectedObject（必須）
            var gravityAffected = currentPlayer.AddComponent<GravityFlipLab.Physics.GravityAffectedObject>();

            // プレイヤーコントローラー
            var playerController = currentPlayer.AddComponent<GravityFlipLab.Player.PlayerController>();

            // PlayerMovement（必須）
            var playerMovement = currentPlayer.AddComponent<PlayerMovement>();

            // RespawnIntegration
            var respawnIntegration = currentPlayer.AddComponent<RespawnIntegration>();

            // コンポーネントの初期化
            InitializePlayerComponents(currentPlayer);

            Debug.Log($"StageManager: Default player created and initialized at position: {position}");
        }

        // デフォルトプレイヤースプライト作成
        private Sprite CreateDefaultPlayerSprite()
        {
            // 16x32の青い四角形テクスチャを作成（プレイヤー形状）
            Texture2D texture = new Texture2D(16, 32);
            Color[] pixels = new Color[16 * 32];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.blue;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 16, 32), new Vector2(0.5f, 0.5f), 16f);
        }

        private Sprite CreateDefaultGoalSprite()
        {
            // 32x32の黄色い四角形テクスチャを作成
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.yellow;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 16f);
        }

        private IEnumerator SetupBackground()
        {
            if (parallaxManager == null || currentStageData.backgroundLayers == null)
            {
                Debug.Log("StageManager: Skipping background setup - parallax manager or background layers not available");
                yield break;
            }

            Debug.Log("StageManager: Setting up background layers");

            // Configure parallax layers based on stage data
            for (int i = 0; i < currentStageData.backgroundLayers.Length && i < parallaxManager.parallaxLayers.Length; i++)
            {
                var layerData = currentStageData.backgroundLayers[i];
                var parallaxLayer = parallaxManager.parallaxLayers[i];

                if (layerData.backgroundSprite != null && parallaxLayer.spriteRenderer != null)
                {
                    parallaxLayer.spriteRenderer.sprite = layerData.backgroundSprite;
                    parallaxLayer.parallaxFactor = layerData.parallaxFactor;
                    parallaxLayer.textureScale = layerData.tileSize;
                    parallaxLayer.spriteRenderer.color = layerData.tintColor;
                    parallaxLayer.enableVerticalParallax = layerData.enableVerticalLoop;

                    // Setup material for UV scrolling
                    Material material = new Material(parallaxLayer.spriteRenderer.material);
                    material.mainTexture = layerData.backgroundSprite.texture;
                    parallaxLayer.spriteRenderer.material = material;
                }

                yield return null;
            }

            Debug.Log("StageManager: Background setup completed");
        }

        private void SetupCamera()
        {
            if (cameraController != null && currentStageData != null)
            {
                // Set camera boundaries based on stage size
                cameraController.SetBoundaries(
                    0f, // Left boundary
                    currentStageData.stageInfo.stageLength, // Right boundary  
                    currentStageData.stageInfo.stageHeight * 0.5f, // Top boundary
                    -currentStageData.stageInfo.stageHeight * 0.5f // Bottom boundary
                );

                Debug.Log("StageManager: Camera boundaries set");
            }
        }

        private IEnumerator LoadObstacles()
        {
            Debug.Log($"StageManager: Loading {currentStageData.obstacles.Count} obstacles");

            foreach (var obstacleData in currentStageData.obstacles)
            {
                GameObject prefab = GetObstaclePrefab(obstacleData.type);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, obstacleParent);
                    instance.transform.position = ApplyGroundLevelOffset(obstacleData.position);
                    instance.transform.rotation = Quaternion.Euler(obstacleData.rotation);
                    instance.transform.localScale = obstacleData.scale;

                    BaseObstacle obstacle = instance.GetComponent<BaseObstacle>();
                    if (obstacle != null)
                    {
                        obstacle.Initialize(obstacleData);
                        activeObstacles.Add(obstacle);
                    }
                }
                yield return null; // Spread loading across frames
            }
        }

        private IEnumerator LoadCollectibles()
        {
            collectiblesRemaining = 0;

            Debug.Log($"StageManager: Loading {currentStageData.collectibles.Count} collectibles");

            foreach (var collectibleData in currentStageData.collectibles)
            {
                GameObject prefab = GetCollectiblePrefab(collectibleData.type);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, collectibleParent);
                    instance.transform.position = ApplyGroundLevelOffset(collectibleData.position);

                    Collectible collectible = instance.GetComponent<Collectible>();
                    if (collectible != null)
                    {
                        collectible.Initialize(collectibleData);
                        activeCollectibles.Add(collectible);
                        collectiblesRemaining++;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator LoadEnvironmental()
        {
            Debug.Log($"StageManager: Loading {currentStageData.environmental.Count} environmental objects");

            foreach (var envData in currentStageData.environmental)
            {
                GameObject prefab = GetEnvironmentalPrefab(envData.type);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, environmentalParent);
                    instance.transform.position = ApplyGroundLevelOffset(envData.position);
                    instance.transform.localScale = envData.scale;

                    // Apply environmental-specific parameters
                    ApplyEnvironmentalParameters(instance, envData);
                    environmentalObjects.Add(instance);
                }
                yield return null;
            }
        }
        private IEnumerator LoadSlopes()
        {
            Debug.Log($"StageManager: Loading {currentStageData.GetSlopeCount()} slopes");

            var slopes = currentStageData.GetSlopes();
            foreach (var slopeData in slopes)
            {
                if (slopeData != null)
                {
                    GameObject prefab = GetSlopePrefab(slopeData.type);
                    if (prefab != null)
                    {
                        GameObject instance = Instantiate(prefab, slopeParent);
                        instance.transform.position = ApplyGroundLevelOffset(slopeData.position);
                        instance.transform.rotation = Quaternion.Euler(slopeData.rotation);
                        instance.transform.localScale = slopeData.scale;

                        SlopeObject slopeObject = instance.GetComponent<SlopeObject>();
                        if (slopeObject != null)
                        {
                            ApplySlopeDataToObject(slopeObject, slopeData);
                            activeSlopes.Add(slopeObject);
                        }
                        else
                        {
                            // プレハブにSlopeObjectコンポーネントがない場合は追加
                            slopeObject = instance.AddComponent<SlopeObject>();
                            ApplySlopeDataToObject(slopeObject, slopeData);
                            activeSlopes.Add(slopeObject);
                        }
                    }
                    else
                    {
                        // プレハブが設定されていない場合はデフォルト傾斜を作成
                        CreateDefaultSlope(slopeData);
                    }
                }
                yield return null; // フレーム分散
            }

            Debug.Log($"StageManager: Loaded {activeSlopes.Count} slope objects");
        }

        // 傾斜データをSlopeObjectに適用
        private void ApplySlopeDataToObject(SlopeObject slopeObject, SlopeData slopeData)
        {
            if (slopeObject == null || slopeData == null) return;

            // 基本設定の適用
            var settings = slopeObject.GetSlopeSettings();
            settings.slopeAngle = slopeData.slopeAngle;
            settings.slopeDirection = slopeData.slopeDirection;
            settings.slopeLength = slopeData.slopeLength;
            settings.speedMultiplier = slopeData.speedMultiplier;
            settings.affectGravity = slopeData.affectGravity;
            settings.gravityRedirection = slopeData.gravityRedirection;

            slopeObject.SetSlopeSettings(settings);

            // 特殊パラメータの適用
            ApplySpecialSlopeParameters(slopeObject, slopeData);
        }

        // 特殊傾斜パラメータの適用
        private void ApplySpecialSlopeParameters(SlopeObject slopeObject, SlopeData slopeData)
        {
            switch (slopeData.type)
            {
                case SlopeType.SpringSlope:
                    var springComponent = slopeObject.GetComponent<SpringSlopeEffect>();
                    if (springComponent == null)
                        springComponent = slopeObject.gameObject.AddComponent<SpringSlopeEffect>();

                    float bounceForce = slopeData.GetParameter("bounceForce", 15f);
                    springComponent.bounceForce = bounceForce;
                    break;

                case SlopeType.IceSlope:
                    var iceComponent = slopeObject.GetComponent<IceSlopeEffect>();
                    if (iceComponent == null)
                        iceComponent = slopeObject.gameObject.AddComponent<IceSlopeEffect>();

                    float friction = slopeData.GetParameter("friction", 0.1f);
                    iceComponent.friction = friction;
                    break;

                case SlopeType.RoughSlope:
                    var roughComponent = slopeObject.GetComponent<RoughSlopeEffect>();
                    if (roughComponent == null)
                        roughComponent = slopeObject.gameObject.AddComponent<RoughSlopeEffect>();

                    float roughFriction = slopeData.GetParameter("friction", 2.0f);
                    roughComponent.friction = roughFriction;
                    break;

                case SlopeType.GravitySlope:
                    var gravityComponent = slopeObject.GetComponent<GravitySlopeEffect>();
                    if (gravityComponent == null)
                        gravityComponent = slopeObject.gameObject.AddComponent<GravitySlopeEffect>();

                    float gravityMultiplier = slopeData.GetParameter("gravityMultiplier", 2.0f);
                    gravityComponent.gravityMultiplier = gravityMultiplier;
                    break;

                case SlopeType.WindSlope:
                    var windComponent = slopeObject.GetComponent<WindSlopeEffect>();
                    if (windComponent == null)
                        windComponent = slopeObject.gameObject.AddComponent<WindSlopeEffect>();

                    Vector2 windDirection = slopeData.GetParameter("windDirection", Vector2.right);
                    float windForce = slopeData.GetParameter("windForce", 10f);
                    windComponent.windDirection = windDirection;
                    windComponent.windForce = windForce;
                    break;
            }
        }

        // デフォルト傾斜の作成
        private void CreateDefaultSlope(SlopeData slopeData)
        {
            GameObject defaultSlope = new GameObject($"Slope_{slopeData.type}_{slopeData.position.x}_{slopeData.position.y}");
            defaultSlope.transform.SetParent(slopeParent);
            defaultSlope.transform.position = ApplyGroundLevelOffset(slopeData.position);
            defaultSlope.transform.rotation = Quaternion.Euler(slopeData.rotation);
            defaultSlope.transform.localScale = slopeData.scale;

            // 基本的なビジュアルコンポーネント
            SpriteRenderer renderer = defaultSlope.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateDefaultSlopeSprite(slopeData.type);
            renderer.color = GetSlopeTypeColor(slopeData.type);

            // コライダーの追加
            BoxCollider2D collider = defaultSlope.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(slopeData.slopeLength, 1f);

            // SlopeObjectコンポーネントの追加
            SlopeObject slopeObject = defaultSlope.AddComponent<SlopeObject>();
            ApplySlopeDataToObject(slopeObject, slopeData);

            activeSlopes.Add(slopeObject);

            Debug.Log($"StageManager: Created default slope: {slopeData.type} at {slopeData.position}");
        }

        // 傾斜プレハブの取得
        private GameObject GetSlopePrefab(SlopeType type)
        {
            int index = (int)type;
            return index < slopePrefabs.Length ? slopePrefabs[index] : null;
        }

        // デフォルト傾斜スプライトの作成
        private Sprite CreateDefaultSlopeSprite(SlopeType type)
        {
            // 16x64の傾斜形状テクスチャを作成
            Texture2D texture = new Texture2D(64, 16);
            Color[] pixels = new Color[64 * 16];
            Color slopeColor = GetSlopeTypeColor(type);

            // 三角形の傾斜形状を作成
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    // 傾斜の形状を決定
                    float slopeHeight = (x / 64f) * 16f;
                    if (y <= slopeHeight)
                    {
                        pixels[y * 64 + x] = slopeColor;
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 64, 16), new Vector2(0.5f, 0f), 16f);
        }

        // 傾斜タイプに応じた色の取得
        private Color GetSlopeTypeColor(SlopeType type)
        {
            switch (type)
            {
                case SlopeType.BasicSlope: return Color.gray;
                case SlopeType.SteepSlope: return Color.red;
                case SlopeType.GentleSlope: return Color.green;
                case SlopeType.SpringSlope: return Color.yellow;
                case SlopeType.IceSlope: return Color.cyan;
                case SlopeType.RoughSlope: return Color.blue;
                case SlopeType.GravitySlope: return Color.magenta;
                case SlopeType.WindSlope: return Color.white;
                default: return Color.gray;
            }
        }
        public List<SlopeObject> GetActiveSlopes()
        {
            return new List<SlopeObject>(activeSlopes);
        }

        public SlopeObject GetSlopeAtPosition(Vector3 position, float tolerance = 1f)
        {
            foreach (var slope in activeSlopes)
            {
                if (slope != null && Vector3.Distance(slope.transform.position, position) <= tolerance)
                {
                    return slope;
                }
            }
            return null;
        }

        public List<SlopeObject> GetSlopesByType(SlopeType type)
        {
            List<SlopeObject> result = new List<SlopeObject>();
            foreach (var slope in activeSlopes)
            {
                if (slope != null && slope.GetSlopeSettings().slopeDirection.ToString().Contains(type.ToString()))
                {
                    result.Add(slope);
                }
            }
            return result;
        }

        public int GetActiveSlopeCount()
        {
            return activeSlopes.Count;
        }
        // 動的傾斜操作メソッド
        public SlopeObject AddSlopeAtPosition(Vector3 position, SlopeType type, float angle = 30f, SlopeDirection direction = SlopeDirection.Ascending)
        {
            var slopeData = new SlopeData(type, position, angle, direction);

            GameObject prefab = GetSlopePrefab(type);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, slopeParent);
            }
            else
            {
                // デフォルト傾斜を作成
                CreateDefaultSlope(slopeData);
                return activeSlopes[activeSlopes.Count - 1];
            }

            instance.transform.position = position;

            SlopeObject slopeObject = instance.GetComponent<SlopeObject>();
            if (slopeObject == null)
                slopeObject = instance.AddComponent<SlopeObject>();

            ApplySlopeDataToObject(slopeObject, slopeData);
            activeSlopes.Add(slopeObject);

            return slopeObject;
        }

        public bool RemoveSlopeAtPosition(Vector3 position, float tolerance = 1f)
        {
            SlopeObject slope = GetSlopeAtPosition(position, tolerance);
            if (slope != null)
            {
                activeSlopes.Remove(slope);
                DestroyImmediate(slope.gameObject);
                return true;
            }
            return false;
        }

        private void InitializeStage()
        {
            stageStartTime = Time.time;

            // Initialize player position and setup enhanced checkpoint system
            if (currentPlayer != null)
            {
                Vector3 playerStartPos = ApplyGroundLevelOffset(currentStageData.stageInfo.playerStartPosition);
                currentPlayer.transform.position = playerStartPos;

                // Set initial checkpoint with enhanced system
                if (useEnhancedCheckpointSystem && CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(playerStartPos, GravityDirection.Down, true);
                }

                // Set camera target
                if (cameraController != null)
                {
                    cameraController.SetTarget(currentPlayer.transform);
                }
            }

            // Set up checkpoints after player is positioned
            SetupCheckpoints();

            // Initialize all obstacles
            foreach (var obstacle in activeObstacles)
            {
                obstacle.StartObstacle();
            }

            Debug.Log($"StageManager: Stage initialized with {(useEnhancedCheckpointSystem ? "enhanced" : "basic")} checkpoint system");
        }

        // Enhanced checkpoint setup method
        private void SetupCheckpoints()
        {
            if (useEnhancedCheckpointSystem)
            {
                SetupEnhancedCheckpoints();
            }
            else
            {
                SetupBasicCheckpoints();
            }
        }

        private void SetupEnhancedCheckpoints()
        {
            // Reset checkpoint manager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.ResetToDefaultCheckpoint();
            }

            // Create checkpoints from stage data
            if (currentStageData?.stageInfo?.checkpointPositions != null && currentStageData.stageInfo.checkpointPositions.Count > 0)
            {
                foreach (var checkpointPos in currentStageData.stageInfo.checkpointPositions)
                {
                    CreateEnhancedCheckpoint(checkpointPos);
                }

                Debug.Log($"StageManager: Setup {currentStageData.stageInfo.checkpointPositions.Count} enhanced checkpoints from stage data");
            }
            else
            {
                // Auto-generate checkpoints if none defined
                GenerateAutoCheckpoints();
            }
        }

        private void SetupBasicCheckpoints()
        {
            // Enhanced version of existing basic checkpoint setup
            CheckpointManager.Instance.ResetToDefaultCheckpoint();

            if (currentStageData?.stageInfo?.checkpointPositions != null)
            {
                foreach (var checkpointPos in currentStageData.stageInfo.checkpointPositions)
                {
                    CreateBasicCheckpoint(checkpointPos);
                }
            }
        }

        private void CreateEnhancedCheckpoint(Vector3 position)
        {
            Vector3 adjustedPosition = ApplyGroundLevelOffset(position);
            GameObject checkpoint;

            if (checkpointPrefab != null)
            {
                checkpoint = Instantiate(checkpointPrefab, adjustedPosition, Quaternion.identity);
            }
            else
            {
                checkpoint = new GameObject("Enhanced Checkpoint");
                checkpoint.transform.position = adjustedPosition;

                // Add visual components
                CreateCheckpointVisuals(checkpoint);
            }

            // Ensure CheckpointTrigger component
            var checkpointTrigger = checkpoint.GetComponent<CheckpointTrigger>();
            if (checkpointTrigger == null)
            {
                checkpointTrigger = checkpoint.AddComponent<CheckpointTrigger>();
            }

            // Configure trigger
            checkpointTrigger.checkpointPosition = adjustedPosition;
            checkpointTrigger.useTransformPosition = true;
            checkpointTrigger.saveGravityState = true;
            checkpointTrigger.requireGrounded = false; // Allow air checkpoints

            // Add collider if needed
            if (checkpoint.GetComponent<Collider2D>() == null)
            {
                var collider = checkpoint.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = Vector2.one * 2f; // Larger trigger area
            }

            // Set layer
            checkpoint.layer = LayerMask.NameToLayer("Checkpoint");

            // Parent to environmental container
            if (environmentalParent != null)
            {
                checkpoint.transform.SetParent(environmentalParent);
            }
        }

        private void CreateBasicCheckpoint(Vector3 position)
        {
            GameObject checkpoint = new GameObject("Checkpoint");
            checkpoint.transform.position = ApplyGroundLevelOffset(position);
            checkpoint.layer = LayerMask.NameToLayer("Checkpoint");

            BoxCollider2D trigger = checkpoint.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = Vector2.one;

            CheckpointTrigger triggerScript = checkpoint.AddComponent<CheckpointTrigger>();
            triggerScript.checkpointPosition = position;

            // Parent to environmental container
            if (environmentalParent != null)
            {
                checkpoint.transform.SetParent(environmentalParent);
            }
        }

        private void CreateCheckpointVisuals(GameObject checkpoint)
        {
            // Create basic visual components for checkpoint
            var spriteRenderer = checkpoint.AddComponent<SpriteRenderer>();

            // Create a simple checkpoint sprite
            Texture2D checkpointTexture = CreateCheckpointTexture();
            Sprite checkpointSprite = Sprite.Create(checkpointTexture,
                new Rect(0, 0, checkpointTexture.width, checkpointTexture.height),
                new Vector2(0.5f, 0.5f));

            spriteRenderer.sprite = checkpointSprite;
            spriteRenderer.color = Color.yellow;
            spriteRenderer.sortingOrder = 10;

            // Add simple animation component
            var animator = checkpoint.AddComponent<Animator>();
            // You can create an AnimatorController for checkpoint animations
        }

        private Texture2D CreateCheckpointTexture()
        {
            // Create a simple checkpoint texture
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];

            for (int i = 0; i < colors.Length; i++)
            {
                int x = i % 32;
                int y = i / 32;

                // Create a simple flag-like pattern
                if ((x > 10 && x < 30) && (y > 10 && y < 30))
                {
                    colors[i] = Color.yellow;
                }
                else if (x == 10)
                {
                    colors[i] = Color.black; // Flagpole
                }
                else
                {
                    colors[i] = Color.clear;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        private void GenerateAutoCheckpoints()
        {
            if (currentStageData?.stageInfo == null) return;

            float stageLength = currentStageData.stageInfo.stageLength;
            Vector3 startPos = ApplyGroundLevelOffset(currentStageData.stageInfo.playerStartPosition);

            // Generate checkpoints at regular intervals
            int checkpointCount = Mathf.FloorToInt(stageLength / checkpointSpacing);

            for (int i = 1; i <= checkpointCount; i++)
            {
                Vector3 checkpointPos = startPos + Vector3.right * (checkpointSpacing * i);

                // Adjust Y position to ground level
                checkpointPos.y = FindGroundLevel(checkpointPos.x);

                CreateEnhancedCheckpoint(checkpointPos);
            }

            Debug.Log($"StageManager: Auto-generated {checkpointCount} checkpoints");
        }

        private float FindGroundLevel(float xPosition)
        {
            // Raycast down to find ground level
            Vector2 rayStart = new Vector2(xPosition, 50f); // Start high
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 100f, groundLayerMask);

            if (hit.collider != null)
            {
                return hit.point.y + 1f; // Slightly above ground
            }

            return 0f; // Default ground level
        }

        public void ClearStage()
        {
            Debug.Log("StageManager: Clearing existing stage");

            // Clear obstacles
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null)
                    DestroyImmediate(obstacle.gameObject);
            }
            activeObstacles.Clear();

            // Clear collectibles
            foreach (var collectible in activeCollectibles)
            {
                if (collectible != null)
                    DestroyImmediate(collectible.gameObject);
            }
            activeCollectibles.Clear();

            // Clear environmental objects
            foreach (var envObj in environmentalObjects)
            {
                if (envObj != null)
                    DestroyImmediate(envObj);
            }
            environmentalObjects.Clear();

            // Clear goal
            if (currentGoal != null)
            {
                DestroyImmediate(currentGoal);
                currentGoal = null;
            }

            // Clear player
            if (currentPlayer != null)
            {
                DestroyImmediate(currentPlayer);
                currentPlayer = null;
            }

            // Clear stage segments
            foreach (var segment in activeSegments)
            {
                if (segment != null)
                    DestroyImmediate(segment);
            }
            activeSegments.Clear();

            // Clear slope objects
            foreach (var slope in activeSlopes)
            {
                if (slope != null)
                    DestroyImmediate(slope.gameObject);
            }
            activeSlopes.Clear();

            // アセットリリースを追加
            ReleaseCurrentStageAssets();

            stageLoaded = false;
            Debug.Log("StageManager: Stage cleared successfully");
        }

        public void CollectibleCollected(Collectible collectible)
        {
            collectiblesRemaining--;
            activeCollectibles.Remove(collectible);
            OnCollectibleCollected?.Invoke(collectiblesRemaining);
        }

        // Enhanced stage completion with checkpoint statistics
        public void CompleteStage()
        {
            float completionTime = Time.time - stageStartTime;
            int energyChipsCollected = currentStageData.stageInfo.energyChipCount - collectiblesRemaining;

            // Get checkpoint statistics
            if (useEnhancedCheckpointSystem && CheckpointManager.Instance != null)
            {
                var checkpointStats = CheckpointManager.Instance.GetCheckpointStatistics();
                Debug.Log($"Stage completed with checkpoint stats: {checkpointStats}");

                // Store checkpoint completion data
                if (GameManager.Instance != null)
                {
                    Debug.Log($"Checkpoint completion: {checkpointStats.completionPercentage:F1}%");
                }
            }

            // Call original completion logic
            GameManager.Instance.CompleteStage(completionTime, GameManager.Instance.sessionDeathCount, energyChipsCollected);
            OnStageCompleted?.Invoke();
        }

        // Performance optimization: Update parallax speed for acceleration zones
        public void SetAccelerationZone(float speedMultiplier)
        {
            if (parallaxManager != null)
            {
                for (int i = 0; i < parallaxManager.parallaxLayers.Length; i++)
                {
                    float originalFactor = currentStageData.backgroundLayers[i].parallaxFactor;
                    parallaxManager.SetParallaxFactor(i, originalFactor * speedMultiplier);
                }
            }
        }

        // ゴール関連のパブリックメソッド
        public void SetGoalPosition(Vector3 position)
        {
            if (currentStageData?.stageInfo != null)
            {
                currentStageData.stageInfo.goalPosition = position;

                // 既存のゴールがあれば位置を更新
                if (currentGoal != null)
                {
                    GoalTrigger goalTrigger = currentGoal.GetComponent<GoalTrigger>();
                    if (goalTrigger != null)
                    {
                        goalTrigger.SetGoalPosition(position);
                    }
                    else
                    {
                        currentGoal.transform.position = position;
                    }
                }
            }
        }

        public Vector3 GetGoalPosition()
        {
            return currentStageData?.stageInfo?.goalPosition ?? Vector3.zero;
        }

        public bool HasGoal()
        {
            return currentGoal != null;
        }

        public GameObject GetCurrentGoal()
        {
            return currentGoal;
        }

        // プレイヤー関連のパブリックメソッド
        public void SetPlayerPosition(Vector3 position)
        {
            if (currentStageData?.stageInfo != null)
            {
                currentStageData.stageInfo.playerStartPosition = position;

                // 既存のプレイヤーがあれば位置を更新
                if (currentPlayer != null)
                {
                    currentPlayer.transform.position = position;
                }
            }
        }

        public Vector3 GetPlayerStartPosition()
        {
            return currentStageData?.stageInfo?.playerStartPosition ?? Vector3.zero;
        }

        public bool HasPlayer()
        {
            return currentPlayer != null;
        }

        public GameObject GetCurrentPlayer()
        {
            return currentPlayer;
        }

        // Enhanced checkpoint management methods
        public void ResetAllCheckpoints()
        {
            // Find all checkpoint triggers and reset them
            CheckpointTrigger[] checkpoints = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);

            foreach (var checkpoint in checkpoints)
            {
                checkpoint.ResetCheckpoint();
            }

            // Reset checkpoint manager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.ResetToDefaultCheckpoint();
            }

            Debug.Log($"StageManager: Reset {checkpoints.Length} checkpoints");
        }

        public float GetCheckpointCompletionPercentage()
        {
            if (useEnhancedCheckpointSystem && CheckpointManager.Instance != null)
            {
                var stats = CheckpointManager.Instance.GetCheckpointStatistics();
                return stats.completionPercentage;
            }

            return 0f;
        }

        public bool ValidateCheckpointPlacements()
        {
            if (currentStageData?.stageInfo?.checkpointPositions == null) return true;

            bool allValid = true;

            foreach (var checkpointPos in currentStageData.stageInfo.checkpointPositions)
            {
                // Check if checkpoint position is accessible
                if (!IsPositionAccessible(checkpointPos))
                {
                    Debug.LogWarning($"StageManager: Checkpoint at {checkpointPos} may not be accessible");
                    allValid = false;
                }

                // Check if checkpoint has ground nearby
                if (!HasGroundNearby(checkpointPos))
                {
                    Debug.LogWarning($"StageManager: Checkpoint at {checkpointPos} has no ground nearby");
                    allValid = false;
                }
            }

            return allValid;
        }
        public void ValidateAllSlopes()
        {
            int validCount = 0;
            int invalidCount = 0;

            foreach (var slope in activeSlopes)
            {
                if (slope != null)
                {
                    var settings = slope.GetSlopeSettings();
                    if (settings.slopeAngle > 0f && settings.slopeAngle <= 60f && settings.slopeLength > 0f)
                    {
                        validCount++;
                    }
                    else
                    {
                        invalidCount++;
                        Debug.LogWarning($"Invalid slope detected at {slope.transform.position}");
                    }
                }
                else
                {
                    invalidCount++;
                }
            }

            Debug.Log($"Slope validation: {validCount} valid, {invalidCount} invalid");
        }
        public SlopeStatistics GetSlopeStatistics()
        {
            SlopeStatistics stats = new SlopeStatistics();
            stats.totalSlopes = activeSlopes.Count;

            foreach (var slope in activeSlopes)
            {
                if (slope != null)
                {
                    // タイプ別統計は傾斜の設定から推測
                    var settings = slope.GetSlopeSettings();
                    if (settings.slopeAngle > 40f)
                        stats.steepSlopes++;
                    else if (settings.slopeAngle < 20f)
                        stats.gentleSlopes++;
                    else
                        stats.normalSlopes++;

                    if (settings.speedMultiplier > 1.5f)
                        stats.accelerationSlopes++;
                    else if (settings.speedMultiplier < 1f)
                        stats.decelerationSlopes++;
                }
            }

            return stats;
        }

        private bool IsPositionAccessible(Vector3 position)
        {
            // Check if there are obstacles blocking the checkpoint
            Collider2D obstacle = Physics2D.OverlapPoint(position, LayerMask.GetMask("Obstacles"));
            return obstacle == null;
        }

        private bool HasGroundNearby(Vector3 position)
        {
            // Check if there's ground within reasonable distance
            float checkDistance = 10f;
            RaycastHit2D groundCheck = Physics2D.Raycast(position, Vector2.down, checkDistance, groundLayerMask);
            return groundCheck.collider != null;
        }

        // Checkpoint configuration methods
        public void SetCheckpointSpacing(float spacing)
        {
            checkpointSpacing = Mathf.Max(100f, spacing);
        }

        public void SetUseEnhancedCheckpointSystem(bool useEnhanced)
        {
            useEnhancedCheckpointSystem = useEnhanced;
        }

        public int GetActiveCheckpointCount()
        {
            CheckpointTrigger[] checkpoints = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
            return checkpoints.Length;
        }

        public List<Vector3> GetAllCheckpointPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            CheckpointTrigger[] checkpoints = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);

            foreach (var checkpoint in checkpoints)
            {
                positions.Add(checkpoint.checkpointPosition);
            }

            return positions;
        }

        // Prefab getters
        private GameObject GetObstaclePrefab(ObstacleType type)
        {
            int index = (int)type;
            return index < obstaclePrefabs.Length ? obstaclePrefabs[index] : null;
        }

        private GameObject GetCollectiblePrefab(CollectibleType type)
        {
            int index = (int)type;
            return index < collectiblePrefabs.Length ? collectiblePrefabs[index] : null;
        }

        private GameObject GetEnvironmentalPrefab(EnvironmentalType type)
        {
            int index = (int)type;
            return index < environmentalPrefabs.Length ? environmentalPrefabs[index] : null;
        }

        private void ApplyEnvironmentalParameters(GameObject instance, EnvironmentalData envData)
        {
            // Apply specific parameters based on environmental type
            switch (envData.type)
            {
                case EnvironmentalType.GravityWell:
                    var gravityWell = instance.GetComponent<GravityFlipLab.Physics.GravityWell>();
                    if (gravityWell != null && envData.parameters.ContainsKey("strength"))
                    {
                        gravityWell.wellStrength = (float)envData.parameters["strength"];
                    }
                    break;

                case EnvironmentalType.WindTunnel:
                    var windTunnel = instance.GetComponent<GravityFlipLab.Physics.WindTunnel>();
                    if (windTunnel != null)
                    {
                        if (envData.parameters.ContainsKey("direction"))
                            windTunnel.windDirection = (Vector2)envData.parameters["direction"];
                        if (envData.parameters.ContainsKey("strength"))
                            windTunnel.windStrength = (float)envData.parameters["strength"];
                    }
                    break;
            }
        }

        // Tilemap地形の初期化を追加
        private IEnumerator SetupTerrain()
        {
            if (!useTilemapTerrain || currentStageData == null) yield break;

            // TilemapGroundManagerの初期化
            if (tilemapGroundManager == null)
            {
                GameObject tilemapManagerObj = new GameObject("TilemapGroundManager");
                tilemapManagerObj.transform.SetParent(transform);
                tilemapGroundManager = tilemapManagerObj.AddComponent<TilemapGroundManager>();
            }

            // StageManagerとの統合
            tilemapGroundManager.IntegrateWithStageManager(this);

            // 地形データの検証
            if (TerrainDataValidator.ValidateStageData(currentStageData))
            {
                // 地形データからTilemapを生成
                yield return StartCoroutine(GenerateTerrainFromData());
            }
            else
            {
                Debug.LogWarning("StageManager: Invalid terrain data, generating basic ground");
                tilemapGroundManager.GenerateBasicGround();
            }

            yield return new WaitForEndOfFrame();
        }

        private IEnumerator GenerateTerrainFromData()
        {
            if (currentStageData.terrainLayers == null) yield break;

            // 地形レイヤーごとに生成
            foreach (var terrainLayer in currentStageData.terrainLayers)
            {
                if (terrainLayer.autoGenerate)
                {
                    yield return StartCoroutine(GenerateTerrainLayer(terrainLayer));
                }
            }

            // セグメントデータからの詳細地形生成
            if (currentStageData.terrainSegments != null)
            {
                yield return StartCoroutine(GenerateTerrainSegments());
            }

            // 地形生成完了後の最適化
            tilemapGroundManager.OptimizeForPerformance();
        }

        private IEnumerator GenerateTerrainLayer(TerrainLayerData layerData)
        {
            if (layerData.tileVariants == null || layerData.tileVariants.Length == 0) yield break;

            Tilemap targetTilemap = tilemapGroundManager.foregroundTilemap;

            switch (layerData.generationMode)
            {
                case TerrainGenerationMode.Flat:
                    StartCoroutine(GenerateFlatLayer(targetTilemap, layerData));
                    break;
                case TerrainGenerationMode.FromHeightmap:
                    if (currentStageData.heightMap != null)
                    {
                        TerrainGenerator.GenerateFromHeightmap(
                            targetTilemap,
                            currentStageData.heightMap,
                            layerData,
                            currentStageData.tileMapSize
                        );
                    }
                    break;
                case TerrainGenerationMode.Custom:
                    if (currentStageData.enableProceduralGeneration)
                    {
                        TerrainGenerator.GenerateProceduralTerrain(
                            targetTilemap,
                            layerData,
                            currentStageData.tileMapSize,
                            currentStageData.proceduralSeed
                        );
                    }
                    break;
            }

            yield return new WaitForEndOfFrame();
        }

        private IEnumerator GenerateFlatLayer(Tilemap tilemap, TerrainLayerData layerData)
        {
            int groundLevel = Mathf.RoundToInt(layerData.baseHeight);
            int thickness = Mathf.RoundToInt(layerData.thickness);
            int width = currentStageData.tileMapSize.x;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < thickness; y++)
                {
                    Vector3Int position = new Vector3Int(x, groundLevel - y, 0);
                    TileBase tile = GetRandomTile(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }

                // 進行状況をフレーム間で分散
                if (x % 50 == 0)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator GenerateTerrainSegments()
        {
            foreach (var segment in currentStageData.terrainSegments)
            {
                if (segment != null)
                {
                    var primaryLayer = currentStageData.terrainLayers[0]; // プライマリレイヤーを使用
                    TerrainGenerator.GenerateTerrainSegment(
                        tilemapGroundManager.foregroundTilemap,
                        segment,
                        primaryLayer
                    );
                }
                yield return null; // フレーム分散
            }
        }

        private TileBase GetRandomTile(TileBase[] tiles, int x, int y)
        {
            if (tiles.Length == 1) return tiles[0];

            int seed = x * 1000 + y;
            System.Random random = new System.Random(seed);
            return tiles[random.Next(tiles.Length)];
        }

        // Debug and visualization
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !useEnhancedCheckpointSystem) return;

            // Draw checkpoint connections
            if (currentStageData?.stageInfo?.checkpointPositions != null)
            {
                Gizmos.color = Color.green;

                Vector3 lastPos = currentStageData.stageInfo.playerStartPosition;
                foreach (var checkpointPos in currentStageData.stageInfo.checkpointPositions)
                {
                    Gizmos.DrawLine(lastPos, checkpointPos);
                    Gizmos.DrawWireSphere(checkpointPos, 1f);
                    lastPos = checkpointPos;
                }

                // Draw line to goal
                Gizmos.DrawLine(lastPos, currentStageData.stageInfo.goalPosition);
            }

            // Draw auto-generated checkpoint positions
            if (currentStageData?.stageInfo != null)
            {
                Gizmos.color = Color.yellow;
                float stageLength = currentStageData.stageInfo.stageLength;
                Vector3 startPos = currentStageData.stageInfo.playerStartPosition;

                int checkpointCount = Mathf.FloorToInt(stageLength / checkpointSpacing);
                for (int i = 1; i <= checkpointCount; i++)
                {
                    Vector3 autoCheckpointPos = startPos + Vector3.right * (checkpointSpacing * i);
                    autoCheckpointPos.y = FindGroundLevel(autoCheckpointPos.x);
                    Gizmos.DrawWireCube(autoCheckpointPos, Vector3.one * 0.5f);
                }
            }

            // アクティブな傾斜を表示
            if (Application.isPlaying && activeSlopes != null)
            {
                foreach (var slope in activeSlopes)
                {
                    if (slope != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(slope.transform.position, Vector3.one * 0.5f);

                        // 傾斜方向を表示
                        Vector3 direction = slope.GetSlopeDirection();
                        Gizmos.DrawLine(slope.transform.position, slope.transform.position + direction * 2f);
                    }
                }
            }
        }

        // Stage statistics for debugging and analytics
        [System.Serializable]
        public class StageStatistics
        {
            public float stageCompletionTime;
            public int totalCheckpoints;
            public int activatedCheckpoints;
            public int totalCollectibles;
            public int collectedItems;
            public int deathCount;
            public float checkpointCompletionPercentage;

            public override string ToString()
            {
                return $"Stage Stats - Time: {stageCompletionTime:F1}s, " +
                       $"Checkpoints: {activatedCheckpoints}/{totalCheckpoints} ({checkpointCompletionPercentage:F1}%), " +
                       $"Items: {collectedItems}/{totalCollectibles}, Deaths: {deathCount}";
            }
        }

        public StageStatistics GetStageStatistics()
        {
            StageStatistics stats = new StageStatistics();

            stats.stageCompletionTime = stageLoaded ? Time.time - stageStartTime : 0f;
            stats.totalCollectibles = currentStageData?.stageInfo?.energyChipCount ?? 0;
            stats.collectedItems = stats.totalCollectibles - collectiblesRemaining;
            stats.deathCount = GameManager.Instance?.sessionDeathCount ?? 0;

            if (useEnhancedCheckpointSystem && CheckpointManager.Instance != null)
            {
                var checkpointStats = CheckpointManager.Instance.GetCheckpointStatistics();
                stats.totalCheckpoints = checkpointStats.totalCheckpoints;
                stats.activatedCheckpoints = checkpointStats.activatedCheckpoints;
                stats.checkpointCompletionPercentage = checkpointStats.completionPercentage;
            }
            else
            {
                stats.totalCheckpoints = GetActiveCheckpointCount();
                stats.activatedCheckpoints = 0; // Basic system doesn't track activation
                stats.checkpointCompletionPercentage = 0f;
            }

            return stats;
        }

        // Integration methods for external systems
        public void NotifyPlayerDeath()
        {
            // This can be called by external systems to notify stage manager of player death
            if (useEnhancedCheckpointSystem)
            {
                Debug.Log("StageManager: Player death notification received - enhanced respawn system will handle");
            }
        }

        public void NotifyPlayerRespawn()
        {
            // This can be called by external systems to notify stage manager of player respawn
            if (useEnhancedCheckpointSystem)
            {
                Debug.Log("StageManager: Player respawn notification received");
            }
        }

        public void NotifyCheckpointActivated(Vector3 checkpointPosition)
        {
            // This can be called when a checkpoint is activated
            Debug.Log($"StageManager: Checkpoint activated notification: {checkpointPosition}");
        }

        // 傾斜統計情報
        [System.Serializable]
        public class SlopeStatistics
        {
            public int totalSlopes;
            public int normalSlopes;
            public int steepSlopes;
            public int gentleSlopes;
            public int accelerationSlopes;
            public int decelerationSlopes;

            public override string ToString()
            {
                return $"Slope Stats - Total: {totalSlopes}, Normal: {normalSlopes}, " +
                       $"Steep: {steepSlopes}, Gentle: {gentleSlopes}, " +
                       $"Acceleration: {accelerationSlopes}, Deceleration: {decelerationSlopes}";
            }
        }
    }
    #endregion
}