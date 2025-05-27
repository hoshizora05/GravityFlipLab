using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// Tilemap地形システムのセットアップと使用例を示すガイドクラス
    /// このクラスはエディター用のヘルパーとしても使用できます
    /// </summary>
    public class TilemapSetupGuide : MonoBehaviour
    {
        [Header("Setup Configuration")]
        public bool autoSetupOnStart = false;
        public bool createSampleStage = false;

        [Header("References")]
        public StageManager stageManager;
        public TilemapGroundManager groundManager;
        public StageDataSO sampleStageData;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                StartCoroutine(AutoSetupCoroutine());
            }
        }

        private IEnumerator AutoSetupCoroutine()
        {
            yield return new WaitForSeconds(0.1f); // 他のコンポーネントの初期化を待つ

            Debug.Log("=== Starting Tilemap Setup ===");

            // 1. 基本コンポーネントのセットアップ
            SetupBasicComponents();
            yield return new WaitForEndOfFrame();

            // 2. サンプルステージの作成
            if (createSampleStage)
            {
                CreateSampleStageData();
                yield return new WaitForEndOfFrame();
            }

            // 3. 地形システムの初期化
            InitializeTerrainSystem();
            yield return new WaitForEndOfFrame();

            Debug.Log("=== Tilemap Setup Complete ===");
        }

        #region 基本セットアップ

        [ContextMenu("Setup Basic Components")]
        public void SetupBasicComponents()
        {
            Debug.Log("Setting up basic components...");

            // StageManagerの取得または作成
            if (stageManager == null)
            {
                stageManager = FindObjectOfType<StageManager>();
                if (stageManager == null)
                {
                    GameObject stageManagerObj = new GameObject("StageManager");
                    stageManager = stageManagerObj.AddComponent<StageManager>();
                    Debug.Log("Created new StageManager");
                }
            }

            // TilemapGroundManagerの取得または作成
            if (groundManager == null)
            {
                groundManager = FindObjectOfType<TilemapGroundManager>();
                if (groundManager == null)
                {
                    GameObject groundManagerObj = new GameObject("TilemapGroundManager");
                    groundManagerObj.transform.SetParent(stageManager.transform);
                    groundManager = groundManagerObj.AddComponent<TilemapGroundManager>();
                    Debug.Log("Created new TilemapGroundManager");
                }
            }

            // StageManagerの設定
            ConfigureStageManager();

            // TilemapGroundManagerの設定
            ConfigureTilemapGroundManager();
        }

        private void ConfigureStageManager()
        {
            stageManager.useTilemapTerrain = true;
            stageManager.tilemapGroundManager = groundManager;

            Debug.Log("StageManager configured for tilemap terrain");
        }

        private void ConfigureTilemapGroundManager()
        {
            // 基本設定
            groundManager.autoGenerateGround = true;
            groundManager.groundLevel = -5f;
            groundManager.groundThickness = 3f;
            groundManager.stageWidthInTiles = 256; // 4096ピクセル ÷ 16ピクセル/タイル

            // デバッグ設定
            groundManager.showDebugInfo = true;
            groundManager.enableRuntimeEdit = true;

            Debug.Log("TilemapGroundManager configured");
        }

        #endregion

        #region サンプルステージ作成

        [ContextMenu("Create Sample Stage Data")]
        public void CreateSampleStageData()
        {
            Debug.Log("Creating sample stage data...");

            // 既存のサンプルデータがあれば使用
            if (sampleStageData != null)
            {
                Debug.Log("Using existing sample stage data");
                return;
            }

            // 新しいサンプルステージデータを作成
            sampleStageData = CreateSampleStage();

            if (stageManager != null)
            {
                stageManager.currentStageData = sampleStageData;
                Debug.Log("Sample stage data assigned to StageManager");
            }
        }

        private StageDataSO CreateSampleStage()
        {
            StageDataSO stageData = ScriptableObject.CreateInstance<StageDataSO>();

            // 基本ステージ情報
            stageData.stageInfo = new StageInfo
            {
                worldNumber = 1,
                stageNumber = 1,
                stageName = "Sample Tutorial Stage",
                timeLimit = 300f,
                energyChipCount = 3,
                playerStartPosition = new Vector3(2f, 0f, 0f),
                goalPosition = new Vector3(60f, 0f, 0f),
                stageLength = 4096f,
                stageHeight = 1024f,
                segmentCount = 16,
                theme = StageTheme.Tech
            };

            // チェックポイント設定
            stageData.stageInfo.checkpointPositions = new List<Vector3>
            {
                new Vector3(16f, 0f, 0f),
                new Vector3(32f, 0f, 0f),
                new Vector3(48f, 0f, 0f)
            };

            // 地形レイヤー設定
            SetupSampleTerrainLayers(stageData);

            // 地形セグメント設定
            SetupSampleTerrainSegments(stageData);

            // 障害物設定
            SetupSampleObstacles(stageData);

            // 収集品設定
            SetupSampleCollectibles(stageData);

            Debug.Log($"Sample stage created: {stageData.stageInfo.stageName}");
            return stageData;
        }

        private void SetupSampleTerrainLayers(StageDataSO stageData)
        {
            stageData.terrainLayers = new TerrainLayerData[3];

            // メイン地面レイヤー
            stageData.terrainLayers[0] = new TerrainLayerData
            {
                layerName = "Main Ground",
                layerType = TerrainLayerType.Ground,
                baseHeight = -5f,
                thickness = 3f,
                autoGenerate = true,
                generationMode = TerrainGenerationMode.Flat,
                sortingOrder = 0
            };

            // プラットフォームレイヤー
            stageData.terrainLayers[1] = new TerrainLayerData
            {
                layerName = "Platforms",
                layerType = TerrainLayerType.Platform,
                baseHeight = 2f,
                thickness = 1f,
                autoGenerate = false,
                generationMode = TerrainGenerationMode.Custom,
                sortingOrder = 10
            };

            // 背景装飾レイヤー
            stageData.terrainLayers[2] = new TerrainLayerData
            {
                layerName = "Background Decoration",
                layerType = TerrainLayerType.Background,
                baseHeight = -10f,
                thickness = 1f,
                autoGenerate = false,
                generationMode = TerrainGenerationMode.Custom,
                sortingOrder = -10
            };
        }

        private void SetupSampleTerrainSegments(StageDataSO stageData)
        {
            stageData.terrainSegments = new TerrainSegmentData[16];

            for (int i = 0; i < 16; i++)
            {
                stageData.terrainSegments[i] = new TerrainSegmentData
                {
                    segmentIndex = i,
                    startPosition = new Vector2Int(i * 16, -5),
                    size = new Vector2Int(16, 3),
                    platforms = new List<PlatformData>(),
                    features = new List<TerrainFeatureData>()
                };

                // セグメントごとに異なるパターンを設定
                switch (i)
                {
                    case 0:
                    case 1:
                        // フラットな地形（チュートリアル）
                        stageData.terrainSegments[i].pattern = TerrainPattern.Flat;
                        break;

                    case 2:
                        // 上り坂
                        stageData.terrainSegments[i].pattern = TerrainPattern.Ascending;
                        stageData.terrainSegments[i].heightVariation = 2f;
                        break;

                    case 3:
                        // 山型
                        stageData.terrainSegments[i].pattern = TerrainPattern.Hill;
                        stageData.terrainSegments[i].heightVariation = 3f;
                        break;

                    case 4:
                        // 階段
                        stageData.terrainSegments[i].pattern = TerrainPattern.Stairs;
                        break;

                    case 5:
                    case 6:
                        // ギャップのある地形
                        stageData.terrainSegments[i].pattern = TerrainPattern.Gaps;
                        break;

                    case 7:
                        // プラットフォーム配置
                        stageData.terrainSegments[i].pattern = TerrainPattern.Platforms;
                        stageData.terrainSegments[i].platforms.Add(
                            TerrainEditorHelper.CreateSimplePlatform(new Vector2Int(i * 16 + 4, 0), new Vector2Int(4, 1))
                        );
                        stageData.terrainSegments[i].platforms.Add(
                            TerrainEditorHelper.CreateSimplePlatform(new Vector2Int(i * 16 + 10, 3), new Vector2Int(3, 1))
                        );
                        break;

                    case 8:
                        // 移動プラットフォーム
                        stageData.terrainSegments[i].pattern = TerrainPattern.Platforms;
                        Vector2[] movePath = { Vector2.zero, new Vector2(0, 4), new Vector2(4, 4), new Vector2(4, 0) };
                        stageData.terrainSegments[i].platforms.Add(
                            TerrainEditorHelper.CreateMovingPlatform(
                                new Vector2Int(i * 16 + 6, 0),
                                new Vector2Int(3, 1),
                                movePath,
                                2f
                            )
                        );
                        break;

                    case 9:
                        // 谷地形
                        stageData.terrainSegments[i].pattern = TerrainPattern.Valley;
                        stageData.terrainSegments[i].heightVariation = 4f;
                        break;

                    case 10:
                        // スパイクの配置
                        stageData.terrainSegments[i].pattern = TerrainPattern.Flat;
                        stageData.terrainSegments[i].features.Add(
                            TerrainEditorHelper.CreateSpike(new Vector2Int(i * 16 + 6, -2))
                        );
                        stageData.terrainSegments[i].features.Add(
                            TerrainEditorHelper.CreateSpike(new Vector2Int(i * 16 + 10, -2))
                        );
                        break;

                    case 11:
                        // 傾斜路
                        stageData.terrainSegments[i].pattern = TerrainPattern.Flat;
                        stageData.terrainSegments[i].features.Add(
                            TerrainEditorHelper.CreateRamp(new Vector2Int(i * 16 + 2, -2), new Vector2Int(8, 4))
                        );
                        break;

                    case 12:
                        // 下り坂
                        stageData.terrainSegments[i].pattern = TerrainPattern.Descending;
                        stageData.terrainSegments[i].heightVariation = 3f;
                        break;

                    default:
                        // フラットな地形
                        stageData.terrainSegments[i].pattern = TerrainPattern.Flat;
                        break;
                }
            }
        }

        private void SetupSampleObstacles(StageDataSO stageData)
        {
            stageData.obstacles = new List<ObstacleData>();

            // スパイクトラップ
            stageData.obstacles.Add(new ObstacleData
            {
                type = ObstacleType.Spike,
                position = new Vector3(20f, -2f, 0f),
                rotation = Vector3.zero,
                scale = Vector3.one
            });

            // 電撃フェンス
            stageData.obstacles.Add(new ObstacleData
            {
                type = ObstacleType.ElectricFence,
                position = new Vector3(35f, 0f, 0f),
                rotation = Vector3.zero,
                scale = new Vector3(1f, 3f, 1f)
            });

            // ピストンクラッシャー
            stageData.obstacles.Add(new ObstacleData
            {
                type = ObstacleType.PistonCrusher,
                position = new Vector3(45f, 5f, 0f),
                rotation = Vector3.zero,
                scale = Vector3.one
            });
        }

        private void SetupSampleCollectibles(StageDataSO stageData)
        {
            stageData.collectibles = new List<CollectibleData>();

            // エナジーチップを3個配置
            stageData.collectibles.Add(new CollectibleData
            {
                type = CollectibleType.EnergyChip,
                position = new Vector3(15f, 2f, 0f),
                value = 1
            });

            stageData.collectibles.Add(new CollectibleData
            {
                type = CollectibleType.EnergyChip,
                position = new Vector3(30f, 4f, 0f),
                value = 1
            });

            stageData.collectibles.Add(new CollectibleData
            {
                type = CollectibleType.EnergyChip,
                position = new Vector3(50f, 1f, 0f),
                value = 1
            });
        }

        #endregion

        #region 地形システム初期化

        [ContextMenu("Initialize Terrain System")]
        public void InitializeTerrainSystem()
        {
            Debug.Log("Initializing terrain system...");

            if (stageManager != null && groundManager != null)
            {
                // StageManagerとTilemapGroundManagerを統合
                groundManager.IntegrateWithStageManager(stageManager);

                // サンプルステージデータがあれば読み込み
                if (sampleStageData != null)
                {
                    groundManager.LoadGroundFromStageData(sampleStageData);
                }
                else
                {
                    // 基本的な地面を生成
                    groundManager.GenerateBasicGround();
                }

                Debug.Log("Terrain system initialized successfully");
            }
            else
            {
                Debug.LogError("StageManager or TilemapGroundManager is missing");
            }
        }

        #endregion

        #region プレイヤー統合

        [ContextMenu("Setup Player Integration")]
        public void SetupPlayerIntegration()
        {
            Debug.Log("Setting up player integration...");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("Player not found. Make sure player GameObject has 'Player' tag.");
                return;
            }

            // PlayerMovementコンポーネントの設定
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                //playerMovement.useTilemapGroundDetection = true;
                playerMovement.groundLayerMask = groundManager.groundLayer;
                Debug.Log("PlayerMovement configured for tilemap detection");
            }

            // GroundDetectorの追加
            var groundDetector = player.GetComponent<GroundDetector>();
            if (groundDetector == null)
            {
                groundDetector = player.AddComponent<GroundDetector>();
            }

            groundDetector.useTilemapDetection = true;
            groundDetector.groundLayerMask = groundManager.groundLayer;
            groundDetector.showDebugRays = true;

            Debug.Log("Player integration setup complete");
        }

        #endregion

        #region デバッグ・テスト機能

        [ContextMenu("Test Terrain Generation")]
        public void TestTerrainGeneration()
        {
            Debug.Log("Testing terrain generation...");

            if (groundManager == null)
            {
                Debug.LogError("TilemapGroundManager not found");
                return;
            }

            // 既存の地形をクリア
            groundManager.ClearAllTiles();

            // テスト用プラットフォームを追加
            groundManager.AddPlatform(new Vector2Int(20, 2), new Vector2Int(4, 1));
            groundManager.AddPlatform(new Vector2Int(30, 5), new Vector2Int(3, 1));
            groundManager.AddOneWayPlatform(new Vector2Int(40, 3), 5);

            Debug.Log("Test terrain generation complete");
        }

        [ContextMenu("Validate Current Setup")]
        public void ValidateCurrentSetup()
        {
            Debug.Log("=== Validating Current Setup ===");

            bool isValid = true;

            // StageManagerの確認
            if (stageManager == null)
            {
                Debug.LogError("❌ StageManager is missing");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ StageManager found");
            }

            // TilemapGroundManagerの確認
            if (groundManager == null)
            {
                Debug.LogError("❌ TilemapGroundManager is missing");
                isValid = false;
            }
            else
            {
                Debug.Log("✅ TilemapGroundManager found");

                // Tilemapの確認
                if (groundManager.foregroundTilemap == null)
                {
                    Debug.LogWarning("⚠️ Foreground tilemap not initialized");
                }
                else
                {
                    Debug.Log("✅ Foreground tilemap initialized");
                }
            }

            // ステージデータの確認
            if (sampleStageData == null && stageManager?.currentStageData == null)
            {
                Debug.LogWarning("⚠️ No stage data available");
            }
            else
            {
                Debug.Log("✅ Stage data available");

                if (sampleStageData != null && sampleStageData.HasValidTerrainData())
                {
                    Debug.Log("✅ Valid terrain data found");

                    var stats = sampleStageData.GetTerrainStatistics();
                    Debug.Log($"📊 Terrain Statistics: {stats}");
                }
            }

            // プレイヤーの確認
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("⚠️ Player not found");
            }
            else
            {
                Debug.Log("✅ Player found");

                var groundDetector = player.GetComponent<GroundDetector>();
                if (groundDetector != null)
                {
                    Debug.Log("✅ GroundDetector attached to player");
                }
                else
                {
                    Debug.LogWarning("⚠️ GroundDetector not found on player");
                }
            }

            if (isValid)
            {
                Debug.Log("🎉 Setup validation passed!");
            }
            else
            {
                Debug.LogError("❌ Setup validation failed. Please fix the issues above.");
            }
        }

        [ContextMenu("Performance Test")]
        public void PerformanceTest()
        {
            Debug.Log("Starting performance test...");

            if (groundManager == null)
            {
                Debug.LogError("TilemapGroundManager not found");
                return;
            }

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            // 地形生成のパフォーマンステスト
            stopwatch.Start();
            groundManager.GenerateBasicGround();
            stopwatch.Stop();
            Debug.Log($"Basic ground generation: {stopwatch.ElapsedMilliseconds}ms");

            // 複数プラットフォーム生成テスト
            stopwatch.Restart();
            for (int i = 0; i < 10; i++)
            {
                groundManager.AddPlatform(new Vector2Int(i * 8, i % 3), new Vector2Int(3, 1));
            }
            stopwatch.Stop();
            Debug.Log($"10 platforms generation: {stopwatch.ElapsedMilliseconds}ms");

            // 最適化テスト
            stopwatch.Restart();
            groundManager.OptimizeForPerformance();
            stopwatch.Stop();
            Debug.Log($"Optimization: {stopwatch.ElapsedMilliseconds}ms");

            Debug.Log("Performance test complete");
        }

        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 指定位置に簡単なテスト用プラットフォームを作成
        /// </summary>
        public void CreateTestPlatformAt(Vector3 worldPosition, Vector2Int size)
        {
            if (groundManager == null) return;

            Vector2Int tilePosition = new Vector2Int(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y)
            );

            groundManager.AddPlatform(tilePosition, size);
            Debug.Log($"Test platform created at {tilePosition} with size {size}");
        }

        /// <summary>
        /// 現在のマウス位置にプラットフォームを作成（エディター用）
        /// </summary>
        public void CreatePlatformAtMousePosition()
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0;

            CreateTestPlatformAt(worldPos, new Vector2Int(3, 1));
        }

        /// <summary>
        /// ステージデータをアセットとして保存
        /// </summary>
        [ContextMenu("Save Sample Stage as Asset")]
        public void SaveSampleStageAsAsset()
        {
            if (sampleStageData == null)
            {
                Debug.LogError("No sample stage data to save");
                return;
            }

#if UNITY_EDITOR
            string path = "Assets/GameData/StageData/SampleStage.asset";
            UnityEditor.AssetDatabase.CreateAsset(sampleStageData, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Sample stage saved as asset: {path}");
#else
            Debug.LogWarning("Asset saving is only available in the editor");
#endif
        }

        #endregion
    }
}