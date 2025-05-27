using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    #region 地形データ型定義

    // 地形レイヤータイプ
    public enum TerrainLayerType
    {
        Ground,         // 基本的な地面
        Platform,       // プラットフォーム
        Background,     // 背景装飾
        Foreground,     // 前景装飾
        Collision,      // 当たり判定のみ
        OneWayPlatform, // ワンウェイプラットフォーム
        Breakable,      // 破壊可能
        Hazard         // 危険地帯
    }

    // 地形生成モード
    public enum TerrainGenerationMode
    {
        Flat,           // フラットな地形
        Hilly,          // 丘陵地形
        Mountainous,    // 山岳地形
        Custom,         // カスタム定義
        FromHeightmap   // ハイトマップから生成
    }

    // 地形パターン
    public enum TerrainPattern
    {
        Flat,
        Ascending,
        Descending,
        Valley,
        Hill,
        Stairs,
        Gaps,
        Platforms
    }

    // プラットフォームタイプ
    public enum PlatformType
    {
        Solid,
        Moving,
        Falling,
        Disappearing,
        OneWay,
        Bouncy,
        Ice,
        Conveyor
    }

    // 地形フィーチャータイプ
    public enum TerrainFeatureType
    {
        Spikes,
        Pit,
        Ramp,
        Wall,
        Ceiling,
        Bridge,
        Tunnel,
        Decoration
    }

    #endregion

    #region 地形データ構造

    // 地形レイヤーデータ
    [System.Serializable]
    public class TerrainLayerData
    {
        [Header("Layer Configuration")]
        public string layerName = "Ground";
        public TerrainLayerType layerType = TerrainLayerType.Ground;
        public int sortingOrder = 0;
        public LayerMask collisionLayer = 1;

        [Header("Tile Assets")]
        public TileBase[] tileVariants;
        public PhysicsMaterial2D physicsMaterial;

        [Header("Generation Settings")]
        public bool autoGenerate = true;
        public float baseHeight = -5f;
        public float thickness = 2f;
        public TerrainGenerationMode generationMode = TerrainGenerationMode.Flat;

        [Header("Noise Settings (for procedural generation)")]
        public float noiseScale = 0.1f;
        public float noiseAmplitude = 2f;
        public int noiseSeed = 0;

        [Header("Tile Selection Rules")]
        public TileSelectionMode tileSelectionMode = TileSelectionMode.Random;
        public bool enableTileRotation = false;
        public AnimationCurve depthTileDistribution = AnimationCurve.Linear(0, 0, 1, 1);

        // デフォルトコンストラクタ
        public TerrainLayerData()
        {
            layerName = "Ground";
            layerType = TerrainLayerType.Ground;
            sortingOrder = 0;
            collisionLayer = 1;
            autoGenerate = true;
            baseHeight = -5f;
            thickness = 2f;
            generationMode = TerrainGenerationMode.Flat;
            noiseScale = 0.1f;
            noiseAmplitude = 2f;
            noiseSeed = 0;
            tileSelectionMode = TileSelectionMode.Random;
            enableTileRotation = false;
            depthTileDistribution = AnimationCurve.Linear(0, 0, 1, 1);
        }

        // 指定タイプでの初期化コンストラクタ
        public TerrainLayerData(TerrainLayerType type)
        {
            layerType = type;
            tileSelectionMode = TileSelectionMode.Random;
            enableTileRotation = false;
            depthTileDistribution = AnimationCurve.Linear(0, 0, 1, 1);

            switch (type)
            {
                case TerrainLayerType.Ground:
                    layerName = "Ground";
                    sortingOrder = 0;
                    baseHeight = -5f;
                    thickness = 3f;
                    autoGenerate = true;
                    collisionLayer = LayerMask.GetMask("Ground");
                    break;
                case TerrainLayerType.Platform:
                    layerName = "Platforms";
                    sortingOrder = 10;
                    baseHeight = 0f;
                    thickness = 1f;
                    autoGenerate = false;
                    collisionLayer = LayerMask.GetMask("Platform");
                    break;
                case TerrainLayerType.Background:
                    layerName = "Background";
                    sortingOrder = -10;
                    baseHeight = -10f;
                    thickness = 1f;
                    autoGenerate = false;
                    collisionLayer = 0; // 背景は衝突しない
                    break;
                case TerrainLayerType.OneWayPlatform:
                    layerName = "OneWayPlatforms";
                    sortingOrder = 5;
                    baseHeight = 2f;
                    thickness = 1f;
                    autoGenerate = false;
                    collisionLayer = LayerMask.GetMask("Platform");
                    break;
                default:
                    layerName = type.ToString();
                    sortingOrder = 0;
                    baseHeight = 0f;
                    thickness = 1f;
                    autoGenerate = false;
                    collisionLayer = 1;
                    break;
            }
        }

        // タイル選択メソッド
        public TileBase GetTileForPosition(int x, int y, int depth = 0)
        {
            if (tileVariants == null || tileVariants.Length == 0)
                return null;

            if (tileVariants.Length == 1)
                return tileVariants[0];

            switch (tileSelectionMode)
            {
                case TileSelectionMode.Random:
                    return GetRandomTile(x, y);

                case TileSelectionMode.Sequential:
                    return GetSequentialTile(x, y);

                case TileSelectionMode.ByDepth:
                    return GetTileByDepth(depth);

                case TileSelectionMode.ByPosition:
                    return GetTileByPosition(x, y);

                case TileSelectionMode.ByHeight:
                    return GetTileByHeight(y);

                default:
                    return tileVariants[0];
            }
        }

        private TileBase GetRandomTile(int x, int y)
        {
            int seed = x * 1000 + y + noiseSeed;
            System.Random random = new System.Random(seed);
            return tileVariants[random.Next(tileVariants.Length)];
        }

        private TileBase GetSequentialTile(int x, int y)
        {
            int index = (x + y) % tileVariants.Length;
            return tileVariants[index];
        }

        private TileBase GetTileByDepth(int depth)
        {
            if (tileVariants.Length < 3) return tileVariants[0];

            float t = depthTileDistribution.Evaluate(depth / thickness);
            int index = Mathf.RoundToInt(t * (tileVariants.Length - 1));
            return tileVariants[Mathf.Clamp(index, 0, tileVariants.Length - 1)];
        }

        private TileBase GetTileByPosition(int x, int y)
        {
            // 位置に基づいた複雑なパターン
            int pattern = (x / 4 + y / 4) % tileVariants.Length;
            return tileVariants[pattern];
        }

        private TileBase GetTileByHeight(int y)
        {
            // 高さに基づいたタイル選択
            float normalizedHeight = (y - baseHeight) / thickness;
            int index = Mathf.FloorToInt(normalizedHeight * tileVariants.Length);
            return tileVariants[Mathf.Clamp(index, 0, tileVariants.Length - 1)];
        }

        // 物理マテリアルの適用
        public void ApplyPhysicsMaterialToCollider(Collider2D collider)
        {
            if (physicsMaterial != null && collider != null)
            {
                collider.sharedMaterial = physicsMaterial as PhysicsMaterial2D;
            }
        }

        // レイヤーマスクの適用
        public void ApplyLayerToGameObject(GameObject obj)
        {
            if (obj != null)
            {
                int layer = GetLayerFromMask(collisionLayer);
                if (layer >= 0)
                {
                    obj.layer = layer;
                }
            }
        }

        private int GetLayerFromMask(LayerMask layerMask)
        {
            int mask = layerMask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        // 検証メソッド
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(layerName))
                return false;

            if (autoGenerate && (tileVariants == null || tileVariants.Length == 0))
                return false;

            if (thickness <= 0)
                return false;

            return true;
        }

        public List<string> GetValidationErrors()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(layerName))
                errors.Add("Layer name is empty");

            if (autoGenerate && (tileVariants == null || tileVariants.Length == 0))
                errors.Add("Auto-generate is enabled but no tile variants assigned");

            if (thickness <= 0)
                errors.Add("Thickness must be greater than 0");

            if (tileVariants != null)
            {
                for (int i = 0; i < tileVariants.Length; i++)
                {
                    if (tileVariants[i] == null)
                        errors.Add($"Tile variant {i} is null");
                }
            }

            return errors;
        }
    }

    // タイル選択モード
    public enum TileSelectionMode
    {
        Random,      // ランダム選択
        Sequential,  // 順次選択
        ByDepth,     // 深度による選択
        ByPosition,  // 位置による選択
        ByHeight     // 高さによる選択
    }

    // 地形セグメントデータ
    [System.Serializable]
    public class TerrainSegmentData
    {
        public int segmentIndex;
        public Vector2Int startPosition;
        public Vector2Int size;
        public TerrainPattern pattern = TerrainPattern.Flat;
        public float heightVariation = 0f;
        public List<PlatformData> platforms = new List<PlatformData>();
        public List<TerrainFeatureData> features = new List<TerrainFeatureData>();

        // デフォルトコンストラクタ
        public TerrainSegmentData()
        {
            segmentIndex = 0;
            startPosition = Vector2Int.zero;
            size = new Vector2Int(16, 3);
            pattern = TerrainPattern.Flat;
            heightVariation = 0f;
            platforms = new List<PlatformData>();
            features = new List<TerrainFeatureData>();
        }

        // インデックス指定コンストラクタ
        public TerrainSegmentData(int index, Vector2Int start, Vector2Int segmentSize)
        {
            segmentIndex = index;
            startPosition = start;
            size = segmentSize;
            pattern = TerrainPattern.Flat;
            heightVariation = 0f;
            platforms = new List<PlatformData>();
            features = new List<TerrainFeatureData>();
        }
    }

    // プラットフォームデータ
    [System.Serializable]
    public class PlatformData
    {
        public Vector2Int position;
        public Vector2Int size;
        public PlatformType platformType = PlatformType.Solid;
        public float moveSpeed = 0f;
        public Vector2[] movePath;
        public bool isOneWay = false;

        [Header("Special Platform Settings")]
        public float fallDelay = 1f;           // FallingPlatform用
        public float disappearTime = 2f;       // DisappearingPlatform用
        public float reappearTime = 3f;        // DisappearingPlatform用
        public Vector2 conveyorDirection = Vector2.right; // ConveyorPlatform用

        // デフォルトコンストラクタ
        public PlatformData()
        {
            position = Vector2Int.zero;
            size = new Vector2Int(3, 1);
            platformType = PlatformType.Solid;
            moveSpeed = 0f;
            isOneWay = false;
            fallDelay = 1f;
            disappearTime = 2f;
            reappearTime = 3f;
            conveyorDirection = Vector2.right;
        }

        // 位置・サイズ指定コンストラクタ
        public PlatformData(Vector2Int pos, Vector2Int platformSize, PlatformType type = PlatformType.Solid)
        {
            position = pos;
            size = platformSize;
            platformType = type;
            moveSpeed = (type == PlatformType.Moving || type == PlatformType.Conveyor) ? 2f : 0f;
            isOneWay = (type == PlatformType.OneWay);
            fallDelay = 1f;
            disappearTime = 2f;
            reappearTime = 3f;
            conveyorDirection = Vector2.right;
        }
    }

    // 地形フィーチャーデータ
    [System.Serializable]
    public class TerrainFeatureData
    {
        public Vector2Int position;
        public TerrainFeatureType featureType;
        public Vector2Int size = Vector2Int.one;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        [Header("Feature Settings")]
        public bool isHazardous = false;       // 危険な地形かどうか
        public float damageAmount = 1f;        // ダメージ量（危険な場合）
        public bool isDecorative = false;     // 装飾用かどうか

        // デフォルトコンストラクタ
        public TerrainFeatureData()
        {
            position = Vector2Int.zero;
            featureType = TerrainFeatureType.Decoration;
            size = Vector2Int.one;
            parameters = new Dictionary<string, object>();
            isHazardous = false;
            damageAmount = 1f;
            isDecorative = true;
        }

        // フィーチャータイプ指定コンストラクタ
        public TerrainFeatureData(TerrainFeatureType type, Vector2Int pos, Vector2Int featureSize = default)
        {
            featureType = type;
            position = pos;
            size = featureSize == default ? Vector2Int.one : featureSize;
            parameters = new Dictionary<string, object>();

            // タイプに応じた初期設定
            switch (type)
            {
                case TerrainFeatureType.Spikes:
                    isHazardous = true;
                    damageAmount = 1f;
                    isDecorative = false;
                    break;
                case TerrainFeatureType.Pit:
                    isHazardous = true;
                    damageAmount = 1f;
                    isDecorative = false;
                    break;
                case TerrainFeatureType.Decoration:
                    isHazardous = false;
                    damageAmount = 0f;
                    isDecorative = true;
                    break;
                default:
                    isHazardous = false;
                    damageAmount = 0f;
                    isDecorative = false;
                    break;
            }
        }
    }

    #endregion

    #region 既存データ構造（互換性のため）

    // 既存のStageInfo（互換性維持）
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

    // 既存のStageTheme
    public enum StageTheme
    {
        Tech,
        Industrial,
        Organic,
        Crystal,
        Void
    }

    // 既存のObstacleType
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

    // 既存のCollectibleType
    public enum CollectibleType
    {
        EnergyChip,
        PowerUp,
        ExtraLife
    }

    // 既存のEnvironmentalType
    public enum EnvironmentalType
    {
        GravityWell,
        WindTunnel,
        SpringPlatform,
        MovingPlatform
    }

    // 既存のBackgroundLayerData
    [System.Serializable]
    public class BackgroundLayerData
    {
        public string layerName;
        public Sprite backgroundSprite;
        public float parallaxFactor = 0.5f; // 0.25f for far, 0.5f for mid, 0.75f for near
        public Vector2 tileSize = new Vector2(512, 512);
        public bool enableVerticalLoop = false;
        public Color tintColor = Color.white;
    }

    // 既存のObstacleData
    [System.Serializable]
    public class ObstacleData
    {
        public ObstacleType type;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }

    // 既存のCollectibleData
    [System.Serializable]
    public class CollectibleData
    {
        public CollectibleType type;
        public Vector3 position;
        public int value = 1;
    }

    // 既存のEnvironmentalData
    [System.Serializable]
    public class EnvironmentalData
    {
        public EnvironmentalType type;
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }

    #endregion

    #region ヘルパークラス・ユーティリティ

    // 地形エディター用のヘルパークラス
    public static class TerrainEditorHelper
    {
        public static TerrainSegmentData CreateFlatSegment(int index, Vector2Int start, Vector2Int size)
        {
            return new TerrainSegmentData
            {
                segmentIndex = index,
                startPosition = start,
                size = size,
                pattern = TerrainPattern.Flat,
                heightVariation = 0f
            };
        }

        public static TerrainSegmentData CreateHillSegment(int index, Vector2Int start, Vector2Int size, float height)
        {
            return new TerrainSegmentData
            {
                segmentIndex = index,
                startPosition = start,
                size = size,
                pattern = TerrainPattern.Hill,
                heightVariation = height
            };
        }

        public static TerrainSegmentData CreateStairsSegment(int index, Vector2Int start, Vector2Int size)
        {
            return new TerrainSegmentData
            {
                segmentIndex = index,
                startPosition = start,
                size = size,
                pattern = TerrainPattern.Stairs,
                heightVariation = 0f
            };
        }

        public static TerrainSegmentData CreateGapsSegment(int index, Vector2Int start, Vector2Int size)
        {
            return new TerrainSegmentData
            {
                segmentIndex = index,
                startPosition = start,
                size = size,
                pattern = TerrainPattern.Gaps,
                heightVariation = 0f
            };
        }

        public static PlatformData CreateSimplePlatform(Vector2Int position, Vector2Int size)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.Solid
            };
        }

        public static PlatformData CreateMovingPlatform(Vector2Int position, Vector2Int size, Vector2[] path, float speed)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.Moving,
                movePath = path,
                moveSpeed = speed
            };
        }

        public static PlatformData CreateFallingPlatform(Vector2Int position, Vector2Int size, float delay = 1f)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.Falling,
                fallDelay = delay
            };
        }

        public static PlatformData CreateDisappearingPlatform(Vector2Int position, Vector2Int size, float disappearTime = 2f, float reappearTime = 3f)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.Disappearing,
                disappearTime = disappearTime,
                reappearTime = reappearTime
            };
        }

        public static PlatformData CreateOneWayPlatform(Vector2Int position, Vector2Int size)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.OneWay,
                isOneWay = true
            };
        }

        public static PlatformData CreateConveyorPlatform(Vector2Int position, Vector2Int size, Vector2 direction, float speed)
        {
            return new PlatformData
            {
                position = position,
                size = size,
                platformType = PlatformType.Conveyor,
                moveSpeed = speed,
                conveyorDirection = direction
            };
        }

        public static TerrainFeatureData CreateSpike(Vector2Int position)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Spikes,
                size = Vector2Int.one,
                isHazardous = true,
                damageAmount = 1f
            };
        }

        public static TerrainFeatureData CreateRamp(Vector2Int position, Vector2Int size)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Ramp,
                size = size
            };
        }

        public static TerrainFeatureData CreateWall(Vector2Int position, Vector2Int size)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Wall,
                size = size
            };
        }

        public static TerrainFeatureData CreatePit(Vector2Int position, Vector2Int size)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Pit,
                size = size,
                isHazardous = true,
                damageAmount = 1f
            };
        }

        public static TerrainFeatureData CreateBridge(Vector2Int position, int length)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Bridge,
                size = new Vector2Int(length, 1)
            };
        }

        public static TerrainFeatureData CreateTunnel(Vector2Int position, Vector2Int size)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Tunnel,
                size = size
            };
        }

        public static TerrainFeatureData CreateDecoration(Vector2Int position, Vector2Int size)
        {
            return new TerrainFeatureData
            {
                position = position,
                featureType = TerrainFeatureType.Decoration,
                size = size,
                isDecorative = true
            };
        }
    }

    // 地形データの検証クラス
    public static class TerrainDataValidator
    {
        public static bool ValidateStageData(StageDataSO stageData)
        {
            bool isValid = true;
            List<string> errors = new List<string>();

            if (stageData == null)
            {
                errors.Add("StageData is null");
                isValid = false;
            }
            else
            {
                // 基本的な検証
                if (stageData.terrainLayers == null || stageData.terrainLayers.Length == 0)
                {
                    errors.Add("No terrain layers defined");
                    isValid = false;
                }
                else
                {
                    // 地形レイヤーの検証
                    for (int i = 0; i < stageData.terrainLayers.Length; i++)
                    {
                        var layer = stageData.terrainLayers[i];
                        if (layer == null)
                        {
                            errors.Add($"Terrain layer {i} is null");
                            isValid = false;
                        }
                        else if (layer.tileVariants == null || layer.tileVariants.Length == 0)
                        {
                            errors.Add($"Terrain layer {i} ({layer.layerName}) has no tile variants");
                            // これは警告レベル（必ずしもエラーではない）
                        }
                    }
                }

                // セグメントデータの検証
                if (stageData.terrainSegments != null)
                {
                    for (int i = 0; i < stageData.terrainSegments.Length; i++)
                    {
                        var segment = stageData.terrainSegments[i];
                        if (segment != null)
                        {
                            if (segment.size.x <= 0 || segment.size.y <= 0)
                            {
                                errors.Add($"Terrain segment {i} has invalid size: {segment.size}");
                                isValid = false;
                            }

                            // プラットフォームの検証
                            if (segment.platforms != null)
                            {
                                for (int j = 0; j < segment.platforms.Count; j++)
                                {
                                    var platform = segment.platforms[j];
                                    if (platform.size.x <= 0 || platform.size.y <= 0)
                                    {
                                        errors.Add($"Platform {j} in segment {i} has invalid size: {platform.size}");
                                        isValid = false;
                                    }
                                }
                            }

                            // フィーチャーの検証
                            if (segment.features != null)
                            {
                                for (int j = 0; j < segment.features.Count; j++)
                                {
                                    var feature = segment.features[j];
                                    if (feature.size.x <= 0 || feature.size.y <= 0)
                                    {
                                        errors.Add($"Feature {j} in segment {i} has invalid size: {feature.size}");
                                        isValid = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!isValid)
            {
                Debug.LogError("Stage data validation failed:\n" + string.Join("\n", errors));
            }
            else
            {
                Debug.Log("Stage data validation passed");
            }

            return isValid;
        }

        public static void LogTerrainInfo(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogWarning("StageData is null");
                return;
            }

            Debug.Log("=== Terrain Data Info ===");
            Debug.Log($"Stage: {stageData.stageInfo?.stageName ?? "Unknown"}");
            Debug.Log($"Tile map size: {stageData.tileMapSize}");
            Debug.Log($"Tile size: {stageData.tileSize}");
            Debug.Log($"Terrain layers: {stageData.terrainLayers?.Length ?? 0}");
            Debug.Log($"Terrain segments: {stageData.terrainSegments?.Length ?? 0}");

            if (stageData.terrainLayers != null)
            {
                for (int i = 0; i < stageData.terrainLayers.Length; i++)
                {
                    var layer = stageData.terrainLayers[i];
                    if (layer != null)
                    {
                        Debug.Log($"Layer {i}: {layer.layerName} ({layer.layerType})");
                        Debug.Log($"  Tiles: {layer.tileVariants?.Length ?? 0}");
                        Debug.Log($"  Auto-generate: {layer.autoGenerate}");
                        Debug.Log($"  Base height: {layer.baseHeight}");
                        Debug.Log($"  Thickness: {layer.thickness}");
                    }
                }
            }

            if (stageData.terrainSegments != null)
            {
                int totalPlatforms = 0;
                int totalFeatures = 0;

                foreach (var segment in stageData.terrainSegments)
                {
                    if (segment != null)
                    {
                        totalPlatforms += segment.platforms?.Count ?? 0;
                        totalFeatures += segment.features?.Count ?? 0;
                    }
                }

                Debug.Log($"Total platforms: {totalPlatforms}");
                Debug.Log($"Total features: {totalFeatures}");
            }
        }

        public static List<string> GetValidationWarnings(StageDataSO stageData)
        {
            List<string> warnings = new List<string>();

            if (stageData == null) return warnings;

            // タイルアセットの警告
            if (stageData.terrainLayers != null)
            {
                for (int i = 0; i < stageData.terrainLayers.Length; i++)
                {
                    var layer = stageData.terrainLayers[i];
                    if (layer != null && layer.autoGenerate && (layer.tileVariants == null || layer.tileVariants.Length == 0))
                    {
                        warnings.Add($"Layer '{layer.layerName}' is set to auto-generate but has no tile variants assigned");
                    }
                }
            }

            // プロシージャル生成の警告
            if (stageData.enableProceduralGeneration && stageData.heightMap == null)
            {
                warnings.Add("Procedural generation is enabled but no height map is assigned");
            }

            return warnings;
        }
    }

    #endregion
}