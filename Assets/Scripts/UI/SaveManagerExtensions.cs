using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GravityFlipLab.UI
{
    public static class SaveManagerExtensions
    {
        public static bool HasSaveData(this SaveManager saveManager)
        {
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "gravity_flip_save.dat");
            return System.IO.File.Exists(savePath);
        }

        public static PlayerProgress CreateNewProgress(this SaveManager saveManager)
        {
            return new PlayerProgress
            {
                currentWorld = 1,
                currentStage = 1,
                totalEnergyChips = 0,
                stageProgress = new Dictionary<string, StageData>(),
                bestTimes = new Dictionary<string, float>(),
                stageRanks = new Dictionary<string, int>(),
                settings = new PlayerSettings()
            };
        }
    }
}