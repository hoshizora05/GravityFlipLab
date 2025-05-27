using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GravityFlipLab.Stage
{
    public class ParallaxBackgroundManager : MonoBehaviour
    {
        [Header("Parallax Settings")]
        public Transform cameraTransform;
        public ParallaxLayer[] parallaxLayers;

        [Header("Performance")]
        public bool enableParallax = true;
        public float updateInterval = 0.016f; // 60fps update

        [Header("Shader Property Names")]
        public string offsetPropertyName = "_Offset";
        public string enableLoopPropertyName = "_EnableLoop";
        public string colorPropertyName = "_Color";

        private Vector3 lastCameraPosition;
        private float lastUpdateTime;

        // シェーダープロパティID（パフォーマンス最適化）
        private int offsetPropertyID;
        private int enableLoopPropertyID;
        private int colorPropertyID;

        [System.Serializable]
        public class ParallaxLayer
        {
            public string layerName;
            public SpriteRenderer spriteRenderer;
            public float parallaxFactor = 0.5f;
            public Vector2 textureScale = Vector2.one;
            public bool enableVerticalParallax = false;
            public bool enableLoop = true;

            [Header("Visual Settings")]
            public Color tintColor = Color.white;
            public Vector2 autoScrollSpeed = Vector2.zero;

            [Header("Runtime Debug")]
            [SerializeField] private Vector2 currentOffset;
            [SerializeField] private MaterialPropertyBlock propertyBlock;

            // プロパティアクセス
            public Vector2 CurrentOffset => currentOffset;
            public MaterialPropertyBlock PropertyBlock => propertyBlock;

            // 内部メソッド
            public void SetCurrentOffset(Vector2 offset)
            {
                currentOffset = offset;
            }

            public void InitializePropertyBlock()
            {
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }
            }

            public void ApplyPropertyBlock()
            {
                if (spriteRenderer != null && propertyBlock != null)
                {
                    spriteRenderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            // シェーダープロパティIDを事前取得（パフォーマンス最適化）
            offsetPropertyID = Shader.PropertyToID(offsetPropertyName);
            enableLoopPropertyID = Shader.PropertyToID(enableLoopPropertyName);
            colorPropertyID = Shader.PropertyToID(colorPropertyName);

            lastCameraPosition = cameraTransform.position;
            InitializeParallaxLayers();
        }

        private void Update()
        {
            if (!enableParallax || Time.time - lastUpdateTime < updateInterval) return;

            UpdateParallax();
            lastUpdateTime = Time.time;
        }

        private void InitializeParallaxLayers()
        {
            foreach (var layer in parallaxLayers)
            {
                if (layer.spriteRenderer == null)
                {
                    Debug.LogWarning($"ParallaxLayer '{layer.layerName}' has no SpriteRenderer assigned.");
                    continue;
                }

                // MaterialPropertyBlockを初期化
                layer.InitializePropertyBlock();

                // テクスチャサイズの設定
                Material material = layer.spriteRenderer.sharedMaterial;
                if (material != null && material.mainTexture != null)
                {
                    Texture mainTex = material.mainTexture;
                    layer.textureScale = new Vector2(mainTex.width, mainTex.height);

                    Debug.Log($"Layer '{layer.layerName}': Texture size {mainTex.width}x{mainTex.height}");
                }
                else
                {
                    Debug.LogWarning($"Layer '{layer.layerName}': No material or texture found.");
                }

                // 初期位置をカメラ位置に基づいて設定
                SetInitialLayerPosition(layer);

                // シェーダープロパティの初期化
                InitializeShaderProperties(layer);
            }
        }

        private void SetInitialLayerPosition(ParallaxLayer layer)
        {
            Transform layerTransform = layer.spriteRenderer.transform;
            Vector3 cameraPos = cameraTransform.position;

            // パラレックス係数に基づく初期位置を計算
            Vector3 initialPosition = new Vector3(
                cameraPos.x * layer.parallaxFactor,
                layer.enableVerticalParallax ? cameraPos.y * layer.parallaxFactor : layerTransform.position.y,
                layerTransform.position.z
            );

            layerTransform.position = initialPosition;

            Debug.Log($"Layer '{layer.layerName}': Initial position set to {initialPosition}");
        }

        private void InitializeShaderProperties(ParallaxLayer layer)
        {
            MaterialPropertyBlock block = layer.PropertyBlock;
            if (block == null) return;

            // オフセットプロパティを初期化
            block.SetVector(offsetPropertyID, Vector4.zero);

            // ループ有効化プロパティを設定
            block.SetFloat(enableLoopPropertyID, layer.enableLoop ? 1f : 0f);

            // 色調プロパティを設定
            block.SetColor(colorPropertyID, layer.tintColor);

            // MaterialPropertyBlockを適用
            layer.ApplyPropertyBlock();

            Debug.Log($"Layer '{layer.layerName}': MaterialPropertyBlock initialized");
            Debug.Log($"  - Offset: {Vector4.zero}");
            Debug.Log($"  - EnableLoop: {(layer.enableLoop ? 1 : 0)}");
            Debug.Log($"  - Color: {layer.tintColor}");
        }

        private void UpdateParallax()
        {
            Vector3 cameraMovement = cameraTransform.position - lastCameraPosition;

            foreach (var layer in parallaxLayers)
            {
                if (layer.spriteRenderer == null || layer.PropertyBlock == null) continue;

                // パラレックス移動量の計算
                Vector2 parallaxMovement = new Vector2(
                    cameraMovement.x * layer.parallaxFactor,
                    layer.enableVerticalParallax ? cameraMovement.y * layer.parallaxFactor : 0f
                );

                // 自動スクロールの追加
                Vector2 autoScroll = layer.autoScrollSpeed * Time.deltaTime;
                parallaxMovement += autoScroll;

                if (layer.enableLoop)
                {
                    UpdateLoopingParallax(layer, parallaxMovement);
                }
                else
                {
                    UpdateNonLoopingParallax(layer, parallaxMovement);
                }
            }

            lastCameraPosition = cameraTransform.position;
        }

        private void UpdateLoopingParallax(ParallaxLayer layer, Vector2 parallaxMovement)
        {
            MaterialPropertyBlock block = layer.PropertyBlock;
            Transform layerTransform = layer.spriteRenderer.transform;

            // 1. オブジェクトの位置をカメラに追従させる（パラレックス係数に基づく）
            Vector3 currentPosition = layerTransform.position;
            Vector3 targetCameraPosition = cameraTransform.position;

            // パラレックス係数に基づく追従位置を計算
            Vector3 followPosition = new Vector3(
                targetCameraPosition.x ,//targetCameraPosition.x * layer.parallaxFactor,
                layer.enableVerticalParallax ? targetCameraPosition.y * layer.parallaxFactor : currentPosition.y,
                currentPosition.z
            );

            // オブジェクトの位置を更新
            layerTransform.position = followPosition;

            // 2. シームレスループのためのUVオフセット計算
            Vector2 normalizedMovement = Vector2.zero;

            // テクスチャサイズに基づいてUVオフセットを計算
            if (layer.textureScale.x > 0)
                normalizedMovement.x = parallaxMovement.x / (layer.textureScale.x * 0.01f);
            if (layer.textureScale.y > 0)
                normalizedMovement.y = parallaxMovement.y / (layer.textureScale.y * 0.01f);

            // 現在のオフセットを更新
            Vector2 newOffset = layer.CurrentOffset + normalizedMovement;
            layer.SetCurrentOffset(newOffset);

            // MaterialPropertyBlockでシェーダープロパティを更新
            block.SetVector(offsetPropertyID, new Vector4(newOffset.x, newOffset.y, 0f, 0f));

            // MaterialPropertyBlockを適用
            layer.ApplyPropertyBlock();
        }

        private void UpdateNonLoopingParallax(ParallaxLayer layer, Vector2 parallaxMovement)
        {
            // 直接位置移動（ループなし）
            Vector3 currentPosition = layer.spriteRenderer.transform.position;
            Vector3 newPosition = currentPosition + new Vector3(
                parallaxMovement.x, parallaxMovement.y, 0f);

            layer.spriteRenderer.transform.position = newPosition;
        }

        #region Public API

        public void SetParallaxFactor(int layerIndex, float factor)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                parallaxLayers[layerIndex].parallaxFactor = factor;
                Debug.Log($"Layer {layerIndex} parallax factor set to {factor}");
            }
        }

        public void SetParallaxEnabled(bool enabled)
        {
            enableParallax = enabled;
            Debug.Log($"Parallax enabled: {enabled}");
        }

        public void SetLayerTintColor(int layerIndex, Color color)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                var layer = parallaxLayers[layerIndex];
                layer.tintColor = color;

                if (layer.PropertyBlock != null)
                {
                    layer.PropertyBlock.SetColor(colorPropertyID, color);
                    layer.ApplyPropertyBlock();
                    Debug.Log($"Layer {layerIndex} tint color set to {color}");
                }
            }
        }

        public void SetLayerLoopEnabled(int layerIndex, bool enableLoop)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                var layer = parallaxLayers[layerIndex];
                layer.enableLoop = enableLoop;

                if (layer.PropertyBlock != null)
                {
                    layer.PropertyBlock.SetFloat(enableLoopPropertyID, enableLoop ? 1f : 0f);
                    layer.ApplyPropertyBlock();
                    Debug.Log($"Layer {layerIndex} loop enabled: {enableLoop}");
                }
            }
        }

        public void SetAutoScrollSpeed(int layerIndex, Vector2 scrollSpeed)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                parallaxLayers[layerIndex].autoScrollSpeed = scrollSpeed;
                Debug.Log($"Layer {layerIndex} auto scroll speed set to {scrollSpeed}");
            }
        }

        public void ResetAllOffsets()
        {
            foreach (var layer in parallaxLayers)
            {
                layer.SetCurrentOffset(Vector2.zero);

                if (layer.PropertyBlock != null)
                {
                    layer.PropertyBlock.SetVector(offsetPropertyID, Vector4.zero);
                    layer.ApplyPropertyBlock();
                }
            }
            Debug.Log("All parallax offsets reset");
        }

        public void RefreshAllPropertyBlocks()
        {
            foreach (var layer in parallaxLayers)
            {
                if (layer.PropertyBlock != null)
                {
                    // 現在の設定で再初期化
                    layer.PropertyBlock.SetVector(offsetPropertyID,
                        new Vector4(layer.CurrentOffset.x, layer.CurrentOffset.y, 0f, 0f));
                    layer.PropertyBlock.SetFloat(enableLoopPropertyID, layer.enableLoop ? 1f : 0f);
                    layer.PropertyBlock.SetColor(colorPropertyID, layer.tintColor);
                    layer.ApplyPropertyBlock();
                }
            }
            Debug.Log("All MaterialPropertyBlocks refreshed");
        }

        public void SyncAllLayersToCamera()
        {
            Vector3 cameraPos = cameraTransform.position;

            foreach (var layer in parallaxLayers)
            {
                if (layer.spriteRenderer == null) continue;

                Transform layerTransform = layer.spriteRenderer.transform;

                // パラレックス係数に基づいてレイヤー位置を同期
                Vector3 syncPosition = new Vector3(
                    cameraPos.x * layer.parallaxFactor,
                    layer.enableVerticalParallax ? cameraPos.y * layer.parallaxFactor : layerTransform.position.y,
                    layerTransform.position.z
                );

                layerTransform.position = syncPosition;
            }

            Debug.Log("All layers synced to camera position");
        }

        public void SetLayerScale(int layerIndex, Vector2 scale)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                var layer = parallaxLayers[layerIndex];
                layer.spriteRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                Debug.Log($"Layer {layerIndex} scale set to {scale}");
            }
        }

        public void DebugLayerInfo(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
            {
                var layer = parallaxLayers[layerIndex];
                Debug.Log($"=== Layer {layerIndex} Debug Info ===");
                Debug.Log($"Name: {layer.layerName}");
                Debug.Log($"Parallax Factor: {layer.parallaxFactor}");
                Debug.Log($"Current Offset: {layer.CurrentOffset}");
                Debug.Log($"Texture Scale: {layer.textureScale}");
                Debug.Log($"Enable Loop: {layer.enableLoop}");
                Debug.Log($"Tint Color: {layer.tintColor}");
                Debug.Log($"Auto Scroll Speed: {layer.autoScrollSpeed}");
                Debug.Log($"Has PropertyBlock: {layer.PropertyBlock != null}");
                Debug.Log($"Has SpriteRenderer: {layer.spriteRenderer != null}");

                if (layer.spriteRenderer != null)
                {
                    Material mat = layer.spriteRenderer.sharedMaterial;
                    if (mat != null)
                    {
                        Debug.Log($"Material Name: {mat.name}");
                        Debug.Log($"Shader Name: {mat.shader.name}");
                        Debug.Log($"Has Main Texture: {mat.mainTexture != null}");

                        if (mat.mainTexture != null)
                        {
                            Debug.Log($"Texture Name: {mat.mainTexture.name}");
                            Debug.Log($"Texture Size: {mat.mainTexture.width}x{mat.mainTexture.height}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Layer {layerIndex} has no material!");
                    }
                }
            }
        }

        #endregion

        #region Unity Editor Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnDrawGizmosSelected()
        {
            if (cameraTransform == null) return;

            // カメラ位置を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraTransform.position, Vector3.one * 2f);

            // 各レイヤーの位置と情報を表示
            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                var layer = parallaxLayers[i];
                if (layer.spriteRenderer == null) continue;

                // レイヤーごとに色を変更
                float hue = (i * 0.3f) % 1f;
                Gizmos.color = Color.HSVToRGB(hue, 0.8f, 1f);

                Vector3 layerPos = layer.spriteRenderer.transform.position;
                Vector3 layerSize = layer.spriteRenderer.bounds.size;

                Gizmos.DrawWireCube(layerPos, layerSize);

#if UNITY_EDITOR
                // エディタでラベル表示
                string labelText = $"{layer.layerName}\nFactor: {layer.parallaxFactor:F2}\nOffset: {layer.CurrentOffset}\nLoop: {(layer.enableLoop ? "ON" : "OFF")}";
                UnityEditor.Handles.Label(layerPos + Vector3.up * (layerSize.y * 0.5f + 1f), labelText);
#endif
            }
        }

        // エディタ用のコンテキストメニュー
        [ContextMenu("Debug All Layers")]
        private void DebugAllLayers()
        {
            Debug.Log("=== Parallax Manager Debug Info ===");
            Debug.Log($"Parallax Enabled: {enableParallax}");
            Debug.Log($"Update Interval: {updateInterval}");
            Debug.Log($"Camera Transform: {(cameraTransform != null ? cameraTransform.name : "NULL")}");
            Debug.Log($"Layer Count: {parallaxLayers.Length}");
            Debug.Log("");

            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                DebugLayerInfo(i);
                Debug.Log("");
            }
        }

        [ContextMenu("Test Parallax Movement")]
        private void TestParallaxMovement()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test Parallax Movement can only be used in Play Mode");
                return;
            }

            Debug.Log("Testing parallax movement...");
            Vector3 originalCameraPos = cameraTransform.position;

            // カメラを一時的に移動してテスト
            cameraTransform.position += Vector3.right * 5f;
            UpdateParallax();

            Debug.Log("Camera moved 5 units to the right, parallax updated");

            // 各レイヤーの新しいオフセットを表示
            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                var layer = parallaxLayers[i];
                Debug.Log($"Layer {i} ({layer.layerName}): New offset = {layer.CurrentOffset}");
            }

            // 元の位置に戻す
            cameraTransform.position = originalCameraPos;
            Debug.Log("Camera position restored, parallax movement test completed");
        }

        [ContextMenu("Refresh All Property Blocks")]
        private void RefreshAllPropertyBlocksMenu()
        {
            RefreshAllPropertyBlocks();
        }

        [ContextMenu("Reset All Offsets")]
        private void ResetAllOffsetsMenu()
        {
            ResetAllOffsets();
        }

        [ContextMenu("Sync All Layers to Camera")]
        private void SyncAllLayersToCameraMenu()
        {
            SyncAllLayersToCamera();
        }

        [ContextMenu("Test Camera Sync")]
        private void TestCameraSync()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Camera Sync Test can only be used in Play Mode");
                return;
            }

            Debug.Log("Testing camera synchronization...");

            // 現在の位置を記録
            Vector3[] originalPositions = new Vector3[parallaxLayers.Length];
            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                if (parallaxLayers[i].spriteRenderer != null)
                    originalPositions[i] = parallaxLayers[i].spriteRenderer.transform.position;
            }

            // カメラを大きく移動
            Vector3 originalCameraPos = cameraTransform.position;
            cameraTransform.position += Vector3.right * 50f + Vector3.up * 20f;

            // 同期実行
            SyncAllLayersToCamera();

            // 結果を表示
            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                if (parallaxLayers[i].spriteRenderer != null)
                {
                    Vector3 newPos = parallaxLayers[i].spriteRenderer.transform.position;
                    Vector3 movement = newPos - originalPositions[i];
                    Debug.Log($"Layer {i} ({parallaxLayers[i].layerName}): Moved {movement} (Factor: {parallaxLayers[i].parallaxFactor})");
                }
            }

            // カメラ位置を戻す
            cameraTransform.position = originalCameraPos;
            SyncAllLayersToCamera();

            Debug.Log("Camera sync test completed");
        }

        #endregion

        private void OnValidate()
        {
            // エディタでの値変更時にMaterialPropertyBlockを更新
            if (Application.isPlaying)
            {
                RefreshAllPropertyBlocks();
            }
        }
    }
}