using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GravityFlipLab.Player;

namespace GravityFlipLab.Stage
{
    /// <summary>
    /// アセット設定の自動化ヘルパークラス
    /// Context Menuから各種アセットを自動作成できます
    /// </summary>
    public class AssetSetupHelper : MonoBehaviour
    {
        [Header("Asset References")]
        public TilemapGroundManager groundManager;

        [Header("Created Assets (Auto-filled)")]
        public TileBase[] groundTileVariants;
        public PhysicsMaterial2D groundPhysicsMaterial;
        public Tilemap foregroundTilemap;
        public TilemapRenderer tilemapRenderer;
        public TilemapCollider2D tilemapCollider;
        public CompositeCollider2D compositeCollider;

        #region 1. TileBase Assets (tileVariants, groundTiles)

        /*
        === TileBase (タイルアセット) ===
        
        🎯 目的: 地面・プラットフォーム・壁などの見た目を定義
        
        📝 作成手順:
        1. スプライト画像を用意 (16x16ピクセル推奨)
        2. Unity に import
        3. Sprite Mode を "Multiple" に設定
        4. Sprite Editor でタイルを分割
        5. Tile アセットを作成
        
        🎨 推奨スプライト仕様:
        - サイズ: 16x16 ピクセル
        - フォーマット: PNG (透明度サポート)
        - PPU (Pixels Per Unit): 16
        - Filter Mode: Point (no filter) - ピクセルアート用
        */

        [ContextMenu("1. Create Ground Tile Assets")]
        public void CreateGroundTileAssets()
        {
            Debug.Log("=== Creating Ground Tile Assets ===");
            Debug.Log("📋 Manual steps required:");
            Debug.Log("1. Create sprites for ground tiles (16x16px recommended)");
            Debug.Log("2. Set Sprite Mode to 'Multiple' if using sprite sheet");
            Debug.Log("3. Use Sprite Editor to slice tiles");
            Debug.Log("4. Right-click in Project > Create > 2D > Tiles > Tile");
            Debug.Log("5. Assign sprite to tile asset");
            Debug.Log("6. Drag tile assets to groundTileVariants array");

            // 自動作成の例（スプライトが既にある場合）
            CreateSampleTileAssets();
        }

