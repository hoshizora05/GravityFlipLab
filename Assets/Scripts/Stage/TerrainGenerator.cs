using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    // 地形生成ユーティリティクラス（完全版）
    public static class TerrainGenerator
    {
        public static void GenerateTerrainSegment(Tilemap tilemap, TerrainSegmentData segmentData, TerrainLayerData layerData)
        {
            Vector2Int start = segmentData.startPosition;
            Vector2Int size = segmentData.size;

            switch (segmentData.pattern)
            {
                case TerrainPattern.Flat:
                    GenerateFlatTerrain(tilemap, start, size, layerData);
                    break;
                case TerrainPattern.Ascending:
                    GenerateAscendingTerrain(tilemap, start, size, layerData, segmentData.heightVariation);
                    break;
                case TerrainPattern.Descending:
                    GenerateDescendingTerrain(tilemap, start, size, layerData, segmentData.heightVariation);
                    break;
                case TerrainPattern.Valley:
                    GenerateValleyTerrain(tilemap, start, size, layerData, segmentData.heightVariation);
                    break;
                case TerrainPattern.Hill:
                    GenerateHillTerrain(tilemap, start, size, layerData, segmentData.heightVariation);
                    break;
                case TerrainPattern.Stairs:
                    GenerateStairsTerrain(tilemap, start, size, layerData);
                    break;
                case TerrainPattern.Gaps:
                    GenerateGapsTerrain(tilemap, start, size, layerData);
                    break;
                case TerrainPattern.Platforms:
                    GeneratePlatformsTerrain(tilemap, start, size, layerData, segmentData.platforms);
                    break;
            }

            // プラットフォームの生成
            foreach (var platform in segmentData.platforms)
            {
                GeneratePlatform(tilemap, platform, layerData);
            }

            // 地形フィーチャーの生成
            foreach (var feature in segmentData.features)
            {
                GenerateTerrainFeature(tilemap, feature, layerData);
            }
        }

        #region 基本地形生成

        private static void GenerateFlatTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < Mathf.RoundToInt(layerData.thickness); y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y, 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                    tilemap.SetTile(position, tile);
                }
            }

            // 物理マテリアルとレイヤーを適用
            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateAscendingTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float heightOffset = (x / (float)size.x) * heightVariation;
                int currentHeight = Mathf.RoundToInt(layerData.thickness + heightOffset);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y + Mathf.RoundToInt(heightOffset), 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                    tilemap.SetTile(position, tile);
                }
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateDescendingTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                float heightOffset = ((size.x - x) / (float)size.x) * heightVariation;
                int currentHeight = Mathf.RoundToInt(layerData.thickness + heightOffset);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y + Mathf.RoundToInt(heightOffset), 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                    tilemap.SetTile(position, tile);
                }
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateValleyTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                // V字型の谷を生成
                float normalizedX = (x / (float)size.x) * 2f - 1f; // -1 to 1
                float valleyDepth = (1f - Mathf.Abs(normalizedX)) * heightVariation;
                int currentHeight = Mathf.RoundToInt(layerData.thickness + valleyDepth);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y + Mathf.RoundToInt(valleyDepth), 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                    tilemap.SetTile(position, tile);
                }
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateHillTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData, float heightVariation)
        {
            for (int x = 0; x < size.x; x++)
            {
                // 山型の丘を生成
                float normalizedX = (x / (float)size.x) * 2f - 1f; // -1 to 1
                float hillHeight = (1f - normalizedX * normalizedX) * heightVariation; // 放物線
                int currentHeight = Mathf.RoundToInt(layerData.thickness + hillHeight);

                for (int y = 0; y < currentHeight; y++)
                {
                    Vector3Int position = new Vector3Int(start.x + x, start.y - y + Mathf.RoundToInt(hillHeight), 0);
                    TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                    tilemap.SetTile(position, tile);
                }
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateStairsTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            int stepCount = Mathf.Min(8, size.x / 2); // 最大8段
            int stepWidth = Mathf.Max(1, size.x / stepCount);
            int stepHeight = 1;

            for (int step = 0; step < stepCount; step++)
            {
                int stepStartX = step * stepWidth;
                int stepEndX = Mathf.Min((step + 1) * stepWidth, size.x);
                int stepY = step * stepHeight;

                for (int x = stepStartX; x < stepEndX; x++)
                {
                    for (int y = 0; y <= stepY + Mathf.RoundToInt(layerData.thickness); y++)
                    {
                        Vector3Int position = new Vector3Int(start.x + x, start.y + stepY - y, 0);
                        TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                        tilemap.SetTile(position, tile);
                    }
                }
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GenerateGapsTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData)
        {
            int gapWidth = 4;
            int platformWidth = 8;

            for (int x = 0; x < size.x; x++)
            {
                if (x % (gapWidth + platformWidth) < platformWidth)
                {
                    // プラットフォーム部分
                    for (int y = 0; y < Mathf.RoundToInt(layerData.thickness); y++)
                    {
                        Vector3Int position = new Vector3Int(start.x + x, start.y - y, 0);
                        TileBase tile = layerData.GetTileForPosition(x, y, y); // TerrainLayerDataのメソッドを使用
                        tilemap.SetTile(position, tile);
                    }
                }
                // ギャップ部分は何も生成しない
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        private static void GeneratePlatformsTerrain(Tilemap tilemap, Vector2Int start, Vector2Int size, TerrainLayerData layerData, List<PlatformData> platforms)
        {
            // 基本地形は生成せず、プラットフォームのみ
            foreach (var platform in platforms)
            {
                GeneratePlatform(tilemap, platform, layerData);
            }

            ApplyLayerProperties(tilemap, layerData);
        }

        // 新しく追加: レイヤープロパティの適用
        private static void ApplyLayerProperties(Tilemap tilemap, TerrainLayerData layerData)
        {
            if (tilemap == null || layerData == null) return;

            // TilemapRendererの設定
            var tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                tilemapRenderer.sortingOrder = layerData.sortingOrder;
            }

            // レイヤーの設定
            layerData.ApplyLayerToGameObject(tilemap.gameObject);

            // 物理マテリアルの設定
            var tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider != null)
            {
                layerData.ApplyPhysicsMaterialToCollider(tilemapCollider);
            }

            var compositeCollider = tilemap.GetComponent<CompositeCollider2D>();
            if (compositeCollider != null)
            {
                layerData.ApplyPhysicsMaterialToCollider(compositeCollider);
            }
        }

        #endregion

        #region プラットフォーム生成

        private static void GeneratePlatform(Tilemap tilemap, PlatformData platform, TerrainLayerData layerData)
        {
            for (int x = 0; x < platform.size.x; x++)
            {
                for (int y = 0; y < platform.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(platform.position.x + x, platform.position.y + y, 0);
                    TileBase tile = GetPlatformTileVariant(layerData.tileVariants, x, y, platform.size, platform.platformType);
                    tilemap.SetTile(position, tile);
                }
            }

            // 特殊プラットフォームの処理
            if (platform.platformType != PlatformType.Solid)
            {
                CreateSpecialPlatform(tilemap, platform, layerData);
            }
        }

        private static TileBase GetPlatformTileVariant(TileBase[] tileVariants, int x, int y, Vector2Int size, PlatformType platformType)
        {
            if (tileVariants == null || tileVariants.Length == 0) return null;
            if (tileVariants.Length == 1) return tileVariants[0];

            // プラットフォームタイプに応じたタイル選択
            switch (platformType)
            {
                case PlatformType.Ice:
                    return tileVariants[Mathf.Min(2, tileVariants.Length - 1)]; // 氷タイル
                case PlatformType.Bouncy:
                    return tileVariants[Mathf.Min(3, tileVariants.Length - 1)]; // 弾性タイル
                default:
                    // 位置に基づく通常選択
                    if (x == 0) return tileVariants[0]; // 左端
                    if (x == size.x - 1) return tileVariants[Mathf.Min(1, tileVariants.Length - 1)]; // 右端
                    return tileVariants[Mathf.Min(1, tileVariants.Length - 1)]; // 中央
            }
        }

        private static void CreateSpecialPlatform(Tilemap tilemap, PlatformData platform, TerrainLayerData layerData)
        {
            // 特殊プラットフォーム用のGameObjectを作成
            GameObject platformObject = new GameObject($"Platform_{platform.platformType}_{platform.position.x}_{platform.position.y}");
            Vector3 worldPosition = tilemap.CellToWorld(new Vector3Int(platform.position.x, platform.position.y, 0));
            platformObject.transform.position = worldPosition;

            switch (platform.platformType)
            {
                case PlatformType.Moving:
                    CreateMovingPlatform(platformObject, platform);
                    break;
                case PlatformType.Falling:
                    CreateFallingPlatform(platformObject, platform);
                    break;
                case PlatformType.Disappearing:
                    CreateDisappearingPlatform(platformObject, platform);
                    break;
                case PlatformType.OneWay:
                    CreateOneWayPlatform(platformObject, platform);
                    break;
                case PlatformType.Conveyor:
                    CreateConveyorPlatform(platformObject, platform);
                    break;
            }
        }

        private static void CreateMovingPlatform(GameObject platformObject, PlatformData platform)
        {
            MovingPlatform movingComponent = platformObject.AddComponent<MovingPlatform>();
            movingComponent.waypoints = platform.movePath;
            movingComponent.moveSpeed = platform.moveSpeed;
            movingComponent.useLocalCoordinates = true;

            // コライダーの追加
            BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(platform.size.x, platform.size.y);
        }

        private static void CreateFallingPlatform(GameObject platformObject, PlatformData platform)
        {
            FallingPlatform fallingComponent = platformObject.AddComponent<FallingPlatform>();
            fallingComponent.fallDelay = 1f; // 1秒後に落下
            fallingComponent.resetTime = 5f; // 5秒後にリセット

            BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(platform.size.x, platform.size.y);

            Rigidbody2D rb = platformObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private static void CreateDisappearingPlatform(GameObject platformObject, PlatformData platform)
        {
            DisappearingPlatform disappearingComponent = platformObject.AddComponent<DisappearingPlatform>();
            disappearingComponent.disappearTime = 2f;
            disappearingComponent.reappearTime = 3f;

            BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(platform.size.x, platform.size.y);

            SpriteRenderer renderer = platformObject.AddComponent<SpriteRenderer>();
        }

        private static void CreateOneWayPlatform(GameObject platformObject, PlatformData platform)
        {
            BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(platform.size.x, platform.size.y);

            PlatformEffector2D effector = platformObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            collider.usedByEffector = true;
        }

        private static void CreateConveyorPlatform(GameObject platformObject, PlatformData platform)
        {
            ConveyorPlatform conveyorComponent = platformObject.AddComponent<ConveyorPlatform>();
            conveyorComponent.conveyorSpeed = platform.moveSpeed;
            conveyorComponent.direction = Vector2.right; // デフォルトは右方向

            BoxCollider2D collider = platformObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(platform.size.x, platform.size.y);
        }

        #endregion

        #region 地形フィーチャー生成

        private static void GenerateTerrainFeature(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            switch (feature.featureType)
            {
                case TerrainFeatureType.Spikes:
                    GenerateSpikes(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Ramp:
                    GenerateRamp(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Wall:
                    GenerateWall(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Ceiling:
                    GenerateCeiling(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Pit:
                    GeneratePit(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Bridge:
                    GenerateBridge(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Tunnel:
                    GenerateTunnel(tilemap, feature, layerData);
                    break;
                case TerrainFeatureType.Decoration:
                    GenerateDecoration(tilemap, feature, layerData);
                    break;
            }
        }

        private static void GenerateSpikes(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            Vector3Int position = new Vector3Int(feature.position.x, feature.position.y, 0);
            if (layerData.tileVariants.Length > 0)
            {
                tilemap.SetTile(position, layerData.tileVariants[0]);
            }

            // スパイクの危険性を示すためのマーカー追加
            GameObject spikeMarker = new GameObject("SpikeHazard");
            spikeMarker.transform.position = tilemap.CellToWorld(position);
            spikeMarker.layer = LayerMask.NameToLayer("Hazard");

            BoxCollider2D hazardCollider = spikeMarker.AddComponent<BoxCollider2D>();
            hazardCollider.isTrigger = true;
            hazardCollider.size = Vector2.one;
        }

        private static void GenerateRamp(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                int height = Mathf.RoundToInt((x / (float)feature.size.x) * feature.size.y);
                for (int y = 0; y <= height; y++)
                {
                    Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y + y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }
            }
        }

        private static void GenerateWall(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y + y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }
            }
        }

        private static void GenerateCeiling(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y - y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }
            }
        }

        private static void GeneratePit(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // 穴の生成（タイルを削除）
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y - y, 0);
                    tilemap.SetTile(position, null);
                }
            }

            // 穴の周囲に警告表示や特殊効果を追加
            GameObject pitMarker = new GameObject("PitHazard");
            Vector3 pitCenter = tilemap.CellToWorld(new Vector3Int(
                feature.position.x + feature.size.x / 2,
                feature.position.y - feature.size.y / 2, 0));
            pitMarker.transform.position = pitCenter;

            BoxCollider2D pitCollider = pitMarker.AddComponent<BoxCollider2D>();
            pitCollider.isTrigger = true;
            pitCollider.size = new Vector2(feature.size.x, feature.size.y);
        }

        private static void GenerateBridge(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // 橋の生成（横一列のプラットフォーム）
            for (int x = 0; x < feature.size.x; x++)
            {
                Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y, 0);
                TileBase tile = GetTileVariant(layerData.tileVariants, x, 0);
                tilemap.SetTile(position, tile);
            }

            // 橋の支柱（オプション）
            if (feature.size.x > 6)
            {
                int pillarCount = feature.size.x / 6;
                for (int i = 1; i <= pillarCount; i++)
                {
                    int pillarX = (feature.size.x / (pillarCount + 1)) * i;
                    for (int y = 1; y <= 4; y++) // 4タイル分の支柱
                    {
                        Vector3Int pillarPos = new Vector3Int(
                            feature.position.x + pillarX,
                            feature.position.y - y, 0);
                        tilemap.SetTile(pillarPos, layerData.tileVariants[0]);
                    }
                }
            }
        }

        private static void GenerateTunnel(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // トンネルの生成（上下に壁、中央は空洞）
            int tunnelHeight = feature.size.y;
            int wallThickness = 1;

            for (int x = 0; x < feature.size.x; x++)
            {
                // 上の壁
                for (int y = 0; y < wallThickness; y++)
                {
                    Vector3Int position = new Vector3Int(
                        feature.position.x + x,
                        feature.position.y + tunnelHeight / 2 + y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }

                // 下の壁
                for (int y = 0; y < wallThickness; y++)
                {
                    Vector3Int position = new Vector3Int(
                        feature.position.x + x,
                        feature.position.y - tunnelHeight / 2 - y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }
            }
        }

        private static void GenerateDecoration(Tilemap tilemap, TerrainFeatureData feature, TerrainLayerData layerData)
        {
            // 装飾用のタイル配置
            for (int x = 0; x < feature.size.x; x++)
            {
                for (int y = 0; y < feature.size.y; y++)
                {
                    Vector3Int position = new Vector3Int(feature.position.x + x, feature.position.y + y, 0);

                    // 装飾は50%の確率で配置（ランダム性）
                    int seed = (feature.position.x + x) * 1000 + (feature.position.y + y);
                    System.Random random = new System.Random(seed);

                    if (random.NextDouble() > 0.5)
                    {
                        TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                        tilemap.SetTile(position, tile);
                    }
                }
            }
        }

        #endregion

        #region ユーティリティメソッド

        private static TileBase GetTileVariant(TileBase[] tileVariants, int x, int y)
        {
            if (tileVariants == null || tileVariants.Length == 0) return null;
            if (tileVariants.Length == 1) return tileVariants[0];

            // 位置に基づいたバリエーション選択
            int seed = x * 1000 + y;
            System.Random random = new System.Random(seed);
            return tileVariants[random.Next(tileVariants.Length)];
        }

        // ハイトマップからの地形生成
        public static void GenerateFromHeightmap(Tilemap tilemap, Texture2D heightmap, TerrainLayerData layerData, Vector2Int mapSize)
        {
            if (heightmap == null) return;

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int z = 0; z < mapSize.y; z++)
                {
                    float u = x / (float)mapSize.x;
                    float v = z / (float)mapSize.y;

                    Color pixelColor = heightmap.GetPixelBilinear(u, v);
                    float height = pixelColor.r * layerData.noiseAmplitude + layerData.baseHeight;

                    for (int y = 0; y < Mathf.RoundToInt(height + layerData.thickness); y++)
                    {
                        Vector3Int position = new Vector3Int(x, Mathf.RoundToInt(layerData.baseHeight) - y, 0);
                        TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                        tilemap.SetTile(position, tile);
                    }
                }
            }
        }

        // プロシージャル地形生成
        public static void GenerateProceduralTerrain(Tilemap tilemap, TerrainLayerData layerData, Vector2Int mapSize, int seed)
        {
            System.Random random = new System.Random(seed);

            for (int x = 0; x < mapSize.x; x++)
            {
                // 複数のノイズレイヤーを組み合わせ
                float noise1 = Mathf.PerlinNoise(x * layerData.noiseScale, seed * 0.01f);
                float noise2 = Mathf.PerlinNoise(x * layerData.noiseScale * 2f, seed * 0.02f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(x * layerData.noiseScale * 4f, seed * 0.03f) * 0.25f;

                float combinedNoise = noise1 + noise2 + noise3;
                int height = Mathf.RoundToInt(layerData.baseHeight + combinedNoise * layerData.noiseAmplitude);

                for (int y = 0; y < height + layerData.thickness; y++)
                {
                    Vector3Int position = new Vector3Int(x, Mathf.RoundToInt(layerData.baseHeight) - y, 0);
                    TileBase tile = GetTileVariant(layerData.tileVariants, x, y);
                    tilemap.SetTile(position, tile);
                }
            }
        }

        // 地形の平滑化
        public static void SmoothTerrain(Tilemap tilemap, Vector2Int area, int iterations = 1)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                Dictionary<Vector3Int, TileBase> newTiles = new Dictionary<Vector3Int, TileBase>();

                for (int x = area.x; x < area.x + area.y; x++)
                {
                    for (int y = -20; y < 20; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        TileBase currentTile = tilemap.GetTile(pos);

                        if (currentTile != null)
                        {
                            // 周囲のタイルをチェック
                            int solidNeighbors = CountSolidNeighbors(tilemap, pos);

                            if (solidNeighbors < 4)
                            {
                                newTiles[pos] = null; // タイルを削除
                            }
                            else
                            {
                                newTiles[pos] = currentTile; // タイルを保持
                            }
                        }
                    }
                }

                // 新しいタイル配置を適用
                foreach (var kvp in newTiles)
                {
                    tilemap.SetTile(kvp.Key, kvp.Value);
                }
            }
        }

        private static int CountSolidNeighbors(Tilemap tilemap, Vector3Int position)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector3Int neighborPos = position + new Vector3Int(dx, dy, 0);
                    if (tilemap.GetTile(neighborPos) != null)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        #endregion
    }

    #region 特殊プラットフォームコンポーネント

    // 動的プラットフォームの基底クラス
    public abstract class DynamicPlatform : MonoBehaviour
    {
        [Header("Platform Settings")]
        public bool isActive = true;
        public LayerMask affectedLayers = 1;

        protected Vector3 originalPosition;
        protected bool playerOnPlatform = false;
        protected List<Transform> objectsOnPlatform = new List<Transform>();

        protected virtual void Start()
        {
            originalPosition = transform.position;
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (IsAffectedLayer(other.gameObject.layer))
            {
                objectsOnPlatform.Add(other.transform);
                if (other.CompareTag("Player"))
                {
                    playerOnPlatform = true;
                    OnPlayerEnter();
                }
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (IsAffectedLayer(other.gameObject.layer))
            {
                objectsOnPlatform.Remove(other.transform);
                if (other.CompareTag("Player"))
                {
                    playerOnPlatform = false;
                    OnPlayerExit();
                }
            }
        }

        protected bool IsAffectedLayer(int layer)
        {
            return (affectedLayers.value & (1 << layer)) != 0;
        }

        protected virtual void OnPlayerEnter() { }
        protected virtual void OnPlayerExit() { }

        // プラットフォーム上のオブジェクトを移動
        protected void MoveObjectsOnPlatform(Vector3 deltaPosition)
        {
            foreach (var obj in objectsOnPlatform)
            {
                if (obj != null)
                {
                    obj.position += deltaPosition;
                }
            }
        }
    }

    // 移動プラットフォーム
    public class MovingPlatform : DynamicPlatform
    {
        [Header("Movement Settings")]
        public Vector2[] waypoints;
        public float moveSpeed = 2f;
        public bool useLocalCoordinates = true;
        public bool pauseAtWaypoints = false;
        public float pauseDuration = 1f;

        private int currentWaypoint = 0;
        private Vector3 startPosition;
        private bool isPaused = false;
        private float pauseTimer = 0f;

        protected override void Start()
        {
            base.Start();
            startPosition = transform.position;
        }

        private void Update()
        {
            if (!isActive || waypoints.Length < 2) return;

            if (isPaused)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    isPaused = false;
                }
                return;
            }

            MovePlatform();
        }

        private void MovePlatform()
        {
            Vector3 targetPosition = useLocalCoordinates
                ? startPosition + (Vector3)waypoints[currentWaypoint]
                : (Vector3)waypoints[currentWaypoint];

            Vector3 oldPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            Vector3 deltaPosition = transform.position - oldPosition;

            // プラットフォーム上のオブジェクトを移動
            MoveObjectsOnPlatform(deltaPosition);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;

                if (pauseAtWaypoints)
                {
                    isPaused = true;
                    pauseTimer = pauseDuration;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Vector3 basePos = Application.isPlaying ? startPosition : transform.position;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 waypointPos = useLocalCoordinates ? basePos + (Vector3)waypoints[i] : (Vector3)waypoints[i];
                Gizmos.DrawWireSphere(waypointPos, 0.5f);

                if (i < waypoints.Length - 1)
                {
                    Vector3 nextPos = useLocalCoordinates ? basePos + (Vector3)waypoints[i + 1] : (Vector3)waypoints[i + 1];
                    Gizmos.DrawLine(waypointPos, nextPos);
                }
            }

            // 最後から最初への線
            if (waypoints.Length > 2)
            {
                Vector3 lastPos = useLocalCoordinates ? basePos + (Vector3)waypoints[waypoints.Length - 1] : (Vector3)waypoints[waypoints.Length - 1];
                Vector3 firstPos = useLocalCoordinates ? basePos + (Vector3)waypoints[0] : (Vector3)waypoints[0];
                Gizmos.DrawLine(lastPos, firstPos);
            }
        }
    }

    // 落下プラットフォーム
    public class FallingPlatform : DynamicPlatform
    {
        [Header("Falling Settings")]
        public float fallDelay = 1f;
        public float fallSpeed = 10f;
        public float resetTime = 5f;
        public bool respawnOnReset = true;

        private bool isFalling = false;
        private bool hasTriggered = false;
        private float fallTimer = 0f;
        private float resetTimer = 0f;
        private Rigidbody2D rb;

        protected override void Start()
        {
            base.Start();
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Update()
        {
            if (!isActive) return;

            if (hasTriggered && !isFalling)
            {
                fallTimer -= Time.deltaTime;
                if (fallTimer <= 0f)
                {
                    StartFalling();
                }
            }

            if (isFalling)
            {
                resetTimer -= Time.deltaTime;
                if (resetTimer <= 0f && respawnOnReset)
                {
                    ResetPlatform();
                }
            }
        }

        protected override void OnPlayerEnter()
        {
            if (!hasTriggered)
            {
                hasTriggered = true;
                fallTimer = fallDelay;
            }
        }

        private void StartFalling()
        {
            isFalling = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallSpeed / 9.81f;
            resetTimer = resetTime;
        }

        private void ResetPlatform()
        {
            transform.position = originalPosition;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            isFalling = false;
            hasTriggered = false;
            fallTimer = 0f;
            resetTimer = 0f;
        }
    }

    // 消失プラットフォーム
    public class DisappearingPlatform : DynamicPlatform
    {
        [Header("Disappearing Settings")]
        public float disappearTime = 2f;
        public float reappearTime = 3f;
        public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private bool isDisappearing = false;
        private bool hasTriggered = false;
        private float timer = 0f;
        private SpriteRenderer spriteRenderer;
        private Collider2D platformCollider;
        private Color originalColor;

        protected override void Start()
        {
            base.Start();
            spriteRenderer = GetComponent<SpriteRenderer>();
            platformCollider = GetComponent<Collider2D>();

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (!isActive || !hasTriggered) return;

            timer -= Time.deltaTime;

            if (!isDisappearing)
            {
                // 消失までの時間
                if (timer <= 0f)
                {
                    StartDisappearing();
                }
                else
                {
                    // 警告の点滅効果
                    float blinkSpeed = Mathf.Lerp(1f, 10f, 1f - (timer / disappearTime));
                    float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                    UpdateAlpha(alpha);
                }
            }
            else
            {
                // 再出現までの時間
                if (timer <= 0f)
                {
                    StartReappearing();
                }
            }
        }

        protected override void OnPlayerEnter()
        {
            if (!hasTriggered)
            {
                hasTriggered = true;
                timer = disappearTime;
            }
        }

        private void StartDisappearing()
        {
            isDisappearing = true;
            timer = reappearTime;

            if (platformCollider != null)
                platformCollider.enabled = false;

            StartCoroutine(FadeOut());
        }

        private void StartReappearing()
        {
            isDisappearing = false;
            hasTriggered = false;
            timer = 0f;

            if (platformCollider != null)
                platformCollider.enabled = true;

            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeOut()
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                float t = elapsedTime / fadeTime;
                float alpha = fadeOutCurve.Evaluate(t);
                UpdateAlpha(alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            UpdateAlpha(0f);
        }

        private IEnumerator FadeIn()
        {
            float fadeTime = 0.5f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                float t = elapsedTime / fadeTime;
                float alpha = fadeInCurve.Evaluate(t);
                UpdateAlpha(alpha);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            UpdateAlpha(1f);
        }

        private void UpdateAlpha(float alpha)
        {
            if (spriteRenderer != null)
            {
                Color color = originalColor;
                color.a = alpha;
                spriteRenderer.color = color;
            }
        }
    }

    // コンベアプラットフォーム
    public class ConveyorPlatform : DynamicPlatform
    {
        [Header("Conveyor Settings")]
        public float conveyorSpeed = 3f;
        public Vector2 direction = Vector2.right;
        public bool affectAirborneObjects = false;

        private void Update()
        {
            if (!isActive) return;

            Vector2 force = direction.normalized * conveyorSpeed;

            foreach (var obj in objectsOnPlatform)
            {
                if (obj != null)
                {
                    Rigidbody2D objRb = obj.GetComponent<Rigidbody2D>();
                    if (objRb != null)
                    {
                        // 地面にいる場合、または空中でも影響する設定の場合
                        if (affectAirborneObjects || IsGrounded(obj))
                        {
                            objRb.AddForce(force, ForceMode2D.Force);
                        }
                    }
                    else
                    {
                        // Rigidbodyがない場合は直接移動
                        obj.position += (Vector3)(force * Time.deltaTime);
                    }
                }
            }
        }

        private bool IsGrounded(Transform obj)
        {
            // 簡単な地面判定
            RaycastHit2D hit = Physics2D.Raycast(obj.position, Vector2.down, 0.6f);
            return hit.collider != null && hit.collider.transform == transform;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(direction.normalized * 2f);

            Gizmos.DrawLine(start, end);

            // 矢印の描画
            Vector3 arrowHead1 = end + (Vector3)(Quaternion.Euler(0, 0, 135) * direction.normalized * 0.5f);
            Vector3 arrowHead2 = end + (Vector3)(Quaternion.Euler(0, 0, -135) * direction.normalized * 0.5f);

            Gizmos.DrawLine(end, arrowHead1);
            Gizmos.DrawLine(end, arrowHead2);
        }
    }

    #endregion
}