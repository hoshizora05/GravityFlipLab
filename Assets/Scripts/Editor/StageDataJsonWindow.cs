#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataとJsonの変換を行うエディタウィンドウ
    /// </summary>
    public class StageDataJsonWindow : EditorWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private string sourceDirectory = "";
        private string targetDirectory = "";
        private List<StageDataInfo> stageDataList = new List<StageDataInfo>();
        private List<JsonFileInfo> jsonFileList = new List<JsonFileInfo>();
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Export to Json", "Import from Json", "Validation", "Settings" };

        [System.Serializable]
        private class StageDataInfo
        {
            public StageDataSO stageData;
            public string name;
            public bool isSelected;
            public string status;
            public bool hasMatchingJson;
        }

        [System.Serializable]
        private class JsonFileInfo
        {
            public string filePath;
            public string fileName;
            public bool isSelected;
            public bool isValid;
            public StageDataSO matchingStageData;
            public string status;
        }

        [MenuItem("Tools/Gravity Flip Lab/Stage Data Json Converter")]
        public static void OpenWindow()
        {
            var window = GetWindow<StageDataJsonWindow>("Stage Data Json Converter");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            sourceDirectory = Application.streamingAssetsPath;
            targetDirectory = Application.streamingAssetsPath;
            RefreshStageDataList();
            RefreshJsonFileList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // ヘッダー
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stage Data Json Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // タブ
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawExportTab();
                    break;
                case 1:
                    DrawImportTab();
                    break;
                case 2:
                    DrawValidationTab();
                    break;
                case 3:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #region Export Tab

        private void DrawExportTab()
        {
            EditorGUILayout.LabelField("Export StageData to Json", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // ディレクトリ選択
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Directory:", GUILayout.Width(100));
            targetDirectory = EditorGUILayout.TextField(targetDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.OpenFolderPanel("Select Target Directory", targetDirectory, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    targetDirectory = newPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 更新ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
            {
                RefreshStageDataList();
            }

            if (GUILayout.Button("Select All", GUILayout.Height(25)))
            {
                foreach (var info in stageDataList)
                {
                    info.isSelected = true;
                }
            }

            if (GUILayout.Button("Select None", GUILayout.Height(25)))
            {
                foreach (var info in stageDataList)
                {
                    info.isSelected = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // StageDataリスト
            EditorGUILayout.LabelField($"Found {stageDataList.Count} StageData assets:", EditorStyles.boldLabel);

            foreach (var info in stageDataList)
            {
                EditorGUILayout.BeginHorizontal();

                info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(20));
                EditorGUILayout.LabelField(info.name, GUILayout.Width(200));

                // ステータス表示
                Color originalColor = GUI.color;
                if (info.hasMatchingJson)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("Json exists", GUILayout.Width(80));
                }
                else
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("New", GUILayout.Width(80));
                }
                GUI.color = originalColor;

                EditorGUILayout.LabelField(info.status, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // エクスポートボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Selected", GUILayout.Height(30)))
            {
                ExportSelectedStageData();
            }

            if (GUILayout.Button("Export All", GUILayout.Height(30)))
            {
                foreach (var info in stageDataList)
                {
                    info.isSelected = true;
                }
                ExportSelectedStageData();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshStageDataList()
        {
            stageDataList.Clear();

            string[] guids = AssetDatabase.FindAssets("t:StageDataSO");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                StageDataSO stageData = AssetDatabase.LoadAssetAtPath<StageDataSO>(assetPath);

                if (stageData != null)
                {
                    var info = new StageDataInfo
                    {
                        stageData = stageData,
                        name = stageData.name,
                        isSelected = false,
                        status = GetStageDataStatus(stageData)
                    };

                    // 対応するJsonファイルの存在確認
                    if (stageData.stageInfo != null)
                    {
                        string expectedJsonPath = Path.Combine(targetDirectory, "StageData",
                            $"World{stageData.stageInfo.worldNumber}",
                            $"Stage{stageData.stageInfo.worldNumber}-{stageData.stageInfo.stageNumber}.json");
                        info.hasMatchingJson = File.Exists(expectedJsonPath);
                    }

                    stageDataList.Add(info);
                }
            }
        }

        private string GetStageDataStatus(StageDataSO stageData)
        {
            if (stageData.stageInfo == null)
                return "StageInfo missing";

            return $"World {stageData.stageInfo.worldNumber} - Stage {stageData.stageInfo.stageNumber}";
        }

        private void ExportSelectedStageData()
        {
            var selectedStageData = stageDataList.Where(info => info.isSelected).ToList();
            if (selectedStageData.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one StageData to export.", "OK");
                return;
            }

            int successCount = 0;
            int totalCount = selectedStageData.Count;

            EditorUtility.DisplayProgressBar("Exporting StageData", "Starting export...", 0);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var info = selectedStageData[i];
                    float progress = (float)i / totalCount;

                    EditorUtility.DisplayProgressBar("Exporting StageData",
                        $"Exporting {info.name} ({i + 1}/{totalCount})", progress);

                    if (info.stageData?.stageInfo != null)
                    {
                        try
                        {
                            string worldDir = Path.Combine(targetDirectory, "StageData", $"World{info.stageData.stageInfo.worldNumber}");
                            if (!Directory.Exists(worldDir))
                            {
                                Directory.CreateDirectory(worldDir);
                            }

                            string fileName = $"Stage{info.stageData.stageInfo.worldNumber}-{info.stageData.stageInfo.stageNumber}.json";
                            string filePath = Path.Combine(worldDir, fileName);

                            StageDataJsonConverter.SaveStageDataToJsonFile(info.stageData, filePath);
                            info.status = "Exported successfully";
                            successCount++;
                        }
                        catch (System.Exception e)
                        {
                            info.status = $"Export failed: {e.Message}";
                            Debug.LogError($"Failed to export {info.name}: {e.Message}");
                        }
                    }
                    else
                    {
                        info.status = "Export failed: StageInfo is null";
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export Complete",
                $"Successfully exported {successCount} out of {totalCount} StageData assets.", "OK");

            RefreshStageDataList();
        }

        #endregion

        #region Import Tab

        private void DrawImportTab()
        {
            EditorGUILayout.LabelField("Import Json to StageData", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // ディレクトリ選択
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source Directory:", GUILayout.Width(100));
            sourceDirectory = EditorGUILayout.TextField(sourceDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.OpenFolderPanel("Select Source Directory", sourceDirectory, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    sourceDirectory = newPath;
                    RefreshJsonFileList();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 更新ボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
            {
                RefreshJsonFileList();
            }

            if (GUILayout.Button("Select All", GUILayout.Height(25)))
            {
                foreach (var info in jsonFileList)
                {
                    info.isSelected = true;
                }
            }

            if (GUILayout.Button("Select None", GUILayout.Height(25)))
            {
                foreach (var info in jsonFileList)
                {
                    info.isSelected = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Jsonファイルリスト
            EditorGUILayout.LabelField($"Found {jsonFileList.Count} Json files:", EditorStyles.boldLabel);

            foreach (var info in jsonFileList)
            {
                EditorGUILayout.BeginHorizontal();

                info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(20));
                EditorGUILayout.LabelField(info.fileName, GUILayout.Width(200));

                // 検証ステータス
                Color originalColor = GUI.color;
                if (info.isValid)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("Valid", GUILayout.Width(50));
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("Invalid", GUILayout.Width(50));
                }
                GUI.color = originalColor;

                // マッチングステータス
                if (info.matchingStageData != null)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("Match found", GUILayout.Width(80));
                }
                else
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("No match", GUILayout.Width(80));
                }
                GUI.color = originalColor;

                EditorGUILayout.LabelField(info.status, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // インポートボタン
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Selected", GUILayout.Height(30)))
            {
                ImportSelectedJsonFiles();
            }

            if (GUILayout.Button("Import All", GUILayout.Height(30)))
            {
                foreach (var info in jsonFileList)
                {
                    info.isSelected = true;
                }
                ImportSelectedJsonFiles();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshJsonFileList()
        {
            jsonFileList.Clear();

            if (!Directory.Exists(sourceDirectory))
                return;

            var jsonFiles = Directory.GetFiles(sourceDirectory, "*.json", SearchOption.AllDirectories);
            var allStageData = FindAllStageDataAssets();

            foreach (string jsonFile in jsonFiles)
            {
                var info = new JsonFileInfo
                {
                    filePath = jsonFile,
                    fileName = Path.GetFileName(jsonFile),
                    isSelected = false,
                    isValid = ValidateJsonFile(jsonFile),
                    matchingStageData = FindMatchingStageData(jsonFile, allStageData),
                    status = GetJsonFileStatus(jsonFile)
                };

                jsonFileList.Add(info);
            }
        }

        private bool ValidateJsonFile(string jsonFile)
        {
            try
            {
                return StageDataJsonLoader.ValidateStageJson(jsonFile);
            }
            catch
            {
                return false;
            }
        }

        private StageDataSO FindMatchingStageData(string jsonFile, StageDataSO[] allStageData)
        {
            string fileName = Path.GetFileNameWithoutExtension(jsonFile);

            // ファイル名から世界とステージ番号を抽出
            if (fileName.StartsWith("Stage") && fileName.Contains("-"))
            {
                string numberPart = fileName.Substring(5);
                string[] parts = numberPart.Split('-');

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int world) &&
                    int.TryParse(parts[1], out int stage))
                {
                    return allStageData.FirstOrDefault(sd =>
                        sd.stageInfo != null &&
                        sd.stageInfo.worldNumber == world &&
                        sd.stageInfo.stageNumber == stage);
                }
            }

            return null;
        }

        private string GetJsonFileStatus(string jsonFile)
        {
            try
            {
                string jsonText = File.ReadAllText(jsonFile);
                var config = JsonUtility.FromJson<StageDataJsonConfig>(jsonText);

                if (config?.stageInfo != null)
                {
                    return $"World {config.stageInfo.worldNumber} - Stage {config.stageInfo.stageNumber}";
                }
                else
                {
                    return "Invalid Json structure";
                }
            }
            catch (System.Exception e)
            {
                return $"Parse error: {e.Message}";
            }
        }

        private void ImportSelectedJsonFiles()
        {
            var selectedJsonFiles = jsonFileList.Where(info => info.isSelected && info.matchingStageData != null).ToList();
            if (selectedJsonFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("No Valid Selection",
                    "Please select at least one valid Json file with matching StageData.", "OK");
                return;
            }

            int successCount = 0;
            int totalCount = selectedJsonFiles.Count;

            EditorUtility.DisplayProgressBar("Importing Json", "Starting import...", 0);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    var info = selectedJsonFiles[i];
                    float progress = (float)i / totalCount;

                    EditorUtility.DisplayProgressBar("Importing Json",
                        $"Importing {info.fileName} ({i + 1}/{totalCount})", progress);

                    try
                    {
                        Undo.RecordObject(info.matchingStageData, $"Import Json {info.fileName}");
                        StageDataJsonConverter.LoadStageDataFromJsonFile(info.filePath, info.matchingStageData);
                        EditorUtility.SetDirty(info.matchingStageData);
                        info.status = "Imported successfully";
                        successCount++;
                    }
                    catch (System.Exception e)
                    {
                        info.status = $"Import failed: {e.Message}";
                        Debug.LogError($"Failed to import {info.fileName}: {e.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog("Import Complete",
                $"Successfully imported {successCount} out of {totalCount} Json files.", "OK");

            RefreshJsonFileList();
        }

        #endregion

        #region Validation Tab

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("Json File Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox("This tab validates Json files and checks for potential issues.", MessageType.Info);
            EditorGUILayout.Space(10);

            // 検証対象ディレクトリ
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Validation Directory:", GUILayout.Width(120));
            sourceDirectory = EditorGUILayout.TextField(sourceDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string newPath = EditorUtility.OpenFolderPanel("Select Directory to Validate", sourceDirectory, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    sourceDirectory = newPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate All Json Files", GUILayout.Height(30)))
            {
                ValidateAllJsonFiles();
            }

            if (GUILayout.Button("Fix Common Issues", GUILayout.Height(30)))
            {
                FixCommonIssues();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 検証結果表示
            if (jsonFileList.Count > 0)
            {
                EditorGUILayout.LabelField("Validation Results:", EditorStyles.boldLabel);

                int validCount = jsonFileList.Count(info => info.isValid);
                int invalidCount = jsonFileList.Count - validCount;

                EditorGUILayout.LabelField($"Valid files: {validCount}");
                EditorGUILayout.LabelField($"Invalid files: {invalidCount}");
                EditorGUILayout.LabelField($"Total files: {jsonFileList.Count}");

                EditorGUILayout.Space(10);

                foreach (var info in jsonFileList)
                {
                    EditorGUILayout.BeginHorizontal();

                    Color originalColor = GUI.color;
                    if (info.isValid)
                    {
                        GUI.color = Color.green;
                        EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                    }
                    GUI.color = originalColor;

                    EditorGUILayout.LabelField(info.fileName, GUILayout.Width(200));
                    EditorGUILayout.LabelField(info.status, GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void ValidateAllJsonFiles()
        {
            RefreshJsonFileList();

            if (jsonFileList.Count == 0)
            {
                EditorUtility.DisplayDialog("No Files Found", "No Json files found in the specified directory.", "OK");
            }
            else
            {
                int validCount = jsonFileList.Count(info => info.isValid);
                EditorUtility.DisplayDialog("Validation Complete",
                    $"Validation completed.\nValid files: {validCount}\nInvalid files: {jsonFileList.Count - validCount}", "OK");
            }
        }

        private void FixCommonIssues()
        {
            EditorUtility.DisplayDialog("Fix Issues", "Common issue fixing is not implemented yet.", "OK");
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Default Directories", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("StreamingAssets Path:", GUILayout.Width(150));
            EditorGUILayout.LabelField(Application.streamingAssetsPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Setup StreamingAssets Directory Structure", GUILayout.Height(30)))
            {
                SetupStreamingAssetsDirectory();
            }

            if (GUILayout.Button("Open StreamingAssets Folder", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(Application.streamingAssetsPath);
            }

            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Sample Json Files", GUILayout.Height(30)))
            {
                CreateSampleJsonFiles();
            }

            if (GUILayout.Button("Reset All Settings", GUILayout.Height(25)))
            {
                ResetSettings();
            }
        }

        private void SetupStreamingAssetsDirectory()
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

            for (int world = 1; world <= 5; world++)
            {
                string worldPath = Path.Combine(stageDataPath, $"World{world}");
                if (!Directory.Exists(worldPath))
                {
                    Directory.CreateDirectory(worldPath);
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "StreamingAssets directory structure created successfully.", "OK");
        }

        private void CreateSampleJsonFiles()
        {
            string baseDirectory = EditorUtility.SaveFolderPanel("Select Directory for Sample Files",
                Application.streamingAssetsPath, "StageData");

            if (string.IsNullOrEmpty(baseDirectory))
                return;

            int createdCount = 0;
            for (int world = 1; world <= 2; world++)
            {
                for (int stage = 1; stage <= 3; stage++)
                {
                    string worldDir = Path.Combine(baseDirectory, $"World{world}");
                    if (!Directory.Exists(worldDir))
                    {
                        Directory.CreateDirectory(worldDir);
                    }

                    string fileName = $"Stage{world}-{stage}.json";
                    string filePath = Path.Combine(worldDir, fileName);

                    if (!File.Exists(filePath))
                    {
                        StageDataJsonLoader.CreateSampleJsonFile(world, stage, filePath);
                        createdCount++;
                    }
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Sample Files Created",
                $"Created {createdCount} sample Json files.", "OK");
        }

        private void ResetSettings()
        {
            sourceDirectory = Application.streamingAssetsPath;
            targetDirectory = Application.streamingAssetsPath;
            stageDataList.Clear();
            jsonFileList.Clear();
            selectedTab = 0;

            EditorUtility.DisplayDialog("Settings Reset", "All settings have been reset to default values.", "OK");
        }

        #endregion

        #region Utility Methods

        private StageDataSO[] FindAllStageDataAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:StageDataSO");
            return guids.Select(guid => AssetDatabase.LoadAssetAtPath<StageDataSO>(AssetDatabase.GUIDToAssetPath(guid)))
                        .Where(asset => asset != null)
                        .ToArray();
        }

        #endregion
    }
}
#endif