        private void CreateSampleTileAssets()
        {
            // Note: 実際のプロジェクトでは、アーティストが作成したスプライトを使用
            // ここでは Unity の Default Sprite を使用した例を示します

#if UNITY_EDITOR
            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Resource/Sprites/Knob.png");
            if (sprites.Length > 0)
            {
                var sprite = sprites[1] as Sprite;
                if (sprite != null)
                {
                    CreateTileFromSprite(sprite, "SampleGroundTile");
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void CreateTileFromSprite(Sprite sprite, string tileName)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;

            string path = $"Assets/Resource/Art/Tiles/Ground/{tileName}.asset";
            UnityEditor.AssetDatabase.CreateAsset(tile, path);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created tile asset: {path}");
        }
#endif

        /*
        🎨 地形タイプ別のスプライト例:
        
        Ground (地面):
        - grass_01.png - 基本的な草地
        - grass_02.png - 草地バリエーション
        - dirt_01.png - 土の地面
        - stone_01.png - 石の地面
        
        Platform (プラットフォーム):
        - platform_metal.png - 金属プラットフォーム
        - platform_wood.png - 木製プラットフォーム
        - platform_glass.png - ガラスプラットフォーム
        
        Wall (壁):
        - wall_brick.png - レンガの壁
        - wall_metal.png - 金属の壁
        - wall_concrete.png - コンクリートの壁
        */

        #endregion

        #region 2. PhysicsMaterial2D (physicsMaterial, groundPhysicsMaterial)

        /*
        === PhysicsMaterial2D (物理マテリアル) ===
        
        🎯 目的: 地面の物理特性（摩擦・反発）を定義
        
        📝 作成手順:
        1. Project ウィンドウで右クリック
        2. Create > 2D > Physics Material 2D
        3. 名前を設定 (例: "GroundMaterial")
        4. Friction と Bounciness を調整
        
        ⚙️ 推奨設定値:
        */

        [ContextMenu("2. Create Physics Materials")]
        public void CreatePhysicsMaterials()
        {
            Debug.Log("=== Creating Physics Materials ===");

            // 通常の地面用マテリアル
            CreatePhysicsMaterial("GroundMaterial", 0.4f, 0f);

            // 滑りやすい地面用マテリアル
            CreatePhysicsMaterial("IceMaterial", 0.1f, 0f);

            // 弾性のある地面用マテリアル
            CreatePhysicsMaterial("BouncyMaterial", 0.4f, 0.8f);

            // 粘着性の地面用マテリアル
            CreatePhysicsMaterial("StickyMaterial", 1.0f, 0f);
        }

#if UNITY_EDITOR
        private void CreatePhysicsMaterial(string materialName, float friction, float bounciness)
        {
            var material = new PhysicsMaterial2D(materialName);
            material.friction = friction;
            material.bounciness = bounciness;

            string path = $"Assets/Resource/Materials/Physics2D/{materialName}.physicsMaterial2D";
            UnityEditor.AssetDatabase.CreateAsset(material, path);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created physics material: {path}");
            Debug.Log($"   Friction: {friction}, Bounciness: {bounciness}");
        }
#endif

        /*
        📊 物理マテリアル設定例:
        
        GroundMaterial (通常の地面):
        - Friction: 0.4 (適度な摩擦)
        - Bounciness: 0.0 (反発しない)
        
        IceMaterial (氷の地面):
        - Friction: 0.1 (滑りやすい)
        - Bounciness: 0.0 (反発しない)
        
        BouncyMaterial (弾性地面):
        - Friction: 0.4 (通常の摩擦)
        - Bounciness: 0.8 (高い反発)
        
        StickyMaterial (粘着地面):
        - Friction: 1.0 (高い摩擦)
        - Bounciness: 0.0 (反発しない)
        */

        #endregion

        #region 3. Tilemap Components (自動作成)

        /*
        === Tilemap Components (Tilemapコンポーネント群) ===
        
        🎯 目的: Tilemapシステムの中核コンポーネント
        
        📝 自動作成される順序:
        1. GameObject with Tilemap component
        2. TilemapRenderer component  
        3. TilemapCollider2D component
        4. CompositeCollider2D component
        5. Rigidbody2D component (Static)
        
        ⚙️ これらは TilemapGroundManager が自動で作成・設定します
        */

        [ContextMenu("3. Setup Tilemap Components")]
        public void SetupTilemapComponents()
        {
            Debug.Log("=== Setting up Tilemap Components ===");

            if (groundManager == null)
            {
                Debug.LogError("❌ TilemapGroundManager not assigned!");
                return;
            }

            // TilemapGroundManager が自動でコンポーネントを作成
            // 手動での設定は不要ですが、参考として設定値を表示

            Debug.Log("📋 Tilemap components will be auto-created with these settings:");
            Debug.Log("   Tilemap: Stores tile data");
            Debug.Log("   TilemapRenderer: Renders tiles visually");
            Debug.Log("   TilemapCollider2D: Generates physics colliders");
            Debug.Log("   CompositeCollider2D: Optimizes collider performance");
            Debug.Log("   Rigidbody2D: Required for CompositeCollider2D (Static type)");

            //// 実際の作成は TilemapGroundManager の InitializeTilemapComponents() で行われる
            //groundManager.GetComponent<TilemapGroundManager>()?.gameObject.name = "✅ Auto-setup ready";
        }

        /*
        🔧 Tilemap Component 設定詳細:
        
        Tilemap:
        - Tile データの格納
        - Cell Size: (1, 1, 1) - デフォルト
        - Cell Layout: Rectangle
        
        TilemapRenderer:
        - Sorting Layer: Default
        - Order in Layer: 0 (地面用)
        - Material: Default Sprite Material
        
        TilemapCollider2D:
        - Used By Composite: ✓ ON
        - Used By Effector: OFF (通常)
        
        CompositeCollider2D:
        - Geometry Type: Polygons
        - Generation Type: Synchronous
        - Vertex Distance: 0.005 (デフォルト)
        
        Rigidbody2D:
        - Body Type: Static
        - Material: 作成した PhysicsMaterial2D
        */

        #endregion

        #region 4. Layer & Sorting Setup

        /*
        === Layer & Sorting Setup (レイヤー・ソート設定) ===
        
        🎯 目的: 適切な描画順序と物理レイヤーの設定
        */

        [ContextMenu("4. Setup Layers and Sorting")]
        public void SetupLayersAndSorting()
        {
            Debug.Log("=== Setting up Layers and Sorting ===");

            Debug.Log("📋 Manual setup required in Edit > Project Settings:");
            Debug.Log("🏷️ Physics 2D Layers (Layers & Tags):");
            Debug.Log("   Layer 8: Ground");
            Debug.Log("   Layer 9: Platform");
            Debug.Log("   Layer 10: Player");
            Debug.Log("   Layer 11: Obstacle");
            Debug.Log("   Layer 12: Collectible");

            Debug.Log("🎨 Sorting Layers (Graphics):");
            Debug.Log("   0: Background (-100)");
            Debug.Log("   1: Terrain (0)");
            Debug.Log("   2: Platform (10)");
            Debug.Log("   3: Player (50)");
            Debug.Log("   4: Effects (100)");
            Debug.Log("   5: UI (200)");

            // LayerMask の値を表示
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                Debug.LogWarning("⚠️ 'Ground' layer not found. Please create it manually.");
            }
            else
            {
                Debug.Log($"✅ Ground layer found: {groundLayer}");
            }
        }

        /*
        🏷️ Layer 設定の詳細:
        
        Physics 2D Layers:
        - Ground (8): 地面・プラットフォーム
        - Platform (9): 移動プラットフォーム
        - Player (10): プレイヤーキャラクター
        - Obstacle (11): 障害物
        - Collectible (12): 収集品
        
        Layer Collision Matrix (Edit > Project Settings > Physics 2D):
        - Player ↔ Ground: ✓ (衝突)
        - Player ↔ Platform: ✓ (衝突)
        - Player ↔ Obstacle: ✓ (衝突)
        - Player ↔ Collectible: ✓ (トリガー)
        - Ground ↔ Other: ✗ (衝突しない)
        */

        #endregion

        #region 5. Complete Asset Assignment

        /*
        === Complete Asset Assignment (アセット割り当て完了) ===
        
        🎯 目的: 作成したアセットを適切なコンポーネントに割り当て
        */

        [ContextMenu("5. Assign All Assets")]
        public void AssignAllAssets()
        {
            Debug.Log("=== Assigning All Assets ===");

            if (groundManager == null)
            {
                Debug.LogError("❌ TilemapGroundManager not assigned!");
                return;
            }

            // 1. Physics Material の割り当て
            AssignPhysicsMaterials();

            // 2. Tile Assets の割り当て
            AssignTileAssets();

            // 3. Component References の割り当て
            AssignComponentReferences();

            Debug.Log("✅ Asset assignment completed!");
        }

        private void AssignPhysicsMaterials()
        {
#if UNITY_EDITOR
            // GroundMaterial を検索して割り当て
            string[] guids = UnityEditor.AssetDatabase.FindAssets("GroundMaterial t:PhysicsMaterial2D");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var material = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
                groundManager.groundPhysicsMaterial = material;
                Debug.Log($"✅ Assigned physics material: {material.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ GroundMaterial not found. Create it first.");
            }
#endif
        }

