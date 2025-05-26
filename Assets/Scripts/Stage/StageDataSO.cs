using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Gravity Flip Lab/Stage Data")]
    public class StageDataSO : ScriptableObject
    {
        [Header("Stage Information")]
        public StageInfo stageInfo;

        [Header("Background Layers")]
        public BackgroundLayerData[] backgroundLayers = new BackgroundLayerData[3];

        [Header("Obstacles")]
        public List<ObstacleData> obstacles = new List<ObstacleData>();

        [Header("Collectibles")]
        public List<CollectibleData> collectibles = new List<CollectibleData>();

        [Header("Environmental")]
        public List<EnvironmentalData> environmental = new List<EnvironmentalData>();
    }

    [System.Serializable]
    public class BackgroundLayerData
    {
        public string layerName;
        public Sprite backgroundSprite;
        public float parallaxFactor = 0.5f; // 0.25f for far, 0.5f for mid, 0.75f for near
        public Vector2 tileSize = new Vector2(512, 512);
        public bool enableVerticalLoop = false;
        public Color tintColor = Color.white;
    }

    [System.Serializable]
    public class ObstacleData
    {
        public ObstacleType type;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class CollectibleData
    {
        public CollectibleType type;
        public Vector3 position;
        public int value = 1;
    }

    [System.Serializable]
    public class EnvironmentalData
    {
        public EnvironmentalType type;
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }
}