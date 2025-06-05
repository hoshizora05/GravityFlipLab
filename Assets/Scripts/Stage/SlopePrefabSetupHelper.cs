using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// 傾斜プレハブの自動セットアップヘルパー
    /// エディターで実行して傾斜プレハブを自動生成
    /// </summary>
    public class SlopePrefabSetupHelper : MonoBehaviour
    {
        [Header("Prefab Generation Settings")]
        public bool createAllSlopeTypes = true;
        public string prefabSavePath = "Assets/Prefabs/Slopes/";

        [Header("Visual Settings")]
        public Material defaultSlopeMaterial;
        public Sprite[] slopeSprites = new Sprite[8]; // 各SlopeType用
        public Color[] slopeColors = new Color[8];

        [Header("Physics Settings")]
        public PhysicsMaterial2D defaultPhysicsMaterial;
        public LayerMask slopeLayer = 1;

        /// <summary>
        /// エディターから実行: 全ての傾斜タイプのプレハブを生成
        /// </summary>
        [ContextMenu("Generate All Slope Prefabs")]
        public void GenerateAllSlopePrefabs()
        {
            if (!createAllSlopeTypes) return;

            System.Array slopeTypes = System.Enum.GetValues(typeof(SlopeType));

            foreach (SlopeType type in slopeTypes)
            {
                CreateSlopePrefab(type);
            }

            Debug.Log($"Generated {slopeTypes.Length} slope prefabs in {prefabSavePath}");
        }

        /// <summary>
        /// 指定された傾斜タイプのプレハブを作成
        /// </summary>
        public GameObject CreateSlopePrefab(SlopeType type)
        {
            // プレハブのルートオブジェクト作成
            GameObject slopePrefab = new GameObject($"Slope_{type}");

            // 基本コンポーネントの追加
            SetupBasicComponents(slopePrefab, type);

            // ビジュアルコンポーネントの設定
            SetupVisualComponents(slopePrefab, type);

            // 物理コンポーネントの設定
            SetupPhysicsComponents(slopePrefab, type);

            // 特殊エフェクトの追加
            SetupSpecialEffects(slopePrefab, type);

            // オーディオコンポーネントの追加
            SetupAudioComponents(slopePrefab, type);

            Debug.Log($"Created slope prefab: {type}");
            return slopePrefab;
        }

        /// <summary>
        /// 基本コンポーネントのセットアップ
        /// </summary>
        private void SetupBasicComponents(GameObject prefab, SlopeType type)
        {
            // レイヤー設定
            prefab.layer = GetLayerFromMask(slopeLayer);

            // タグ設定
            prefab.tag = "Slope";

            // SlopeObjectコンポーネント
            SlopeObject slopeObject = prefab.AddComponent<SlopeObject>();

            // タイプに応じた基本設定
            var settings = GetDefaultSettingsForType(type);
            slopeObject.SetSlopeSettings(settings);
        }

        /// <summary>
        /// ビジュアルコンポーネントのセットアップ
        /// </summary>
        private void SetupVisualComponents(GameObject prefab, SlopeType type)
        {
            // SpriteRenderer
            SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();

            int typeIndex = (int)type;
            if (typeIndex < slopeSprites.Length && slopeSprites[typeIndex] != null)
            {
                renderer.sprite = slopeSprites[typeIndex];
            }
            else
            {
                // デフォルトスプライトを生成
                renderer.sprite = CreateDefaultSlopeSprite(type);
            }

            // 色設定
            if (typeIndex < slopeColors.Length)
            {
                renderer.color = slopeColors[typeIndex];
            }
            else
            {
                renderer.color = GetDefaultColorForType(type);
            }

            // マテリアル設定
            if (defaultSlopeMaterial != null)
            {
                renderer.material = defaultSlopeMaterial;
            }

            // ソートオーダー
            renderer.sortingLayerName = "Foreground";
            renderer.sortingOrder = 10;
        }

        /// <summary>
        /// 物理コンポーネントのセットアップ（PolygonCollider2D使用）
        /// </summary>
        private void SetupPhysicsComponents(GameObject prefab, SlopeType type)
        {
            // 既存のコライダーをクリーンアップ
            CleanupExistingColliders(prefab);

            // 1. トリガーコライダー（検知用）- BoxCollider2D、回転なし
            BoxCollider2D triggerCollider = prefab.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = GetTriggerColliderSizeForType(type);
            triggerCollider.offset = GetTriggerColliderOffsetForType(type);

            // 物理マテリアル
            if (defaultPhysicsMaterial != null)
            {
                triggerCollider.sharedMaterial = defaultPhysicsMaterial;
            }

            // 2. 物理コライダー（衝突用）- PolygonCollider2D、形状で表現
            PolygonCollider2D physicsCollider = prefab.AddComponent<PolygonCollider2D>();
            physicsCollider.isTrigger = false;
            physicsCollider.points = GenerateSlopePolygonPoints(type);

            if (defaultPhysicsMaterial != null)
            {
                physicsCollider.sharedMaterial = defaultPhysicsMaterial;
            }

            Debug.Log($"Setup physics components for {type}: 1 Trigger + 1 Polygon collider");
        }

        /// <summary>
        /// 既存のコライダーをクリーンアップ
        /// </summary>
        private void CleanupExistingColliders(GameObject prefab)
        {
            // 既存のBoxCollider2Dを全て削除
            BoxCollider2D[] existingBoxColliders = prefab.GetComponents<BoxCollider2D>();
            foreach (var collider in existingBoxColliders)
            {
                if (Application.isEditor)
                    DestroyImmediate(collider);
                else
                    Destroy(collider);
            }

            // 既存のPolygonCollider2Dを全て削除
            PolygonCollider2D[] existingPolygonColliders = prefab.GetComponents<PolygonCollider2D>();
            foreach (var collider in existingPolygonColliders)
            {
                if (Application.isEditor)
                    DestroyImmediate(collider);
                else
                    Destroy(collider);
            }
        }

        /// <summary>
        /// 傾斜タイプに応じたPolygonCollider2Dのポイント生成
        /// </summary>
        private Vector2[] GenerateSlopePolygonPoints(SlopeType type)
        {
            var settings = GetDefaultSettingsForType(type);
            float length = settings.slopeLength;
            float angle = settings.slopeAngle * Mathf.Deg2Rad;
            float height = Mathf.Tan(angle) * length;
            float halfLength = length * 0.5f;
            float baseThickness = 0.5f; // 傾斜の厚み

            List<Vector2> points = new List<Vector2>();

            if (settings.slopeDirection == SlopeDirection.Ascending)
            {
                // 上り坂: 左下 → 左上 → 右上 → 右下
                points.Add(new Vector2(-halfLength, -baseThickness));  // 左下
                points.Add(new Vector2(-halfLength, 0f));              // 左上（開始点）
                points.Add(new Vector2(halfLength, height));           // 右上（終了点）
                points.Add(new Vector2(halfLength, height - baseThickness)); // 右下

                // 底面を追加（必要に応じて）
                if (height > baseThickness)
                {
                    points.Add(new Vector2(halfLength - (baseThickness / Mathf.Tan(angle)), -baseThickness));
                }
            }
            else
            {
                // 下り坂: 左上 → 右上 → 右下 → 左下
                points.Add(new Vector2(-halfLength, height));           // 左上（開始点）
                points.Add(new Vector2(halfLength, 0f));               // 右上（終了点）
                points.Add(new Vector2(halfLength, -baseThickness));   // 右下
                points.Add(new Vector2(-halfLength, height - baseThickness)); // 左下

                // 底面を追加（必要に応じて）
                if (height > baseThickness)
                {
                    points.Add(new Vector2(-halfLength + (baseThickness / Mathf.Tan(angle)), -baseThickness));
                }
            }

            return points.ToArray();
        }

        /// <summary>
        /// トリガーコライダーのサイズ取得（タイプ別）
        /// </summary>
        private Vector2 GetTriggerColliderSizeForType(SlopeType type)
        {
            var settings = GetDefaultSettingsForType(type);
            float length = settings.slopeLength;
            float angle = settings.slopeAngle * Mathf.Deg2Rad;
            float height = Mathf.Tan(angle) * length;

            // 傾斜全体をカバーするサイズ（余裕を持たせる）
            return new Vector2(length + 1f, Mathf.Max(2f, height + 1.5f));
        }

        /// <summary>
        /// トリガーコライダーのオフセット取得（タイプ別）
        /// </summary>
        private Vector2 GetTriggerColliderOffsetForType(SlopeType type)
        {
            var settings = GetDefaultSettingsForType(type);
            float angle = settings.slopeAngle * Mathf.Deg2Rad;
            float length = settings.slopeLength;
            float height = Mathf.Tan(angle) * length;

            // 傾斜の中心付近に配置
            float yOffset = settings.slopeDirection == SlopeDirection.Ascending
                ? height * 0.3f
                : height * 0.7f;

            return new Vector2(0f, yOffset);
        }

        /// <summary>
        /// 特殊エフェクトコンポーネントのセットアップ
        /// </summary>
        private void SetupSpecialEffects(GameObject prefab, SlopeType type)
        {
            switch (type)
            {
                case SlopeType.SpringSlope:
                    SetupSpringEffects(prefab);
                    break;

                case SlopeType.IceSlope:
                    SetupIceEffects(prefab);
                    break;

                case SlopeType.RoughSlope:
                    SetupRoughEffects(prefab);
                    break;

                case SlopeType.GravitySlope:
                    SetupGravityEffects(prefab);
                    break;

                case SlopeType.WindSlope:
                    SetupWindEffects(prefab);
                    break;
            }
        }

        /// <summary>
        /// スプリング傾斜のエフェクト設定
        /// </summary>
        private void SetupSpringEffects(GameObject prefab)
        {
            SpringSlopeEffect spring = prefab.AddComponent<SpringSlopeEffect>();
            spring.bounceForce = 15f;
            spring.accelerationMultiplier = 1.5f;

            // パーティクルシステム
            GameObject particleObj = new GameObject("BounceEffect");
            particleObj.transform.SetParent(prefab.transform);
            particleObj.transform.localPosition = Vector3.zero;

            ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = Color.yellow;
            main.startSize = 0.2f;
            main.startLifetime = 1f;
            main.maxParticles = 50;

            var emission = particles.emission;
            emission.enabled = false; // トリガーで発生

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;

            spring.bounceEffect = particles;
        }

        /// <summary>
        /// 氷傾斜のエフェクト設定
        /// </summary>
        private void SetupIceEffects(GameObject prefab)
        {
            IceSlopeEffect ice = prefab.AddComponent<IceSlopeEffect>();
            ice.friction = 0.1f;
            ice.slideAcceleration = 1.5f;

            // 氷の視覚効果
            GameObject iceEffect = new GameObject("IceEffect");
            iceEffect.transform.SetParent(prefab.transform);
            iceEffect.transform.localPosition = Vector3.zero;

            ParticleSystem iceParticles = iceEffect.AddComponent<ParticleSystem>();
            var main = iceParticles.main;
            main.startColor = Color.cyan;
            main.startSize = 0.1f;
            main.startLifetime = 2f;
            main.maxParticles = 30;

            ice.slideEffect = iceParticles;
        }

        /// <summary>
        /// 荒い傾斜のエフェクト設定
        /// </summary>
        private void SetupRoughEffects(GameObject prefab)
        {
            RoughSlopeEffect rough = prefab.AddComponent<RoughSlopeEffect>();
            rough.friction = 2.0f;
            rough.decelerationFactor = 0.8f;

            // 埃のエフェクト
            GameObject dustEffect = new GameObject("DustEffect");
            dustEffect.transform.SetParent(prefab.transform);
            dustEffect.transform.localPosition = Vector3.zero;

            ParticleSystem dustParticles = dustEffect.AddComponent<ParticleSystem>();
            var main = dustParticles.main;
            main.startColor = Color.blue;
            main.startSize = 0.15f;
            main.startLifetime = 1.5f;
            main.maxParticles = 40;

            rough.dustEffect = dustParticles;
        }

        /// <summary>
        /// 重力傾斜のエフェクト設定
        /// </summary>
        private void SetupGravityEffects(GameObject prefab)
        {
            GravitySlopeEffect gravity = prefab.AddComponent<GravitySlopeEffect>();
            gravity.gravityMultiplier = 2.0f;

            // 重力場の視覚効果
            GameObject gravityField = new GameObject("GravityField");
            gravityField.transform.SetParent(prefab.transform);
            gravityField.transform.localPosition = Vector3.zero;

            ParticleSystem gravityParticles = gravityField.AddComponent<ParticleSystem>();
            var main = gravityParticles.main;
            main.startColor = Color.cyan;
            main.startSize = 0.3f;
            main.startLifetime = 3f;
            main.maxParticles = 20;

            gravity.gravityEffect = gravityParticles;
        }

        /// <summary>
        /// 風傾斜のエフェクト設定
        /// </summary>
        private void SetupWindEffects(GameObject prefab)
        {
            WindSlopeEffect wind = prefab.AddComponent<WindSlopeEffect>();
            wind.windDirection = Vector2.right;
            wind.windForce = 10f;

            // 風のラインエフェクト
            GameObject windLines = new GameObject("WindLines");
            windLines.transform.SetParent(prefab.transform);
            windLines.transform.localPosition = Vector3.zero;

            LineRenderer[] lines = new LineRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject lineObj = new GameObject($"WindLine_{i}");
                lineObj.transform.SetParent(windLines.transform);
                lineObj.transform.localPosition = new Vector3(0, (i - 1) * 0.5f, 0);

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.endColor = Color.white;
                line.startWidth = 0.05f;
                line.endWidth = 0.02f;
                line.positionCount = 2;

                lines[i] = line;
            }

            wind.windLines = lines;
        }

        /// <summary>
        /// オーディオコンポーネントのセットアップ
        /// </summary>
        private void SetupAudioComponents(GameObject prefab, SlopeType type)
        {
            AudioSource audioSource = prefab.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D音響
            audioSource.volume = 0.7f;
            audioSource.pitch = 1f;

            // タイプ別の音響設定
            switch (type)
            {
                case SlopeType.SpringSlope:
                    audioSource.pitch = 1.2f;
                    break;
                case SlopeType.IceSlope:
                    audioSource.pitch = 0.8f;
                    audioSource.volume = 0.5f;
                    break;
                case SlopeType.RoughSlope:
                    audioSource.pitch = 0.9f;
                    audioSource.volume = 0.8f;
                    break;
            }
        }

        // ヘルパーメソッド群
        private SlopeSettings GetDefaultSettingsForType(SlopeType type)
        {
            SlopeSettings settings = new SlopeSettings();

            switch (type)
            {
                case SlopeType.BasicSlope:
                    settings.slopeAngle = 30f;
                    settings.speedMultiplier = 1.2f;
                    break;
                case SlopeType.SteepSlope:
                    settings.slopeAngle = 45f;
                    settings.speedMultiplier = 1.5f;
                    break;
                case SlopeType.GentleSlope:
                    settings.slopeAngle = 15f;
                    settings.speedMultiplier = 1.1f;
                    break;
                case SlopeType.SpringSlope:
                    settings.slopeAngle = 30f;
                    settings.speedMultiplier = 2.0f;
                    break;
                case SlopeType.IceSlope:
                    settings.slopeAngle = 30f;
                    settings.speedMultiplier = 1.8f;
                    break;
                case SlopeType.RoughSlope:
                    settings.slopeAngle = 25f;
                    settings.speedMultiplier = 0.8f;
                    break;
                case SlopeType.GravitySlope:
                    settings.slopeAngle = 35f;
                    settings.speedMultiplier = 1.3f;
                    break;
                case SlopeType.WindSlope:
                    settings.slopeAngle = 20f;
                    settings.speedMultiplier = 1.4f;
                    break;
            }

            return settings;
        }

        private Color GetDefaultColorForType(SlopeType type)
        {
            switch (type)
            {
                case SlopeType.BasicSlope: return Color.gray;
                case SlopeType.SteepSlope: return Color.red;
                case SlopeType.GentleSlope: return Color.green;
                case SlopeType.SpringSlope: return Color.yellow;
                case SlopeType.IceSlope: return Color.cyan;
                case SlopeType.RoughSlope: return new Color(0.6f, 0.4f, 0.2f); // 茶色
                case SlopeType.GravitySlope: return Color.magenta;
                case SlopeType.WindSlope: return Color.white;
                default: return Color.gray;
            }
        }

        private Vector2 GetColliderSizeForType(SlopeType type)
        {
            // この メソッドは削除（GetTriggerColliderSizeForTypeに統合）
            return GetTriggerColliderSizeForType(type);
        }

        private bool RequiresPhysicsCollider(SlopeType type)
        {
            // PolygonCollider2D使用により、このメソッドは不要
            // 全ての傾斜タイプで同じ設定を使用
            return false;
        }

        private Sprite CreateDefaultSlopeSprite(SlopeType type)
        {
            // 64x32の傾斜スプライトを生成
            Texture2D texture = new Texture2D(64, 32);
            Color[] pixels = new Color[64 * 32];
            Color slopeColor = GetDefaultColorForType(type);

            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float slopeHeight = GetSlopeHeight(x, type);
                    if (y <= slopeHeight)
                    {
                        pixels[y * 64 + x] = slopeColor;
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0f), 16f);
        }

        private float GetSlopeHeight(int x, SlopeType type)
        {
            float normalizedX = x / 64f;

            switch (type)
            {
                case SlopeType.SteepSlope:
                    return normalizedX * 28f; // 急傾斜
                case SlopeType.GentleSlope:
                    return normalizedX * 12f; // 緩傾斜
                default:
                    return normalizedX * 20f; // 標準傾斜
            }
        }

        private int GetLayerFromMask(LayerMask layerMask)
        {
            int mask = layerMask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    return i;
                }
            }
            return 0;
        }
    }
}