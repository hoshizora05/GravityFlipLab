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
            Debug.Log($"Scene loaded: {scene.name}");

            // Update game state based on scene
            GameState newState = GetGameStateFromScene(scene.name);
            GameManager.Instance.ChangeGameState(newState);
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

}