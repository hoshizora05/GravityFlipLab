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
    public class ConfigManager : MonoBehaviour
    {
        private static ConfigManager _instance;
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ConfigManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ConfigManager");
                        _instance = go.AddComponent<ConfigManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Game Configuration")]
        public float defaultGameSpeed = 1.0f;
        public float assistModeSpeedMultiplier = 0.8f;
        public int maxWorlds = 5;
        public int stagesPerWorld = 10;

        [Header("Physics")]
        public float baseGravity = -9.81f;
        public float gravityFlipDuration = 0.1f;
        public float playerSpeed = 5.0f;
        public float jumpForce = 8.0f;

        [Header("Gameplay")]
        public float invincibilityDuration = 0.1f;
        public int maxEnergyChipsPerStage = 3;
        public int assistModeBarrierCount = 3;

        [Header("Audio")]
        public float defaultMasterVolume = 1.0f;
        public float defaultBGMVolume = 0.8f;
        public float defaultSEVolume = 1.0f;

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
            // Ensure GameManager and PlayerProgress exist before applying settings
            if (GameManager.Instance != null && GameManager.Instance.playerProgress != null)
            {
                // Apply saved settings
                ApplySettings(GameManager.Instance.playerProgress.settings);
            }
            else
            {
                Debug.LogWarning("GameManager or PlayerProgress not available during ConfigManager initialization");
                // Apply default settings
                ApplyDefaultSettings();
            }

            Debug.Log("ConfigManager initialized");
        }

        public void ApplySettings(PlayerSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("PlayerSettings is null, applying default settings");
                ApplyDefaultSettings();
                return;
            }

            // Apply audio settings
            AudioListener.volume = settings.masterVolume;

            // Apply game speed based on assist mode
            if (GameManager.Instance != null)
            {
                float gameSpeed = settings.assistModeEnabled ?
                    defaultGameSpeed * assistModeSpeedMultiplier : defaultGameSpeed;
                GameManager.Instance.SetGameSpeed(gameSpeed);
            }

            // Apply other settings as needed
            QualitySettings.SetQualityLevel(0); // Ensure consistent quality
        }

        public void ApplyDefaultSettings()
        {
            // Apply default audio settings
            AudioListener.volume = defaultMasterVolume;

            // Apply default game speed
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameSpeed(defaultGameSpeed);
            }

            // Apply default quality settings
            QualitySettings.SetQualityLevel(0);
        }

        public bool IsStageUnlocked(int world, int stage)
        {
            if (world == 1 && stage == 1) return true; // First stage is always unlocked

            // Ensure GameManager and PlayerProgress exist
            if (GameManager.Instance?.playerProgress == null)
            {
                Debug.LogWarning("GameManager or PlayerProgress not available for stage unlock check");
                return false;
            }

            // Check if previous stage is cleared
            if (stage > 1)
            {
                string prevStageKey = $"{world}-{stage - 1}";
                return GameManager.Instance.playerProgress.stageProgress.ContainsKey(prevStageKey) &&
                       GameManager.Instance.playerProgress.stageProgress[prevStageKey].isCleared;
            }

            // Check if last stage of previous world is cleared
            if (world > 1)
            {
                string lastStageKey = $"{world - 1}-{stagesPerWorld}";
                return GameManager.Instance.playerProgress.stageProgress.ContainsKey(lastStageKey) &&
                       GameManager.Instance.playerProgress.stageProgress[lastStageKey].isCleared;
            }

            return false;
        }

        public string GetStageKey(int world, int stage)
        {
            return $"{world}-{stage}";
        }

        public void ValidateWorldStage(ref int world, ref int stage)
        {
            world = Mathf.Clamp(world, 1, maxWorlds);
            stage = Mathf.Clamp(stage, 1, stagesPerWorld);
        }

        // Additional utility methods for the menu system
        public int GetTotalUnlockedStages()
        {
            if (GameManager.Instance?.playerProgress == null) return 1;

            int unlockedCount = 0;
            for (int world = 1; world <= maxWorlds; world++)
            {
                for (int stage = 1; stage <= stagesPerWorld; stage++)
                {
                    if (IsStageUnlocked(world, stage))
                    {
                        unlockedCount++;
                    }
                }
            }
            return unlockedCount;
        }

        public int GetTotalClearedStages()
        {
            if (GameManager.Instance?.playerProgress == null) return 0;

            int clearedCount = 0;
            foreach (var stageData in GameManager.Instance.playerProgress.stageProgress.Values)
            {
                if (stageData.isCleared)
                {
                    clearedCount++;
                }
            }
            return clearedCount;
        }

        public float GetCompletionPercentage()
        {
            int totalStages = maxWorlds * stagesPerWorld;
            int clearedStages = GetTotalClearedStages();
            return (float)clearedStages / totalStages * 100f;
        }

        // Settings validation and default value methods
        public PlayerSettings GetValidatedSettings(PlayerSettings settings)
        {
            if (settings == null)
            {
                return CreateDefaultSettings();
            }

            // Validate and clamp values
            settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
            settings.bgmVolume = Mathf.Clamp01(settings.bgmVolume);
            settings.seVolume = Mathf.Clamp01(settings.seVolume);
            settings.languageIndex = Mathf.Clamp(settings.languageIndex, 0, 11); // 0-11 for 12 languages
            settings.colorBlindMode = Mathf.Clamp(settings.colorBlindMode, 0, 3);
            settings.uiScale = Mathf.Clamp(settings.uiScale, 0.5f, 2.0f);

            return settings;
        }

        public PlayerSettings CreateDefaultSettings()
        {
            return new PlayerSettings
            {
                masterVolume = defaultMasterVolume,
                bgmVolume = defaultBGMVolume,
                seVolume = defaultSEVolume,
                languageIndex = 0,
                assistModeEnabled = false,
                colorBlindMode = 0,
                highContrastMode = false,
                uiScale = 1.0f,
                primaryInput = KeyCode.Space
            };
        }

        // Debug and development helper methods
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void UnlockAllStages()
        {
            if (GameManager.Instance?.playerProgress == null) return;

            for (int world = 1; world <= maxWorlds; world++)
            {
                for (int stage = 1; stage <= stagesPerWorld; stage++)
                {
                    string stageKey = GetStageKey(world, stage);
                    if (!GameManager.Instance.playerProgress.stageProgress.ContainsKey(stageKey))
                    {
                        GameManager.Instance.playerProgress.stageProgress[stageKey] = new StageData();
                    }
                    GameManager.Instance.playerProgress.stageProgress[stageKey].isCleared = true;
                }
            }

            Debug.Log("All stages unlocked (Debug mode)");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ResetAllProgress()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerProgress = SaveManager.Instance.CreateNewProgress();
                Debug.Log("All progress reset (Debug mode)");
            }
        }
    }
}