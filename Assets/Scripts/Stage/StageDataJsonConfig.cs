using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// StageDataSOの設定をJsonで管理するためのデータクラス
    /// </summary>
    [System.Serializable]
    public class StageDataJsonConfig
    {
        [Header("Stage Information")]
        public StageInfoJson stageInfo;

        [Header("Obstacles")]
        public List<ObstacleDataJson> obstacles = new List<ObstacleDataJson>();

        [Header("Collectibles")]
        public List<CollectibleDataJson> collectibles = new List<CollectibleDataJson>();

        [Header("Environmental")]
        public List<EnvironmentalDataJson> environmental = new List<EnvironmentalDataJson>();

        [Header("Tilemap Settings")]
        public Vector2IntJson tileMapSize = new Vector2IntJson(256, 64);
        public float tileSize = 16f;
        public bool useCompositeCollider = true;

        [Header("Slopes")]
        public List<SlopeDataJson> slopes = new List<SlopeDataJson>();

        // デフォルトコンストラクタ
        public StageDataJsonConfig()
        {
            stageInfo = new StageInfoJson();
            obstacles = new List<ObstacleDataJson>();
            collectibles = new List<CollectibleDataJson>();
            environmental = new List<EnvironmentalDataJson>();
            tileMapSize = new Vector2IntJson(256, 64);
            tileSize = 16f;
            useCompositeCollider = true;
            slopes = new List<SlopeDataJson>();
        }
    }

    #region Json対応データ構造

    [System.Serializable]
    public class StageInfoJson
    {
        public int worldNumber;
        public int stageNumber;
        public string stageName;
        public float timeLimit = 300f;
        public int energyChipCount = 3;
        public Vector3Json playerStartPosition = new Vector3Json();
        public Vector3Json goalPosition = new Vector3Json(50f, 0f, 0f);
        public List<Vector3Json> checkpointPositions = new List<Vector3Json>();
        public StageTheme theme = StageTheme.Tech;
        public float stageLength = 4096f;
        public float stageHeight = 1024f;
        public int segmentCount = 16;

        public StageInfoJson()
        {
            worldNumber = 1;
            stageNumber = 1;
            stageName = "New Stage";
            timeLimit = 300f;
            energyChipCount = 3;
            playerStartPosition = new Vector3Json();
            goalPosition = new Vector3Json(50f, 0f, 0f);
            checkpointPositions = new List<Vector3Json>();
            theme = StageTheme.Tech;
            stageLength = 4096f;
            stageHeight = 1024f;
            segmentCount = 16;
        }
    }

    [System.Serializable]
    public class ObstacleDataJson
    {
        public ObstacleType type;
        public Vector3Json position = new Vector3Json();
        public Vector3Json rotation = new Vector3Json();
        public Vector3Json scale = new Vector3Json(1f, 1f, 1f);
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        public ObstacleDataJson()
        {
            type = ObstacleType.Spike;
            position = new Vector3Json();
            rotation = new Vector3Json();
            scale = new Vector3Json(1f, 1f, 1f);
            parameters = new Dictionary<string, object>();
        }
    }

    [System.Serializable]
    public class CollectibleDataJson
    {
        public CollectibleType type;
        public Vector3Json position = new Vector3Json();
        public int value = 1;

        public CollectibleDataJson()
        {
            type = CollectibleType.EnergyChip;
            position = new Vector3Json();
            value = 1;
        }
    }

    [System.Serializable]
    public class EnvironmentalDataJson
    {
        public EnvironmentalType type;
        public Vector3Json position = new Vector3Json();
        public Vector3Json scale = new Vector3Json(1f, 1f, 1f);
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        public EnvironmentalDataJson()
        {
            type = EnvironmentalType.GravityWell;
            position = new Vector3Json();
            scale = new Vector3Json(1f, 1f, 1f);
            parameters = new Dictionary<string, object>();
        }
    }

    [System.Serializable]
    public class SlopeDataJson
    {
        [Header("Basic Settings")]
        public SlopeType type = SlopeType.BasicSlope;
        public Vector3Json position = new Vector3Json();
        public Vector3Json rotation = new Vector3Json();
        public Vector3Json scale = new Vector3Json(1f, 1f, 1f);

        [Header("Slope Configuration")]
        public float slopeAngle = 30f;
        public SlopeDirection slopeDirection = SlopeDirection.Ascending;
        public float slopeLength = 5f;

        [Header("Physics Effects")]
        public float speedMultiplier = 1.2f;
        public bool affectGravity = true;
        public float gravityRedirection = 0.5f;

        [Header("Special Properties")]
        public Dictionary<string, object> parameters = new Dictionary<string, object>();

        public SlopeDataJson()
        {
            type = SlopeType.BasicSlope;
            position = new Vector3Json();
            rotation = new Vector3Json();
            scale = new Vector3Json(1f, 1f, 1f);
            slopeAngle = 30f;
            slopeDirection = SlopeDirection.Ascending;
            slopeLength = 5f;
            speedMultiplier = 1.2f;
            affectGravity = true;
            gravityRedirection = 0.5f;
            parameters = new Dictionary<string, object>();
        }
    }

    // Json用のVector構造体
    [System.Serializable]
    public struct Vector3Json
    {
        public float x;
        public float y;
        public float z;

        public Vector3Json(float x = 0f, float y = 0f, float z = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static Vector3Json FromVector3(Vector3 vector)
        {
            return new Vector3Json(vector.x, vector.y, vector.z);
        }
    }

    [System.Serializable]
    public struct Vector2IntJson
    {
        public int x;
        public int y;

        public Vector2IntJson(int x = 0, int y = 0)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(x, y);
        }

        public static Vector2IntJson FromVector2Int(Vector2Int vector)
        {
            return new Vector2IntJson(vector.x, vector.y);
        }
    }

    // PhysicsMaterial2Dの参照用
    [System.Serializable]
    public class PhysicsMaterial2DRef
    {
        public string materialName;
        public float friction = 0.4f;
        public float bounciness = 0f;

        public PhysicsMaterial2DRef()
        {
            materialName = "";
            friction = 0.4f;
            bounciness = 0f;
        }
    }

    #endregion
}