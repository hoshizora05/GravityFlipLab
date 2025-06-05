using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Static helper methods for working with Addressables.
    /// Provides utility functions for common Addressable operations.
    /// </summary>
    public static class AddressableHelper
    {
        /// <summary>
        /// Validates if an addressable key exists.
        /// </summary>
        /// <param name="key">The addressable key to validate</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public static async Task<bool> ValidateKeyExists(string key)
        {
            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationsHandle.Task;
                
                bool exists = locationsHandle.Status == AsyncOperationStatus.Succeeded && 
                             locationsHandle.Result != null && 
                             locationsHandle.Result.Count > 0;
                
                Addressables.Release(locationsHandle);
                return exists;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error validating key '{key}': {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the asset type for a specific addressable key.
        /// </summary>
        /// <param name="key">The addressable key to check</param>
        /// <returns>The type of the asset, or null if not found</returns>
        public static async Task<Type> GetAssetTypeForKey(string key)
        {
            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationsHandle.Task;
                
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || 
                    locationsHandle.Result == null || 
                    locationsHandle.Result.Count == 0)
                {
                    Addressables.Release(locationsHandle);
                    return null;
                }
                
                Type assetType = locationsHandle.Result[0].ResourceType;
                Addressables.Release(locationsHandle);
                return assetType;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting asset type for key '{key}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets all keys for assets with a specific label.
        /// </summary>
        /// <param name="label">The label to search for</param>
        /// <returns>List of keys that have the specified label</returns>
        public static async Task<List<string>> GetKeysWithLabel(string label)
        {
            List<string> keys = new List<string>();
            
            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(UnityEngine.Object));
                await locationsHandle.Task;
                
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || 
                    locationsHandle.Result == null)
                {
                    Addressables.Release(locationsHandle);
                    return keys;
                }
                
                foreach (IResourceLocation location in locationsHandle.Result)
                {
                    if (location.PrimaryKey is string key)
                    {
                        keys.Add(key);
                    }
                }
                
                Addressables.Release(locationsHandle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting keys with label '{label}': {ex.Message}");
            }
            
            return keys;
        }
        
        /// <summary>
        /// Gets the size of an addressable asset in bytes.
        /// </summary>
        /// <param name="key">The addressable key</param>
        /// <returns>Size in bytes, or -1 if size couldn't be determined</returns>
        public static async Task<long> GetAssetSize(string key)
        {
            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(key);
                await sizeHandle.Task;
                
                long size = -1;
                if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    size = sizeHandle.Result;
                }
                
                Addressables.Release(sizeHandle);
                return size;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting size for key '{key}': {ex.Message}");
                return -1;
            }
        }
        
        /// <summary>
        /// Gets the download status of an addressable asset.
        /// </summary>
        /// <param name="key">The addressable key</param>
        /// <returns>True if the asset is already downloaded, false otherwise</returns>
        public static async Task<bool> IsAssetDownloaded(string key)
        {
            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(key);
                await sizeHandle.Task;
                
                bool isDownloaded = sizeHandle.Status == AsyncOperationStatus.Succeeded && sizeHandle.Result == 0;
                Addressables.Release(sizeHandle);
                return isDownloaded;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error checking download status for key '{key}': {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets all labels in the addressable system.
        /// </summary>
        /// <returns>HashSet of all labels</returns>
        public static HashSet<string> GetAllLabels()
        {
            HashSet<string> labels = new HashSet<string>();
            
            try
            {
                foreach (var locator in Addressables.ResourceLocators)
                {
                    foreach (var key in locator.Keys)
                    {
                        if (key is string)
                            continue;
                        
                        if (key is IKeyEvaluator)
                        {
                            // Handle label keys
                            var labelKey = key as IKeyEvaluator;
                            if (labelKey != null)
                            {
                                labels.Add(labelKey.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting all labels: {ex.Message}");
            }
            
            return labels;
        }
        
        /// <summary>
        /// Gets all string keys in the addressable system.
        /// </summary>
        /// <returns>HashSet of all string keys</returns>
        public static HashSet<string> GetAllStringKeys()
        {
            HashSet<string> stringKeys = new HashSet<string>();
            
            try
            {
                foreach (var locator in Addressables.ResourceLocators)
                {
                    foreach (var key in locator.Keys)
                    {
                        if (key is string stringKey)
                        {
                            stringKeys.Add(stringKey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting all string keys: {ex.Message}");
            }
            
            return stringKeys;
        }
        
        /// <summary>
        /// Gets the list of dependent addressable keys for a given key.
        /// </summary>
        /// <param name="key">The addressable key</param>
        /// <returns>List of dependent keys, or empty list if none</returns>
        public static async Task<List<string>> GetDependencies(string key)
        {
            List<string> dependencies = new List<string>();
            
            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationsHandle.Task;
                
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded || 
                    locationsHandle.Result == null || 
                    locationsHandle.Result.Count == 0)
                {
                    Addressables.Release(locationsHandle);
                    return dependencies;
                }
                
                foreach (var location in locationsHandle.Result)
                {
                    foreach (var dependency in location.Dependencies)
                    {
                        if (dependency.PrimaryKey is string dependencyKey)
                        {
                            dependencies.Add(dependencyKey);
                        }
                    }
                }
                
                Addressables.Release(locationsHandle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableHelper] Error getting dependencies for key '{key}': {ex.Message}");
            }
            
            return dependencies;
        }
        
        /// <summary>
        /// Gets the formatted size string for a byte count (e.g. "1.5 MB").
        /// </summary>
        /// <param name="bytes">Byte count</param>
        /// <returns>Formatted size string</returns>
        public static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}