        private void AssignTileAssets()
        {
#if UNITY_EDITOR
            // Ground Tiles を検索して割り当て
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Resource/Art/Tiles/Ground" });
            List<TileBase> groundTiles = new List<TileBase>();

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null)
                {
                    groundTiles.Add(tile);
                }
            }

            if (groundTiles.Count > 0)
            {
                groundManager.groundTiles = groundTiles.ToArray();
                Debug.Log($"✅ Assigned {groundTiles.Count} ground tiles");
            }
            else
            {
                Debug.LogWarning("⚠️ No ground tiles found. Create them first.");
            }
#endif
        }

        private void AssignComponentReferences()
        {
            // TilemapGroundManager が自動作成したコンポーネントを取得
            if (groundManager.foregroundTilemap != null)
            {
                foregroundTilemap = groundManager.foregroundTilemap;
                tilemapRenderer = foregroundTilemap.GetComponent<TilemapRenderer>();
                tilemapCollider = foregroundTilemap.GetComponent<TilemapCollider2D>();
                compositeCollider = foregroundTilemap.GetComponent<CompositeCollider2D>();

                Debug.Log("✅ Component references assigned");
            }
            else
            {
                Debug.LogWarning("⚠️ Tilemap components not found. Initialize TilemapGroundManager first.");
            }
        }

        #endregion

        #region 6. Asset Validation

        /*
        === Asset Validation (アセット検証) ===
        
        🎯 目的: 全てのアセットが正しく設定されているかチェック
        */

        [ContextMenu("6. Validate All Assets")]
        public void ValidateAllAssets()
        {
            Debug.Log("=== Validating All Assets ===");

            bool allValid = true;

            // TilemapGroundManager の確認
            if (groundManager == null)
            {
                Debug.LogError("❌ TilemapGroundManager not assigned");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ TilemapGroundManager assigned");

                // Ground Tiles の確認
                if (groundManager.groundTiles == null || groundManager.groundTiles.Length == 0)
                {
                    Debug.LogWarning("⚠️ Ground tiles not assigned");
                }
                else
                {
                    Debug.Log($"✅ {groundManager.groundTiles.Length} ground tiles assigned");
                }

                // Physics Material の確認
                if (groundManager.groundPhysicsMaterial == null)
                {
                    Debug.LogWarning("⚠️ Ground physics material not assigned");
                }
                else
                {
                    Debug.Log($"✅ Physics material assigned: {groundManager.groundPhysicsMaterial.name}");
                }

                // Tilemap Components の確認
                if (groundManager.foregroundTilemap == null)
                {
                    Debug.LogWarning("⚠️ Foreground tilemap not initialized");
                }
                else
                {
                    Debug.Log("✅ Tilemap components initialized");
                }
            }

            if (allValid)
            {
                Debug.Log("<color=green>🎉 All assets are properly configured!</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>⚠️ Some assets need attention. Check the warnings above.</color>");
            }
        }

        #endregion

        #region 7. Quick Setup for Testing

        /*
        === Quick Setup for Testing (テスト用クイックセットアップ) ===
        
        🎯 目的: アーティストのアセットがない場合のテスト用セットアップ
        */

        [ContextMenu("7. Quick Test Setup (No Art Assets)")]
        public void QuickTestSetup()
        {
            Debug.Log("=== Quick Test Setup ===");
            Debug.Log("Creating temporary assets for testing...");

            // テスト用のタイルアセットを作成
            CreateTestTileAssets();

            // テスト用の物理マテリアルを作成
            CreateTestPhysicsMaterials();

            // アセットを割り当て
            AssignTestAssets();

            Debug.Log("✅ Test setup completed!");
            Debug.Log("💡 Replace with actual art assets when available");
        }

        private void CreateTestTileAssets()
        {
#if UNITY_EDITOR
            // Unity の Default Sprite を使用してテスト用タイルを作成
            var defaultSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (defaultSprite != null)
            {
                CreateTileFromSprite(defaultSprite, "TestGroundTile");
            }

            // 白いスプライトを作成
            var whiteSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (whiteSprite != null)
            {
                CreateTileFromSprite(whiteSprite, "TestPlatformTile");
            }
#endif
        }

        private void CreateTestPhysicsMaterials()
        {
            CreatePhysicsMaterial("TestGroundMaterial", 0.4f, 0f);
        }

        private void AssignTestAssets()
        {
            if (groundManager != null)
            {
#if UNITY_EDITOR
                // テスト用タイルを検索して割り当て
                string[] guids = UnityEditor.AssetDatabase.FindAssets("TestGroundTile t:TileBase");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    groundManager.groundTiles = new TileBase[] { tile };
                }

                // テスト用物理マテリアルを割り当て
                guids = UnityEditor.AssetDatabase.FindAssets("TestGroundMaterial t:PhysicsMaterial2D");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    var material = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
                    groundManager.groundPhysicsMaterial = material;
                }
#endif
            }
        }

        #endregion
    }
}