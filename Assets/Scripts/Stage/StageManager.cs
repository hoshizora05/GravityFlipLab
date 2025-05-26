using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    #region Stage Data Structures

    [System.Serializable]
    public class StageInfo
    {
        public int worldNumber;
        public int stageNumber;
        public string stageName;
        public float timeLimit = 300f; // 5 minutes default
        public int energyChipCount = 3;
        public Vector3 playerStartPosition;
        public Vector3 goalPosition;
        public List<Vector3> checkpointPositions = new List<Vector3>();
        public StageTheme theme = StageTheme.Tech;

        [Header("Stage Layout")]
        public float stageLength = 4096f; // Stage width in pixels
        public float stageHeight = 1024f; // Stage height in pixels
        public int segmentCount = 16; // Number of 256px segments
    }


    public enum StageTheme
    {
        Tech,
        Industrial,
        Organic,
        Crystal,
        Void
    }

    
    public enum ObstacleType
    {
        Spike,
        ElectricFence,
        PistonCrusher,
        RotatingSaw,
        HoverDrone,
        TimerGate,
        PhaseBlock,
        PressureSwitch
    }

    public enum CollectibleType
    {
        EnergyChip,
        PowerUp,
        ExtraLife
    }

    public enum EnvironmentalType
    {
        GravityWell,
        WindTunnel,
        SpringPlatform,
        MovingPlatform
    }

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

            // Initialize stage
            InitializeStage();

            stageLoaded = true;
            OnStageLoaded?.Invoke();
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
                    parallaxLayer.textureSize = layerData.tileSize;
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
    }
    #endregion
}