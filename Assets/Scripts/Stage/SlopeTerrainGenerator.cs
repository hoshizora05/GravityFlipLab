using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 傾斜地形を生成するシステム
    /// TilemapGroundManagerと連携して動作
    /// </summary>
    [System.Serializable]
    public class SlopeTerrainConfiguration
    {
        [Header("Basic Slope Settings")]
        public float startHeight = 0f;
        public float endHeight = 5f;
        public int widthInTiles = 20;
        public SlopeType slopeType = SlopeType.Linear;

        [Header("Curve Settings")]
        public AnimationCurve slopeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool smoothTransitions = true;
        public int transitionTiles = 3;

        [Header("Advanced Settings")]
        public bool generateColliders = true;
        public bool optimizeForPerformance = true;
        public PhysicsMaterial2D slopeMaterial;

        public enum SlopeType
        {
            Linear,      // 直線傾斜
            Curved,      // カーブ傾斜
            Stairs,      // 階段状
            Hill,        // 山型
            Valley,      // 谷型
            Smooth       // 滑らか
        }

        public float GetMaxAngle()
        {
            float heightDiff = Mathf.Abs(endHeight - startHeight);
            return Mathf.Atan2(heightDiff, widthInTiles) * Mathf.Rad2Deg;
        }

        public bool IsWalkable(float maxWalkableAngle = 45f)
        {
            return GetMaxAngle() <= maxWalkableAngle;
        }
    }

    public class SlopeTerrainSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        public bool autoGenerateOnStart = false;
        public bool useHybridGeneration = true; // Tilemap + Prefab

        [Header("Tilemap Integration")]
        public TilemapGroundManager tilemapGroundManager;
        public Tilemap targetTilemap;
        public TerrainLayerData defaultTerrainLayer;

        [Header("Performance")]
        public int maxSlopesPerFrame = 5;
        public bool enableBatching = true;

        [Header("Debug")]
        public bool showDebugInfo = false;
        public bool visualizeSlopes = true;

        // Generated slopes tracking
        private List<GeneratedSlope> generatedSlopes = new List<GeneratedSlope>();
        private Queue<SlopeGenerationRequest> pendingGenerations = new Queue<SlopeGenerationRequest>();
        private bool isGenerating = false;

        [System.Serializable]
        public class GeneratedSlope
        {
            public Vector2Int startPosition;
            public Vector2Int size;
            public SlopeTerrainConfiguration.SlopeType type;
            public float angle;
            public List<Vector3Int> tilePositions = new List<Vector3Int>();
        }

        [System.Serializable]
        private class SlopeGenerationRequest
        {
            public Vector2Int position;
            public SlopeTerrainConfiguration config;
            public TerrainLayerData terrainLayer;
            public System.Action<bool> onComplete;
        }

        #region Initialization

        private void Awake()
        {
            InitializeSystem();
        }

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateDefaultSlopes();
            }
        }

        private void InitializeSystem()
        {
            // Get tilemap manager if not assigned
            if (tilemapGroundManager == null)
            {
                tilemapGroundManager = FindFirstObjectByType<TilemapGroundManager>();
            }

            // Get target tilemap
            if (targetTilemap == null && tilemapGroundManager != null)
            {
                targetTilemap = tilemapGroundManager.foregroundTilemap;
            }

            if (showDebugInfo)
                Debug.Log("SlopeTerrainSystem initialized");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 傾斜を生成する（メインメソッド）
        /// </summary>
        public void GenerateSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer = null, System.Action<bool> onComplete = null)
        {
            if (config == null)
            {
                Debug.LogError("SlopeTerrainSystem: Configuration is null");
                onComplete?.Invoke(false);
                return;
            }

            // Use default terrain layer if none provided
            if (terrainLayer == null)
                terrainLayer = defaultTerrainLayer;

            if (terrainLayer == null)
            {
                Debug.LogError("SlopeTerrainSystem: No terrain layer available");
                onComplete?.Invoke(false);
                return;
            }

            // Add to generation queue
            var request = new SlopeGenerationRequest
            {
                position = position,
                config = config,
                terrainLayer = terrainLayer,
                onComplete = onComplete
            };

            pendingGenerations.Enqueue(request);

            // Start processing if not already generating
            if (!isGenerating)
            {
                StartCoroutine(ProcessGenerationQueue());
            }
        }

        /// <summary>
        /// プリセット傾斜を生成
        /// </summary>
        public void GeneratePresetSlope(Vector2Int position, SlopePreset preset,
            int widthInTiles = 20, System.Action<bool> onComplete = null)
        {
            var config = CreatePresetConfiguration(preset, widthInTiles);
            GenerateSlope(position, config, null, onComplete);
        }

        /// <summary>
        /// 全ての傾斜をクリア
        /// </summary>
        public void ClearAllSlopes()
        {
            foreach (var slope in generatedSlopes)
            {
                ClearSlope(slope);
            }
            generatedSlopes.Clear();

            if (showDebugInfo)
                Debug.Log("All slopes cleared");
        }

        /// <summary>
        /// 特定位置の傾斜をクリア
        /// </summary>
        public void ClearSlopeAtPosition(Vector2Int position)
        {
            var slope = generatedSlopes.Find(s =>
                position.x >= s.startPosition.x &&
                position.x <= s.startPosition.x + s.size.x);

            if (slope != null)
            {
                ClearSlope(slope);
                generatedSlopes.Remove(slope);
            }
        }

        #endregion

        #region Generation System

        private IEnumerator ProcessGenerationQueue()
        {
            isGenerating = true;
            int processedThisFrame = 0;

            while (pendingGenerations.Count > 0)
            {
                var request = pendingGenerations.Dequeue();
                bool success = GenerateSlopeImmediate(request.position, request.config, request.terrainLayer);
                request.onComplete?.Invoke(success);

                processedThisFrame++;

                // Yield every few slopes to maintain frame rate
                if (processedThisFrame >= maxSlopesPerFrame)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }

            isGenerating = false;

            if (showDebugInfo)
                Debug.Log("Slope generation queue completed");
        }

        private bool GenerateSlopeImmediate(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer)
        {
            if (targetTilemap == null)
            {
                Debug.LogError("SlopeTerrainSystem: No target tilemap available");
                return false;
            }

            try
            {
                var generatedSlope = new GeneratedSlope
                {
                    startPosition = position,
                    size = new Vector2Int(config.widthInTiles, Mathf.CeilToInt(Mathf.Abs(config.endHeight - config.startHeight)) + 1),
                    type = config.slopeType,
                    angle = config.GetMaxAngle()
                };

                // Generate based on slope type
                switch (config.slopeType)
                {
                    case SlopeTerrainConfiguration.SlopeType.Linear:
                        GenerateLinearSlope(position, config, terrainLayer, generatedSlope);
                        break;
                    case SlopeTerrainConfiguration.SlopeType.Curved:
                        GenerateCurvedSlope(position, config, terrainLayer, generatedSlope);
                        break;
                    case SlopeTerrainConfiguration.SlopeType.Stairs:
                        GenerateStairsSlope(position, config, terrainLayer, generatedSlope);
                        break;
                    case SlopeTerrainConfiguration.SlopeType.Hill:
                        GenerateHillSlope(position, config, terrainLayer, generatedSlope);
                        break;
                    case SlopeTerrainConfiguration.SlopeType.Valley:
                        GenerateValleySlope(position, config, terrainLayer, generatedSlope);
                        break;
                    case SlopeTerrainConfiguration.SlopeType.Smooth:
                        GenerateSmoothSlope(position, config, terrainLayer, generatedSlope);
                        break;
                }

                // Apply smooth transitions if enabled
                if (config.smoothTransitions)
                {
                    ApplySmoothTransitions(position, config, terrainLayer);
                }

                // Apply physics material if specified
                if (config.slopeMaterial != null)
                {
                    ApplySlopeMaterial(config.slopeMaterial);
                }

                generatedSlopes.Add(generatedSlope);

                if (showDebugInfo)
                    Debug.Log($"Generated {config.slopeType} slope at {position} with angle {generatedSlope.angle:F1}°");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SlopeTerrainSystem: Failed to generate slope - {e.Message}");
                return false;
            }
        }

        #endregion

        #region Slope Generation Methods

        private void GenerateLinearSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            float heightDiff = config.endHeight - config.startHeight;

            for (int x = 0; x < config.widthInTiles; x++)
            {
                float progress = (float)x / (config.widthInTiles - 1);
                float currentHeight = config.startHeight + (heightDiff * progress);
                int heightInTiles = Mathf.RoundToInt(currentHeight);

                GenerateTerrainColumn(position.x + x, position.y, heightInTiles, terrainLayer, slope);
            }
        }

        private void GenerateCurvedSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            float heightDiff = config.endHeight - config.startHeight;

            for (int x = 0; x < config.widthInTiles; x++)
            {
                float progress = (float)x / (config.widthInTiles - 1);
                float curveValue = config.slopeCurve.Evaluate(progress);
                float currentHeight = config.startHeight + (heightDiff * curveValue);
                int heightInTiles = Mathf.RoundToInt(currentHeight);

                GenerateTerrainColumn(position.x + x, position.y, heightInTiles, terrainLayer, slope);
            }
        }

        private void GenerateStairsSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(config.endHeight - config.startHeight)));
            int stepWidth = Mathf.Max(1, config.widthInTiles / steps);
            float stepHeight = (config.endHeight - config.startHeight) / steps;

            for (int step = 0; step < steps; step++)
            {
                int stepStartX = step * stepWidth;
                int stepEndX = Mathf.Min((step + 1) * stepWidth, config.widthInTiles);
                int currentStepHeight = Mathf.RoundToInt(config.startHeight + (stepHeight * step));

                for (int x = stepStartX; x < stepEndX; x++)
                {
                    GenerateTerrainColumn(position.x + x, position.y, currentStepHeight, terrainLayer, slope);
                }
            }
        }

        private void GenerateHillSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            float maxHeight = Mathf.Max(config.startHeight, config.endHeight) + Mathf.Abs(config.endHeight - config.startHeight);

            for (int x = 0; x < config.widthInTiles; x++)
            {
                float progress = (float)x / (config.widthInTiles - 1);
                // Parabolic hill shape
                float hillValue = 1f - (progress - 0.5f) * (progress - 0.5f) * 4f;
                float currentHeight = config.startHeight + (maxHeight - config.startHeight) * hillValue;
                int heightInTiles = Mathf.RoundToInt(currentHeight);

                GenerateTerrainColumn(position.x + x, position.y, heightInTiles, terrainLayer, slope);
            }
        }

        private void GenerateValleySlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            float minHeight = Mathf.Min(config.startHeight, config.endHeight) - Mathf.Abs(config.endHeight - config.startHeight);

            for (int x = 0; x < config.widthInTiles; x++)
            {
                float progress = (float)x / (config.widthInTiles - 1);
                // Inverted parabolic valley shape
                float valleyValue = (progress - 0.5f) * (progress - 0.5f) * 4f;
                float currentHeight = config.startHeight - (config.startHeight - minHeight) * valleyValue;
                int heightInTiles = Mathf.RoundToInt(currentHeight);

                GenerateTerrainColumn(position.x + x, position.y, heightInTiles, terrainLayer, slope);
            }
        }

        private void GenerateSmoothSlope(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer, GeneratedSlope slope)
        {
            // Use smoothed curve for very gradual slopes
            AnimationCurve smoothCurve = new AnimationCurve();
            smoothCurve.AddKey(0f, 0f);
            smoothCurve.AddKey(0.5f, 0.5f);
            smoothCurve.AddKey(1f, 1f);

            // Smooth all tangents
            for (int i = 0; i < smoothCurve.length; i++)
            {
                smoothCurve.SmoothTangents(i, 0.5f);
            }

            float heightDiff = config.endHeight - config.startHeight;

            for (int x = 0; x < config.widthInTiles; x++)
            {
                float progress = (float)x / (config.widthInTiles - 1);
                float smoothValue = smoothCurve.Evaluate(progress);
                float currentHeight = config.startHeight + (heightDiff * smoothValue);
                int heightInTiles = Mathf.RoundToInt(currentHeight);

                GenerateTerrainColumn(position.x + x, position.y, heightInTiles, terrainLayer, slope);
            }
        }

        private void GenerateTerrainColumn(int x, int baseY, int height, TerrainLayerData terrainLayer,
            GeneratedSlope slope)
        {
            // Generate tiles from base position up to height
            for (int y = 0; y < height + 2; y++) // +2 for ground thickness
            {
                Vector3Int tilePos = new Vector3Int(x, baseY - y, 0);
                TileBase tile = GetTileForPosition(terrainLayer, x, y);

                if (tile != null)
                {
                    targetTilemap.SetTile(tilePos, tile);
                    slope.tilePositions.Add(tilePos);
                }
            }
        }

        private TileBase GetTileForPosition(TerrainLayerData terrainLayer, int x, int y)
        {
            if (terrainLayer?.tileVariants == null || terrainLayer.tileVariants.Length == 0)
                return null;

            return terrainLayer.GetTileForPosition(x, y, y);
        }

        #endregion

        #region Utility Methods

        private void ApplySmoothTransitions(Vector2Int position, SlopeTerrainConfiguration config,
            TerrainLayerData terrainLayer)
        {
            // Smooth transitions at the start and end of slopes
            int transitionTiles = config.transitionTiles;

            // Left transition
            for (int i = 0; i < transitionTiles; i++)
            {
                float blendFactor = (float)i / transitionTiles;
                ApplyTransitionBlending(position.x - transitionTiles + i, position.y,
                    config.startHeight, blendFactor, terrainLayer);
            }

            // Right transition
            for (int i = 0; i < transitionTiles; i++)
            {
                float blendFactor = 1f - (float)i / transitionTiles;
                ApplyTransitionBlending(position.x + config.widthInTiles + i, position.y,
                    config.endHeight, blendFactor, terrainLayer);
            }
        }

        private void ApplyTransitionBlending(int x, int baseY, float targetHeight, float blendFactor,
            TerrainLayerData terrainLayer)
        {
            // Get existing terrain height
            int existingHeight = GetExistingTerrainHeight(x, baseY);

            // Blend between existing and target height
            float blendedHeight = Mathf.Lerp(existingHeight, targetHeight, blendFactor);
            int finalHeight = Mathf.RoundToInt(blendedHeight);

            // Apply blended terrain
            AdjustTerrainHeight(x, baseY, finalHeight, terrainLayer);
        }

        private int GetExistingTerrainHeight(int x, int baseY)
        {
            // Find the highest tile at this position
            for (int y = baseY + 10; y >= baseY - 10; y--)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (targetTilemap.GetTile(pos) != null)
                {
                    return y;
                }
            }
            return baseY;
        }

        private void AdjustTerrainHeight(int x, int baseY, int targetHeight, TerrainLayerData terrainLayer)
        {
            int currentHeight = GetExistingTerrainHeight(x, baseY);

            if (targetHeight > currentHeight)
            {
                // Add tiles upward
                for (int y = currentHeight + 1; y <= targetHeight; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase tile = GetTileForPosition(terrainLayer, x, y - baseY);
                    if (tile != null)
                    {
                        targetTilemap.SetTile(pos, tile);
                    }
                }
            }
            else if (targetHeight < currentHeight)
            {
                // Remove tiles downward
                for (int y = currentHeight; y > targetHeight; y--)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    targetTilemap.SetTile(pos, null);
                }
            }
        }

        private void ApplySlopeMaterial(PhysicsMaterial2D material)
        {
            if (targetTilemap != null)
            {
                var collider = targetTilemap.GetComponent<TilemapCollider2D>();
                if (collider != null)
                {
                    collider.sharedMaterial = material;
                }
            }
        }

        private void ClearSlope(GeneratedSlope slope)
        {
            foreach (var tilePos in slope.tilePositions)
            {
                targetTilemap.SetTile(tilePos, null);
            }
        }

        #endregion

        #region Preset System

        public enum SlopePreset
        {
            GentleUphill,
            SteepUphill,
            GentleDownhill,
            SteepDownhill,
            SmallHill,
            LargeHill,
            ShallowValley,
            DeepValley,
            SimpleStairs,
            SmoothRamp
        }

        private SlopeTerrainConfiguration CreatePresetConfiguration(SlopePreset preset, int widthInTiles)
        {
            var config = new SlopeTerrainConfiguration
            {
                widthInTiles = widthInTiles,
                smoothTransitions = true,
                generateColliders = true
            };

            switch (preset)
            {
                case SlopePreset.GentleUphill:
                    config.startHeight = 0f;
                    config.endHeight = 3f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Linear;
                    break;

                case SlopePreset.SteepUphill:
                    config.startHeight = 0f;
                    config.endHeight = 6f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Linear;
                    break;

                case SlopePreset.GentleDownhill:
                    config.startHeight = 3f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Linear;
                    break;

                case SlopePreset.SteepDownhill:
                    config.startHeight = 6f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Linear;
                    break;

                case SlopePreset.SmallHill:
                    config.startHeight = 0f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Hill;
                    break;

                case SlopePreset.LargeHill:
                    config.startHeight = 0f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Hill;
                    config.widthInTiles = widthInTiles * 2;
                    break;

                case SlopePreset.ShallowValley:
                    config.startHeight = 0f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Valley;
                    break;

                case SlopePreset.DeepValley:
                    config.startHeight = 0f;
                    config.endHeight = 0f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Valley;
                    config.widthInTiles = widthInTiles * 2;
                    break;

                case SlopePreset.SimpleStairs:
                    config.startHeight = 0f;
                    config.endHeight = 4f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Stairs;
                    break;

                case SlopePreset.SmoothRamp:
                    config.startHeight = 0f;
                    config.endHeight = 4f;
                    config.slopeType = SlopeTerrainConfiguration.SlopeType.Smooth;
                    break;
            }

            return config;
        }

        #endregion

        #region Default Generation

        private void GenerateDefaultSlopes()
        {
            if (showDebugInfo)
                Debug.Log("Generating default test slopes...");

            // Generate a variety of test slopes
            GeneratePresetSlope(new Vector2Int(50, 0), SlopePreset.GentleUphill, 15);
            GeneratePresetSlope(new Vector2Int(80, 3), SlopePreset.SmallHill, 20);
            GeneratePresetSlope(new Vector2Int(120, 0), SlopePreset.SimpleStairs, 12);
            GeneratePresetSlope(new Vector2Int(150, 0), SlopePreset.SmoothRamp, 18);
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!visualizeSlopes || !Application.isPlaying) return;

            foreach (var slope in generatedSlopes)
            {
                DrawSlopeGizmo(slope);
            }
        }

        private void DrawSlopeGizmo(GeneratedSlope slope)
        {
            if (targetTilemap == null) return;

            // Choose color based on slope type
            switch (slope.type)
            {
                case SlopeTerrainConfiguration.SlopeType.Linear:
                    Gizmos.color = Color.green;
                    break;
                case SlopeTerrainConfiguration.SlopeType.Curved:
                    Gizmos.color = Color.blue;
                    break;
                case SlopeTerrainConfiguration.SlopeType.Stairs:
                    Gizmos.color = Color.yellow;
                    break;
                case SlopeTerrainConfiguration.SlopeType.Hill:
                    Gizmos.color = Color.red;
                    break;
                case SlopeTerrainConfiguration.SlopeType.Valley:
                    Gizmos.color = Color.cyan;
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }

            // Draw slope bounds
            Vector3 worldStart = targetTilemap.CellToWorld(new Vector3Int(slope.startPosition.x, slope.startPosition.y, 0));
            Vector3 size = new Vector3(slope.size.x, slope.size.y, 1f);
            Gizmos.DrawWireCube(worldStart + size * 0.5f, size);

            // Draw angle indicator
            Vector3 center = worldStart + new Vector3(slope.size.x * 0.5f, slope.size.y * 0.5f, 0);
            Vector3 angleDir = Quaternion.Euler(0, 0, slope.angle) * Vector3.right;
            Gizmos.DrawLine(center, center + angleDir * 2f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

#if UNITY_EDITOR
            foreach (var slope in generatedSlopes)
            {
                Vector3 worldPos = targetTilemap.CellToWorld(new Vector3Int(slope.startPosition.x, slope.startPosition.y + slope.size.y, 0));
                string info = $"{slope.type}\nAngle: {slope.angle:F1}°\nTiles: {slope.tilePositions.Count}";
                UnityEditor.Handles.Label(worldPos + Vector3.up, info);
            }
#endif
        }

        #endregion

        #region Public Information

        public int GetGeneratedSlopeCount()
        {
            return generatedSlopes.Count;
        }

        public List<GeneratedSlope> GetGeneratedSlopes()
        {
            return new List<GeneratedSlope>(generatedSlopes);
        }

        public bool IsGenerating()
        {
            return isGenerating;
        }

        public int GetPendingGenerationCount()
        {
            return pendingGenerations.Count;
        }

        #endregion
    }
}