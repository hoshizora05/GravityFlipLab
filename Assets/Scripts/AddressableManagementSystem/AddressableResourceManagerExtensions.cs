using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
// AddressableResourceManager クラスへのアクセスが必要
using AddressableManagementSystem;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Extensions for the AddressableResourceManager to provide convenience methods for common operations.
    /// </summary>
    public static class AddressableResourceManagerExtensions
    {
        /// <summary>
        /// Loads a GameObject and instantiates it in the scene.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the prefab</param>
        /// <param name="parent">Optional parent transform</param>
        /// <param name="worldPositionStays">Whether to maintain world position when setting parent</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="callback">Optional callback when instantiation completes</param>
        /// <returns>Task that completes with the instantiated GameObject</returns>
        public static async Task<GameObject> LoadAndInstantiatePrefabAsync(
            this AddressableResourceManager manager,
            string key,
            Transform parent = null,
            bool worldPositionStays = true,
            // ★修正: 完全修飾名を使用
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<GameObject> callback = null)
        {
            try
            {
                // Load the prefab
                // ★修正: 完全修飾名を使用
                var prefabHandle = manager.LoadAssetAsync<GameObject>(key, priority);
                await prefabHandle.Task;

                if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load prefab: {key} - {prefabHandle.OperationException}");
                    return null;
                }

                // Instantiate the prefab
                GameObject prefab = prefabHandle.Result;
                GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);

                // Set a name that doesn't have "(Clone)" appended
                instance.name = prefab.name;

                // Invoke callback if provided
                callback?.Invoke(instance);

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error instantiating prefab {key}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a texture and applies it to a UI RawImage component.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the texture</param>
        /// <param name="rawImage">RawImage component to apply the texture to</param>
        /// <param name="priority">Loading priority</param>
        /// <returns>Task that completes when the texture is applied</returns>
        public static async Task LoadTextureToRawImageAsync(
            this AddressableResourceManager manager,
            string key,
            UnityEngine.UI.RawImage rawImage,
            // ★修正: 完全修飾名を使用
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal)
        {
            if (rawImage == null)
            {
                Debug.LogError("RawImage component cannot be null");
                return;
            }

            try
            {
                // ★修正: 完全修飾名を使用
                var textureHandle = manager.LoadAssetAsync<Texture2D>(key, priority);
                await textureHandle.Task;

                if (textureHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    // Apply texture to the RawImage
                    rawImage.texture = textureHandle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load texture: {key} - {textureHandle.OperationException}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading texture {key} to RawImage: {e.Message}");
            }
        }

        /// <summary>
        /// Loads a sprite and applies it to a UI Image component.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="key">Addressable key for the sprite</param>
        /// <param name="image">Image component to apply the sprite to</param>
        /// <param name="priority">Loading priority</param>
        /// <returns>Task that completes when the sprite is applied</returns>
        public static async Task LoadSpriteToImageAsync(
            this AddressableResourceManager manager,
            string key,
            UnityEngine.UI.Image image,
            // ★修正: 完全修飾名を使用
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal)
        {
            if (image == null)
            {
                Debug.LogError("Image component cannot be null");
                return;
            }

            try
            {
                // ★修正: 完全修飾名を使用
                var spriteHandle = manager.LoadAssetAsync<Sprite>(key, priority);
                await spriteHandle.Task;

                if (spriteHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    // Apply sprite to the Image
                    image.sprite = spriteHandle.Result;
                }
                else
                {
                    Debug.LogError($"Failed to load sprite: {key} - {spriteHandle.OperationException}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading sprite {key} to Image: {e.Message}");
            }
        }

        /// <summary>
        /// Loads multiple assets of the same type at once.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="keys">Collection of asset keys to load</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="progressCallback">Optional callback to report batch loading progress</param>
        /// <returns>Task that completes with a dictionary of loaded assets</returns>
        public static async Task<Dictionary<string, T>> LoadAssetBatchAsync<T>(
            this AddressableResourceManager manager,
            IEnumerable<string> keys,
            // ★修正: 完全修飾名を使用
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<float> progressCallback = null)
        {
            Dictionary<string, T> results = new Dictionary<string, T>();
            // ★修正: AsyncOperationHandle<T> を直接 Task として扱うのではなく、Handle を保持する
            List<AsyncOperationHandle<T>> handles = new List<AsyncOperationHandle<T>>();
            List<string> keyList = new List<string>(keys);

            try
            {
                // Start all load operations
                foreach (var key in keyList)
                {
                    // ★修正: 完全修飾名を使用し、Handle をリストに追加
                    var handle = manager.LoadAssetAsync<T>(key, priority);
                    handles.Add(handle);
                }

                // Wait for all tasks to complete or report progress
                int completedCount = 0;
                while (completedCount < handles.Count)
                {
                    completedCount = 0;
                    float totalProgress = 0f;
                    foreach (var handle in handles) // ★修正: handles を使用
                    {
                        totalProgress += handle.PercentComplete;
                        if (handle.IsDone) // ★修正: IsDone を使用
                        {
                            completedCount++;
                        }
                    }

                    float progress = handles.Count > 0 ? totalProgress / handles.Count : 1f; // ★修正: 進捗計算を修正
                    progressCallback?.Invoke(progress);

                    if (completedCount < handles.Count)
                    {
                        await Task.Delay(50); // Wait a bit before checking again
                    }
                }

                // Collect results
                for (int i = 0; i < keyList.Count; i++)
                {
                    var handle = handles[i]; // ★修正: handles を使用
                    if (handle.Status == AsyncOperationStatus.Succeeded) // ★修正: handle.Status を使用
                    {
                        results[keyList[i]] = handle.Result; // ★修正: handle.Result を使用
                    }
                    else
                    {
                        Debug.LogError($"Failed to load asset: {keyList[i]} - {handle.OperationException}"); // ★修正: handle.OperationException を使用
                    }
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading asset batch: {e.Message}");
                // ★追加: 失敗した場合でも部分的な結果を返すか、空を返すかを検討。ここでは部分的な結果を返す。
                // ★追加: ハンドルを解放する必要があるか検討 (通常、完了後に自動解放されることが多いが、エラー時は注意)
                foreach (var handle in handles)
                {
                    if (handle.IsValid() && !handle.IsDone)
                    {
                        // エラー発生時にまだ完了していないハンドルがあれば解放を試みる（Addressables.Releaseの仕様による）
                        // Addressables.Release(handle); // 必要に応じて解放
                    }
                }
                return results;
            }
            // ★修正: finally ブロックは不要になったため削除
        }


        /// <summary>
        /// Loads all assets with a specific label.
        /// </summary>
        /// <typeparam name="T">Type of assets to load</typeparam>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="label">Addressable label to load assets for</param>
        /// <param name="priority">Loading priority</param>
        /// <param name="progressCallback">Optional callback to report batch loading progress</param>
        /// <returns>Task that completes with a list of loaded assets</returns>
        public static async Task<List<T>> LoadAssetsWithLabelAsync<T>(
            this AddressableResourceManager manager,
            string label,
            // ★修正: 完全修飾名を使用
            AddressableResourceManager.LoadPriority priority = AddressableResourceManager.LoadPriority.Normal,
            Action<float> progressCallback = null)
        {
            List<T> results = new List<T>();

            try
            {
                // Get all assets with the specified label
                var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
                await locationsHandle.Task;

                if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load resource locations for label: {label} - {locationsHandle.OperationException}");
                    Addressables.Release(locationsHandle); // ★追加: ハンドルを解放
                    return results;
                }

                var locations = locationsHandle.Result;
                if (locations.Count == 0)
                {
                    Debug.LogWarning($"No assets found with label: {label}");
                    Addressables.Release(locationsHandle); // ★追加: ハンドルを解放
                    return results;
                }

                // Create a list of keys from the locations
                // ★修正: location.PrimaryKey を直接使うのではなく、適切なキー抽出を行う
                // Addressables のバージョンや設定によっては、PrimaryKey だけでLoadAssetAsyncを呼べない場合がある
                // LoadAssetsAsync を使う方がラベル指定には適している
                // List<string> keys = new List<string>();
                // foreach (var location in locations)
                // {
                //     // PrimaryKey が string であることを期待するのではなく、
                //     // Addressables.LoadAssetAsync が受け付ける形式である必要がある
                //     // 通常、IResourceLocation そのものや、location.PrimaryKey をキーとして使えることが多い
                //     if (location.PrimaryKey != null) // キーが存在するか確認
                //     {
                //         keys.Add(location.PrimaryKey); // ここでは PrimaryKey を使うと仮定
                //     }
                // }
                // Addressables.Release(locationsHandle); // ★追加: locations を使い終わったらハンドルを解放

                // ★改善: ラベルから直接ロードする Addressables.LoadAssetsAsync を使う方が効率的
                var loadAssetsHandle = Addressables.LoadAssetsAsync<T>(label, asset => {
                    // 個々のロード完了時のコールバック (オプション)
                    if (asset != null)
                    {
                        results.Add(asset);
                    }
                });

                // 進捗監視
                while (!loadAssetsHandle.IsDone)
                {
                    progressCallback?.Invoke(loadAssetsHandle.PercentComplete);
                    await Task.Yield(); // 次のフレームまで待機
                }

                if (loadAssetsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load assets with label {label}: {loadAssetsHandle.OperationException}");
                }
                // else // 成功時はコールバックで results に追加されているはず
                // {
                //    // loadAssetsHandle.Result にはロードされたアセットのリストが含まれるが、
                //    // コールバックで追加している場合は不要な場合もある
                // }

                Addressables.Release(loadAssetsHandle); // ★追加: ハンドルを解放
                Addressables.Release(locationsHandle); // ★追加: ハンドルを解放 (LoadAssetsAsyncを使う場合でもlocationsHandleは解放推奨)

                return results; // コールバックで追加された結果を返す
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading assets with label {label}: {e.Message}");
                // ★追加: 例外発生時もハンドルの解放を試みる (try-finally推奨)
                return results;
            }
        }

        /// <summary>
        /// Releases multiple assets at once.
        /// </summary>
        /// <param name="manager">The AddressableResourceManager instance</param>
        /// <param name="keys">Collection of asset keys to release</param>
        public static void ReleaseAssets(
            this AddressableResourceManager manager,
            IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                manager.ReleaseAsset(key);
            }
        }
    }
}