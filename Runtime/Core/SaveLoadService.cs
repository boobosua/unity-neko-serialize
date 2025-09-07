using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace NekoSerialize
{
    public static class SaveLoadService
    {
        private static Dictionary<string, object> s_saveData = new();
        private static SaveDataHandler s_dataHandler;
        private static SaveLoadSettings s_settings;
        private static bool s_isInitialized = false;

        /// <summary>
        /// Initialize the save/load service. Called automatically on first access.
        /// </summary>
        public static void Initialize()
        {
            if (s_isInitialized)
                return;

            LoadSettings();
            InitializeDataHandler();
            LoadGame();
            s_isInitialized = true;

            Debug.Log("[SaveLoadService] Initialized successfully.");
        }

        /// <summary>
        /// Initialize the save/load service asynchronously.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (s_isInitialized)
                return;

            LoadSettings();
            InitializeDataHandler();
            await LoadAllAsync();
            s_isInitialized = true;

            Debug.Log("[SaveLoadService] Initialized successfully.");
        }

        /// <summary>
        /// Load settings from Resources folder.
        /// </summary>
        private static void LoadSettings()
        {
            s_settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");
            if (s_settings == null)
            {
                Debug.LogWarning("[SaveLoadService] No SaveLoadSettings found in Resources folder. Using default settings in memory.");
                s_settings = ScriptableObject.CreateInstance<SaveLoadSettings>();
            }
        }

        /// <summary>
        /// Initialize the data handler based on current settings.
        /// </summary>
        private static void InitializeDataHandler()
        {
            s_dataHandler = s_settings.SaveLocation switch
            {
                SaveLocation.PlayerPrefs => new PlayerPrefsHandler(s_settings),
                SaveLocation.JsonFile => new SingleJsonFileHandler(s_settings),
                _ => new PlayerPrefsHandler(s_settings)
            };
        }

        /// <summary>
        /// Save data with the specified key.
        /// </summary>
        public static void Save<T>(string key, T data, bool autoSave = true)
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_saveData[key] = data;

            if (autoSave)
            {
                SaveAll();
            }
        }

        /// <summary>
        /// Load data for the specified key.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return defaultValue;
            }
            if (s_saveData.TryGetValue(key, out var data))
            {
                try
                {
                    if (data is JObject jObj)
                    {
                        using var reader = jObj.CreateReader();
                        var serializer = UnityJsonSettings.CreateSerializer();
                        return serializer.Deserialize<T>(reader) ?? defaultValue;
                    }
                    else if (data is T directValue)
                    {
                        return directValue;
                    }
                    else
                    {
                        var serializer = UnityJsonSettings.CreateSerializer();
                        var jToken = JToken.FromObject(data);
                        return jToken.ToObject<T>(serializer) ?? defaultValue;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveLoadService] Error loading data for key: {key}, Exception: {e}.");
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if data exists for the specified key.
        /// </summary>
        public static bool HasData(string key)
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return false;
            }
            return s_saveData.ContainsKey(key);
        }

        /// <summary>
        /// Delete data for the specified key.
        /// </summary>
        public static void DeleteData(string key)
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            if (s_saveData.ContainsKey(key))
            {
                s_saveData.Remove(key);
                Debug.Log($"[SaveLoadService] Deleted data for key: {key}");
            }
        }

        /// <summary>
        /// Save all cached data to persistent storage.
        /// </summary>
        public static void SaveAll()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_dataHandler.SaveData(s_saveData);
        }

        /// <summary>
        /// Load all data from persistent storage.
        /// </summary>
        public static void LoadAll()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_saveData = s_dataHandler.LoadData();
        }

        /// <summary>
        /// Save all data asynchronously.
        /// </summary>
        public static async Task SaveAllAsync()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            try
            {
                var dataToSave = new Dictionary<string, object>(s_saveData);
                await s_dataHandler.SaveDataAsync(dataToSave);
                Debug.Log("[SaveLoadService] All data saved asynchronously");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadService] Error during async save: {e.Message}");
            }
        }

        /// <summary>
        /// Load all data asynchronously.
        /// </summary>
        public static async Task LoadAllAsync()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            try
            {
                var loadedData = await s_dataHandler.LoadDataAsync();
                s_saveData = loadedData;
                Debug.Log("[SaveLoadService] All data loaded asynchronously");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoadService] Error during async load: {e.Message}");
                s_saveData = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Check if any save data exists.
        /// </summary>
        public static bool SaveDataExists()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return false;
            }
            return s_dataHandler.SaveDataExists();
        }

        /// <summary>
        /// Delete all save data.
        /// </summary>
        public static void DeleteAllData()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_dataHandler.DeleteSaveData();
            s_saveData.Clear();
            Debug.Log("[SaveLoadService] All save data deleted");
        }

        /// <summary>
        /// Load game data from persistent storage.
        /// </summary>
        private static void LoadGame()
        {
            s_saveData = s_dataHandler.LoadData();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get current settings (for editor and runtime use).
        /// </summary>
        public static SaveLoadSettings GetSettings()
        {
            if (!s_isInitialized)
            {
                Debug.LogWarning("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return null;
            }
            return s_settings;
        }

        /// <summary>
        /// Refresh settings (for editor use).
        /// </summary>
        public static void RefreshSettings()
        {
            LoadSettings();
            InitializeDataHandler();
            Debug.Log("[SaveLoadService] Settings refreshed");
        }
#endif
    }
}
