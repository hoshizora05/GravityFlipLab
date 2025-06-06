using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataのJson読み込み機能を提供するクラス
    /// </summary>
    public static class StageDataJsonLoader
    {
        #region ファイルパス設定

        /// <summary>
        /// デフォルトのJsonディレクトリパス
        /// </summary>
        public static string DefaultJsonDirectory => Path.Combine(Application.streamingAssetsPath, "StageData");

        /// <summary>
        /// ワールドとステージ番号からファイルパスを生成
        /// </summary>
        public static string GetStageJsonPath(int worldNumber, int stageNumber)
        {
            return Path.Combine(DefaultJsonDirectory, $"World{worldNumber}", $"Stage{worldNumber}-{stageNumber}.json");
        }

        /// <summary>
        /// ワールドとステージ番号からファイルパスを生成（カスタムディレクトリ）
        /// </summary>
        public static string GetStageJsonPath(string baseDirectory, int worldNumber, int stageNumber)
        {
            return Path.Combine(baseDirectory, $"World{worldNumber}", $"Stage{worldNumber}-{stageNumber}.json");
        }

        #endregion

        #region 同期読み込み

        /// <summary>
        /// 指定されたパスからStageDataを同期読み込み
        /// </summary>
        public static bool LoadStageDataFromPath(string jsonPath, StageDataSO stageData)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("Json path is null or empty");
                return false;
            }

            if (stageData == null)
            {
                Debug.LogError("StageData is null");
                return false;
            }

            try
            {
                StageDataJsonConverter.LoadStageDataFromJsonFile(jsonPath, stageData);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load StageData from {jsonPath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// ワールドとステージ番号でStageDataを同期読み込み
        /// </summary>
        public static bool LoadStageData(int worldNumber, int stageNumber, StageDataSO stageData)
        {
            string jsonPath = GetStageJsonPath(worldNumber, stageNumber);
            return LoadStageDataFromPath(jsonPath, stageData);
        }

        /// <summary>
        /// カスタムディレクトリからStageDataを同期読み込み
        /// </summary>
        public static bool LoadStageDataFromDirectory(string baseDirectory, int worldNumber, int stageNumber, StageDataSO stageData)
        {
            string jsonPath = GetStageJsonPath(baseDirectory, worldNumber, stageNumber);
            return LoadStageDataFromPath(jsonPath, stageData);
        }

        /// <summary>
        /// ResourcesフォルダからStageDataを読み込み
        /// </summary>
        public static bool LoadStageDataFromResources(int worldNumber, int stageNumber, StageDataSO stageData)
        {
            string resourcePath = $"StageData/World{worldNumber}/Stage{worldNumber}-{stageNumber}";

            try
            {
                TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
                if (jsonAsset == null)
                {
                    Debug.LogError($"Json file not found in Resources: {resourcePath}");
                    return false;
                }

                StageDataJsonConverter.LoadStageDataFromJsonText(jsonAsset.text, stageData);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load StageData from Resources {resourcePath}: {e.Message}");
                return false;
            }
        }

        #endregion

        #region 非同期読み込み

        /// <summary>
        /// 指定されたパスからStageDataを非同期読み込み
        /// </summary>
        public static IEnumerator LoadStageDataFromPathAsync(string jsonPath, StageDataSO stageData, System.Action<bool> onComplete = null)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                Debug.LogError("Json path is null or empty");
                onComplete?.Invoke(false);
                yield break;
            }

            if (stageData == null)
            {
                Debug.LogError("StageData is null");
                onComplete?.Invoke(false);
                yield break;
            }

            bool success = false;

            // ファイル読み込みを別スレッドで実行
            string jsonText = null;
            bool loadComplete = false;
            System.Exception loadException = null;

            System.Threading.Thread loadThread = new System.Threading.Thread(() =>
            {
                try
                {
                    if (File.Exists(jsonPath))
                    {
                        jsonText = File.ReadAllText(jsonPath);
                    }
                    else
                    {
                        loadException = new FileNotFoundException($"Json file not found: {jsonPath}");
                    }
                }
                catch (System.Exception e)
                {
                    loadException = e;
                }
                finally
                {
                    loadComplete = true;
                }
            });

            loadThread.Start();

            // 読み込み完了まで待機
            while (!loadComplete)
            {
                yield return null;
            }

            // メインスレッドでStageDataに適用
            if (loadException != null)
            {
                Debug.LogError($"Failed to load Json file: {loadException.Message}");
            }
            else if (!string.IsNullOrEmpty(jsonText))
            {
                try
                {
                    StageDataJsonConverter.LoadStageDataFromJsonText(jsonText, stageData);
                    success = true;
                    Debug.Log($"StageData loaded asynchronously from: {jsonPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse Json: {e.Message}");
                }
            }

            onComplete?.Invoke(success);
        }

        /// <summary>
        /// ワールドとステージ番号でStageDataを非同期読み込み
        /// </summary>
        public static IEnumerator LoadStageDataAsync(int worldNumber, int stageNumber, StageDataSO stageData, System.Action<bool> onComplete = null)
        {
            string jsonPath = GetStageJsonPath(worldNumber, stageNumber);
            yield return LoadStageDataFromPathAsync(jsonPath, stageData, onComplete);
        }

        /// <summary>
        /// カスタムディレクトリからStageDataを非同期読み込み
        /// </summary>
        public static IEnumerator LoadStageDataFromDirectoryAsync(string baseDirectory, int worldNumber, int stageNumber, StageDataSO stageData, System.Action<bool> onComplete = null)
        {
            string jsonPath = GetStageJsonPath(baseDirectory, worldNumber, stageNumber);
            yield return LoadStageDataFromPathAsync(jsonPath, stageData, onComplete);
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// 指定されたJsonファイルが存在するかチェック
        /// </summary>
        public static bool DoesStageJsonExist(int worldNumber, int stageNumber)
        {
            string jsonPath = GetStageJsonPath(worldNumber, stageNumber);
            return File.Exists(jsonPath);
        }

        /// <summary>
        /// 指定されたディレクトリのJsonファイルが存在するかチェック
        /// </summary>
        public static bool DoesStageJsonExist(string baseDirectory, int worldNumber, int stageNumber)
        {
            string jsonPath = GetStageJsonPath(baseDirectory, worldNumber, stageNumber);
            return File.Exists(jsonPath);
        }

        /// <summary>
        /// 指定されたワールドの全ステージJsonファイルパスを取得
        /// </summary>
        public static List<string> GetAllStageJsonPaths(int worldNumber, int maxStages = 10)
        {
            List<string> paths = new List<string>();

            for (int stageNumber = 1; stageNumber <= maxStages; stageNumber++)
            {
                string jsonPath = GetStageJsonPath(worldNumber, stageNumber);
                if (File.Exists(jsonPath))
                {
                    paths.Add(jsonPath);
                }
            }

            return paths;
        }

        /// <summary>
        /// 指定されたディレクトリの全ステージJsonファイルパスを取得
        /// </summary>
        public static List<string> GetAllStageJsonPaths(string baseDirectory, int worldNumber, int maxStages = 10)
        {
            List<string> paths = new List<string>();

            for (int stageNumber = 1; stageNumber <= maxStages; stageNumber++)
            {
                string jsonPath = GetStageJsonPath(baseDirectory, worldNumber, stageNumber);
                if (File.Exists(jsonPath))
                {
                    paths.Add(jsonPath);
                }
            }

            return paths;
        }

        /// <summary>
        /// JsonConfigの妥当性をチェック
        /// </summary>
        public static bool ValidateStageJson(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"Json file not found: {jsonPath}");
                return false;
            }

            try
            {
                string jsonText = File.ReadAllText(jsonPath);
                var jsonConfig = JsonUtility.FromJson<StageDataJsonConfig>(jsonText);
                return StageDataJsonConverter.ValidateJsonConfig(jsonConfig);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to validate Json file {jsonPath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 複数のStageDataを一括読み込み
        /// </summary>
        public static Dictionary<string, bool> LoadMultipleStageData(Dictionary<string, StageDataSO> stageDataMap)
        {
            Dictionary<string, bool> results = new Dictionary<string, bool>();

            foreach (var kvp in stageDataMap)
            {
                string identifier = kvp.Key; // 例: "Stage1-1"
                StageDataSO stageData = kvp.Value;

                // identifierから世界とステージ番号を抽出
                if (TryParseStageIdentifier(identifier, out int worldNumber, out int stageNumber))
                {
                    bool success = LoadStageData(worldNumber, stageNumber, stageData);
                    results[identifier] = success;
                }
                else
                {
                    Debug.LogError($"Invalid stage identifier format: {identifier}");
                    results[identifier] = false;
                }
            }

            return results;
        }

        /// <summary>
        /// ステージ識別子から世界とステージ番号を抽出
        /// </summary>
        private static bool TryParseStageIdentifier(string identifier, out int worldNumber, out int stageNumber)
        {
            worldNumber = 0;
            stageNumber = 0;

            if (string.IsNullOrEmpty(identifier))
                return false;

            // "Stage1-1" 形式を想定
            if (identifier.StartsWith("Stage") && identifier.Contains("-"))
            {
                string numberPart = identifier.Substring(5); // "Stage" を除去
                string[] parts = numberPart.Split('-');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out worldNumber) &&
                    int.TryParse(parts[1], out stageNumber))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region デバッグ・テスト用

        /// <summary>
        /// テスト用のサンプルJsonConfigを生成
        /// </summary>
        public static StageDataJsonConfig CreateSampleJsonConfig(int worldNumber = 1, int stageNumber = 1)
        {
            var config = new StageDataJsonConfig
            {
                stageInfo = new StageInfoJson
                {
                    worldNumber = worldNumber,
                    stageNumber = stageNumber,
                    stageName = $"World {worldNumber} - Stage {stageNumber}",
                    timeLimit = 300f,
                    energyChipCount = 3,
                    playerStartPosition = new Vector3Json(0f, 0f, 0f),
                    goalPosition = new Vector3Json(50f, 0f, 0f),
                    theme = StageTheme.Tech,
                    stageLength = 4096f,
                    stageHeight = 1024f,
                    segmentCount = 16
                },
                tileMapSize = new Vector2IntJson(256, 64),
                tileSize = 16f,
                useCompositeCollider = true
            };

            // サンプルの障害物を追加
            config.obstacles.Add(new ObstacleDataJson
            {
                type = ObstacleType.Spike,
                position = new Vector3Json(10f, -2f, 0f)
            });

            // サンプルの収集アイテムを追加
            config.collectibles.Add(new CollectibleDataJson
            {
                type = CollectibleType.EnergyChip,
                position = new Vector3Json(15f, 2f, 0f),
                value = 1
            });

            // サンプルの傾斜を追加
            config.slopes.Add(new SlopeDataJson
            {
                type = SlopeType.BasicSlope,
                position = new Vector3Json(20f, -2f, 0f),
                rotation = new Vector3Json(),
                scale = new Vector3Json(1f, 1f, 1f),
                slopeAngle = 30f,
                slopeDirection = SlopeDirection.Ascending,
                slopeLength = 5f,
                speedMultiplier = 1.2f,
                affectGravity = true,
                gravityRedirection = 0.5f
            });

            return config;
        }

        /// <summary>
        /// サンプルJsonファイルを生成して保存
        /// </summary>
        public static void CreateSampleJsonFile(int worldNumber, int stageNumber, string outputPath = null)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = GetStageJsonPath(worldNumber, stageNumber);
            }

            var sampleConfig = CreateSampleJsonConfig(worldNumber, stageNumber);
            string jsonText = JsonUtility.ToJson(sampleConfig, true);

            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, jsonText);
            Debug.Log($"Sample Json file created: {outputPath}");
        }

        #endregion
    }
}