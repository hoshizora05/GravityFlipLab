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
    #region Data Structures

    [System.Serializable]
    public class PlayerProgress
    {
        public int currentWorld = 1;
        public int currentStage = 1;
        public Dictionary<string, StageData> stageProgress = new Dictionary<string, StageData>();
        public Dictionary<string, float> bestTimes = new Dictionary<string, float>();
        public Dictionary<string, int> stageRanks = new Dictionary<string, int>(); // 0=未クリア, 1=C, 2=B, 3=A, 4=S
        public int totalEnergyChips = 0;
        public PlayerSettings settings = new PlayerSettings();
    }

    [System.Serializable]
    public class StageData
    {
        public bool isCleared = false;
        public float bestTime = float.MaxValue;
        public int deathCount = 0;
        public int energyChipsCollected = 0;
        public int maxEnergyChips = 3;
        public int rank = 0; // 0=未クリア, 1=C, 2=B, 3=A, 4=S
    }

    [System.Serializable]
    public class PlayerSettings
    {
        public float masterVolume = 1.0f;
        public float bgmVolume = 0.8f;
        public float seVolume = 1.0f;
        public int languageIndex = 0; // 0=日本語, 1=English, etc.
        public bool assistModeEnabled = false;
        public int colorBlindMode = 0; // 0=Normal, 1=Protanopia, 2=Deuteranopia, 3=Tritanopia
        public bool highContrastMode = false;
        public float uiScale = 1.0f;
        public KeyCode primaryInput = KeyCode.Space;
    }

    public enum GameState
    {
        Title,
        MainMenu,
        StageSelect,
        Gameplay,
        Paused,
        GameOver,
        Loading,
        Options,
        Leaderboard,
        Shop
    }

    public enum SceneType
    {
        Title,
        MainMenu,
        StageSelect,
        Gameplay,
        Leaderboard,
        Options,
        Shop
    }

    #endregion

    #region Game Manager (Singleton)

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Game Settings")]
        public bool debugMode = false;
        public float gameSpeed = 1.0f;

        [Header("Current Session")]
        public GameState currentState = GameState.MainMenu;
        public int currentWorld = 1;
        public int currentStage = 1;
        public float sessionStartTime;
        public int sessionDeathCount = 0;
        public int sessionEnergyChips = 0;

        // Events
        public static event System.Action<GameState> OnGameStateChanged;
        public static event System.Action<int, int> OnStageChanged;
        public static event System.Action<float> OnGameSpeedChanged;

        // Components
        public PlayerProgress playerProgress { get; set; }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Load player progress
            playerProgress = SaveManager.Instance.LoadProgress();

            // Initialize managers
            ConfigManager.Instance.Initialize();
            SceneTransitionManager.Instance.Initialize();

            Debug.Log("GameManager initialized successfully");
        }

        public void ChangeGameState(GameState newState)
        {
            if (currentState != newState)
            {
                GameState previousState = currentState;
                currentState = newState;

                OnGameStateChanged?.Invoke(newState);

                if (debugMode)
                    Debug.Log($"Game state changed: {previousState} -> {newState}");
            }
        }

        public void SetCurrentStage(int world, int stage)
        {
            currentWorld = world;
            currentStage = stage;
            OnStageChanged?.Invoke(world, stage);
        }

        public void StartStage(int world, int stage)
        {
            SetCurrentStage(world, stage);
            sessionStartTime = Time.time;
            sessionDeathCount = 0;
            sessionEnergyChips = 0;
            ChangeGameState(GameState.Gameplay);
        }

        public void CompleteStage(float clearTime, int deathCount, int energyChips)
        {
            string stageKey = $"{currentWorld}-{currentStage}";

            // Update stage progress
            if (!playerProgress.stageProgress.ContainsKey(stageKey))
            {
                playerProgress.stageProgress[stageKey] = new StageData();
            }

            StageData stageData = playerProgress.stageProgress[stageKey];
            stageData.isCleared = true;

            // Update best time
            if (clearTime < stageData.bestTime)
            {
                stageData.bestTime = clearTime;
                playerProgress.bestTimes[stageKey] = clearTime;
            }

            stageData.deathCount = deathCount;
            stageData.energyChipsCollected = energyChips;

            // Calculate rank
            stageData.rank = CalculateStageRank(clearTime, deathCount, energyChips);
            playerProgress.stageRanks[stageKey] = stageData.rank;

            // Update total energy chips
            playerProgress.totalEnergyChips += energyChips;

            // Save progress
            SaveManager.Instance.SaveProgress(playerProgress);

            if (debugMode)
                Debug.Log($"Stage {stageKey} completed - Time: {clearTime:F2}s, Deaths: {deathCount}, Chips: {energyChips}, Rank: {GetRankString(stageData.rank)}");
        }

        private int CalculateStageRank(float clearTime, int deathCount, int energyChips)
        {
            // Rank calculation logic based on time, deaths, and energy chips
            int score = 0;

            // Time scoring (example thresholds)
            if (clearTime <= 30f) score += 40;
            else if (clearTime <= 60f) score += 30;
            else if (clearTime <= 90f) score += 20;
            else score += 10;

            // Death penalty
            score -= deathCount * 5;

            // Energy chip bonus
            score += energyChips * 10;

            // Determine rank
            if (score >= 50) return 4; // S
            if (score >= 40) return 3; // A
            if (score >= 25) return 2; // B
            if (score >= 10) return 1; // C
            return 0; // No rank
        }

        private string GetRankString(int rank)
        {
            switch (rank)
            {
                case 4: return "S";
                case 3: return "A";
                case 2: return "B";
                case 1: return "C";
                default: return "None";
            }
        }

        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0.1f, 2.0f);
            Time.timeScale = gameSpeed;
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }

        public void PauseGame()
        {
            if (currentState == GameState.Gameplay)
            {
                ChangeGameState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeGameState(GameState.Gameplay);
                Time.timeScale = gameSpeed;
            }
        }

        public void RestartStage()
        {
            SceneTransitionManager.Instance.ReloadCurrentScene();
        }

        public void ReturnToStageSelect()
        {
            ChangeGameState(GameState.StageSelect);
            SceneTransitionManager.Instance.LoadScene(SceneType.StageSelect);
        }

        public void ReturnToMainMenu()
        {
            ChangeGameState(GameState.MainMenu);
            SceneTransitionManager.Instance.LoadScene(SceneType.MainMenu);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            //if (pauseStatus && currentState == GameState.Gameplay)
            //{
            //    PauseGame();
            //}
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            //if (!hasFocus && currentState == GameState.Gameplay)
            //{
            //    PauseGame();
            //}
        }
    }

    #endregion
}