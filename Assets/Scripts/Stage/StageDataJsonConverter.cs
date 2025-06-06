using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataSOとJsonConfigの相互変換を行うクラス
    /// </summary>
    public static class StageDataJsonConverter
    {
        #region Json to StageDataSO 変換

        /// <summary>
        /// JsonConfigからStageDataSOに変換
        /// </summary>
        public static void ApplyJsonConfigToStageData(StageDataJsonConfig jsonConfig, StageDataSO stageData)
        {
            if (jsonConfig == null || stageData == null)
            {
                Debug.LogError("JsonConfig or StageData is null");
                return;
            }

            // StageInfo変換
            ConvertStageInfo(jsonConfig.stageInfo, stageData);

            // Obstacles変換
            ConvertObstacles(jsonConfig.obstacles, stageData);

            // Collectibles変換
            ConvertCollectibles(jsonConfig.collectibles, stageData);

            // Environmental変換
            ConvertEnvironmental(jsonConfig.environmental, stageData);

            // Tilemap設定変換
            ConvertTilemapSettings(jsonConfig, stageData);

            // Slopes変換
            ConvertSlopes(jsonConfig.slopes, stageData);

            Debug.Log($"JsonConfig applied to StageData: {stageData.name}");
        }

        private static void ConvertStageInfo(StageInfoJson jsonInfo, StageDataSO stageData)
        {
            if (jsonInfo == null) return;

            if (stageData.stageInfo == null)
                stageData.stageInfo = new StageInfo();

            stageData.stageInfo.worldNumber = jsonInfo.worldNumber;
            stageData.stageInfo.stageNumber = jsonInfo.stageNumber;
            stageData.stageInfo.stageName = jsonInfo.stageName;
            stageData.stageInfo.timeLimit = jsonInfo.timeLimit;
            stageData.stageInfo.energyChipCount = jsonInfo.energyChipCount;
            stageData.stageInfo.playerStartPosition = jsonInfo.playerStartPosition.ToVector3();
            stageData.stageInfo.goalPosition = jsonInfo.goalPosition.ToVector3();
            stageData.stageInfo.theme = jsonInfo.theme;
            stageData.stageInfo.stageLength = jsonInfo.stageLength;
            stageData.stageInfo.stageHeight = jsonInfo.stageHeight;
            stageData.stageInfo.segmentCount = jsonInfo.segmentCount;

            // Checkpoints変換
            stageData.stageInfo.checkpointPositions = new List<Vector3>();
            if (jsonInfo.checkpointPositions != null)
            {
                foreach (var checkpoint in jsonInfo.checkpointPositions)
                {
                    stageData.stageInfo.checkpointPositions.Add(checkpoint.ToVector3());
                }
            }
        }

        private static void ConvertObstacles(List<ObstacleDataJson> jsonObstacles, StageDataSO stageData)
        {
            if (jsonObstacles == null) return;

            stageData.obstacles = new List<ObstacleData>();
            foreach (var jsonObstacle in jsonObstacles)
            {
                var obstacle = new ObstacleData
                {
                    type = jsonObstacle.type,
                    position = jsonObstacle.position.ToVector3(),
                    rotation = jsonObstacle.rotation.ToVector3(),
                    scale = jsonObstacle.scale.ToVector3(),
                    parameters = jsonObstacle.parameters ?? new Dictionary<string, object>()
                };
                stageData.obstacles.Add(obstacle);
            }
        }

        private static void ConvertCollectibles(List<CollectibleDataJson> jsonCollectibles, StageDataSO stageData)
        {
            if (jsonCollectibles == null) return;

            stageData.collectibles = new List<CollectibleData>();
            foreach (var jsonCollectible in jsonCollectibles)
            {
                var collectible = new CollectibleData
                {
                    type = jsonCollectible.type,
                    position = jsonCollectible.position.ToVector3(),
                    value = jsonCollectible.value
                };
                stageData.collectibles.Add(collectible);
            }
        }

        private static void ConvertEnvironmental(List<EnvironmentalDataJson> jsonEnvironmental, StageDataSO stageData)
        {
            if (jsonEnvironmental == null) return;

            stageData.environmental = new List<EnvironmentalData>();
            foreach (var jsonEnv in jsonEnvironmental)
            {
                var env = new EnvironmentalData
                {
                    type = jsonEnv.type,
                    position = jsonEnv.position.ToVector3(),
                    scale = jsonEnv.scale.ToVector3(),
                    parameters = jsonEnv.parameters ?? new Dictionary<string, object>()
                };
                stageData.environmental.Add(env);
            }
        }

        private static void ConvertTilemapSettings(StageDataJsonConfig jsonConfig, StageDataSO stageData)
        {
            stageData.tileMapSize = jsonConfig.tileMapSize.ToVector2Int();
            stageData.tileSize = jsonConfig.tileSize;
            stageData.useCompositeCollider = jsonConfig.useCompositeCollider;
        }

        private static void ConvertSlopes(List<SlopeDataJson> jsonSlopes, StageDataSO stageData)
        {
            if (jsonSlopes == null) return;

            stageData.slopes = new List<SlopeData>();
            foreach (var jsonSlope in jsonSlopes)
            {
                var slope = new SlopeData
                {
                    type = jsonSlope.type,
                    position = jsonSlope.position.ToVector3(),
                    rotation = jsonSlope.rotation.ToVector3(),
                    scale = jsonSlope.scale.ToVector3(),
                    slopeAngle = jsonSlope.slopeAngle,
                    slopeDirection = jsonSlope.slopeDirection,
                    slopeLength = jsonSlope.slopeLength,
                    speedMultiplier = jsonSlope.speedMultiplier,
                    affectGravity = jsonSlope.affectGravity,
                    gravityRedirection = jsonSlope.gravityRedirection,
                    parameters = jsonSlope.parameters ?? new Dictionary<string, object>()
                };

                stageData.slopes.Add(slope);
            }
        }

        #endregion

        #region StageDataSO to Json 変換

        /// <summary>
        /// StageDataSOからJsonConfigに変換
        /// </summary>
        public static StageDataJsonConfig ConvertStageDataToJsonConfig(StageDataSO stageData)
        {
            if (stageData == null)
            {
                Debug.LogError("StageData is null");
                return new StageDataJsonConfig();
            }

            var jsonConfig = new StageDataJsonConfig();

            // StageInfo変換
            ConvertStageInfoToJson(stageData.stageInfo, jsonConfig);

            // Obstacles変換
            ConvertObstaclesToJson(stageData.obstacles, jsonConfig);

            // Collectibles変換
            ConvertCollectiblesToJson(stageData.collectibles, jsonConfig);

            // Environmental変換
            ConvertEnvironmentalToJson(stageData.environmental, jsonConfig);

            // Tilemap設定変換
            ConvertTilemapSettingsToJson(stageData, jsonConfig);

            // Slopes変換
            ConvertSlopesToJson(stageData.slopes, jsonConfig);

            return jsonConfig;
        }

        private static void ConvertStageInfoToJson(StageInfo stageInfo, StageDataJsonConfig jsonConfig)
        {
            if (stageInfo == null) return;

            jsonConfig.stageInfo = new StageInfoJson
            {
                worldNumber = stageInfo.worldNumber,
                stageNumber = stageInfo.stageNumber,
                stageName = stageInfo.stageName,
                timeLimit = stageInfo.timeLimit,
                energyChipCount = stageInfo.energyChipCount,
                playerStartPosition = Vector3Json.FromVector3(stageInfo.playerStartPosition),
                goalPosition = Vector3Json.FromVector3(stageInfo.goalPosition),
                theme = stageInfo.theme,
                stageLength = stageInfo.stageLength,
                stageHeight = stageInfo.stageHeight,
                segmentCount = stageInfo.segmentCount
            };

            // Checkpoints変換
            if (stageInfo.checkpointPositions != null)
            {
                jsonConfig.stageInfo.checkpointPositions = new List<Vector3Json>();
                foreach (var checkpoint in stageInfo.checkpointPositions)
                {
                    jsonConfig.stageInfo.checkpointPositions.Add(Vector3Json.FromVector3(checkpoint));
                }
            }
        }

        private static void ConvertObstaclesToJson(List<ObstacleData> obstacles, StageDataJsonConfig jsonConfig)
        {
            if (obstacles == null) return;

            jsonConfig.obstacles = new List<ObstacleDataJson>();
            foreach (var obstacle in obstacles)
            {
                var jsonObstacle = new ObstacleDataJson
                {
                    type = obstacle.type,
                    position = Vector3Json.FromVector3(obstacle.position),
                    rotation = Vector3Json.FromVector3(obstacle.rotation),
                    scale = Vector3Json.FromVector3(obstacle.scale),
                    parameters = obstacle.parameters ?? new Dictionary<string, object>()
                };
                jsonConfig.obstacles.Add(jsonObstacle);
            }
        }

        private static void ConvertCollectiblesToJson(List<CollectibleData> collectibles, StageDataJsonConfig jsonConfig)
        {
            if (collectibles == null) return;

            jsonConfig.collectibles = new List<CollectibleDataJson>();
            foreach (var collectible in collectibles)
            {
                var jsonCollectible = new CollectibleDataJson
                {
                    type = collectible.type,
                    position = Vector3Json.FromVector3(collectible.position),
                    value = collectible.value
                };
                jsonConfig.collectibles.Add(jsonCollectible);
            }
        }

        private static void ConvertEnvironmentalToJson(List<EnvironmentalData> environmental, StageDataJsonConfig jsonConfig)
        {
            if (environmental == null) return;

            jsonConfig.environmental = new List<EnvironmentalDataJson>();
            foreach (var env in environmental)
            {
                var jsonEnv = new EnvironmentalDataJson
                {
                    type = env.type,
                    position = Vector3Json.FromVector3(env.position),
                    scale = Vector3Json.FromVector3(env.scale),
                    parameters = env.parameters ?? new Dictionary<string, object>()
                };
                jsonConfig.environmental.Add(jsonEnv);
            }
        }

        private static void ConvertTilemapSettingsToJson(StageDataSO stageData, StageDataJsonConfig jsonConfig)
        {
            jsonConfig.tileMapSize = Vector2IntJson.FromVector2Int(stageData.tileMapSize);
            jsonConfig.tileSize = stageData.tileSize;
            jsonConfig.useCompositeCollider = stageData.useCompositeCollider;
        }

        private static void ConvertSlopesToJson(List<SlopeData> slopes, StageDataJsonConfig jsonConfig)
        {
            if (slopes == null) return;

            jsonConfig.slopes = new List<SlopeDataJson>();
            foreach (var slope in slopes)
            {
                var jsonSlope = new SlopeDataJson
                {
                    type = slope.type,
                    position = Vector3Json.FromVector3(slope.position),
                    rotation = Vector3Json.FromVector3(slope.rotation),
                    scale = Vector3Json.FromVector3(slope.scale),
                    slopeAngle = slope.slopeAngle,
                    slopeDirection = slope.slopeDirection,
                    slopeLength = slope.slopeLength,
                    speedMultiplier = slope.speedMultiplier,
                    affectGravity = slope.affectGravity,
                    gravityRedirection = slope.gravityRedirection,
                    parameters = slope.parameters ?? new Dictionary<string, object>()
                };

                jsonConfig.slopes.Add(jsonSlope);
            }
        }

        #endregion

        #region ファイル操作

        /// <summary>
        /// JsonファイルからStageDataSOを読み込み
        /// </summary>
        public static void LoadStageDataFromJsonFile(string filePath, StageDataSO stageData)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Json file not found: {filePath}");
                return;
            }

            try
            {
                string jsonText = File.ReadAllText(filePath);
                var jsonConfig = JsonUtility.FromJson<StageDataJsonConfig>(jsonText);
                ApplyJsonConfigToStageData(jsonConfig, stageData);

                Debug.Log($"StageData loaded from Json: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load StageData from Json: {e.Message}");
            }
        }

        /// <summary>
        /// StageDataSOをJsonファイルに保存
        /// </summary>
        public static void SaveStageDataToJsonFile(StageDataSO stageData, string filePath)
        {
            if (stageData == null)
            {
                Debug.LogError("StageData is null");
                return;
            }

            try
            {
                var jsonConfig = ConvertStageDataToJsonConfig(stageData);
                string jsonText = JsonUtility.ToJson(jsonConfig, true);

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, jsonText);
                Debug.Log($"StageData saved to Json: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save StageData to Json: {e.Message}");
            }
        }

        /// <summary>
        /// JsonテキストからStageDataSOを読み込み
        /// </summary>
        public static void LoadStageDataFromJsonText(string jsonText, StageDataSO stageData)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError("Json text is null or empty");
                return;
            }

            try
            {
                var jsonConfig = JsonUtility.FromJson<StageDataJsonConfig>(jsonText);
                ApplyJsonConfigToStageData(jsonConfig, stageData);

                Debug.Log("StageData loaded from Json text");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load StageData from Json text: {e.Message}");
            }
        }

        /// <summary>
        /// StageDataSOをJsonテキストに変換
        /// </summary>
        public static string ConvertStageDataToJsonText(StageDataSO stageData, bool prettyPrint = true)
        {
            if (stageData == null)
            {
                Debug.LogError("StageData is null");
                return "";
            }

            try
            {
                var jsonConfig = ConvertStageDataToJsonConfig(stageData);
                return JsonUtility.ToJson(jsonConfig, prettyPrint);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to convert StageData to Json text: {e.Message}");
                return "";
            }
        }

        #endregion

        #region 検証

        /// <summary>
        /// JsonConfigの基本的な検証
        /// </summary>
        public static bool ValidateJsonConfig(StageDataJsonConfig jsonConfig)
        {
            if (jsonConfig == null)
            {
                Debug.LogError("JsonConfig is null");
                return false;
            }

            List<string> errors = new List<string>();

            // StageInfo検証
            if (jsonConfig.stageInfo == null)
            {
                errors.Add("StageInfo is null");
            }
            else
            {
                if (jsonConfig.stageInfo.worldNumber <= 0)
                    errors.Add("WorldNumber must be greater than 0");

                if (jsonConfig.stageInfo.stageNumber <= 0)
                    errors.Add("StageNumber must be greater than 0");

                if (string.IsNullOrEmpty(jsonConfig.stageInfo.stageName))
                    errors.Add("StageName is empty");

                if (jsonConfig.stageInfo.timeLimit <= 0)
                    errors.Add("TimeLimit must be greater than 0");
            }

            // TileMapSize検証
            if (jsonConfig.tileMapSize.x <= 0 || jsonConfig.tileMapSize.y <= 0)
                errors.Add("TileMapSize must be greater than 0");

            // TileSize検証
            if (jsonConfig.tileSize <= 0)
                errors.Add("TileSize must be greater than 0");

            // Slopes検証
            if (jsonConfig.slopes != null)
            {
                for (int i = 0; i < jsonConfig.slopes.Count; i++)
                {
                    var slope = jsonConfig.slopes[i];
                    if (slope.slopeAngle <= 0 || slope.slopeAngle > 60)
                        errors.Add($"Slope {i}: SlopeAngle must be between 0 and 60");

                    if (slope.slopeLength <= 0)
                        errors.Add($"Slope {i}: SlopeLength must be greater than 0");

                    if (slope.speedMultiplier <= 0)
                        errors.Add($"Slope {i}: SpeedMultiplier must be greater than 0");
                }
            }

            if (errors.Count > 0)
            {
                Debug.LogError("JsonConfig validation failed:\n" + string.Join("\n", errors));
                return false;
            }

            Debug.Log("JsonConfig validation passed");
            return true;
        }

        #endregion
    }
}