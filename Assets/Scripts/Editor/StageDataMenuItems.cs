#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataのJson変換用メニューアイテム
    /// </summary>
    public static class StageDataMenuItems
    {
        private const string MENU_ROOT = "Tools/Gravity Flip Lab/Stage Data/";

        #region 単体変換

        [MenuItem(MENU_ROOT + "Export Selected StageData to Json")]
        public static void ExportSelectedStageDataToJson()
        {
            var selectedStageData = GetSelectedStageData();
            if (selectedStageData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a StageDataSO asset.", "OK");
                return;
            }

            ExportStageDataToJson(selectedStageData);
        }

        [MenuItem(MENU_ROOT + "Export Selected StageData to Json", true)]
        public static bool ValidateExportSelectedStageDataToJson()
        {
            return GetSelectedStageData() != null;
        }

        [MenuItem(MENU_ROOT + "Import Json to Selected StageData")]
        public static void ImportJsonToSelectedStageData()
        {
            var selectedStageData = GetSelectedStageData();
            if (selectedStageData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a StageDataSO asset.", "OK");
                return;
            }

            ImportJsonToStageData(selectedStageData);
        }

        [MenuItem(MENU_ROOT + "Import Json to Selected StageData", true)]
        public static bool ValidateImportJsonToSelectedStageData()
        {
            return GetSelectedStageData() != null;
        }

        #endregion

        #region 一括変換

        [MenuItem(MENU_ROOT + "Batch Export All StageData to Json")]
        public static void BatchExportAllStageDataToJson()
        {
            var allStageData = FindAllStageDataAssets();
            if (allStageData.Length == 0)
            {
                EditorUtility.DisplayDialog("No StageData Found", "No StageDataSO assets found in the project.", "OK");
                return;
            }

            string baseDirectory = EditorUtility.OpenFolderPanel("Select Export Directory",
                Application.streamingAssetsPath, "");

            if (string.IsNullOrEmpty(baseDirectory))
                return;

            BatchExportStageData(allStageData, baseDirectory);
        }

        [MenuItem(MENU_ROOT + "Batch Import Json to StageData")]
        public static void BatchImportJsonToStageData()
        {
            string sourceDirectory = EditorUtility.OpenFolderPanel("Select Json Directory",
                Application.streamingAssetsPath, "");

            if (string.IsNullOrEmpty(sourceDirectory))
                return;

            BatchImportStageData(sourceDirectory);
        }

        [MenuItem(MENU_ROOT + "Create Sample Json Files")]
        public static void CreateSampleJsonFiles()
        {
            string baseDirectory = EditorUtility.SaveFolderPanel("Select Directory for Sample Files",
                Application.streamingAssetsPath, "StageData");

            if (string.IsNullOrEmpty(baseDirectory))
                return;

            CreateSampleFiles(baseDirectory);
        }

        #endregion

        #region ユーティリティ

        [MenuItem(MENU_ROOT + "Validate All StageData Json")]
        public static void ValidateAllStageDataJson()
        {
            string jsonDirectory = EditorUtility.OpenFolderPanel("Select Json Directory",
                Application.streamingAssetsPath, "");

            if (string.IsNullOrEmpty(jsonDirectory))
                return;

            ValidateJsonFiles(jsonDirectory);
        }

        [MenuItem(MENU_ROOT + "Setup StreamingAssets Directory")]
        public static void SetupStreamingAssetsDirectory()
        {
            string streamingAssetsPath = Application.streamingAssetsPath;
            string stageDataPath = Path.Combine(streamingAssetsPath, "StageData");

            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }

            if (!Directory.Exists(stageDataPath))
            {
                Directory.CreateDirectory(stageDataPath);
            }

            // ワールドディレクトリを作成
            for (int world = 1; world <= 5; world++)
            {
                string worldPath = Path.Combine(stageDataPath, $"World{world}");
                if (!Directory.Exists(worldPath))
                {
                    Directory.CreateDirectory(worldPath);
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"StreamingAssets directory structure created at:\n{stageDataPath}", "OK");
        }

        #endregion

        #region プライベートメソッド

        private static StageDataSO GetSelectedStageData()
        {
            var selectedObject = Selection.activeObject;
            return selectedObject as StageDataSO;
        }

        private static StageDataSO[] FindAllStageDataAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageDataSO");
            return guids.Select(guid => AssetDatabase.LoadAssetAtPath<StageDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
                        .Where(asset => asset != null)
                        .ToArray();
        }

        private static void ExportStageDataToJson(StageDataSO stageData)
        {
            if (stageData?.stageInfo == null)
            {
                EditorUtility.DisplayDialog("Error", "StageInfo is not set up properly.", "OK");
                return;
            }

            string defaultFileName = $"Stage{stageData.stageInfo.worldNumber}-{stageData.stageInfo.stageNumber}.json";
            string defaultDirectory = Path.Combine(Application.streamingAssetsPath, "StageData",
                $"World{stageData.stageInfo.worldNumber}");

            if (!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }

            string savePath = EditorUtility.SaveFilePanel("Export StageData to Json",
                defaultDirectory, defaultFileName, "json");

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    StageDataJsonConverter.SaveStageDataToJsonFile(stageData, savePath);
                    EditorUtility.DisplayDialog("Success", $"StageData exported to:\n{savePath}", "OK");

                    if (savePath.StartsWith(Application.dataPath))
                    {
                        AssetDatabase.Refresh();
                    }
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to export Json:\n{e.Message}", "OK");
                }
            }
        }

        private static void ImportJsonToStageData(StageDataSO stageData)
        {
            string defaultDirectory = Path.Combine(Application.streamingAssetsPath, "StageData");
            string loadPath = EditorUtility.OpenFilePanel("Import StageData from Json",
                defaultDirectory, "json");

            if (!string.IsNullOrEmpty(loadPath))
            {
                try
                {
                    Undo.RecordObject(stageData, "Import StageData from Json");
                    StageDataJsonConverter.LoadStageDataFromJsonFile(loadPath, stageData);
                    EditorUtility.SetDirty(stageData);
                    EditorUtility.DisplayDialog("Success", $"StageData imported from:\n{loadPath}", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to import Json:\n{e.Message}", "OK");
                }
            }
        }

        private static void BatchExportStageData(StageDataSO[] stageDataArray, string baseDirectory)
        {
            int successCount = 0;
            int totalCount = stageDataArray.Length;

            EditorUtility.DisplayProgressBar("Exporting StageData", "Starting batch export...", 0);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var stageData = stageDataArray[i];
                    float progress = (float)i / totalCount;

                    EditorUtility.DisplayProgressBar("Exporting StageData",
                        $"Exporting {stageData.name} ({i + 1}/{totalCount})", progress);

                    if (stageData?.stageInfo != null)
                    {
                        try
                        {
                            string worldDir = Path.Combine(baseDirectory, $"World{stageData.stageInfo.worldNumber}");
                            if (!Directory.Exists(worldDir))
                            {
                                Directory.CreateDirectory(worldDir);
                            }

                            string fileName = $"Stage{stageData.stageInfo.worldNumber}-{stageData.stageInfo.stageNumber}.json";
                            string filePath = Path.Combine(worldDir, fileName);

                            StageDataJsonConverter.SaveStageDataToJsonFile(stageData, filePath);
                            successCount++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Failed to export {stageData.name}: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping {stageData.name}: StageInfo is null");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Batch Export Complete",
                $"Successfully exported {successCount} out of {totalCount} StageData assets to:\n{baseDirectory}", "OK");
        }

        private static void BatchImportStageData(string sourceDirectory)
        {
            var allStageData = FindAllStageDataAssets();
            var stageDataDict = allStageData.ToDictionary(sd => $"Stage{sd.stageInfo?.worldNumber}-{sd.stageInfo?.stageNumber}", sd => sd);

            var jsonFiles = Directory.GetFiles(sourceDirectory, "*.json", SearchOption.AllDirectories);
            int successCount = 0;
            int totalCount = jsonFiles.Length;

            EditorUtility.DisplayProgressBar("Importing Json", "Starting batch import...", 0);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    string jsonFile = jsonFiles[i];
                    float progress = (float)i / totalCount;

                    EditorUtility.DisplayProgressBar("Importing Json",
                        $"Importing {Path.GetFileName(jsonFile)} ({i + 1}/{totalCount})", progress);

                    string fileName = Path.GetFileNameWithoutExtension(jsonFile);

                    if (stageDataDict.TryGetValue(fileName, out StageDataSO stageData))
                    {
                        try
                        {
                            Undo.RecordObject(stageData, $"Import Json {fileName}");
                            StageDataJsonConverter.LoadStageDataFromJsonFile(jsonFile, stageData);
                            EditorUtility.SetDirty(stageData);
                            successCount++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Failed to import {fileName}: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No matching StageData found for {fileName}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Batch Import Complete",
                $"Successfully imported {successCount} out of {totalCount} Json files.", "OK");
        }

        private static void CreateSampleFiles(string baseDirectory)
        {
            int createdCount = 0;

            EditorUtility.DisplayProgressBar("Creating Sample Files", "Creating sample Json files...", 0);

            try
            {
                for (int world = 1; world <= 3; world++)
                {
                    string worldDir = Path.Combine(baseDirectory, $"World{world}");
                    if (!Directory.Exists(worldDir))
                    {
                        Directory.CreateDirectory(worldDir);
                    }

                    for (int stage = 1; stage <= 3; stage++)
                    {
                        float progress = ((world - 1) * 3 + stage - 1) / 9f;
                        EditorUtility.DisplayProgressBar("Creating Sample Files",
                            $"Creating World {world} - Stage {stage}", progress);

                        string fileName = $"Stage{world}-{stage}.json";
                        string filePath = Path.Combine(worldDir, fileName);

                        if (!File.Exists(filePath))
                        {
                            StageDataJsonLoader.CreateSampleJsonFile(world, stage, filePath);
                            createdCount++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Sample Files Created",
                $"Created {createdCount} sample Json files in:\n{baseDirectory}", "OK");
        }

        private static void ValidateJsonFiles(string jsonDirectory)
        {
            var jsonFiles = Directory.GetFiles(jsonDirectory, "*.json", SearchOption.AllDirectories);
            int validCount = 0;
            int totalCount = jsonFiles.Length;

            EditorUtility.DisplayProgressBar("Validating Json Files", "Starting validation...", 0);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    string jsonFile = jsonFiles[i];
                    float progress = (float)i / totalCount;

                    EditorUtility.DisplayProgressBar("Validating Json Files",
                        $"Validating {Path.GetFileName(jsonFile)} ({i + 1}/{totalCount})", progress);

                    if (StageDataJsonLoader.ValidateStageJson(jsonFile))
                    {
                        validCount++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Validation Complete",
                $"Validation results:\nValid files: {validCount}\nInvalid files: {totalCount - validCount}\nTotal files: {totalCount}", "OK");
        }

        #endregion
    }
}
#endif