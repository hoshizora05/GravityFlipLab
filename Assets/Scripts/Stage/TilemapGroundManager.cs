using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class TilemapGroundManager : MonoBehaviour
    {
        [Header("Tilemap References")]
        public Tilemap foregroundTilemap;
        public TilemapRenderer tilemapRenderer;
        public TilemapCollider2D tilemapCollider;
        public CompositeCollider2D compositeCollider;

        [Header("Ground Tiles")]
        public TileBase[] groundTiles;
        public TileBase[] platformTiles;
        public TileBase[] wallTiles;
        public TileBase[] ceilingTiles;

        [Header("Tile Settings")]
        public PhysicsMaterial2D groundPhysicsMaterial;
        public LayerMask groundLayer = 1;
        public int tilemapSortingOrder = 100;
        [Tooltip("None: 全てのタイルコライダーを統合 / Intersect: 重複部分のみ / Merge: 隣接タイルを結合")]
        public Collider2D.CompositeOperation compositeOperation = Collider2D.CompositeOperation.None;

        [Header("Generation Settings")]
        public bool autoGenerateGround = true;
        public bool autoGenerateCeiling = true;
        public float groundLevel = -5f;
        public float groundThickness = 2f;
        public float ceilingLevel = 10f;
        public float ceilingThickness = 2f;
        public int stageWidthInTiles = 256; // 4096ピクセル ÷ 16ピクセル/タイル

        [Header("Platform Generation")]
        public Vector2Int[] platformPositions;
        public Vector2Int[] platformSizes;

        [Header("Ceiling Platform Generation")]
        public Vector2Int[] ceilingPlatformPositions;
        public Vector2Int[] ceilingPlatformSizes;

        [Header("Debug")]
        public bool showDebugInfo = false;
        public bool enableRuntimeEdit = false;

        private Dictionary<Vector3Int, TileType> tileTypeMap = new Dictionary<Vector3Int, TileType>();
        private StageDataSO currentStageData;

        public enum TileType
        {
            None,
            Ground,
            Platform,
            Wall,
            Ceiling,
            CeilingPlatform,
            OneWayPlatform
        }

        private void Awake()
        {
            InitializeTilemapComponents();
        }

        private void Start()
        {
            SetupTilemapConfiguration();

            if (autoGenerateGround)
            {
                GenerateBasicGround();
            }

            if (autoGenerateCeiling)
            {
                GenerateBasicCeiling();
            }
        }

        private void InitializeTilemapComponents()
        {
            // Tilemapコンポーネントの取得または作成
            if (foregroundTilemap == null)
            {
                GameObject tilemapObj = new GameObject("ForegroundTilemap");
                tilemapObj.transform.SetParent(transform);

                foregroundTilemap = tilemapObj.AddComponent<Tilemap>();
                tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();
            }

            // TilemapCollider2Dの設定
            if (tilemapCollider == null)
            {
                tilemapCollider = foregroundTilemap.GetComponent<TilemapCollider2D>();
                if (tilemapCollider == null)
                {
                    tilemapCollider = foregroundTilemap.gameObject.AddComponent<TilemapCollider2D>();
                }
            }

            // CompositeCollider2Dの設定（パフォーマンス向上のため）
            if (compositeCollider == null)
            {
                compositeCollider = foregroundTilemap.GetComponent<CompositeCollider2D>();
                if (compositeCollider == null)
                {
                    compositeCollider = foregroundTilemap.gameObject.AddComponent<CompositeCollider2D>();

                    // Rigidbody2Dも必要
                    Rigidbody2D rb = foregroundTilemap.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = foregroundTilemap.gameObject.AddComponent<Rigidbody2D>();
                    }
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }
        }

        private void SetupTilemapConfiguration()
        {
            // レイヤー設定
            foregroundTilemap.gameObject.layer = Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2));

            // ソートオーダー設定
            tilemapRenderer.sortingOrder = tilemapSortingOrder;

            // 物理マテリアル設定
            if (groundPhysicsMaterial != null)
            {
                tilemapCollider.sharedMaterial = groundPhysicsMaterial;
            }

            // CompositeCollider2D設定
            // 地面と天井が離れている場合はNoneを使用（Intersectだと天井コライダーが消失する）
            tilemapCollider.compositeOperation = compositeOperation;
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.generationType = CompositeCollider2D.GenerationType.Synchronous;
        }

        public void LoadGroundFromStageData(StageDataSO stageData)
        {
            currentStageData = stageData;
            ClearAllTiles();

            if (stageData?.stageInfo != null)
            {
                stageWidthInTiles = Mathf.RoundToInt(stageData.stageInfo.stageLength / 16f);
                GenerateGroundFromData(stageData);
            }
        }

        private void GenerateGroundFromData(StageDataSO stageData)
        {
            Debug.Log($"Generating terrain from stage data: {stageData.stageInfo.stageName}");

            // ステージサイズの設定
            stageWidthInTiles = Mathf.RoundToInt(stageData.stageInfo.stageLength / stageData.tileSize);

            // 地形レイヤーの処理
            if (stageData.terrainLayers != null && stageData.terrainLayers.Length > 0)
            {
                // 各地形レイヤーを生成
                foreach (var terrainLayer in stageData.terrainLayers)
                {
                    if (terrainLayer != null && terrainLayer.autoGenerate)
                    {
                        GenerateTerrainLayer(terrainLayer, stageData);
                    }
                }
            }
            else
            {
                Debug.LogWarning("No terrain layers found, generating basic terrain");
                GenerateBasicGround();
                GenerateBasicCeiling();
            }

            // 地形セグメントの処理
            if (stageData.terrainSegments != null && stageData.terrainSegments.Length > 0)
            {
                GenerateTerrainSegments(stageData.terrainSegments, stageData);
            }

            // 傾斜の処理
            if (stageData.HasSlopes())
            {
                GenerateSlopeTerrain(stageData.GetSlopes(), stageData);
            }

            // 壁の生成
            GenerateWalls();

            // タイルマップの最終処理
            RefreshTilemap();

            Debug.Log($"Terrain generation completed for stage: {stageData.stageInfo.stageName}");
        }

        private void GenerateTerrainLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            if (terrainLayer.tileVariants == null || terrainLayer.tileVariants.Length == 0)
            {
                Debug.LogWarning($"Terrain layer {terrainLayer.layerName} has no tile variants");
                return;
            }

            switch (terrainLayer.layerType)
            {
                case TerrainLayerType.Ground:
                    GenerateGroundLayer(terrainLayer, stageData);
                    break;
                case TerrainLayerType.Platform:
                    GeneratePlatformLayer(terrainLayer, stageData);
                    break;
                case TerrainLayerType.Background:
                case TerrainLayerType.Foreground:
                    // 背景・前景は現在のシステムでは処理しない
                    break;
                case TerrainLayerType.Collision:
                    GenerateCollisionLayer(terrainLayer, stageData);
                    break;
                case TerrainLayerType.OneWayPlatform:
                    GenerateOneWayPlatformLayer(terrainLayer, stageData);
                    break;
                case TerrainLayerType.Breakable:
                    GenerateBreakableLayer(terrainLayer, stageData);
                    break;
                case TerrainLayerType.Hazard:
                    GenerateHazardLayer(terrainLayer, stageData);
                    break;
            }
        }

        private void GenerateGroundLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            int baseHeight = Mathf.RoundToInt(terrainLayer.baseHeight);
            int thickness = Mathf.RoundToInt(terrainLayer.thickness);

            switch (terrainLayer.generationMode)
            {
                case TerrainGenerationMode.Flat:
                    GenerateFlatGround(terrainLayer, baseHeight, thickness, stageData);
                    break;
                case TerrainGenerationMode.Hilly:
                    GenerateHillyGround(terrainLayer, baseHeight, thickness, stageData);
                    break;
                case TerrainGenerationMode.Mountainous:
                    GenerateMountainousGround(terrainLayer, baseHeight, thickness, stageData);
                    break;
                case TerrainGenerationMode.Custom:
                    GenerateCustomGround(terrainLayer, baseHeight, thickness, stageData);
                    break;
                case TerrainGenerationMode.FromHeightmap:
                    if (stageData.heightMap != null)
                    {
                        GenerateHeightmapGround(terrainLayer, stageData);
                    }
                    else
                    {
                        GenerateFlatGround(terrainLayer, baseHeight, thickness, stageData);
                    }
                    break;
            }
        }

        private void GenerateFlatGround(TerrainLayerData terrainLayer, int baseHeight, int thickness, StageDataSO stageData)
        {
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = 0; y < thickness; y++)
                {
                    Vector3Int position = new Vector3Int(x, baseHeight - y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateHillyGround(TerrainLayerData terrainLayer, int baseHeight, int thickness, StageDataSO stageData)
        {
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                // ノイズを使用して丘陵地形を生成
                float noiseValue = Mathf.PerlinNoise(x * terrainLayer.noiseScale, terrainLayer.noiseSeed * 0.01f);
                int heightVariation = Mathf.RoundToInt(noiseValue * terrainLayer.noiseAmplitude);
                int currentHeight = thickness + heightVariation;

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(x, baseHeight + heightVariation - y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateMountainousGround(TerrainLayerData terrainLayer, int baseHeight, int thickness, StageDataSO stageData)
        {
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                // 複数のノイズレイヤーを組み合わせて山岳地形を生成
                float noise1 = Mathf.PerlinNoise(x * terrainLayer.noiseScale, terrainLayer.noiseSeed * 0.01f);
                float noise2 = Mathf.PerlinNoise(x * terrainLayer.noiseScale * 2f, terrainLayer.noiseSeed * 0.02f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(x * terrainLayer.noiseScale * 4f, terrainLayer.noiseSeed * 0.03f) * 0.25f;

                float combinedNoise = noise1 + noise2 + noise3;
                int heightVariation = Mathf.RoundToInt(combinedNoise * terrainLayer.noiseAmplitude);
                int currentHeight = thickness + heightVariation;

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(x, baseHeight + heightVariation - y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateCustomGround(TerrainLayerData terrainLayer, int baseHeight, int thickness, StageDataSO stageData)
        {
            if (stageData.enableProceduralGeneration)
            {
                // プロシージャル生成
                System.Random random = new System.Random(stageData.proceduralSeed + terrainLayer.noiseSeed);

                for (int x = 0; x < stageWidthInTiles; x++)
                {
                    // カスタム曲線を使用した高度計算
                    float t = x / (float)stageWidthInTiles;
                    float curveValue = stageData.terrainCurve.Evaluate(t);
                    int heightVariation = Mathf.RoundToInt(curveValue * terrainLayer.noiseAmplitude);
                    int currentHeight = thickness + heightVariation;

                    for (int y = 0; y < currentHeight; y++)
                    {
                        Vector3Int position = new Vector3Int(x, baseHeight + heightVariation - y, 0);
                        TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                        foregroundTilemap.SetTile(position, tile);
                        tileTypeMap[position] = TileType.Ground;
                    }
                }
            }
            else
            {
                // 通常のフラット地形
                GenerateFlatGround(terrainLayer, baseHeight, thickness, stageData);
            }
        }

        private void GenerateHeightmapGround(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            int baseHeight = Mathf.RoundToInt(terrainLayer.baseHeight);
            int thickness = Mathf.RoundToInt(terrainLayer.thickness);

            for (int x = 0; x < stageWidthInTiles; x++)
            {
                // ハイトマップから高度を取得
                float u = x / (float)stageWidthInTiles;
                Color pixelColor = stageData.heightMap.GetPixelBilinear(u, 0.5f);
                int heightVariation = Mathf.RoundToInt(pixelColor.r * terrainLayer.noiseAmplitude);
                int currentHeight = thickness + heightVariation;

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(x, baseHeight + heightVariation - y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GeneratePlatformLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            // プラットフォームレイヤーは個別のプラットフォーム配置として処理
            // 現在はplatformPositionsを使用
            foreach (var platformPos in platformPositions)
            {
                Vector2Int size = new Vector2Int(4, 1); // デフォルトサイズ

                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(platformPos.x + x, platformPos.y + y, 0);
                        TileBase tile = terrainLayer.GetTileForPosition(x, y, 0);

                        foregroundTilemap.SetTile(position, tile);
                        tileTypeMap[position] = TileType.Platform;
                    }
                }
            }
        }

        private void GenerateCollisionLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            // 衝突判定のみのレイヤー（見た目は透明）
            int baseHeight = Mathf.RoundToInt(terrainLayer.baseHeight);
            int thickness = Mathf.RoundToInt(terrainLayer.thickness);

            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = 0; y < thickness; y++)
                {
                    Vector3Int position = new Vector3Int(x, baseHeight - y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, y, y);

                    if (tile != null)
                    {
                        foregroundTilemap.SetTile(position, tile);
                        tileTypeMap[position] = TileType.Ground; // 衝突判定として扱う
                    }
                }
            }
        }

        private void GenerateOneWayPlatformLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            // ワンウェイプラットフォームレイヤー
            foreach (var platformPos in platformPositions)
            {
                Vector2Int size = new Vector2Int(4, 1);

                for (int x = 0; x < size.x; x++)
                {
                    Vector3Int position = new Vector3Int(platformPos.x + x, platformPos.y, 0);
                    TileBase tile = terrainLayer.GetTileForPosition(x, 0, 0);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.OneWayPlatform;
                }

                // ワンウェイプラットフォームの特別なコライダー設定
                SetupOneWayCollider(new Vector3Int(platformPos.x, platformPos.y, 0));
            }
        }

        private void GenerateBreakableLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            // 破壊可能なブロック（今回は通常の地形として扱う）
            GenerateFlatGround(terrainLayer, Mathf.RoundToInt(terrainLayer.baseHeight),
                              Mathf.RoundToInt(terrainLayer.thickness), stageData);
        }

        private void GenerateHazardLayer(TerrainLayerData terrainLayer, StageDataSO stageData)
        {
            // 危険地帯（今回は通常の地形として扱う）
            GenerateFlatGround(terrainLayer, Mathf.RoundToInt(terrainLayer.baseHeight),
                              Mathf.RoundToInt(terrainLayer.thickness), stageData);
        }

        private void GenerateTerrainSegments(TerrainSegmentData[] segments, StageDataSO stageData)
        {
            // プライマリ地形レイヤーを取得
            TerrainLayerData primaryLayer = GetPrimaryTerrainLayer(stageData);
            if (primaryLayer == null) return;

            foreach (var segment in segments)
            {
                if (segment != null)
                {
                    GenerateTerrainSegment(segment, primaryLayer, stageData);
                }
            }
        }

        private void GenerateTerrainSegment(TerrainSegmentData segment, TerrainLayerData layerData, StageDataSO stageData)
        {
            Vector2Int start = segment.startPosition;
            Vector2Int size = segment.size;

            switch (segment.pattern)
            {
                case TerrainPattern.Flat:
                    GenerateSegmentFlat(start, size, layerData);
                    break;
                case TerrainPattern.Ascending:
                    GenerateSegmentAscending(start, size, layerData, segment.heightVariation);
                    break;
                case TerrainPattern.Descending:
                    GenerateSegmentDescending(start, size, layerData, segment.heightVariation);
                    break;
                case TerrainPattern.Valley:
                    GenerateSegmentValley(start, size, layerData, segment.heightVariation);
                    break;
                case TerrainPattern.Hill:
                    GenerateSegmentHill(start, size, layerData, segment.heightVariation);
                    break;
                case TerrainPattern.Stairs:
                    GenerateSegmentStairs(start, size, layerData);
                    break;
                case TerrainPattern.Gaps:
                    GenerateSegmentGaps(start, size, layerData);
                    break;
                case TerrainPattern.Platforms:
                    GenerateSegmentPlatforms(segment, layerData);
                    break;
            }

            // セグメント内のプラットフォームを生成
            foreach (var platform in segment.platforms)
            {
                GenerateSegmentPlatform(platform, layerData, start);
            }

            // セグメント内の地形フィーチャーを生成
            foreach (var feature in segment.features)
            {
                GenerateSegmentFeature(feature, layerData, start);
            }
        }

        private void GenerateSegmentFlat(Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateSegmentAscending(Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float progress = x / (float)size.x;
                int heightOffset = Mathf.RoundToInt(progress * heightVariation);
                int currentHeight = size.y + heightOffset;

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y + heightOffset - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateSegmentDescending(Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float progress = 1f - (x / (float)size.x);
                int heightOffset = Mathf.RoundToInt(progress * heightVariation);
                int currentHeight = size.y + heightOffset;

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y + heightOffset - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateSegmentValley(Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float normalizedX = (x / (float)size.x) * 2f - 1f; // -1 to 1
                float valleyDepth = (1f - Mathf.Abs(normalizedX)) * heightVariation;
                int currentHeight = size.y + Mathf.RoundToInt(valleyDepth);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y + Mathf.RoundToInt(valleyDepth) - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateSegmentHill(Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float normalizedX = (x / (float)size.x) * 2f - 1f; // -1 to 1
                float hillHeight = (1f - normalizedX * normalizedX) * heightVariation; // 放物線
                int currentHeight = size.y + Mathf.RoundToInt(hillHeight);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y + Mathf.RoundToInt(hillHeight) - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(position, tile);
                    tileTypeMap[position] = TileType.Ground;
                }
            }
        }

        private void GenerateSegmentStairs(Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            int stepCount = Mathf.Min(8, size.x / 2);
            int stepWidth = Mathf.Max(1, size.x / stepCount);
            int stepHeight = 1;

            for (int step = 0; step < stepCount; step++)
            {
                int stepStartX = step * stepWidth;
                int stepEndX = Mathf.Min((step + 1) * stepWidth, size.x);
                int stepY = step * stepHeight;

                for (int x = stepStartX; x < stepEndX; x++)
                {
                    for (int y = 0; y <= stepY + size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(start.x + x, start.y + stepY - y, 0);
                        TileBase tile = layerData.GetTileForPosition(x, y, y);

                        foregroundTilemap.SetTile(position, tile);
                        tileTypeMap[position] = TileType.Ground;
                    }
                }
            }
        }

        private void GenerateSegmentGaps(Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            int gapWidth = 4;
            int platformWidth = 8;

            for (int x = 0; x < size.x; x++)
            {
                if (x % (gapWidth + platformWidth) < platformWidth)
                {
                    // プラットフォーム部分
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(start.x + x, start.y - y, 0);
                        TileBase tile = layerData.GetTileForPosition(x, y, y);

                        foregroundTilemap.SetTile(position, tile);
                        tileTypeMap[position] = TileType.Ground;
                    }
                }
                // ギャップ部分は何も生成しない
            }
        }

        private void GenerateSegmentPlatforms(TerrainSegmentData segment, TerrainLayerData layerData)
        {
            // プラットフォームのみのセグメント（基本地形なし）
            foreach (var platform in segment.platforms)
            {
                GenerateSegmentPlatform(platform, layerData, segment.startPosition);
            }
        }

        private void GenerateSegmentPlatform(PlatformData platform, TerrainLayerData layerData, Vector2Int segmentStart)
        {
            Vector2Int absolutePos = segmentStart + platform.position;

            for (int x = 0; x < platform.size.x; x++)
            {
                for (int y = 0; y < platform.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(absolutePos.x + x, absolutePos.y + y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(position, tile);

                    // プラットフォームタイプに応じたtileType設定
                    TileType tileType = platform.isOneWay ? TileType.OneWayPlatform : TileType.Platform;
                    tileTypeMap[position] = tileType;
                }
            }
        }

        private void GenerateSegmentFeature(TerrainFeatureData feature, TerrainLayerData layerData, Vector2Int segmentStart)
        {
            Vector2Int absolutePos = segmentStart + feature.position;

            switch (feature.featureType)
            {
                case TerrainFeatureType.Spikes:
                    GenerateFeatureSpikes(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Pit:
                    GenerateFeaturePit(absolutePos, feature);
                    break;
                case TerrainFeatureType.Ramp:
                    GenerateFeatureRamp(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Wall:
                    GenerateFeatureWall(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Ceiling:
                    GenerateFeatureCeiling(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Bridge:
                    GenerateFeatureBridge(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Tunnel:
                    GenerateFeatureTunnel(absolutePos, feature, layerData);
                    break;
                case TerrainFeatureType.Decoration:
                    GenerateFeatureDecoration(absolutePos, feature, layerData);
                    break;
            }
        }

        private void GenerateFeatureSpikes(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            Vector3Int tilePos = new Vector3Int(position.x, position.y, 0);
            TileBase tile = layerData.tileVariants[0];

            foregroundTilemap.SetTile(tilePos, tile);
            tileTypeMap[tilePos] = TileType.Ground;
        }

        private void GenerateFeaturePit(Vector2Int position, TerrainFeatureData feature)
        {
            // 穴の生成（タイルを削除）
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(position.x + x, position.y - y, 0);
                    foregroundTilemap.SetTile(tilePos, null);
                    tileTypeMap.Remove(tilePos);
                }
            }
        }

        private void GenerateFeatureRamp(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                int height = Mathf.RoundToInt((x / (float)feature.size.x) * feature.size.y);
                for (int y = 0; y <= height; y++)
                {
                    Vector3Int tilePos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ground;
                }
            }
        }

        private void GenerateFeatureWall(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Wall;
                }
            }
        }

        private void GenerateFeatureCeiling(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int tilePos = new Vector3Int(position.x + x, position.y - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ceiling;
                }
            }
        }

        private void GenerateFeatureBridge(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // 橋の生成（横一列のプラットフォーム）
            for (int x = 0; x < feature.size.x; x++)
            {
                Vector3Int tilePos = new Vector3Int(position.x + x, position.y, 0);
                TileBase tile = layerData.GetTileForPosition(x, 0, 0);

                foregroundTilemap.SetTile(tilePos, tile);
                tileTypeMap[tilePos] = TileType.Platform;
            }
        }

        private void GenerateFeatureTunnel(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // トンネルの生成（上下に壁、中央は空洞）
            int tunnelHeight = feature.size.y;
            int wallThickness = 1;

            for (int x = 0; x < feature.size.x; x++)
            {
                // 上の壁
                for (int y = 0; y < wallThickness; y++)
                {
                    Vector3Int tilePos = new Vector3Int(
                        position.x + x,
                        position.y + tunnelHeight / 2 + y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ceiling;
                }

                // 下の壁
                for (int y = 0; y < wallThickness; y++)
                {
                    Vector3Int tilePos = new Vector3Int(
                        position.x + x,
                        position.y - tunnelHeight / 2 - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, 0);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ground;
                }
            }
        }

        private void GenerateFeatureDecoration(Vector2Int position, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // 装飾用のタイル配置（50%の確率で配置）
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    int seed = (position.x + x) * 1000 + (position.y + y);
                    System.Random random = new System.Random(seed);

                    if (random.NextDouble() > 0.5)
                    {
                        Vector3Int tilePos = new Vector3Int(position.x + x, position.y + y, 0);
                        TileBase tile = layerData.GetTileForPosition(x, y, 0);

                        foregroundTilemap.SetTile(tilePos, tile);
                        tileTypeMap[tilePos] = TileType.Ground;
                    }
                }
            }
        }

        private void GenerateSlopeTerrain(List<SlopeData> slopes, StageDataSO stageData)
        {
            // 傾斜データからタイルマップに傾斜地形を生成
            TerrainLayerData primaryLayer = GetPrimaryTerrainLayer(stageData);
            if (primaryLayer == null) return;

            foreach (var slope in slopes)
            {
                if (slope != null && slope.IsValid())
                {
                    GenerateSlopeTiles(slope, primaryLayer);
                }
            }
        }

        private void GenerateSlopeTiles(SlopeData slope, TerrainLayerData layerData)
        {
            Vector2Int startPos = new Vector2Int(
                Mathf.RoundToInt(slope.position.x / 16f), // ワールド座標をタイル座標に変換
                Mathf.RoundToInt(slope.position.y / 16f)
            );

            int slopeWidthInTiles = Mathf.RoundToInt(slope.slopeLength / 16f);
            int maxHeightInTiles = Mathf.RoundToInt(Mathf.Tan(slope.slopeAngle * Mathf.Deg2Rad) * slope.slopeLength / 16f);

            switch (slope.slopeDirection)
            {
                case SlopeDirection.Ascending:
                    GenerateAscendingSlopeTiles(startPos, slopeWidthInTiles, maxHeightInTiles, layerData);
                    break;
                case SlopeDirection.Descending:
                    GenerateDescendingSlopeTiles(startPos, slopeWidthInTiles, maxHeightInTiles, layerData);
                    break;
                //case SlopeDirection.Both:
                //    // 山型傾斜
                //    int halfWidth = slopeWidthInTiles / 2;
                //    GenerateAscendingSlopeTiles(startPos, halfWidth, maxHeightInTiles, layerData);
                //    GenerateDescendingSlopeTiles(new Vector2Int(startPos.x + halfWidth, startPos.y + maxHeightInTiles),
                //                               halfWidth, maxHeightInTiles, layerData);
                //    break;
            }
        }

        private void GenerateAscendingSlopeTiles(Vector2Int start, int width, int maxHeight, TerrainLayerData layerData)
        {
            for (int x = 0; x < width; x++)
            {
                int currentHeight = Mathf.RoundToInt((x / (float)width) * maxHeight);

                for (int y = 0; y <= currentHeight + 2; y++) // +2 for thickness
                {
                    Vector3Int tilePos = new Vector3Int(start.x + x, start.y + currentHeight - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ground;
                }
            }
        }

        private void GenerateDescendingSlopeTiles(Vector2Int start, int width, int maxHeight, TerrainLayerData layerData)
        {
            for (int x = 0; x < width; x++)
            {
                int currentHeight = Mathf.RoundToInt(((width - x) / (float)width) * maxHeight);

                for (int y = 0; y <= currentHeight + 2; y++) // +2 for thickness
                {
                    Vector3Int tilePos = new Vector3Int(start.x + x, start.y + currentHeight - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y);

                    foregroundTilemap.SetTile(tilePos, tile);
                    tileTypeMap[tilePos] = TileType.Ground;
                }
            }
        }

        private TerrainLayerData GetPrimaryTerrainLayer(StageDataSO stageData)
        {
            if (stageData.terrainLayers == null || stageData.terrainLayers.Length == 0)
                return null;

            // 最初のGroundタイプレイヤーを探す
            foreach (var layer in stageData.terrainLayers)
            {
                if (layer != null && layer.layerType == TerrainLayerType.Ground &&
                    layer.tileVariants != null && layer.tileVariants.Length > 0)
                {
                    return layer;
                }
            }

            // なければ最初の有効なレイヤーを返す
            foreach (var layer in stageData.terrainLayers)
            {
                if (layer != null && layer.tileVariants != null && layer.tileVariants.Length > 0)
                {
                    return layer;
                }
            }

            return null;
        }

        public void GenerateBasicGround()
        {
            if (groundTiles == null || groundTiles.Length == 0)
            {
                Debug.LogWarning("Ground tiles not assigned!");
                return;
            }

            int groundLevelTile = Mathf.RoundToInt(groundLevel);
            int thicknessTiles = Mathf.RoundToInt(groundThickness);

            // 基本的な地面を生成
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = groundLevelTile; y > groundLevelTile - thicknessTiles; y--)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tileToPlace = GetGroundTileVariant(x, y);

                    foregroundTilemap.SetTile(position, tileToPlace);
                    tileTypeMap[position] = TileType.Ground;
                }
            }

            RefreshTilemap();
        }

        public void GenerateBasicCeiling()
        {
            if (ceilingTiles == null || ceilingTiles.Length == 0)
            {
                Debug.LogWarning("Ceiling tiles not assigned!");
                return;
            }

            int ceilingLevelTile = Mathf.RoundToInt(ceilingLevel);
            int thicknessTiles = Mathf.RoundToInt(ceilingThickness);

            // 基本的な天井を生成
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = ceilingLevelTile; y < ceilingLevelTile + thicknessTiles; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tileToPlace = GetCeilingTileVariant(x, y);

                    foregroundTilemap.SetTile(position, tileToPlace);
                    tileTypeMap[position] = TileType.Ceiling;
                }
            }

            RefreshTilemap();
        }

        private void GeneratePlatforms()
        {
            if (platformTiles == null || platformTiles.Length == 0) return;

            foreach (var platformPos in platformPositions)
            {
                // デフォルトサイズまたは指定サイズでプラットフォーム生成
                Vector2Int size = new Vector2Int(4, 1); // デフォルト

                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(platformPos.x + x, platformPos.y + y, 0);
                        TileBase tileToPlace = GetPlatformTileVariant(x, y, size);

                        foregroundTilemap.SetTile(position, tileToPlace);
                        tileTypeMap[position] = TileType.Platform;
                    }
                }
            }
        }

        private void GenerateCeilingPlatforms()
        {
            if (ceilingTiles == null || ceilingTiles.Length == 0) return;

            foreach (var ceilingPlatformPos in ceilingPlatformPositions)
            {
                // デフォルトサイズまたは指定サイズで天井プラットフォーム生成
                Vector2Int size = new Vector2Int(4, 1); // デフォルト

                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(ceilingPlatformPos.x + x, ceilingPlatformPos.y + y, 0);
                        TileBase tileToPlace = GetCeilingPlatformTileVariant(x, y, size);

                        foregroundTilemap.SetTile(position, tileToPlace);
                        tileTypeMap[position] = TileType.CeilingPlatform;
                    }
                }
            }
        }

        private void GenerateWalls()
        {
            if (wallTiles == null || wallTiles.Length == 0) return;

            //int wallHeight = 20; // タイル数
            int groundLevelTile = Mathf.RoundToInt(groundLevel);
            int ceilingLevelTile = Mathf.RoundToInt(ceilingLevel);

            // 壁の高さを地面から天井まで拡張
            int totalWallHeight = ceilingLevelTile - groundLevelTile + Mathf.RoundToInt(ceilingThickness);

            // 左の壁
            for (int y = groundLevelTile; y < groundLevelTile + totalWallHeight; y++)
            {
                Vector3Int position = new Vector3Int(-1, y, 0);
                foregroundTilemap.SetTile(position, wallTiles[0]);
                tileTypeMap[position] = TileType.Wall;
            }

            // 右の壁
            for (int y = groundLevelTile; y < groundLevelTile + totalWallHeight; y++)
            {
                Vector3Int position = new Vector3Int(stageWidthInTiles, y, 0);
                foregroundTilemap.SetTile(position, wallTiles[0]);
                tileTypeMap[position] = TileType.Wall;
            }
        }

        private TileBase GetGroundTileVariant(int x, int y)
        {
            // 地面タイルのバリエーション選択ロジック
            if (groundTiles.Length == 1) return groundTiles[0];

            // 位置に基づいたランダム選択（シード値使用で一貫性保証）
            int seed = x * 1000 + y;
            System.Random random = new System.Random(seed);
            return groundTiles[random.Next(groundTiles.Length)];
        }

        private TileBase GetCeilingTileVariant(int x, int y)
        {
            // 天井タイルのバリエーション選択ロジック
            if (ceilingTiles.Length == 1) return ceilingTiles[0];

            // 位置に基づいたランダム選択（シード値使用で一貫性保証）
            int seed = x * 1000 + y + 10000; // 地面と異なるシード値を使用
            System.Random random = new System.Random(seed);
            return ceilingTiles[random.Next(ceilingTiles.Length)];
        }

        private TileBase GetPlatformTileVariant(int localX, int localY, Vector2Int size)
        {
            if (platformTiles.Length == 1) return platformTiles[0];

            // プラットフォームの位置に応じたタイル選択
            if (localX == 0) return platformTiles[0]; // 左端
            if (localX == size.x - 1) return platformTiles[platformTiles.Length - 1]; // 右端
            return platformTiles[1]; // 中央
        }

        private TileBase GetCeilingPlatformTileVariant(int localX, int localY, Vector2Int size)
        {
            if (ceilingTiles.Length == 1) return ceilingTiles[0];

            // 天井プラットフォームの位置に応じたタイル選択
            if (localX == 0) return ceilingTiles[0]; // 左端
            if (localX == size.x - 1) return ceilingTiles[ceilingTiles.Length - 1]; // 右端
            return ceilingTiles.Length > 1 ? ceilingTiles[1] : ceilingTiles[0]; // 中央
        }

        public void ClearAllTiles()
        {
            foregroundTilemap.SetTilesBlock(foregroundTilemap.cellBounds, new TileBase[foregroundTilemap.cellBounds.size.x * foregroundTilemap.cellBounds.size.y * foregroundTilemap.cellBounds.size.z]);
            tileTypeMap.Clear();
            RefreshTilemap();
        }

        public void RefreshTilemap()
        {
            foregroundTilemap.RefreshAllTiles();
            tilemapCollider.enabled = false;
            tilemapCollider.enabled = true;
        }

        public TileType GetTileTypeAtPosition(Vector3 worldPosition)
        {
            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            return tileTypeMap.ContainsKey(cellPosition) ? tileTypeMap[cellPosition] : TileType.None;
        }

        public bool IsGroundAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType == TileType.Ground || tileType == TileType.Platform;
        }

        public bool IsCeilingAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType == TileType.Ceiling || tileType == TileType.CeilingPlatform;
        }

        public bool IsSolidTileAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType != TileType.None;
        }

        public Vector3 GetTileWorldPosition(Vector3Int cellPosition)
        {
            return foregroundTilemap.CellToWorld(cellPosition);
        }

        // ランタイムでのタイル編集機能（デバッグ用）
        public void SetTileAtWorldPosition(Vector3 worldPosition, TileBase tile, TileType tileType)
        {
            if (!enableRuntimeEdit) return;

            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            foregroundTilemap.SetTile(cellPosition, tile);
            tileTypeMap[cellPosition] = tileType;
        }

        public void RemoveTileAtWorldPosition(Vector3 worldPosition)
        {
            if (!enableRuntimeEdit) return;

            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            foregroundTilemap.SetTile(cellPosition, null);
            tileTypeMap.Remove(cellPosition);
        }

        // プラットフォーム追加用のヘルパーメソッド
        public void AddPlatform(Vector2Int position, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tileToPlace = GetPlatformTileVariant(x, y, size);

                    foregroundTilemap.SetTile(cellPos, tileToPlace);
                    tileTypeMap[cellPos] = TileType.Platform;
                }
            }
            RefreshTilemap();
        }

        // 天井プラットフォーム追加用のヘルパーメソッド
        public void AddCeilingPlatform(Vector2Int position, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tileToPlace = GetCeilingPlatformTileVariant(x, y, size);

                    foregroundTilemap.SetTile(cellPos, tileToPlace);
                    tileTypeMap[cellPos] = TileType.CeilingPlatform;
                }
            }
            RefreshTilemap();
        }

        // ワンウェイプラットフォーム用の特別処理
        public void AddOneWayPlatform(Vector2Int position, int width)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cellPos = new Vector3Int(position.x + x, position.y, 0);
                foregroundTilemap.SetTile(cellPos, platformTiles[0]);
                tileTypeMap[cellPos] = TileType.OneWayPlatform;

                // ワンウェイプラットフォーム用のコライダー設定
                SetupOneWayCollider(cellPos);
            }
            RefreshTilemap();
        }

        // 天井用ワンウェイプラットフォーム（重力反転時に使用）
        public void AddOneWayCeilingPlatform(Vector2Int position, int width)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cellPos = new Vector3Int(position.x + x, position.y, 0);
                foregroundTilemap.SetTile(cellPos, ceilingTiles[0]);
                tileTypeMap[cellPos] = TileType.OneWayPlatform;

                // 天井用ワンウェイプラットフォームのコライダー設定
                SetupOneWayCeilingCollider(cellPos);
            }
            RefreshTilemap();
        }

        private void SetupOneWayCollider(Vector3Int cellPosition)
        {
            // ワンウェイプラットフォーム用の特別なコライダー設定
            Vector3 worldPos = foregroundTilemap.CellToWorld(cellPosition);

            GameObject oneWayPlatform = new GameObject($"OneWayPlatform_{cellPosition.x}_{cellPosition.y}");
            oneWayPlatform.transform.position = worldPos;
            oneWayPlatform.transform.SetParent(transform);

            BoxCollider2D platformCollider = oneWayPlatform.AddComponent<BoxCollider2D>();
            platformCollider.size = Vector2.one;

            PlatformEffector2D effector = oneWayPlatform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            effector.rotationalOffset = 0f; // 通常のプラットフォーム（下から上へ）

            platformCollider.usedByEffector = true;
        }

        private void SetupOneWayCeilingCollider(Vector3Int cellPosition)
        {
            // 天井用ワンウェイプラットフォームの特別なコライダー設定
            Vector3 worldPos = foregroundTilemap.CellToWorld(cellPosition);

            GameObject oneWayCeiling = new GameObject($"OneWayCeiling_{cellPosition.x}_{cellPosition.y}");
            oneWayCeiling.transform.position = worldPos;
            oneWayCeiling.transform.SetParent(transform);

            BoxCollider2D ceilingCollider = oneWayCeiling.AddComponent<BoxCollider2D>();
            ceilingCollider.size = Vector2.one;

            PlatformEffector2D effector = oneWayCeiling.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            effector.rotationalOffset = 180f; // 天井用（上から下へ）

            ceilingCollider.usedByEffector = true;
        }

        // ギズモによるデバッグ表示
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || foregroundTilemap == null) return;

            // タイルマップの境界を表示
            Gizmos.color = Color.green;
            BoundsInt bounds = foregroundTilemap.cellBounds;
            Vector3 center = foregroundTilemap.CellToWorld(new Vector3Int((int)bounds.center.x, (int)bounds.center.y, 0));
            Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 1);
            Gizmos.DrawWireCube(center, size);

            // 地面レベルの表示
            Gizmos.color = Color.blue;
            Vector3 groundStart = new Vector3(0, groundLevel, 0);
            Vector3 groundEnd = new Vector3(stageWidthInTiles, groundLevel, 0);
            Gizmos.DrawLine(groundStart, groundEnd);

            // 天井レベルの表示
            Gizmos.color = Color.red;
            Vector3 ceilingStart = new Vector3(0, ceilingLevel, 0);
            Vector3 ceilingEnd = new Vector3(stageWidthInTiles, ceilingLevel, 0);
            Gizmos.DrawLine(ceilingStart, ceilingEnd);

            // プラットフォーム位置の表示
            Gizmos.color = Color.yellow;
            foreach (var platformPos in platformPositions)
            {
                Vector3 worldPos = foregroundTilemap.CellToWorld(new Vector3Int(platformPos.x, platformPos.y, 0));
                Gizmos.DrawWireCube(worldPos, Vector3.one);
            }

            // 天井プラットフォーム位置の表示
            Gizmos.color = Color.magenta;
            foreach (var ceilingPlatformPos in ceilingPlatformPositions)
            {
                Vector3 worldPos = foregroundTilemap.CellToWorld(new Vector3Int(ceilingPlatformPos.x, ceilingPlatformPos.y, 0));
                Gizmos.DrawWireCube(worldPos, Vector3.one);
            }
        }

        // StageManagerとの統合用メソッド
        public void IntegrateWithStageManager(StageManager stageManager)
        {
            if (stageManager.foregroundTilemap == null)
            {
                stageManager.foregroundTilemap = foregroundTilemap;
            }

            if (stageManager.foregroundCollider == null)
            {
                stageManager.foregroundCollider = tilemapCollider;
            }

            Debug.Log("TilemapGroundManager integrated with StageManager");
        }

        // パフォーマンス最適化用メソッド
        public void OptimizeForPerformance()
        {
            // コライダーの最適化
            if (compositeCollider != null)
            {
                compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
                compositeCollider.GenerateGeometry();
            }

            // 不要なタイルの削除（画面外など）
            CleanupDistantTiles();
        }

        private void CleanupDistantTiles()
        {
            if (Camera.main == null) return;

            Vector3 cameraPos = Camera.main.transform.position;
            float cleanupDistance = 50f; // ピクセル単位

            List<Vector3Int> tilesToRemove = new List<Vector3Int>();

            foreach (var kvp in tileTypeMap)
            {
                Vector3 tileWorldPos = foregroundTilemap.CellToWorld(kvp.Key);
                float distance = Vector3.Distance(cameraPos, tileWorldPos);

                if (distance > cleanupDistance)
                {
                    tilesToRemove.Add(kvp.Key);
                }
            }

            foreach (var tilePos in tilesToRemove)
            {
                foregroundTilemap.SetTile(tilePos, null);
                tileTypeMap.Remove(tilePos);
            }

            if (tilesToRemove.Count > 0)
            {
                RefreshTilemap();
                Debug.Log($"Cleaned up {tilesToRemove.Count} distant tiles");
            }
        }

        // 天井生成の個別制御用メソッド
        public void SetCeilingGenerationEnabled(bool enabled)
        {
            autoGenerateCeiling = enabled;
        }

        public void RegenerateCeiling()
        {
            // 既存の天井タイルを削除
            ClearTilesByType(TileType.Ceiling);
            ClearTilesByType(TileType.CeilingPlatform);

            // 天井を再生成
            if (autoGenerateCeiling)
            {
                GenerateBasicCeiling();
                GenerateCeilingPlatforms();
            }
        }

        private void ClearTilesByType(TileType targetType)
        {
            List<Vector3Int> tilesToClear = new List<Vector3Int>();

            foreach (var kvp in tileTypeMap)
            {
                if (kvp.Value == targetType)
                {
                    tilesToClear.Add(kvp.Key);
                }
            }

            foreach (var tilePos in tilesToClear)
            {
                foregroundTilemap.SetTile(tilePos, null);
                tileTypeMap.Remove(tilePos);
            }
        }

        // 天井の高さを動的に変更
        public void SetCeilingLevel(float newCeilingLevel)
        {
            ceilingLevel = newCeilingLevel;
            RegenerateCeiling();
        }

        // 天井の厚さを動的に変更
        public void SetCeilingThickness(float newThickness)
        {
            ceilingThickness = newThickness;
            RegenerateCeiling();
        }

        // コライダー設定の検証とデバッグ情報出力
        [ContextMenu("Validate Collider Setup")]
        public void ValidateColliderSetup()
        {
            Debug.Log("=== Tilemap Collider Validation ===");

            if (tilemapCollider == null)
            {
                Debug.LogError("TilemapCollider2D is null!");
                return;
            }

            if (compositeCollider == null)
            {
                Debug.LogError("CompositeCollider2D is null!");
                return;
            }

            Debug.Log($"CompositeOperation: {tilemapCollider.compositeOperation}");
            Debug.Log($"GeometryType: {compositeCollider.geometryType}");
            Debug.Log($"GenerationType: {compositeCollider.generationType}");
            Debug.Log($"Composite Points Count: {compositeCollider.pointCount}");
            Debug.Log($"Ground Level: {groundLevel}, Ceiling Level: {ceilingLevel}");

            // 地面と天井の距離をチェック
            float distance = ceilingLevel - groundLevel;
            Debug.Log($"Ground to Ceiling Distance: {distance}");

            if (distance < 5f)
            {
                Debug.LogWarning("Ground and ceiling are very close. Consider using Intersect or Merge operation.");
            }
            else if (tilemapCollider.compositeOperation == Collider2D.CompositeOperation.Intersect)
            {
                Debug.LogWarning("Ground and ceiling are far apart. Intersect operation may cause ceiling collider to disappear. Consider using None operation.");
            }

            // タイルタイプの統計
            var typeCount = new Dictionary<TileType, int>();
            foreach (var tileType in tileTypeMap.Values)
            {
                if (!typeCount.ContainsKey(tileType))
                    typeCount[tileType] = 0;
                typeCount[tileType]++;
            }

            Debug.Log("Tile Type Distribution:");
            foreach (var kvp in typeCount)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} tiles");
            }
        }
    }
}