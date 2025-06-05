using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableManagementSystem
{
    /// <summary>
    /// Manages localized assets using the Addressable system.
    /// Provides functionality to load assets based on the current language setting.
    /// </summary>
    public class AddressableLocalizationManager : MonoBehaviour
    {
        #region Singleton

        private static AddressableLocalizationManager _instance;
        
        /// <summary>
        /// Singleton instance of the AddressableLocalizationManager
        /// </summary>
        public static AddressableLocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AddressableLocalizationManager]");
                    _instance = go.AddComponent<AddressableLocalizationManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        #endregion

        #region Inspector Properties
        
        [Tooltip("The default language to use if not set")]
        [SerializeField] private string _defaultLanguage = "en";
        
        [Tooltip("The format for localized asset keys: {0} = base key, {1} = language code")]
        [SerializeField] private string _localizedKeyFormat = "{0}_{1}";
        
        [Tooltip("Whether to preload common localized assets on language change")]
        [SerializeField] private bool _preloadCommonAssets = true;
        
        [Tooltip("Label for common localized assets that should be preloaded")]
        [SerializeField] private string _commonAssetsLabel = "CommonLocalized";
        
        [Tooltip("Available languages")]
        [SerializeField] private List<LanguageDefinition> _availableLanguages = new List<LanguageDefinition>();
        
        #endregion

        #region Fields and Properties
        
        private string _currentLanguage;
        private Dictionary<string, object> _localizedAssetCache = new Dictionary<string, object>();
        private bool _isChangingLanguage = false;
        
        /// <summary>
        /// Gets the current active language code.
        /// </summary>
        public string CurrentLanguage => _currentLanguage;
        
        /// <summary>
        /// Gets a list of all available language codes.
        /// </summary>
        public List<string> AvailableLanguages
        {
            get
            {
                List<string> languages = new List<string>();
                foreach (var lang in _availableLanguages)
                {
                    languages.Add(lang.LanguageCode);
                }
                return languages;
            }
        }
        
        /// <summary>
        /// Event triggered when the language changes.
        /// </summary>
        public event Action<string> OnLanguageChanged;
        
        #endregion

        #region Initialization

        private void Initialize()
        {
            // Set default language if current language is not set
            if (string.IsNullOrEmpty(_currentLanguage))
            {
                // Try to get from player prefs
                string savedLanguage = PlayerPrefs.GetString("Language", "");
                
                if (!string.IsNullOrEmpty(savedLanguage) && IsLanguageSupported(savedLanguage))
                {
                    _currentLanguage = savedLanguage;
                }
                else
                {
                    // Try to use system language
                    SystemLanguage systemLanguage = Application.systemLanguage;
                    string systemLangCode = SystemLanguageToCode(systemLanguage);
                    
                    if (IsLanguageSupported(systemLangCode))
                    {
                        _currentLanguage = systemLangCode;
                    }
                    else
                    {
                        // Fall back to default
                        _currentLanguage = _defaultLanguage;
                    }
                }
            }
            
            Debug.Log($"[AddressableLocalizationManager] Initialized with language: {_currentLanguage}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the active language and preloads common assets for that language.
        /// </summary>
        /// <param name="languageCode">The language code to set</param>
        /// <returns>Task that completes when language change is complete</returns>
        public async Task SetLanguageAsync(string languageCode)
        {
            if (!IsLanguageSupported(languageCode))
            {
                Debug.LogError($"[AddressableLocalizationManager] Language '{languageCode}' is not supported");
                return;
            }
            
            if (_currentLanguage == languageCode)
            {
                Debug.Log($"[AddressableLocalizationManager] Language is already set to '{languageCode}'");
                return;
            }
            
            if (_isChangingLanguage)
            {
                Debug.LogWarning($"[AddressableLocalizationManager] Language change already in progress");
                return;
            }
            
            _isChangingLanguage = true;
            
            try
            {
                Debug.Log($"[AddressableLocalizationManager] Changing language from '{_currentLanguage}' to '{languageCode}'");
                
                // Save the previous language (if needed for any special cleanup)
                string previousLanguage = _currentLanguage;
                
                // Set the new language
                _currentLanguage = languageCode;
                
                // Save to player prefs
                PlayerPrefs.SetString("Language", languageCode);
                PlayerPrefs.Save();
                
                // Clear cache for assets that might be language-specific
                ClearCache();
                
                // Preload common assets if enabled
                if (_preloadCommonAssets)
                {
                    await PreloadCommonAssetsAsync();
                }
                
                // Notify listeners
                OnLanguageChanged?.Invoke(languageCode);
                
                Debug.Log($"[AddressableLocalizationManager] Language changed to '{languageCode}'");
            }
            finally
            {
                _isChangingLanguage = false;
            }
        }
        
        /// <summary>
        /// Loads a localized asset asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of asset to load</typeparam>
        /// <param name="baseKey">Base key for the asset</param>
        /// <param name="fallbackToBase">Whether to fall back to the base key if localized version not found</param>
        /// <returns>Task that completes with the loaded asset, or null if not found</returns>
        public async Task<T> LoadLocalizedAssetAsync<T>(string baseKey, bool fallbackToBase = true) where T : UnityEngine.Object
        {
            string localizedKey = GetLocalizedKey(baseKey);
            
            // Check cache first
            if (_localizedAssetCache.TryGetValue(localizedKey, out object cachedAsset) && cachedAsset is T typedAsset)
            {
                return typedAsset;
            }
            
            // Try to load localized version
            bool localizedExists = await AddressableHelper.ValidateKeyExists(localizedKey);
            
            if (localizedExists)
            {
                var resourceManager = AddressableResourceManager.Instance;
                var handle = resourceManager.LoadAssetAsync<T>(localizedKey);
                
                await handle.Task;
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    T result = handle.Result;
                    _localizedAssetCache[localizedKey] = result;
                    return result;
                }
            }
            
            // If no localized version or failed to load, try base key if allowed
            if (fallbackToBase)
            {
                bool baseExists = await AddressableHelper.ValidateKeyExists(baseKey);
                
                if (baseExists)
                {
                    var resourceManager = AddressableResourceManager.Instance;
                    var handle = resourceManager.LoadAssetAsync<T>(baseKey);
                    
                    await handle.Task;
                    
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        T result = handle.Result;
                        _localizedAssetCache[localizedKey] = result; 
                        return result;
                    }
                }
            }
            
            Debug.LogWarning($"[AddressableLocalizationManager] Failed to load localized asset for key '{baseKey}' in language '{_currentLanguage}'");
            return null;
        }
        
        /// <summary>
        /// Gets the localized text for a specified key.
        /// </summary>
        /// <param name="key">The text key to look up</param>
        /// <returns>Task that completes with the localized text, or the key itself if not found</returns>
        public async Task<string> GetLocalizedTextAsync(string key)
        {
            // Attempt to load a text file that exactly matches {baseKey}_{languageCode}
            var textAsset = await LoadLocalizedAssetAsync<TextAsset>(key, false);
            
            if (textAsset != null)
            {
                return textAsset.text;
            }
            
            // Otherwise, try a dictionary approach (shared file containing multiple localized entries)
            string dictionaryKey = "LocalizationDictionary"; 
            var dictAsset = await LoadLocalizedAssetAsync<TextAsset>(dictionaryKey, false);
            
            if (dictAsset != null)
            {
                try
                {
                    // Parse JSON dictionary
                    var dict = JsonUtility.FromJson<LocalizationDictionary>(dictAsset.text);
                    
                    if (dict != null && dict.entries != null)
                    {
                        foreach (var entry in dict.entries)
                        {
                            if (entry.key == key)
                            {
                                return entry.value;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AddressableLocalizationManager] Error parsing localization dictionary: {e.Message}");
                }
            }
            
            // Fall back to returning the key itself if everything else fails
            return key;
        }
        
        /// <summary>
        /// Gets information about a specific language.
        /// </summary>
        /// <param name="languageCode">The language code to get information for</param>
        /// <returns>LanguageDefinition for the specified language, or null if not found</returns>
        public LanguageDefinition GetLanguageInfo(string languageCode)
        {
            foreach (var lang in _availableLanguages)
            {
                if (lang.LanguageCode == languageCode)
                {
                    return lang;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Adds a new supported language at runtime.
        /// </summary>
        /// <param name="languageDefinition">The language definition to add</param>
        public void AddSupportedLanguage(LanguageDefinition languageDefinition)
        {
            if (string.IsNullOrEmpty(languageDefinition.LanguageCode))
            {
                Debug.LogError("[AddressableLocalizationManager] Cannot add language with empty code");
                return;
            }
            
            // Check if already exists
            foreach (var lang in _availableLanguages)
            {
                if (lang.LanguageCode == languageDefinition.LanguageCode)
                {
                    Debug.LogWarning($"[AddressableLocalizationManager] Language '{languageDefinition.LanguageCode}' already exists, updating definition");
                    lang.DisplayName = languageDefinition.DisplayName;
                    lang.FlagSprite = languageDefinition.FlagSprite;
                    return;
                }
            }
            
            // Add new language
            _availableLanguages.Add(languageDefinition);
            Debug.Log($"[AddressableLocalizationManager] Added new supported language: {languageDefinition.LanguageCode} - {languageDefinition.DisplayName}");
        }
        
        /// <summary>
        /// Gets the localized key for an asset based on the current language.
        /// </summary>
        /// <param name="baseKey">The base key for the asset</param>
        /// <returns>The localized key</returns>
        public string GetLocalizedKey(string baseKey)
        {
            return string.Format(_localizedKeyFormat, baseKey, _currentLanguage);
        }
        
        /// <summary>
        /// Preloads common localized assets for faster access.
        /// </summary>
        /// <returns>Task that completes when preloading is finished</returns>
        public async Task PreloadCommonAssetsAsync()
        {
            if (string.IsNullOrEmpty(_commonAssetsLabel))
                return;
            
            Debug.Log($"[AddressableLocalizationManager] Preloading common assets for language '{_currentLanguage}'");
            
            try
            {
                // Get all keys with the common assets label
                var keys = await AddressableHelper.GetKeysWithLabel(_commonAssetsLabel);
                
                // Filter for current language
                List<string> relevantKeys = new List<string>();
                foreach (var key in keys)
                {
                    // If the key naming convention includes the language code
                    if (key.Contains(_currentLanguage))
                    {
                        relevantKeys.Add(key);
                    }
                }
                
                if (relevantKeys.Count == 0)
                {
                    Debug.Log($"[AddressableLocalizationManager] No common assets found for language '{_currentLanguage}'");
                    return;
                }
                
                // Preload assets
                await AddressableResourceManager.Instance.PreloadAssetsAsync<UnityEngine.Object>(relevantKeys, AddressableResourceManager.LoadPriority.Low);
                
                Debug.Log($"[AddressableLocalizationManager] Preloaded {relevantKeys.Count} common assets for language '{_currentLanguage}'");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableLocalizationManager] Error preloading common assets: {e.Message}");
            }
        }
        
        #endregion

        #region Private / Utility Methods

        /// <summary>
        /// Checks whether a given language code is supported.
        /// </summary>
        private bool IsLanguageSupported(string languageCode)
        {
            foreach (var lang in _availableLanguages)
            {
                if (lang.LanguageCode == languageCode)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Converts Unity's SystemLanguage to a language code recognized in your project.
        /// </summary>
        private string SystemLanguageToCode(SystemLanguage systemLanguage)
        {
            // You can customize these mappings for your own needs:
            switch (systemLanguage)
            {
                case SystemLanguage.English:   return "en";
                case SystemLanguage.Japanese:  return "ja";
                case SystemLanguage.French:    return "fr";
                case SystemLanguage.German:    return "de";
                case SystemLanguage.Spanish:   return "es";
                // etc.
                default:
                    return _defaultLanguage; // Fallback
            }
        }

        /// <summary>
        /// Clears the cached references to any loaded localized assets.
        /// </summary>
        private void ClearCache()
        {
            _localizedAssetCache.Clear();
        }

        #endregion
    }

    #region Supporting Data Classes

    /// <summary>
    /// Simple structure describing a language.
    /// </summary>
    [Serializable]
    public class LanguageDefinition
    {
        public string LanguageCode;
        public string DisplayName;
        public Sprite FlagSprite;
    }

    /// <summary>
    /// Basic JSON-friendly structure for a localization dictionary.
    /// </summary>
    [Serializable]
    public class LocalizationDictionary
    {
        public List<LocalizationEntry> entries;
    }

    /// <summary>
    /// Key/value text pair for localized strings.
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }

    #endregion
}
