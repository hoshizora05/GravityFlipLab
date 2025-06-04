using UnityEngine;

namespace GravityFlipLab
{
    /// <summary>
    /// アプリケーション終了時の適切なクリーンアップを管理
    /// </summary>
    public class ApplicationCleanupManager : MonoBehaviour
    {
        private static ApplicationCleanupManager _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                GameObject cleanupManager = new GameObject("ApplicationCleanupManager");
                _instance = cleanupManager.AddComponent<ApplicationCleanupManager>();
                DontDestroyOnLoad(cleanupManager);

                Debug.Log("ApplicationCleanupManager: Initialized");
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log("ApplicationCleanupManager: Application quit detected - starting cleanup");
            CleanupAllSystems();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // アプリケーションがフォーカスを失った時（モバイル対応）
                Debug.Log("ApplicationCleanupManager: Application lost focus");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // アプリケーションが一時停止された時（モバイル対応）
                Debug.Log("ApplicationCleanupManager: Application paused");
            }
        }

        private void CleanupAllSystems()
        {
            Debug.Log("ApplicationCleanupManager: Cleaning up all systems");

            // SceneTransitionManagerの手動クリーンアップ
            try
            {
                SceneTransitionManager.ManualCleanup();
                Debug.Log("ApplicationCleanupManager: SceneTransitionManager cleaned up");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplicationCleanupManager: SceneTransitionManager cleanup failed: {e.Message}");
            }

            // FadeTransitionManagerのクリーンアップ
            try
            {
                if (FadeTransitionManager.Instance != null)
                {
                    var fadeTransitionType = typeof(FadeTransitionManager);
                    var instanceField = fadeTransitionType.GetField("_instance",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (FadeTransitionManager.Instance.gameObject != null)
                    {
                        DestroyImmediate(FadeTransitionManager.Instance.gameObject);
                    }

                    instanceField?.SetValue(null, null);
                    Debug.Log("ApplicationCleanupManager: FadeTransitionManager cleaned up");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplicationCleanupManager: FadeTransitionManager cleanup failed: {e.Message}");
            }

            // GameManagerのクリーンアップ
            try
            {
                if (GameManager.Instance != null)
                {
                    var gameManagerType = typeof(GameManager);
                    var instanceField = gameManagerType.GetField("_instance",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (GameManager.Instance.gameObject != null)
                    {
                        DestroyImmediate(GameManager.Instance.gameObject);
                    }

                    instanceField?.SetValue(null, null);
                    Debug.Log("ApplicationCleanupManager: GameManager cleaned up");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplicationCleanupManager: GameManager cleanup failed: {e.Message}");
            }

            // StageManagerのクリーンアップ（シーン固有）
            try
            {
                Stage.StageManager.ManualCleanup();
                Debug.Log("ApplicationCleanupManager: StageManager cleaned up");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplicationCleanupManager: StageManager cleanup failed: {e.Message}");
            }

            // SaveManagerのクリーンアップ
            try
            {
                if (SaveManager.Instance != null)
                {
                    var saveManagerType = typeof(SaveManager);
                    var instanceField = saveManagerType.GetField("_instance",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                    if (SaveManager.Instance.gameObject != null)
                    {
                        DestroyImmediate(SaveManager.Instance.gameObject);
                    }

                    instanceField?.SetValue(null, null);
                    Debug.Log("ApplicationCleanupManager: SaveManager cleaned up");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplicationCleanupManager: SaveManager cleanup failed: {e.Message}");
            }

            // CheckpointManagerのクリーンアップ
            try
            {
                var checkpointManagerType = System.Type.GetType("GravityFlipLab.CheckpointManager");
                if (checkpointManagerType != null)
                {
                    var instanceProperty = checkpointManagerType.GetProperty("Instance",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instanceField = checkpointManagerType.GetField("_instance",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        instanceField?.SetValue(null, null);
                        Debug.Log("ApplicationCleanupManager: CheckpointManager cleaned up");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"ApplicationCleanupManager: CheckpointManager cleanup failed (this is normal if not implemented): {e.Message}");
            }

            Debug.Log("ApplicationCleanupManager: System cleanup completed");
        }

        /// <summary>
        /// エディター停止時のクリーンアップ（エディター専用）
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("ApplicationCleanupManager: Editor play mode exiting - cleaning up");
                if (_instance != null)
                {
                    _instance.CleanupAllSystems();
                }
            }
        }
#endif
    }
}