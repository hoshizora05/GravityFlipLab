using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Gravity Flip Lab/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("Stage Information")]
        public StageInfo stageInfo;

        [Header("Background Layers")]
        public BackgroundLayerData[] backgroundLayers = new BackgroundLayerData[3];

        [Header("Obstacles")]
        public List<ObstacleData> obstacles = new List<ObstacleData>();

        [Header("Collectibles")]
        public List<CollectibleData> collectibles = new List<CollectibleData>();

        [Header("Environmental")]
        public List<EnvironmentalData> environmental = new List<EnvironmentalData>();

        [Header("Terrain Configuration")]
        public TerrainLayerData[] terrainLayers = new TerrainLayerData[3];
        public TerrainSegmentData[] terrainSegments = new TerrainSegmentData[16];

        [Header("Tilemap Settings")]
        public Vector2Int tileMapSize = new Vector2Int(256, 64); // 256x64タイル
        public float tileSize = 16f; // 1タイル = 16ピクセル
        public bool useCompositeCollider = true;

        [Header("Advanced Features")]
        public Texture2D heightMap;
        public AnimationCurve terrainCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool enableProceduralGeneration = false;
        public int proceduralSeed = 0;

        [Header("Slopes")]
        public List<SlopeData> slopes = new List<SlopeData>();

        private void OnValidate()
        {
            // 初期化処理
            InitializeTerrainLayers();
            InitializeTerrainSegments();
            ValidateTerrainData();
            ValidateSlopes();
        }

        private void InitializeTerrainLayers()
        {
            if (terrainLayers == null || terrainLayers.Length != 3)
            {
                terrainLayers = new TerrainLayerData[3];
            }

            // デフォルト設定の適用
            for (int i = 0; i < terrainLayers.Length; i++)
            {
                if (terrainLayers[i] == null)
                {
                    terrainLayers[i] = CreateDefaultTerrainLayer(i);
                }
            }
        }

        private TerrainLayerData CreateDefaultTerrainLayer(int index)
        {
            TerrainLayerData layer = new TerrainLayerData();

            switch (index)
            {
                case 0: // Ground Layer
                    layer.layerName = "Ground";
                    layer.layerType = TerrainLayerType.Ground;
                    layer.baseHeight = -5f;
                    layer.thickness = 3f;
                    layer.autoGenerate = true;
                    layer.sortingOrder = 0;
                    break;
                case 1: // Platform Layer
                    layer.layerName = "Platforms";
                    layer.layerType = TerrainLayerType.Platform;
                    layer.baseHeight = 0f;
                    layer.thickness = 1f;
                    layer.autoGenerate = false;
                    layer.sortingOrder = 10;
                    break;
                case 2: // Background Layer
                    layer.layerName = "Background";
                    layer.layerType = TerrainLayerType.Background;
                    layer.baseHeight = -10f;
                    layer.thickness = 1f;
                    layer.autoGenerate = false;
                    layer.sortingOrder = -10;
                    break;
            }

            return layer;
        }

        private void InitializeTerrainSegments()
        {
            if (terrainSegments == null || terrainSegments.Length != 16)
            {
                terrainSegments = new TerrainSegmentData[16];
            }

            if (stageInfo != null)
            {
                float segmentWidth = stageInfo.stageLength / 16f; // 16セグメント

                for (int i = 0; i < terrainSegments.Length; i++)
                {
                    if (terrainSegments[i] == null)
                    {
                        terrainSegments[i] = CreateDefaultTerrainSegment(i, segmentWidth);
                    }
                }
            }
        }

        private TerrainSegmentData CreateDefaultTerrainSegment(int index, float segmentWidth)
        {
            int segmentWidthInTiles = Mathf.RoundToInt(segmentWidth / tileSize);

            return new TerrainSegmentData
            {
                segmentIndex = index,
                startPosition = new Vector2Int(index * segmentWidthInTiles, -5),
                size = new Vector2Int(segmentWidthInTiles, 3),
                pattern = TerrainPattern.Flat,
                heightVariation = 0f,
                platforms = new List<PlatformData>(),
                features = new List<TerrainFeatureData>()
            };
        }

        private void ValidateTerrainData()
        {
            bool hasValidationErrors = false;

            // 基本的な検証
            if (terrainLayers != null)
            {
                for (int i = 0; i < terrainLayers.Length; i++)
                {
                    if (terrainLayers[i] == null)
                    {
                        Debug.LogWarning($"Terrain layer {i} is null in {name}");
                        hasValidationErrors = true;
                    }
                }
            }

            // セグメントデータの検証
            if (terrainSegments != null)
            {
                for (int i = 0; i < terrainSegments.Length; i++)
                {
                    if (terrainSegments[i] != null)
                    {
                        if (terrainSegments[i].size.x <= 0 || terrainSegments[i].size.y <= 0)
                        {
                            Debug.LogWarning($"Invalid segment size in segment {i} of {name}");
                            hasValidationErrors = true;
                        }
                    }
                }
            }

            if (!hasValidationErrors)
            {
                Debug.Log($"Terrain data validation passed for {name}");
            }
        }

        // エディター用ヘルパーメソッド
        [ContextMenu("Initialize Default Terrain")]
        public void InitializeDefaultTerrain()
        {
            InitializeTerrainLayers();
            InitializeTerrainSegments();

            Debug.Log($"Default terrain initialized for {name}");
        }

        [ContextMenu("Generate Test Terrain")]
        public void GenerateTestTerrain()
        {
            // テスト用の地形パターンを生成
            if (terrainSegments != null && terrainSegments.Length >= 8)
            {
                // フラット地形
                terrainSegments[0].pattern = TerrainPattern.Flat;
                terrainSegments[1].pattern = TerrainPattern.Flat;

                // 上り坂
                terrainSegments[2].pattern = TerrainPattern.Ascending;
                terrainSegments[2].heightVariation = 3f;

                // 山型
                terrainSegments[3].pattern = TerrainPattern.Hill;
                terrainSegments[3].heightVariation = 4f;

                // 階段
                terrainSegments[4].pattern = TerrainPattern.Stairs;

                // ギャップ
                terrainSegments[5].pattern = TerrainPattern.Gaps;

                // 下り坂
                terrainSegments[6].pattern = TerrainPattern.Descending;
                terrainSegments[6].heightVariation = 3f;

                // プラットフォーム
                terrainSegments[7].pattern = TerrainPattern.Platforms;
                terrainSegments[7].platforms = new List<PlatformData>
                {
                    TerrainEditorHelper.CreateSimplePlatform(new Vector2Int(5, 2), new Vector2Int(4, 1)),
                    TerrainEditorHelper.CreateSimplePlatform(new Vector2Int(12, 4), new Vector2Int(3, 1))
                };

                Debug.Log($"Test terrain patterns generated for {name}");
            }
        }

        [ContextMenu("Add Sample Features")]
        public void AddSampleFeatures()
        {
            if (terrainSegments != null && terrainSegments.Length > 0)
            {
                // スパイクを追加
                terrainSegments[1].features.Add(TerrainEditorHelper.CreateSpike(new Vector2Int(8, -2)));
                terrainSegments[1].features.Add(TerrainEditorHelper.CreateSpike(new Vector2Int(12, -2)));

                // 傾斜を追加
                terrainSegments[3].features.Add(TerrainEditorHelper.CreateRamp(new Vector2Int(2, -2), new Vector2Int(6, 3)));

                Debug.Log($"Sample features added to {name}");
            }
        }

        [ContextMenu("Clear All Terrain")]
        public void ClearAllTerrain()
        {
            for (int i = 0; i < terrainSegments.Length; i++)
            {
                if (terrainSegments[i] != null)
                {
                    terrainSegments[i].platforms.Clear();
                    terrainSegments[i].features.Clear();
                    terrainSegments[i].pattern = TerrainPattern.Flat;
                    terrainSegments[i].heightVariation = 0f;
                }
            }

            Debug.Log($"All terrain cleared for {name}");
        }

        // ランタイム用メソッド
        public TerrainLayerData GetPrimaryTerrainLayer()
        {
            return terrainLayers != null && terrainLayers.Length > 0 ? terrainLayers[0] : null;
        }

        public TerrainLayerData GetTerrainLayerByType(TerrainLayerType layerType)
        {
            if (terrainLayers == null) return null;

            foreach (var layer in terrainLayers)
            {
                if (layer != null && layer.layerType == layerType)
                {
                    return layer;
                }
            }

            return null;
        }

        public TerrainSegmentData GetSegmentByIndex(int index)
        {
            if (terrainSegments == null || index < 0 || index >= terrainSegments.Length)
                return null;

            return terrainSegments[index];
        }

        public Vector2Int GetSegmentCoordinateFromWorldPosition(Vector3 worldPosition)
        {
            if (stageInfo == null) return Vector2Int.zero;

            float segmentWidth = stageInfo.stageLength / terrainSegments.Length;
            int segmentX = Mathf.FloorToInt(worldPosition.x / segmentWidth);
            int segmentY = 0; // 現在は2Dのため Y は 0

            return new Vector2Int(
                Mathf.Clamp(segmentX, 0, terrainSegments.Length - 1),
                segmentY
            );
        }

        public bool HasValidTerrainData()
        {
            if (terrainLayers == null || terrainSegments == null) return false;

            // 少なくとも1つの有効な地形レイヤーがあるか
            foreach (var layer in terrainLayers)
            {
                if (layer != null && layer.tileVariants != null && layer.tileVariants.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // ステージの全体統計を取得
        public TerrainStatistics GetTerrainStatistics()
        {
            TerrainStatistics stats = new TerrainStatistics();

            if (terrainSegments != null)
            {
                foreach (var segment in terrainSegments)
                {
                    if (segment != null)
                    {
                        stats.totalSegments++;
                        stats.totalPlatforms += segment.platforms?.Count ?? 0;
                        stats.totalFeatures += segment.features?.Count ?? 0;

                        switch (segment.pattern)
                        {
                            case TerrainPattern.Flat:
                                stats.flatSegments++;
                                break;
                            case TerrainPattern.Ascending:
                            case TerrainPattern.Descending:
                                stats.slopeSegments++;
                                break;
                            case TerrainPattern.Gaps:
                                stats.gapSegments++;
                                break;
                            case TerrainPattern.Platforms:
                                stats.platformSegments++;
                                break;
                        }
                    }
                }
            }

            return stats;
        }
        public void ValidateSlopes()
        {
            // 傾斜データの検証
            if (slopes != null)
            {
                for (int i = 0; i < slopes.Count; i++)
                {
                    var slope = slopes[i];
                    if (slope != null && !slope.IsValid())
                    {
                        Debug.LogWarning($"Invalid slope data at index {i} in {name}");

                        // 自動修正を試行
                        if (slope.slopeAngle <= 0f || slope.slopeAngle > 60f)
                        {
                            slope.slopeAngle = Mathf.Clamp(slope.slopeAngle, 5f, 45f);
                        }
                        if (slope.slopeLength <= 0f)
                        {
                            slope.slopeLength = 5f;
                        }
                        if (slope.speedMultiplier <= 0f)
                        {
                            slope.speedMultiplier = 1.2f;
                        }
                    }
                }
            }
        }
        // エディター用のコンテキストメニューを追加
        [ContextMenu("Add Sample Slopes")]
        public void AddSampleSlopes()
        {
            if (slopes == null)
                slopes = new List<SlopeData>();

            // 基本的な傾斜のサンプルを追加
            slopes.Add(SlopeDataHelper.CreateBasicSlope(new Vector3(20f, -2f, 0f), 30f, SlopeDirection.Ascending));
            slopes.Add(SlopeDataHelper.CreateSteepSlope(new Vector3(35f, -2f, 0f), SlopeDirection.Descending));
            slopes.Add(SlopeDataHelper.CreateGentleSlope(new Vector3(50f, -2f, 0f), SlopeDirection.Ascending));
            slopes.Add(SlopeDataHelper.CreateSpringSlope(new Vector3(65f, -2f, 0f), 20f));
            slopes.Add(SlopeDataHelper.CreateIceSlope(new Vector3(80f, -2f, 0f), 35f, SlopeDirection.Descending));

            Debug.Log($"Added 5 sample slopes to {name}");
        }

        [ContextMenu("Create Hill Pattern")]
        public void CreateHillPattern()
        {
            if (slopes == null)
                slopes = new List<SlopeData>();

            // 山型パターンの追加
            var hillSlopes = SlopeDataHelper.CreateHillSlopes(new Vector3(100f, -2f, 0f), 8f);
            slopes.AddRange(hillSlopes);

            Debug.Log($"Added hill pattern to {name}");
        }

        [ContextMenu("Create Stair Pattern")]
        public void CreateStairPattern()
        {
            if (slopes == null)
                slopes = new List<SlopeData>();

            // 階段パターンの追加
            var stairSlopes = SlopeDataHelper.CreateStairSlopes(new Vector3(120f, -2f, 0f), 4, 5f);
            slopes.AddRange(stairSlopes);

            Debug.Log($"Added stair pattern to {name}");
        }

        [ContextMenu("Clear All Slopes")]
        public void ClearAllSlopes()
        {
            if (slopes != null)
            {
                int count = slopes.Count;
                slopes.Clear();
                Debug.Log($"Cleared {count} slopes from {name}");
            }
        }

        [ContextMenu("Validate Slopes")]
        public void ValidateSlopesManually()
        {
            if (slopes == null)
            {
                Debug.Log("No slopes to validate");
                return;
            }

            bool isValid = SlopeDataValidator.ValidateSlopeDataList(slopes);
            SlopeDataValidator.LogSlopeInfo(slopes);

            if (isValid)
            {
                Debug.Log($"All {slopes.Count} slopes are valid in {name}");
            }
        }

        // ランタイム用のアクセサメソッド
        public List<SlopeData> GetSlopes()
        {
            return slopes ?? new List<SlopeData>();
        }

        public SlopeData GetSlopeByIndex(int index)
        {
            if (slopes == null || index < 0 || index >= slopes.Count)
                return null;
            return slopes[index];
        }

        public List<SlopeData> GetSlopesByType(SlopeType type)
        {
            List<SlopeData> result = new List<SlopeData>();
            if (slopes != null)
            {
                foreach (var slope in slopes)
                {
                    if (slope != null && slope.type == type)
                    {
                        result.Add(slope);
                    }
                }
            }
            return result;
        }

        public bool HasSlopes()
        {
            return slopes != null && slopes.Count > 0;
        }

        public int GetSlopeCount()
        {
            return slopes?.Count ?? 0;
        }

        public void AddSlopeStatistics(ref TerrainStatistics stats)
        {
            if (slopes != null)
            {
                stats.totalSlopes = slopes.Count;

                // タイプ別カウント
                foreach (var slope in slopes)
                {
                    if (slope != null)
                    {
                        switch (slope.type)
                        {
                            case SlopeType.BasicSlope:
                                stats.basicSlopes++;
                                break;
                            case SlopeType.SteepSlope:
                                stats.steepSlopes++;
                                break;
                            case SlopeType.SpringSlope:
                                stats.specialSlopes++;
                                break;
                            case SlopeType.IceSlope:
                                stats.specialSlopes++;
                                break;
                            default:
                                stats.otherSlopes++;
                                break;
                        }
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class TerrainStatistics
    {
        public int totalSegments;
        public int flatSegments;
        public int slopeSegments;
        public int gapSegments;
        public int platformSegments;
        public int totalPlatforms;
        public int totalFeatures;

        [Header("Slope Statistics")]
        public int totalSlopes;
        public int basicSlopes;
        public int steepSlopes;
        public int specialSlopes;
        public int otherSlopes;

        public float GetDifficultyScore()
        {
            float baseScore = 0f;
            baseScore += slopeSegments * 1.2f;
            baseScore += gapSegments * 1.5f;
            baseScore += platformSegments * 1.3f;
            baseScore += totalFeatures * 0.8f;

            return baseScore / Mathf.Max(1, totalSegments);
        }
        // 難易度計算に傾斜を含める
        public float CalculateSlopeDifficulty()
        {
            float slopeScore = 0f;
            slopeScore += basicSlopes * 0.5f;
            slopeScore += steepSlopes * 1.2f;
            slopeScore += specialSlopes * 1.5f;
            slopeScore += otherSlopes * 0.8f;

            return slopeScore;
        }

        public override string ToString()
        {
            return $"Segments: {totalSegments}, Platforms: {totalPlatforms}, Features: {totalFeatures}, Difficulty: {GetDifficultyScore():F1} Slopes: {totalSlopes} (Basic: {basicSlopes}, Steep: {steepSlopes}, Special: {specialSlopes})";
        }
    }
}