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
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SaveManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveManager");
                        _instance = go.AddComponent<SaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const string SAVE_FILE_NAME = "gravity_flip_save.dat";
        private const string ENCRYPTION_KEY = "GravityFlipLab2024Key";

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

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

        public PlayerProgress LoadProgress()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string encryptedData = File.ReadAllText(SaveFilePath);
                    string jsonData = DecryptString(encryptedData, ENCRYPTION_KEY);
                    PlayerProgress progress = JsonUtility.FromJson<PlayerProgress>(jsonData);

                    // Validate data integrity
                    if (ValidateProgressData(progress))
                    {
                        Debug.Log("Player progress loaded successfully");
                        return progress;
                    }
                    else
                    {
                        Debug.LogWarning("Save data validation failed, creating new progress");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load progress: {e.Message}");
            }

            // Return new progress if loading failed
            return CreateNewProgress();
        }

        public void SaveProgress(PlayerProgress progress)
        {
            try
            {
                string jsonData = JsonUtility.ToJson(progress, true);
                string encryptedData = EncryptString(jsonData, ENCRYPTION_KEY);

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SaveFilePath, encryptedData);
                Debug.Log("Player progress saved successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save progress: {e.Message}");
            }
        }

        public PlayerProgress CreateNewProgress()
        {
            PlayerProgress newProgress = new PlayerProgress();
            newProgress.currentWorld = 1;
            newProgress.currentStage = 1;
            newProgress.totalEnergyChips = 0;
            newProgress.settings = new PlayerSettings();

            Debug.Log("Created new player progress");
            return newProgress;
        }

        private bool ValidateProgressData(PlayerProgress progress)
        {
            if (progress == null) return false;
            if (progress.currentWorld < 1 || progress.currentWorld > 5) return false;
            if (progress.currentStage < 1 || progress.currentStage > 10) return false;
            if (progress.totalEnergyChips < 0) return false;
            if (progress.settings == null) return false;

            return true;
        }

        private string EncryptString(string plainText, string key)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            using (Aes aes = Aes.Create())
            {
                aes.Key = ResizeKey(keyBytes, 32); // AES-256
                aes.IV = new byte[16]; // Zero IV for simplicity

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        private string DecryptString(string cipherText, string key)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            using (Aes aes = Aes.Create())
            {
                aes.Key = ResizeKey(keyBytes, 32); // AES-256
                aes.IV = new byte[16]; // Zero IV for simplicity

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private byte[] ResizeKey(byte[] key, int size)
        {
            byte[] resizedKey = new byte[size];
            for (int i = 0; i < size; i++)
            {
                resizedKey[i] = key[i % key.Length];
            }
            return resizedKey;
        }

        public void DeleteSaveData()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("Save data deleted successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save data: {e.Message}");
            }
        }

        public bool HasSaveData()
        {
            return File.Exists(SaveFilePath);
        }
    }
}