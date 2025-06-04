using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

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

        public void LoadStage(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("StageManager: Cannot load null stage data");
                return;
            }

            Debug.Log($"StageManager: Loading stage data: {stageData.name}");
            currentStageData = stageData;
            StartCoroutine(LoadStageCoroutine());
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

        public void LoadStageByNumber(int world, int stage)
        {
            string resourcePath = $"StageData/World{world}/Stage{world}-{stage}";
            StageDataSO stageData = Resources.Load<StageDataSO>(resourcePath);

            if (stageData != null)
            {
                Debug.Log($"StageManager: Loaded stage data from resources: {resourcePath}");
                LoadStage(stageData);
            }
            else
            {
                Debug.LogError($"StageManager: Stage data not found: {resourcePath}");

                // フォールバック: デフォルトのステージデータを作成
                CreateDefaultStageData(world, stage);
            }
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

        private IEnumerator LoadStageCoroutine()
        {
            Debug.Log("StageManager: Starting stage load coroutine");

            // Clear existing stage
            ClearStage();

            yield return new WaitForEndOfFrame();

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

            // Setup goal
            SetupGoal();

            // Setup player
            SetupPlayer();

            //yield return StartCoroutine(SetupTerrain());

            // Initialize stage
            InitializeStage();

            stageLoaded = true;
            OnStageLoaded?.Invoke();

            Debug.Log("StageManager: Stage load completed successfully");
        }

        // ゴールセットアップメソッド
        private void SetupGoal()
        {
            if (currentStageData?.stageInfo == null)
            {
                Debug.LogWarning("StageManager: Cannot setup goal - no stage data or stage info");
                return;
            }

            Vector3 goalPosition = currentStageData.stageInfo.goalPosition;
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

            Vector3 playerStartPosition = currentStageData.stageInfo.playerStartPosition;
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
                    instance.transform.position = obstacleData.position;
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
                    instance.transform.position = collectibleData.position;

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
                    instance.transform.position = envData.position;
                    instance.transform.localScale = envData.scale;

                    // Apply environmental-specific parameters
                    ApplyEnvironmentalParameters(instance, envData);
                    environmentalObjects.Add(instance);
                }
                yield return null;
            }
        }

        private void InitializeStage()
        {
            stageStartTime = Time.time;

            // Initialize player position and setup enhanced checkpoint system
            if (currentPlayer != null)
            {
                Vector3 playerStartPos = currentStageData.stageInfo.playerStartPosition;
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
            GameObject checkpoint;

            if (checkpointPrefab != null)
            {
                checkpoint = Instantiate(checkpointPrefab, position, Quaternion.identity);
            }
            else
            {
                checkpoint = new GameObject("Enhanced Checkpoint");
                checkpoint.transform.position = position;

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
            checkpointTrigger.checkpointPosition = position;
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
            checkpoint.transform.position = position;
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
            Vector3 startPos = currentStageData.stageInfo.playerStartPosition;

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
    }
    #endregion
}