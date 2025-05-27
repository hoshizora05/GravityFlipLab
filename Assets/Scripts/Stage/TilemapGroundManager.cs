using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class TilemapGroundManager : MonoBehaviour
    {
        [Header("Tilemap References")]
        public Tilemap foregroundTilemap;
        public TilemapRenderer tilemapRenderer;
        public TilemapCollider2D tilemapCollider;
        public CompositeCollider2D compositeCollider;

        [Header("Ground Tiles")]
        public TileBase[] groundTiles;
        public TileBase[] platformTiles;
        public TileBase[] wallTiles;
        public TileBase[] ceilingTiles;

        [Header("Tile Settings")]
        public PhysicsMaterial2D groundPhysicsMaterial;
        public LayerMask groundLayer = 1;
        public int tilemapSortingOrder = 100;
        [Tooltip("None: 全てのタイルコライダーを統合 / Intersect: 重複部分のみ / Merge: 隣接タイルを結合")]
        public Collider2D.CompositeOperation compositeOperation = Collider2D.CompositeOperation.None;

        [Header("Generation Settings")]
        public bool autoGenerateGround = true;
        public bool autoGenerateCeiling = true;
        public float groundLevel = -5f;
        public float groundThickness = 2f;
        public float ceilingLevel = 10f;
        public float ceilingThickness = 2f;
        public int stageWidthInTiles = 256; // 4096ピクセル ÷ 16ピクセル/タイル

        [Header("Platform Generation")]
        public Vector2Int[] platformPositions;
        public Vector2Int[] platformSizes;

        [Header("Ceiling Platform Generation")]
        public Vector2Int[] ceilingPlatformPositions;
        public Vector2Int[] ceilingPlatformSizes;

        [Header("Debug")]
        public bool showDebugInfo = false;
        public bool enableRuntimeEdit = false;

        private Dictionary<Vector3Int, TileType> tileTypeMap = new Dictionary<Vector3Int, TileType>();
        private StageDataSO currentStageData;

        public enum TileType
        {
            None,
            Ground,
            Platform,
            Wall,
            Ceiling,
            CeilingPlatform,
            OneWayPlatform
        }

        private void Awake()
        {
            InitializeTilemapComponents();
        }

        private void Start()
        {
            SetupTilemapConfiguration();

            if (autoGenerateGround)
            {
                GenerateBasicGround();
            }

            if (autoGenerateCeiling)
            {
                GenerateBasicCeiling();
            }
        }

        private void InitializeTilemapComponents()
        {
            // Tilemapコンポーネントの取得または作成
            if (foregroundTilemap == null)
            {
                GameObject tilemapObj = new GameObject("ForegroundTilemap");
                tilemapObj.transform.SetParent(transform);

                foregroundTilemap = tilemapObj.AddComponent<Tilemap>();
                tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();
            }

            // TilemapCollider2Dの設定
            if (tilemapCollider == null)
            {
                tilemapCollider = foregroundTilemap.GetComponent<TilemapCollider2D>();
                if (tilemapCollider == null)
                {
                    tilemapCollider = foregroundTilemap.gameObject.AddComponent<TilemapCollider2D>();
                }
            }

            // CompositeCollider2Dの設定（パフォーマンス向上のため）
            if (compositeCollider == null)
            {
                compositeCollider = foregroundTilemap.GetComponent<CompositeCollider2D>();
                if (compositeCollider == null)
                {
                    compositeCollider = foregroundTilemap.gameObject.AddComponent<CompositeCollider2D>();

                    // Rigidbody2Dも必要
                    Rigidbody2D rb = foregroundTilemap.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = foregroundTilemap.gameObject.AddComponent<Rigidbody2D>();
                    }
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }
        }

        private void SetupTilemapConfiguration()
        {
            // レイヤー設定
            foregroundTilemap.gameObject.layer = Mathf.RoundToInt(Mathf.Log(groundLayer.value, 2));

            // ソートオーダー設定
            tilemapRenderer.sortingOrder = tilemapSortingOrder;

            // 物理マテリアル設定
            if (groundPhysicsMaterial != null)
            {
                tilemapCollider.sharedMaterial = groundPhysicsMaterial;
            }

            // CompositeCollider2D設定
            // 地面と天井が離れている場合はNoneを使用（Intersectだと天井コライダーが消失する）
            tilemapCollider.compositeOperation = compositeOperation;
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.generationType = CompositeCollider2D.GenerationType.Synchronous;
        }

        public void LoadGroundFromStageData(StageDataSO stageData)
        {
            currentStageData = stageData;
            ClearAllTiles();

            if (stageData?.stageInfo != null)
            {
                stageWidthInTiles = Mathf.RoundToInt(stageData.stageInfo.stageLength / 16f);
                GenerateGroundFromData(stageData);
            }
        }

        private void GenerateGroundFromData(StageDataSO stageData)
        {
            // ステージデータから地形情報を読み込んで生成
            // 今回は基本的な地面を生成し、将来的にはステージデータから詳細な地形を読み込む
            GenerateBasicGround();
            GenerateBasicCeiling();

            // プラットフォームの生成
            GeneratePlatforms();
            GenerateCeilingPlatforms();

            // 壁の生成
            GenerateWalls();

            Debug.Log($"Ground and ceiling generated for stage: {stageData.stageInfo.stageName}");
        }

        public void GenerateBasicGround()
        {
            if (groundTiles == null || groundTiles.Length == 0)
            {
                Debug.LogWarning("Ground tiles not assigned!");
                return;
            }

            int groundLevelTile = Mathf.RoundToInt(groundLevel);
            int thicknessTiles = Mathf.RoundToInt(groundThickness);

            // 基本的な地面を生成
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = groundLevelTile; y > groundLevelTile - thicknessTiles; y--)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tileToPlace = GetGroundTileVariant(x, y);

                    foregroundTilemap.SetTile(position, tileToPlace);
                    tileTypeMap[position] = TileType.Ground;
                }
            }

            RefreshTilemap();
        }

        public void GenerateBasicCeiling()
        {
            if (ceilingTiles == null || ceilingTiles.Length == 0)
            {
                Debug.LogWarning("Ceiling tiles not assigned!");
                return;
            }

            int ceilingLevelTile = Mathf.RoundToInt(ceilingLevel);
            int thicknessTiles = Mathf.RoundToInt(ceilingThickness);

            // 基本的な天井を生成
            for (int x = 0; x < stageWidthInTiles; x++)
            {
                for (int y = ceilingLevelTile; y < ceilingLevelTile + thicknessTiles; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tileToPlace = GetCeilingTileVariant(x, y);

                    foregroundTilemap.SetTile(position, tileToPlace);
                    tileTypeMap[position] = TileType.Ceiling;
                }
            }

            RefreshTilemap();
        }

        private void GeneratePlatforms()
        {
            if (platformTiles == null || platformTiles.Length == 0) return;

            foreach (var platformPos in platformPositions)
            {
                // デフォルトサイズまたは指定サイズでプラットフォーム生成
                Vector2Int size = new Vector2Int(4, 1); // デフォルト

                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(platformPos.x + x, platformPos.y + y, 0);
                        TileBase tileToPlace = GetPlatformTileVariant(x, y, size);

                        foregroundTilemap.SetTile(position, tileToPlace);
                        tileTypeMap[position] = TileType.Platform;
                    }
                }
            }
        }

        private void GenerateCeilingPlatforms()
        {
            if (ceilingTiles == null || ceilingTiles.Length == 0) return;

            foreach (var ceilingPlatformPos in ceilingPlatformPositions)
            {
                // デフォルトサイズまたは指定サイズで天井プラットフォーム生成
                Vector2Int size = new Vector2Int(4, 1); // デフォルト

                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        Vector3Int position = new Vector3Int(ceilingPlatformPos.x + x, ceilingPlatformPos.y + y, 0);
                        TileBase tileToPlace = GetCeilingPlatformTileVariant(x, y, size);

                        foregroundTilemap.SetTile(position, tileToPlace);
                        tileTypeMap[position] = TileType.CeilingPlatform;
                    }
                }
            }
        }

        private void GenerateWalls()
        {
            if (wallTiles == null || wallTiles.Length == 0) return;

            //int wallHeight = 20; // タイル数
            int groundLevelTile = Mathf.RoundToInt(groundLevel);
            int ceilingLevelTile = Mathf.RoundToInt(ceilingLevel);

            // 壁の高さを地面から天井まで拡張
            int totalWallHeight = ceilingLevelTile - groundLevelTile + Mathf.RoundToInt(ceilingThickness);

            // 左の壁
            for (int y = groundLevelTile; y < groundLevelTile + totalWallHeight; y++)
            {
                Vector3Int position = new Vector3Int(-1, y, 0);
                foregroundTilemap.SetTile(position, wallTiles[0]);
                tileTypeMap[position] = TileType.Wall;
            }

            // 右の壁
            for (int y = groundLevelTile; y < groundLevelTile + totalWallHeight; y++)
            {
                Vector3Int position = new Vector3Int(stageWidthInTiles, y, 0);
                foregroundTilemap.SetTile(position, wallTiles[0]);
                tileTypeMap[position] = TileType.Wall;
            }
        }

        private TileBase GetGroundTileVariant(int x, int y)
        {
            // 地面タイルのバリエーション選択ロジック
            if (groundTiles.Length == 1) return groundTiles[0];

            // 位置に基づいたランダム選択（シード値使用で一貫性保証）
            int seed = x * 1000 + y;
            System.Random random = new System.Random(seed);
            return groundTiles[random.Next(groundTiles.Length)];
        }

        private TileBase GetCeilingTileVariant(int x, int y)
        {
            // 天井タイルのバリエーション選択ロジック
            if (ceilingTiles.Length == 1) return ceilingTiles[0];

            // 位置に基づいたランダム選択（シード値使用で一貫性保証）
            int seed = x * 1000 + y + 10000; // 地面と異なるシード値を使用
            System.Random random = new System.Random(seed);
            return ceilingTiles[random.Next(ceilingTiles.Length)];
        }

        private TileBase GetPlatformTileVariant(int localX, int localY, Vector2Int size)
        {
            if (platformTiles.Length == 1) return platformTiles[0];

            // プラットフォームの位置に応じたタイル選択
            if (localX == 0) return platformTiles[0]; // 左端
            if (localX == size.x - 1) return platformTiles[platformTiles.Length - 1]; // 右端
            return platformTiles[1]; // 中央
        }

        private TileBase GetCeilingPlatformTileVariant(int localX, int localY, Vector2Int size)
        {
            if (ceilingTiles.Length == 1) return ceilingTiles[0];

            // 天井プラットフォームの位置に応じたタイル選択
            if (localX == 0) return ceilingTiles[0]; // 左端
            if (localX == size.x - 1) return ceilingTiles[ceilingTiles.Length - 1]; // 右端
            return ceilingTiles.Length > 1 ? ceilingTiles[1] : ceilingTiles[0]; // 中央
        }

        public void ClearAllTiles()
        {
            foregroundTilemap.SetTilesBlock(foregroundTilemap.cellBounds, new TileBase[foregroundTilemap.cellBounds.size.x * foregroundTilemap.cellBounds.size.y * foregroundTilemap.cellBounds.size.z]);
            tileTypeMap.Clear();
            RefreshTilemap();
        }

        public void RefreshTilemap()
        {
            foregroundTilemap.RefreshAllTiles();
            tilemapCollider.enabled = false;
            tilemapCollider.enabled = true;
        }

        public TileType GetTileTypeAtPosition(Vector3 worldPosition)
        {
            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            return tileTypeMap.ContainsKey(cellPosition) ? tileTypeMap[cellPosition] : TileType.None;
        }

        public bool IsGroundAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType == TileType.Ground || tileType == TileType.Platform;
        }

        public bool IsCeilingAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType == TileType.Ceiling || tileType == TileType.CeilingPlatform;
        }

        public bool IsSolidTileAtPosition(Vector3 worldPosition)
        {
            TileType tileType = GetTileTypeAtPosition(worldPosition);
            return tileType != TileType.None;
        }

        public Vector3 GetTileWorldPosition(Vector3Int cellPosition)
        {
            return foregroundTilemap.CellToWorld(cellPosition);
        }

        // ランタイムでのタイル編集機能（デバッグ用）
        public void SetTileAtWorldPosition(Vector3 worldPosition, TileBase tile, TileType tileType)
        {
            if (!enableRuntimeEdit) return;

            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            foregroundTilemap.SetTile(cellPosition, tile);
            tileTypeMap[cellPosition] = tileType;
        }

        public void RemoveTileAtWorldPosition(Vector3 worldPosition)
        {
            if (!enableRuntimeEdit) return;

            Vector3Int cellPosition = foregroundTilemap.WorldToCell(worldPosition);
            foregroundTilemap.SetTile(cellPosition, null);
            tileTypeMap.Remove(cellPosition);
        }

        // プラットフォーム追加用のヘルパーメソッド
        public void AddPlatform(Vector2Int position, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tileToPlace = GetPlatformTileVariant(x, y, size);

                    foregroundTilemap.SetTile(cellPos, tileToPlace);
                    tileTypeMap[cellPos] = TileType.Platform;
                }
            }
            RefreshTilemap();
        }

        // 天井プラットフォーム追加用のヘルパーメソッド
        public void AddCeilingPlatform(Vector2Int position, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(position.x + x, position.y + y, 0);
                    TileBase tileToPlace = GetCeilingPlatformTileVariant(x, y, size);

                    foregroundTilemap.SetTile(cellPos, tileToPlace);
                    tileTypeMap[cellPos] = TileType.CeilingPlatform;
                }
            }
            RefreshTilemap();
        }

        // ワンウェイプラットフォーム用の特別処理
        public void AddOneWayPlatform(Vector2Int position, int width)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cellPos = new Vector3Int(position.x + x, position.y, 0);
                foregroundTilemap.SetTile(cellPos, platformTiles[0]);
                tileTypeMap[cellPos] = TileType.OneWayPlatform;

                // ワンウェイプラットフォーム用のコライダー設定
                SetupOneWayCollider(cellPos);
            }
            RefreshTilemap();
        }

        // 天井用ワンウェイプラットフォーム（重力反転時に使用）
        public void AddOneWayCeilingPlatform(Vector2Int position, int width)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cellPos = new Vector3Int(position.x + x, position.y, 0);
                foregroundTilemap.SetTile(cellPos, ceilingTiles[0]);
                tileTypeMap[cellPos] = TileType.OneWayPlatform;

                // 天井用ワンウェイプラットフォームのコライダー設定
                SetupOneWayCeilingCollider(cellPos);
            }
            RefreshTilemap();
        }

        private void SetupOneWayCollider(Vector3Int cellPosition)
        {
            // ワンウェイプラットフォーム用の特別なコライダー設定
            Vector3 worldPos = foregroundTilemap.CellToWorld(cellPosition);

            GameObject oneWayPlatform = new GameObject($"OneWayPlatform_{cellPosition.x}_{cellPosition.y}");
            oneWayPlatform.transform.position = worldPos;
            oneWayPlatform.transform.SetParent(transform);

            BoxCollider2D platformCollider = oneWayPlatform.AddComponent<BoxCollider2D>();
            platformCollider.size = Vector2.one;

            PlatformEffector2D effector = oneWayPlatform.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            effector.rotationalOffset = 0f; // 通常のプラットフォーム（下から上へ）

            platformCollider.usedByEffector = true;
        }

        private void SetupOneWayCeilingCollider(Vector3Int cellPosition)
        {
            // 天井用ワンウェイプラットフォームの特別なコライダー設定
            Vector3 worldPos = foregroundTilemap.CellToWorld(cellPosition);

            GameObject oneWayCeiling = new GameObject($"OneWayCeiling_{cellPosition.x}_{cellPosition.y}");
            oneWayCeiling.transform.position = worldPos;
            oneWayCeiling.transform.SetParent(transform);

            BoxCollider2D ceilingCollider = oneWayCeiling.AddComponent<BoxCollider2D>();
            ceilingCollider.size = Vector2.one;

            PlatformEffector2D effector = oneWayCeiling.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
            effector.rotationalOffset = 180f; // 天井用（上から下へ）

            ceilingCollider.usedByEffector = true;
        }

        // ギズモによるデバッグ表示
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || foregroundTilemap == null) return;

            // タイルマップの境界を表示
            Gizmos.color = Color.green;
            BoundsInt bounds = foregroundTilemap.cellBounds;
            Vector3 center = foregroundTilemap.CellToWorld(new Vector3Int((int)bounds.center.x, (int)bounds.center.y, 0));
            Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 1);
            Gizmos.DrawWireCube(center, size);

            // 地面レベルの表示
            Gizmos.color = Color.blue;
            Vector3 groundStart = new Vector3(0, groundLevel, 0);
            Vector3 groundEnd = new Vector3(stageWidthInTiles, groundLevel, 0);
            Gizmos.DrawLine(groundStart, groundEnd);

            // 天井レベルの表示
            Gizmos.color = Color.red;
            Vector3 ceilingStart = new Vector3(0, ceilingLevel, 0);
            Vector3 ceilingEnd = new Vector3(stageWidthInTiles, ceilingLevel, 0);
            Gizmos.DrawLine(ceilingStart, ceilingEnd);

            // プラットフォーム位置の表示
            Gizmos.color = Color.yellow;
            foreach (var platformPos in platformPositions)
            {
                Vector3 worldPos = foregroundTilemap.CellToWorld(new Vector3Int(platformPos.x, platformPos.y, 0));
                Gizmos.DrawWireCube(worldPos, Vector3.one);
            }

            // 天井プラットフォーム位置の表示
            Gizmos.color = Color.magenta;
            foreach (var ceilingPlatformPos in ceilingPlatformPositions)
            {
                Vector3 worldPos = foregroundTilemap.CellToWorld(new Vector3Int(ceilingPlatformPos.x, ceilingPlatformPos.y, 0));
                Gizmos.DrawWireCube(worldPos, Vector3.one);
            }
        }

        // StageManagerとの統合用メソッド
        public void IntegrateWithStageManager(StageManager stageManager)
        {
            if (stageManager.foregroundTilemap == null)
            {
                stageManager.foregroundTilemap = foregroundTilemap;
            }

            if (stageManager.foregroundCollider == null)
            {
                stageManager.foregroundCollider = tilemapCollider;
            }

            Debug.Log("TilemapGroundManager integrated with StageManager");
        }

        // パフォーマンス最適化用メソッド
        public void OptimizeForPerformance()
        {
            // コライダーの最適化
            if (compositeCollider != null)
            {
                compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
                compositeCollider.GenerateGeometry();
            }

            // 不要なタイルの削除（画面外など）
            CleanupDistantTiles();
        }

        private void CleanupDistantTiles()
        {
            if (Camera.main == null) return;

            Vector3 cameraPos = Camera.main.transform.position;
            float cleanupDistance = 50f; // ピクセル単位

            List<Vector3Int> tilesToRemove = new List<Vector3Int>();

            foreach (var kvp in tileTypeMap)
            {
                Vector3 tileWorldPos = foregroundTilemap.CellToWorld(kvp.Key);
                float distance = Vector3.Distance(cameraPos, tileWorldPos);

                if (distance > cleanupDistance)
                {
                    tilesToRemove.Add(kvp.Key);
                }
            }

            foreach (var tilePos in tilesToRemove)
            {
                foregroundTilemap.SetTile(tilePos, null);
                tileTypeMap.Remove(tilePos);
            }

            if (tilesToRemove.Count > 0)
            {
                RefreshTilemap();
                Debug.Log($"Cleaned up {tilesToRemove.Count} distant tiles");
            }
        }

        // 天井生成の個別制御用メソッド
        public void SetCeilingGenerationEnabled(bool enabled)
        {
            autoGenerateCeiling = enabled;
        }

        public void RegenerateCeiling()
        {
            // 既存の天井タイルを削除
            ClearTilesByType(TileType.Ceiling);
            ClearTilesByType(TileType.CeilingPlatform);

            // 天井を再生成
            if (autoGenerateCeiling)
            {
                GenerateBasicCeiling();
                GenerateCeilingPlatforms();
            }
        }

        private void ClearTilesByType(TileType targetType)
        {
            List<Vector3Int> tilesToClear = new List<Vector3Int>();

            foreach (var kvp in tileTypeMap)
            {
                if (kvp.Value == targetType)
                {
                    tilesToClear.Add(kvp.Key);
                }
            }

            foreach (var tilePos in tilesToClear)
            {
                foregroundTilemap.SetTile(tilePos, null);
                tileTypeMap.Remove(tilePos);
            }
        }

        // 天井の高さを動的に変更
        public void SetCeilingLevel(float newCeilingLevel)
        {
            ceilingLevel = newCeilingLevel;
            RegenerateCeiling();
        }

        // 天井の厚さを動的に変更
        public void SetCeilingThickness(float newThickness)
        {
            ceilingThickness = newThickness;
            RegenerateCeiling();
        }

        // コライダー設定の検証とデバッグ情報出力
        [ContextMenu("Validate Collider Setup")]
        public void ValidateColliderSetup()
        {
            Debug.Log("=== Tilemap Collider Validation ===");

            if (tilemapCollider == null)
            {
                Debug.LogError("TilemapCollider2D is null!");
                return;
            }

            if (compositeCollider == null)
            {
                Debug.LogError("CompositeCollider2D is null!");
                return;
            }

            Debug.Log($"CompositeOperation: {tilemapCollider.compositeOperation}");
            Debug.Log($"GeometryType: {compositeCollider.geometryType}");
            Debug.Log($"GenerationType: {compositeCollider.generationType}");
            Debug.Log($"Composite Points Count: {compositeCollider.pointCount}");
            Debug.Log($"Ground Level: {groundLevel}, Ceiling Level: {ceilingLevel}");

            // 地面と天井の距離をチェック
            float distance = ceilingLevel - groundLevel;
            Debug.Log($"Ground to Ceiling Distance: {distance}");

            if (distance < 5f)
            {
                Debug.LogWarning("Ground and ceiling are very close. Consider using Intersect or Merge operation.");
            }
            else if (tilemapCollider.compositeOperation == Collider2D.CompositeOperation.Intersect)
            {
                Debug.LogWarning("Ground and ceiling are far apart. Intersect operation may cause ceiling collider to disappear. Consider using None operation.");
            }

            // タイルタイプの統計
            var typeCount = new Dictionary<TileType, int>();
            foreach (var tileType in tileTypeMap.Values)
            {
                if (!typeCount.ContainsKey(tileType))
                    typeCount[tileType] = 0;
                typeCount[tileType]++;
            }

            Debug.Log("Tile Type Distribution:");
            foreach (var kvp in typeCount)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} tiles");
            }
        }
    }
}