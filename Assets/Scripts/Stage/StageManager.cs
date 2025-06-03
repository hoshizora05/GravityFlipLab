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
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StageManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StageManager");
                        _instance = go.AddComponent<StageManager>();
                    }
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

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            CreateParentObjects();
            InitializeComponents();
            LoadCurrentStage();
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
        }

        public void LoadStage(StageDataSO stageData)
        {
            if (stageData == null) return;

            currentStageData = stageData;
            StartCoroutine(LoadStageCoroutine());
        }

        public void LoadCurrentStage()
        {
            if (currentStageData != null)
            {
                LoadStage(currentStageData);
            }
            else
            {
                // Try to load stage based on GameManager's current stage
                LoadStageByNumber(GameManager.Instance.currentWorld, GameManager.Instance.currentStage);
            }
        }

        public void LoadStageByNumber(int world, int stage)
        {
            string resourcePath = $"StageData/World{world}/Stage{world}-{stage}";
            StageDataSO stageData = Resources.Load<StageDataSO>(resourcePath);

            if (stageData != null)
            {
                LoadStage(stageData);
            }
            else
            {
                Debug.LogError($"Stage data not found: {resourcePath}");
            }
        }

        private IEnumerator LoadStageCoroutine()
        {
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

            // Setup goal - 新規追加
            SetupGoal();

            //yield return StartCoroutine(SetupTerrain());

            // Initialize stage
            InitializeStage();

            stageLoaded = true;
            OnStageLoaded?.Invoke();
        }

        // 新規追加: ゴールセットアップメソッド
        private void SetupGoal()
        {
            if (currentStageData?.stageInfo == null) return;

            Vector3 goalPosition = currentStageData.stageInfo.goalPosition;

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

                Debug.Log($"Goal created at position: {goalPosition}");
            }
            else
            {
                // プレハブが設定されていない場合、シンプルなゴールを作成
                CreateDefaultGoal(goalPosition);
            }
        }

        // 新規追加: デフォルトゴール作成メソッド
        private void CreateDefaultGoal(Vector3 position)
        {
            currentGoal = new GameObject("StageGoal");
            currentGoal.transform.position = position;

            // ビジュアルコンポーネントの追加
            SpriteRenderer renderer = currentGoal.AddComponent<SpriteRenderer>();
            renderer.color = Color.yellow;
            // 基本的な四角形スプライトを設定（実際の開発ではゴール用スプライトを使用）
            renderer.sprite = CreateDefaultGoalSprite();

            // コライダーの追加
            BoxCollider2D collider = currentGoal.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2f, 3f); // プレイヤーより大きめに設定

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

            Debug.Log($"Default goal created at position: {position}");
        }

        // 新規追加: デフォルトゴールスプライト作成
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
            if (parallaxManager == null || currentStageData.backgroundLayers == null) yield break;

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
            }
        }

        private IEnumerator LoadObstacles()
        {
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

            // Initialize player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = currentStageData.stageInfo.playerStartPosition;

                // Set camera target
                if (cameraController != null)
                {
                    cameraController.SetTarget(player.transform);
                }
            }

            // Set up checkpoints
            SetupCheckpoints();

            // Initialize all obstacles
            foreach (var obstacle in activeObstacles)
            {
                obstacle.StartObstacle();
            }
        }

        private void SetupCheckpoints()
        {
            CheckpointManager.Instance.ResetToDefaultCheckpoint();

            foreach (var checkpointPos in currentStageData.stageInfo.checkpointPositions)
            {
                // Create checkpoint trigger objects if needed
                GameObject checkpoint = new GameObject("Checkpoint");
                checkpoint.transform.position = checkpointPos;
                checkpoint.layer = LayerMask.NameToLayer("Checkpoint");

                BoxCollider2D trigger = checkpoint.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = Vector2.one;

                // Add checkpoint trigger component
                CheckpointTrigger triggerScript = checkpoint.AddComponent<CheckpointTrigger>();
                triggerScript.checkpointPosition = checkpointPos;
            }
        }

        public void ClearStage()
        {
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

            // Clear goal - 新規追加
            if (currentGoal != null)
            {
                DestroyImmediate(currentGoal);
                currentGoal = null;
            }

            // Clear stage segments
            foreach (var segment in activeSegments)
            {
                if (segment != null)
                    DestroyImmediate(segment);
            }
            activeSegments.Clear();

            stageLoaded = false;
        }

        public void CollectibleCollected(Collectible collectible)
        {
            collectiblesRemaining--;
            activeCollectibles.Remove(collectible);
            OnCollectibleCollected?.Invoke(collectiblesRemaining);
        }

        public void CompleteStage()
        {
            float completionTime = Time.time - stageStartTime;
            int energyChipsCollected = currentStageData.stageInfo.energyChipCount - collectiblesRemaining;

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

        // 新規追加: ゴール関連のパブリックメソッド
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
                Debug.LogWarning("Invalid terrain data, generating basic ground");
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
    }
    #endregion
}