#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataSOのカスタムエディタ
    /// </summary>
    [CustomEditor(typeof(StageDataSO))]
    public class StageDataEditor : UnityEditor.Editor
    {
        private StageDataSO stageData;
        private string jsonPreview = "";
        private bool showJsonPreview = false;
        private Vector2 jsonScrollPosition = Vector2.zero;

        private void OnEnable()
        {
            stageData = (StageDataSO)target;
        }

        public override void OnInspectorGUI()
        {
            // デフォルトインスペクターを表示
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Json Conversion Tools", EditorStyles.boldLabel);

            // Json変換ボタン群
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export to Json", GUILayout.Height(30)))
            {
                ExportToJson();
            }

            if (GUILayout.Button("Import from Json", GUILayout.Height(30)))
            {
                ImportFromJson();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preview Json", GUILayout.Height(25)))
            {
                PreviewJson();
            }

            if (GUILayout.Button("Copy Json to Clipboard", GUILayout.Height(25)))
            {
                CopyJsonToClipboard();
            }

            EditorGUILayout.EndHorizontal();

            // Jsonプレビュー表示
            if (showJsonPreview && !string.IsNullOrEmpty(jsonPreview))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Json Preview:", EditorStyles.boldLabel);

                jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition,
                    GUILayout.Height(200), GUILayout.ExpandWidth(true));

                EditorGUILayout.TextArea(jsonPreview, GUILayout.ExpandHeight(true));

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Hide Preview"))
                {
                    showJsonPreview = false;
                }
            }

            // ファイル情報表示
            EditorGUILayout.Space(10);
            ShowFileInfo();
        }

        private void ExportToJson()
        {
            if (stageData?.stageInfo == null)
            {
                EditorUtility.DisplayDialog("Error", "StageInfo is not set up properly.", "OK");
                return;
            }

            string defaultPath = GetDefaultJsonPath();
            string savePath = EditorUtility.SaveFilePanel("Export StageData to Json",
                Path.GetDirectoryName(defaultPath),
                Path.GetFileName(defaultPath),
                "json");

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    StageDataJsonConverter.SaveStageDataToJsonFile(stageData, savePath);
                    EditorUtility.DisplayDialog("Success", $"StageData exported to:\n{savePath}", "OK");

                    // プロジェクト内のファイルの場合、アセットを更新
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

        private void ImportFromJson()
        {
            string defaultPath = GetDefaultJsonPath();
            string loadPath = EditorUtility.OpenFilePanel("Import StageData from Json",
                Path.GetDirectoryName(defaultPath), "json");

            if (!string.IsNullOrEmpty(loadPath))
            {
                try
                {
                    // バックアップを作成
                    Undo.RecordObject(stageData, "Import StageData from Json");

                    StageDataJsonConverter.LoadStageDataFromJsonFile(loadPath, stageData);

                    // エディタを更新
                    EditorUtility.SetDirty(stageData);

                    EditorUtility.DisplayDialog("Success", $"StageData imported from:\n{loadPath}", "OK");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to import Json:\n{e.Message}", "OK");
                }
            }
        }

        private void PreviewJson()
        {
            try
            {
                jsonPreview = StageDataJsonConverter.ConvertStageDataToJsonText(stageData, true);
                showJsonPreview = true;
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate Json preview:\n{e.Message}", "OK");
            }
        }

        private void CopyJsonToClipboard()
        {
            try
            {
                string jsonText = StageDataJsonConverter.ConvertStageDataToJsonText(stageData, true);
                EditorGUIUtility.systemCopyBuffer = jsonText;
                EditorUtility.DisplayDialog("Success", "Json copied to clipboard!", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to copy Json:\n{e.Message}", "OK");
            }
        }

        private string GetDefaultJsonPath()
        {
            if (stageData?.stageInfo != null)
            {
                string fileName = $"Stage{stageData.stageInfo.worldNumber}-{stageData.stageInfo.stageNumber}.json";
                return Path.Combine(Application.streamingAssetsPath, "StageData",
                    $"World{stageData.stageInfo.worldNumber}", fileName);
            }
            else
            {
                return Path.Combine(Application.streamingAssetsPath, "StageData", "Stage1-1.json");
            }
        }

        private void ShowFileInfo()
        {
            EditorGUILayout.LabelField("File Information", EditorStyles.boldLabel);

            string assetPath = AssetDatabase.GetAssetPath(stageData);
            EditorGUILayout.LabelField("Asset Path:", assetPath);

            string expectedJsonPath = GetDefaultJsonPath();
            EditorGUILayout.LabelField("Expected Json Path:", expectedJsonPath);

            bool jsonExists = File.Exists(expectedJsonPath);
            EditorGUILayout.LabelField("Json File Exists:", jsonExists ? "Yes" : "No");

            if (jsonExists)
            {
                FileInfo fileInfo = new FileInfo(expectedJsonPath);
                EditorGUILayout.LabelField("Json File Size:", $"{fileInfo.Length} bytes");
                EditorGUILayout.LabelField("Last Modified:", fileInfo.LastWriteTime.ToString());
            }
        }
    }
}
#endif