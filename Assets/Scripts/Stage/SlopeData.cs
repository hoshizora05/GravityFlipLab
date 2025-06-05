using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 傾斜の種類定義
    /// </summary>
    public enum SlopeType
    {
        BasicSlope,         // 基本的な傾斜
        SteepSlope,         // 急な傾斜
        GentleSlope,        // 緩やかな傾斜
        SpringSlope,        // バネ傾斜（加速効果あり）
        IceSlope,           // 氷の傾斜（滑りやすい）
        RoughSlope,         // 荒い傾斜（減速効果あり）
        GravitySlope,       // 重力影響傾斜
        WindSlope          // 風の傾斜
    }

    /// <summary>
    /// 傾斜データ構造
    /// ステージデータで使用される傾斜の配置情報
    /// </summary>
    [System.Serializable]
    public class SlopeData
    {
        [Header("Basic Settings")]
        public SlopeType type = SlopeType.BasicSlope;
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;

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

        // デフォルトコンストラクタ
        public SlopeData()
        {
            type = SlopeType.BasicSlope;
            position = Vector3.zero;
            rotation = Vector3.zero;
            scale = Vector3.one;
            slopeAngle = 30f;
            slopeDirection = SlopeDirection.Ascending;
            slopeLength = 5f;
            speedMultiplier = 1.2f;
            affectGravity = true;
            gravityRedirection = 0.5f;
            parameters = new Dictionary<string, object>();
        }

        // 位置とタイプ指定コンストラクタ
        public SlopeData(SlopeType slopeType, Vector3 pos, float angle = 30f, SlopeDirection direction = SlopeDirection.Ascending)
        {
            type = slopeType;
            position = pos;
            rotation = Vector3.zero;
            scale = Vector3.one;
            slopeAngle = angle;
            slopeDirection = direction;
            slopeLength = 5f;
            parameters = new Dictionary<string, object>();

            // タイプに応じたデフォルト設定
            ApplyTypeDefaults();
        }

        /// <summary>
        /// 傾斜タイプに応じたデフォルト設定を適用
        /// </summary>
        private void ApplyTypeDefaults()
        {
            switch (type)
            {
                case SlopeType.BasicSlope:
                    speedMultiplier = 1.2f;
                    affectGravity = true;
                    gravityRedirection = 0.5f;
                    break;

                case SlopeType.SteepSlope:
                    speedMultiplier = 1.5f;
                    affectGravity = true;
                    gravityRedirection = 0.8f;
                    slopeAngle = 45f;
                    break;

                case SlopeType.GentleSlope:
                    speedMultiplier = 1.1f;
                    affectGravity = true;
                    gravityRedirection = 0.3f;
                    slopeAngle = 15f;
                    break;

                case SlopeType.SpringSlope:
                    speedMultiplier = 2.0f;
                    affectGravity = true;
                    gravityRedirection = 0.7f;
                    parameters["bounceForce"] = 15f;
                    break;

                case SlopeType.IceSlope:
                    speedMultiplier = 1.8f;
                    affectGravity = true;
                    gravityRedirection = 0.9f;
                    parameters["friction"] = 0.1f;
                    break;

                case SlopeType.RoughSlope:
                    speedMultiplier = 0.8f;
                    affectGravity = true;
                    gravityRedirection = 0.2f;
                    parameters["friction"] = 2.0f;
                    break;

                case SlopeType.GravitySlope:
                    speedMultiplier = 1.3f;
                    affectGravity = true;
                    gravityRedirection = 1.5f;
                    parameters["gravityMultiplier"] = 2.0f;
                    break;

                case SlopeType.WindSlope:
                    speedMultiplier = 1.4f;
                    affectGravity = false;
                    gravityRedirection = 0f;
                    parameters["windForce"] = 10f;
                    parameters["windDirection"] = Vector2.right;
                    break;
            }
        }

        /// <summary>
        /// パラメータを安全に取得
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (parameters != null && parameters.ContainsKey(key))
            {
                try
                {
                    return (T)parameters[key];
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// パラメータを設定
        /// </summary>
        public void SetParameter(string key, object value)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            parameters[key] = value;
        }

        /// <summary>
        /// 傾斜データの検証
        /// </summary>
        public bool IsValid()
        {
            if (slopeAngle <= 0f || slopeAngle > 60f)
                return false;

            if (slopeLength <= 0f)
                return false;

            if (speedMultiplier <= 0f)
                return false;

            return true;
        }

        /// <summary>
        /// 検証エラーのリストを取得
        /// </summary>
        public List<string> GetValidationErrors()
        {
            List<string> errors = new List<string>();

            if (slopeAngle <= 0f || slopeAngle > 60f)
                errors.Add($"Invalid slope angle: {slopeAngle}° (must be between 0-60°)");

            if (slopeLength <= 0f)
                errors.Add($"Invalid slope length: {slopeLength} (must be > 0)");

            if (speedMultiplier <= 0f)
                errors.Add($"Invalid speed multiplier: {speedMultiplier} (must be > 0)");

            return errors;
        }
    }

    /// <summary>
    /// 傾斜作成用のヘルパークラス
    /// </summary>
    public static class SlopeDataHelper
    {
        public static SlopeData CreateBasicSlope(Vector3 position, float angle = 30f, SlopeDirection direction = SlopeDirection.Ascending)
        {
            return new SlopeData(SlopeType.BasicSlope, position, angle, direction);
        }

        public static SlopeData CreateSteepSlope(Vector3 position, SlopeDirection direction = SlopeDirection.Ascending)
        {
            return new SlopeData(SlopeType.SteepSlope, position, 45f, direction);
        }

        public static SlopeData CreateGentleSlope(Vector3 position, SlopeDirection direction = SlopeDirection.Ascending)
        {
            return new SlopeData(SlopeType.GentleSlope, position, 15f, direction);
        }

        public static SlopeData CreateSpringSlope(Vector3 position, float bounceForce = 15f)
        {
            var slope = new SlopeData(SlopeType.SpringSlope, position, 30f, SlopeDirection.Ascending);
            slope.SetParameter("bounceForce", bounceForce);
            return slope;
        }

        public static SlopeData CreateIceSlope(Vector3 position, float angle = 30f, SlopeDirection direction = SlopeDirection.Ascending)
        {
            var slope = new SlopeData(SlopeType.IceSlope, position, angle, direction);
            slope.SetParameter("friction", 0.1f);
            return slope;
        }

        public static SlopeData CreateRoughSlope(Vector3 position, float angle = 30f, SlopeDirection direction = SlopeDirection.Ascending)
        {
            var slope = new SlopeData(SlopeType.RoughSlope, position, angle, direction);
            slope.SetParameter("friction", 2.0f);
            return slope;
        }

        public static SlopeData CreateGravitySlope(Vector3 position, float gravityMultiplier = 2.0f)
        {
            var slope = new SlopeData(SlopeType.GravitySlope, position, 35f, SlopeDirection.Ascending);
            slope.SetParameter("gravityMultiplier", gravityMultiplier);
            return slope;
        }

        public static SlopeData CreateWindSlope(Vector3 position, Vector2 windDirection, float windForce = 10f)
        {
            var slope = new SlopeData(SlopeType.WindSlope, position, 20f, SlopeDirection.Ascending);
            slope.SetParameter("windDirection", windDirection);
            slope.SetParameter("windForce", windForce);
            return slope;
        }

        /// <summary>
        /// 複数の傾斜を連続して作成
        /// </summary>
        public static List<SlopeData> CreateSlopeChain(Vector3 startPosition, SlopeType type, int count, float spacing = 6f)
        {
            List<SlopeData> slopes = new List<SlopeData>();

            for (int i = 0; i < count; i++)
            {
                Vector3 position = startPosition + Vector3.right * (spacing * i);
                slopes.Add(new SlopeData(type, position));
            }

            return slopes;
        }

        /// <summary>
        /// 山型の傾斜セット（上り→下り）を作成
        /// </summary>
        public static List<SlopeData> CreateHillSlopes(Vector3 startPosition, float spacing = 6f)
        {
            List<SlopeData> slopes = new List<SlopeData>();

            // 上り坂
            slopes.Add(new SlopeData(SlopeType.BasicSlope, startPosition, 30f, SlopeDirection.Ascending));

            // 下り坂
            Vector3 downPosition = startPosition + Vector3.right * spacing;
            slopes.Add(new SlopeData(SlopeType.BasicSlope, downPosition, 30f, SlopeDirection.Descending));

            return slopes;
        }

        /// <summary>
        /// 階段状の傾斜セットを作成
        /// </summary>
        public static List<SlopeData> CreateStairSlopes(Vector3 startPosition, int stepCount = 3, float stepSpacing = 4f)
        {
            List<SlopeData> slopes = new List<SlopeData>();

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 position = startPosition + new Vector3(stepSpacing * i, i * 1.5f, 0);
                var slope = new SlopeData(SlopeType.BasicSlope, position, 25f, SlopeDirection.Ascending);
                slope.slopeLength = 3f;
                slopes.Add(slope);
            }

            return slopes;
        }
    }

    /// <summary>
    /// 傾斜データの検証クラス
    /// </summary>
    public static class SlopeDataValidator
    {
        public static bool ValidateSlopeDataList(List<SlopeData> slopes)
        {
            if (slopes == null) return true; // null は有効とする

            bool allValid = true;
            List<string> allErrors = new List<string>();

            for (int i = 0; i < slopes.Count; i++)
            {
                var slope = slopes[i];
                if (slope == null)
                {
                    allErrors.Add($"Slope {i} is null");
                    allValid = false;
                    continue;
                }

                if (!slope.IsValid())
                {
                    var errors = slope.GetValidationErrors();
                    foreach (var error in errors)
                    {
                        allErrors.Add($"Slope {i}: {error}");
                    }
                    allValid = false;
                }
            }

            if (!allValid)
            {
                Debug.LogError("Slope data validation failed:\n" + string.Join("\n", allErrors));
            }
            else if (slopes.Count > 0)
            {
                Debug.Log($"Slope data validation passed for {slopes.Count} slopes");
            }

            return allValid;
        }

        public static void LogSlopeInfo(List<SlopeData> slopes)
        {
            if (slopes == null || slopes.Count == 0)
            {
                Debug.Log("No slope data found");
                return;
            }

            Debug.Log("=== Slope Data Info ===");
            Debug.Log($"Total slopes: {slopes.Count}");

            // タイプ別統計
            var typeCount = new Dictionary<SlopeType, int>();
            foreach (var slope in slopes)
            {
                if (slope != null)
                {
                    if (!typeCount.ContainsKey(slope.type))
                        typeCount[slope.type] = 0;
                    typeCount[slope.type]++;
                }
            }

            foreach (var kvp in typeCount)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} slopes");
            }
        }
    }
}