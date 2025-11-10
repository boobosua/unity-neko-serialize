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
    /// 
    /// Architecture:
    /// - Save<T>(): Memory-only operations (fast, no I/O)
    /// - SaveDirect<T>(): Direct storage operations (immediate I/O)
    /// - SaveAll(): Batch write all cached data to storage
    /// 
    /// This separation prevents recursion bugs and gives better control over I/O operations.
    /// </summary>
    public static class SaveLoadService
    {
        private const string LastSaveTimeKey = "LastSaveTime";

        private static Dictionary<string, object> s_saveData = new();
        private static readonly List<ISaveableComponent> s_saveableComponents = new();
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
            await LoadAllAsync();
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
        /// Save data to memory cache only. Use SaveAll() or SaveDirect() to persist to storage.
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_saveData[key] = data;
        }

        /// <summary>
        /// Save data directly to persistent storage immediately.
        /// </summary>
        public static void SaveDirect<T>(string key, T data)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_saveData[key] = data;
            s_dataHandler.SaveData(s_saveData);
        }

        /// <summary>
        /// Load data for the specified key.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return defaultValue;
            }
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
                        using var reader = jObj.CreateReader();
                        var serializer = UnityJsonSettings.CreateSerializer();
                        return serializer.Deserialize<T>(reader) ?? defaultValue;
                    }
                    // Fallback for other object types (should be rare with generic approach)
                    else
                    {
                        var serializer = UnityJsonSettings.CreateSerializer();
                        var jToken = JToken.FromObject(data);
                        return jToken.ToObject<T>(serializer) ?? defaultValue;
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

        /// <summary>
        /// Registers a saveable component with the manager.
        /// </summary>
        public static void RegisterSaveableComponent(ISaveableComponent saveable)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }

            if (!s_saveableComponents.Contains(saveable))
            {
                s_saveableComponents.Add(saveable);

                if (saveable.AutoLoad)
                {
                    saveable.Load();
                }
            }
        }

        /// <summary>
        /// Unregister a saveable component from the manager.
        /// </summary>
        public static void UnregisterSaveableComponent(ISaveableComponent saveable)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }

            if (s_saveableComponents.Contains(saveable))
            {
                if (saveable.AutoSave)
                {
                    saveable.Save();
                }

                s_saveableComponents.Remove(saveable);
            }
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
                AutoSaveManager.Instance.Initialize(s_settings);
            }
        }

        /// <summary>
        /// Check if data exists for the specified key.
        /// </summary>
        public static bool HasData(string key)
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
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
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            if (s_saveData.ContainsKey(key))
            {
                s_saveData.Remove(key);
                Log.Info($"[SaveLoadService] Deleted data for key: {key.Colorize(Swatch.DE)}");
            }
        }

        /// <summary>
        /// Save all cached data to persistent storage.
        /// </summary>
        public static void SaveAll()
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }

            // Save all registered components first.
            foreach (var component in s_saveableComponents)
            {
                if (component.AutoSave)
                {
                    component.Save();
                }
            }

            // Store last save time in Utc time - now safe from recursion
            Save(LastSaveTimeKey, DateTimeService.UtcNow);
            s_dataHandler.SaveData(s_saveData);
        }

        /// <summary>
        /// Load all data from persistent storage.
        /// </summary>
        public static void LoadAll()
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
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
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            try
            {
                Save(LastSaveTimeKey, DateTimeService.UtcNow);
                var dataToSave = new Dictionary<string, object>(s_saveData);
                await s_dataHandler.SaveDataAsync(dataToSave);
                Log.Info("[SaveLoadService] All data saved asynchronously");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveLoadService] Error during async save: {e.Message.Colorize(Swatch.VR)}");
            }
        }

        /// <summary>
        /// Load all data asynchronously.
        /// </summary>
        public static async Task LoadAllAsync()
        {
            if (!s_isInitialized)
            {
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            try
            {
                var loadedData = await s_dataHandler.LoadDataAsync();
                s_saveData = loadedData;
                Log.Info("[SaveLoadService] All data loaded asynchronously");
            }
            catch (Exception e)
            {
                Log.Error($"[SaveLoadService] Error during async load: {e.Message.Colorize(Swatch.VR)}");
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
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
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
                Log.Warn("[SaveLoadService] Service not initialized. Call SaveLoadService.Initialize() or SaveLoadService.InitializeAsync() first.");
                return;
            }
            s_dataHandler.DeleteSaveData();
            s_saveData.Clear();
            Log.Info("[SaveLoadService] All save data deleted");
        }

        /// <summary>
        /// Load game data from persistent storage.
        /// </summary>
        private static void LoadGame()
        {
            s_saveData = s_dataHandler.LoadData();
        }

        /// <summary>
        /// Get the last save time.
        /// </summary>
        public static DateTime GetLastSaveTime()
        {
            return Load(LastSaveTimeKey, DateTime.MinValue);
        }

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
                if (s_saveableComponents.Count > 0)
                {
                    for (int i = s_saveableComponents.Count - 1; i >= 0; i--)
                    {
                        var component = s_saveableComponents[i];
                        if (component.AutoSave)
                        {
                            component.Save();
                        }

                        s_saveableComponents.RemoveAt(i);
                    }

                    s_saveableComponents.Clear();
                }

                SaveAll();

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
                var persistedData = s_dataHandler.LoadData();
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
