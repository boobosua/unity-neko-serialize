using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NekoLib.Core;
using NekoLib.Extensions;
using NekoLib.Logger;
using NekoLib.Services;
using NekoLib.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Core save/load service with separated memory and storage operations.
    /// </summary>
    public static class SaveLoadService
    {
        private const string LastSaveTimeKey = "LastSaveTime";

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
            FetchSaveData();
            StartAutoSaveIfNeeded();
            s_isInitialized = true;

            Log.Info("[SaveLoadService] Initialized successfully.");
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
            await FetchSaveDataAsync();
            StartAutoSaveIfNeeded();
            s_isInitialized = true;

            Log.Info("[SaveLoadService] Initialized successfully.");
        }

        /// <summary>
        /// Load settings from Resources folder.
        /// </summary>
        private static void LoadSettings()
        {
            s_settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");
            if (s_settings == null)
            {
                Log.Warn("[SaveLoadService] No SaveLoadSettings found in Resources folder. Using default settings in memory.");
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
        /// Ensure the service is initialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!s_isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Save data directly to persistent storage immediately.
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            EnsureInitialized();

            s_saveData[key] = data;
            s_saveData[LastSaveTimeKey] = DateTimeService.UtcNow;
            s_dataHandler.WriteData(s_saveData);
        }

        /// <summary>
        /// Save data asynchronously to persistent storage.
        /// </summary>
        public static async Task SaveAsync<T>(string key, T data)
        {
            EnsureInitialized();

            s_saveData[key] = data;
            s_saveData[LastSaveTimeKey] = DateTimeService.UtcNow;
            await s_dataHandler.WriteDataAsync(s_saveData);
        }

        /// <summary>
        /// Load data for the specified key.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            EnsureInitialized();

            if (s_saveData.TryGetValue(key, out var data))
            {
                try
                {
                    // Direct type match - fastest path (no conversion needed)
                    if (data is T directValue)
                    {
                        return directValue;
                    }
                    // Handle JObject from JSON deserialization
                    else if (data is JObject jObj)
                    {
                        return JsonSerializerUtils.DeserializeJObject<T>(jObj);
                    }
                    else
                    {
                        return JsonSerializerUtils.DeserializeJToken<T>(data);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"[SaveLoadService] Error loading data for key: {key.Colorize(Swatch.VR)}, Exception: {e}.");
                }
            }

            Log.Warn($"[SaveLoadService] No data found for key: {key.Colorize(Swatch.VR)}. Returning default value.");
            return defaultValue;
        }

        public static async Task<T> LoadAsync<T>(string key, T defaultValue = default)
        {
            EnsureInitialized();
            return await Task.Run(() => Load<T>(key, defaultValue));
        }

        /// <summary>
        /// Determines if auto-save is enabled based on current settings.
        /// </summary>
        private static bool AutoSaveEnabled()
        {
            if (s_settings == null)
                return false;

            return s_settings.AutoSaveInterval > 0f || s_settings.AutoSaveOnFocusLost;
        }

        /// <summary>
        /// Start auto-save if enabled in settings.
        /// </summary>
        private static void StartAutoSaveIfNeeded()
        {
            if (AutoSaveEnabled())
            {
                SaveLoadManager.Instance.Initialize(s_settings);
            }
        }

        /// <summary>
        /// Check if data exists for the specified key.
        /// </summary>
        public static bool HasData(string key)
        {
            EnsureInitialized();
            return s_saveData.ContainsKey(key);
        }

        /// <summary>
        /// Delete data for the specified key.
        /// </summary>
        public static void DeleteData(string key)
        {
            EnsureInitialized();

            if (s_saveData.ContainsKey(key))
            {
                s_saveData.Remove(key);
                Log.Info($"[SaveLoadService] Deleted data for key: {key.Colorize(Swatch.DE)}");
            }
        }

        /// <summary>
        /// Load all data from persistent storage.
        /// </summary>
        private static void FetchSaveData()
        {
            s_saveData = s_dataHandler.ReadData();
        }

        /// <summary>
        /// Load all data asynchronously.
        /// </summary>
        private static async Task FetchSaveDataAsync()
        {
            try
            {
                s_saveData = await s_dataHandler.ReadDataAsync();
                Log.Info("[SaveLoadService] All data loaded asynchronously");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveLoadService] Error during async load: {e.Message.Colorize(Swatch.VR)}");
                s_saveData = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Delete all save data.
        /// </summary>
        public static void DeleteAllData()
        {
            EnsureInitialized();
            s_dataHandler.DeleteData();
            s_saveData.Clear();
            Log.Info("[SaveLoadService] All save data deleted");
        }

        /// <summary>
        /// Get the last save time.
        /// </summary>
        public static DateTime GetLastSaveTime()
        {
            return Load(LastSaveTimeKey, DateTime.MinValue);
        }

        public static void SaveAll() { }

#if UNITY_EDITOR
        /// <summary>
        /// Handle cleanup when play mode exits without domain reload (for editor use).
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void HandlePlayModeStateChanged()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode && Utils.IsReloadDomainDisabled())
            {
                DisposeService();
            }
        }

        /// <summary>
        /// Dispose and cleanup the service (for editor use).
        /// </summary>
        private static void DisposeService()
        {
            if (!s_isInitialized) return;

            try
            {
                // Clear cached data.
                s_saveData?.Clear();
                s_saveData = null;

                // Reset state.
                s_dataHandler = null;
                s_settings = null;
                s_isInitialized = false;

                Log.Info("[SaveLoadService] Service disposed and cleaned up.");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveLoadService] Error during disposal: {e.Message.Colorize(Swatch.VR)}");
            }
        }


        /// <summary>
        /// Check if the service is initialized (for editor use).
        /// </summary>
        public static bool IsInitialized => s_isInitialized;

        /// <summary>
        /// Get all save data (for editor use).
        /// </summary>
        public static Dictionary<string, object> GetAllSaveData()
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() first.");
                return new Dictionary<string, object>();
            }
            return new Dictionary<string, object>(s_saveData);
        }

        /// <summary>
        /// Check if data is persisted to storage (for editor use).
        /// </summary>
        public static bool IsDataPersisted(string key)
        {
            if (!s_isInitialized) return false;

            try
            {
                var persistedData = s_dataHandler.ReadData();
                return persistedData.ContainsKey(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current settings (for editor and runtime use).
        /// </summary>
        public static SaveLoadSettings GetSettings()
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
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
            Log.Info("[SaveLoadService] Settings refreshed");
        }
#endif
    }
}
