using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GravityFlipLab
{
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager _instance;
        public static SceneTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SceneTransitionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneTransitionManager");
                        _instance = go.AddComponent<SceneTransitionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Scene Configuration")]
        public string titleSceneName = "TitleScene";
        public string mainMenuSceneName = "MainMenuScene";
        public string stageSelectSceneName = "StageSelect";
        public string gameplaySceneName = "GameScene";
        public string leaderboardSceneName = "Leaderboard";
        public string optionsSceneName = "Options";
        public string shopSceneName = "Shop";

        [Header("Transition Settings")]
        public float transitionDuration = 0.5f;
        public bool useLoadingScreen = true;

        // Events
        public static event System.Action<string> OnSceneLoadStart;
        public static event System.Action<string> OnSceneLoadComplete;

        private bool isLoading = false;
        private string currentSceneName;
        private bool isApplicationQuitting = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                isApplicationQuitting = false;
                Debug.Log("SceneTransitionManager: Instance created with DontDestroyOnLoad");
            }
            else if (_instance != this)
            {
                Debug.Log("SceneTransitionManager: Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }
        }

        public void Initialize()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("SceneTransitionManager initialized");
        }

        public void LoadScene(SceneType sceneType)
        {
            string sceneName = GetSceneName(sceneType);
            LoadScene(sceneName);
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading || sceneName == currentSceneName) return;

            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        public void LoadGameplayScene(int world, int stage)
        {
            GameManager.Instance.SetCurrentStage(world, stage);
            LoadScene(SceneType.Gameplay);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(currentSceneName);
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            isLoading = true;
            GameManager.Instance.ChangeGameState(GameState.Loading);

            OnSceneLoadStart?.Invoke(sceneName);

            // Optional loading screen transition
            if (useLoadingScreen)
            {
                // Show loading screen
                yield return new WaitForSeconds(transitionDuration);
            }

            // Load the scene asynchronously
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            loadOperation.allowSceneActivation = false;

            // Wait for loading to complete
            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            // Optional additional loading time
            yield return new WaitForSeconds(0.1f);

            // Activate the scene
            loadOperation.allowSceneActivation = true;

            // Wait for scene activation
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            currentSceneName = sceneName;
            isLoading = false;

            OnSceneLoadComplete?.Invoke(sceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"SceneTransitionManager: Scene loaded: {scene.name}");

            // Update game state based on scene
            GameState newState = GetGameStateFromScene(scene.name);
            GameManager.Instance.ChangeGameState(newState);

            // シーン固有の初期化処理
            HandleSceneSpecificInitialization(scene.name);
        }

        /// <summary>
        /// シーン固有の初期化処理
        /// </summary>
        private void HandleSceneSpecificInitialization(string sceneName)
        {
            // GameSceneの場合はStageManagerに通知
            if (sceneName == gameplaySceneName)
            {
                Debug.Log("SceneTransitionManager: Gameplay scene loaded, notifying StageManager");

                // StageManagerの初期化を待つ
                StartCoroutine(NotifyStageManagerAfterDelay());
            }
        }

        /// <summary>
        /// StageManagerに遅延通知（StageManagerの準備を待つ）
        /// </summary>
        private IEnumerator NotifyStageManagerAfterDelay()
        {
            // フレームを待ってStageManagerの準備を確認
            yield return new WaitForEndOfFrame();

            // StageManagerの検索（既存のものを探すのみ、作成はしない）
            var stageManager = FindFirstObjectByType<Stage.StageManager>();
            int attempts = 0;
            const int maxAttempts = 10;

            while (stageManager == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                stageManager = FindFirstObjectByType<Stage.StageManager>();
                attempts++;
            }

            if (stageManager != null)
            {
                Debug.Log("SceneTransitionManager: Found StageManager, sending scene loaded notification");
                stageManager.OnSceneLoaded();
            }
            else
            {
                Debug.LogWarning("SceneTransitionManager: StageManager not found after scene load - it should be manually placed in the GameScene");

                // StageManagerが見つからない場合のフォールバック：シンプルな作成
                CreateBasicStageManager();
            }
        }

        /// <summary>
        /// StageManagerが見つからない場合の基本的なStageManager作成
        /// </summary>
        private void CreateBasicStageManager()
        {
            Debug.Log("SceneTransitionManager: Creating basic StageManager as fallback");

            GameObject stageManagerObj = new GameObject("StageManager");
            var stageManager = stageManagerObj.AddComponent<Stage.StageManager>();

            // 基本的な初期化を試行
            if (stageManager != null)
            {
                stageManager.OnSceneLoaded();
                Debug.Log("SceneTransitionManager: Basic StageManager created and initialized");
            }
        }

        private string GetSceneName(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.Title: return titleSceneName;
                case SceneType.MainMenu: return mainMenuSceneName;
                case SceneType.StageSelect: return stageSelectSceneName;
                case SceneType.Gameplay: return gameplaySceneName;
                case SceneType.Leaderboard: return leaderboardSceneName;
                case SceneType.Options: return optionsSceneName;
                case SceneType.Shop: return shopSceneName;
                default: return mainMenuSceneName;
            }
        }

        private GameState GetGameStateFromScene(string sceneName)
        {
            if (sceneName == titleSceneName) return GameState.Title;
            if (sceneName == mainMenuSceneName) return GameState.MainMenu;
            if (sceneName == stageSelectSceneName) return GameState.StageSelect;
            if (sceneName == gameplaySceneName) return GameState.Gameplay;
            if (sceneName == leaderboardSceneName) return GameState.Leaderboard;
            if (sceneName == optionsSceneName) return GameState.Options;
            if (sceneName == shopSceneName) return GameState.Shop;

            return GameState.MainMenu;
        }

        private void OnDestroy()
        {
            Debug.Log("SceneTransitionManager: OnDestroy called");

            // アプリケーション終了中でない場合のみクリーンアップ
            if (!isApplicationQuitting)
            {
                // イベントとリスナーのクリーンアップ
                SceneManager.sceneLoaded -= OnSceneLoaded;

                // 進行中のコルーチンを停止
                StopAllCoroutines();

                Debug.Log("SceneTransitionManager: Cleanup completed (not during app quit)");
            }
        }

        private void OnApplicationQuit()
        {
            Debug.Log("SceneTransitionManager: OnApplicationQuit called");
            isApplicationQuitting = true;

            // アプリケーション終了時のクリーンアップ
            SceneManager.sceneLoaded -= OnSceneLoaded;
            StopAllCoroutines();

            // 静的イベントのクリーンアップ
            OnSceneLoadStart = null;
            OnSceneLoadComplete = null;

            // インスタンス参照のクリア
            _instance = null;

            Debug.Log("SceneTransitionManager: Application quit cleanup completed");
        }

#if UNITY_EDITOR
        // エディター専用：プレイモード終了時のクリーンアップ
        private void OnApplicationFocus(bool hasFocus)
        {
            // エディターでプレイモードが終了する際の処理
            if (!hasFocus && !Application.isPlaying)
            {
                isApplicationQuitting = true;
            }
        }
#endif

        /// <summary>
        /// 手動でのクリーンアップメソッド（必要に応じて外部から呼び出し）
        /// </summary>
        public static void ManualCleanup()
        {
            Debug.Log("SceneTransitionManager: Manual cleanup requested");

            if (_instance != null)
            {
                _instance.isApplicationQuitting = true;

                // イベントのクリーンアップ
                SceneManager.sceneLoaded -= _instance.OnSceneLoaded;
                OnSceneLoadStart = null;
                OnSceneLoadComplete = null;

                // インスタンスの破棄
                if (_instance.gameObject != null)
                {
                    DestroyImmediate(_instance.gameObject);
                }

                _instance = null;
            }
        }
    }
}