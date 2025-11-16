using System;
using System.Threading.Tasks;

namespace NekoSerialize
{
    public static class NSR
    {
        /// <summary>
        /// Initializes the NekoSerialize SaveLoadService.
        /// </summary>
        public static void Initialize()
        {
            SaveLoadService.Initialize();
        }

        /// <summary>
        /// Asynchronously initializes the NekoSerialize SaveLoadService.
        /// </summary>
        public static async Task InitializeAsync()
        {
            await SaveLoadService.InitializeAsync();
        }

        /// <summary>
        /// Saves the specified data to memory cache. Use SaveAll() to persist to storage.
        /// </summary>
        public static void Save<T>(string key, T data)
        {
            SaveLoadService.Save(key, data);
        }

        /// <summary>
        /// Loads the specified data from the save service.
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            return SaveLoadService.Load(key, defaultValue);
        }

        /// <summary>
        /// Checks if the specified data exists in the save service.
        /// </summary>
        public static bool Exists(string key)
        {
            return SaveLoadService.HasData(key);
        }

        /// <summary>
        /// Deletes the specified data from the save service.
        /// </summary>
        public static void Delete(string key)
        {
            SaveLoadService.DeleteData(key);
        }

        /// <summary>
        /// Clears all data from the save service.
        /// </summary>
        public static void ClearAll()
        {
            SaveLoadService.DeleteAllData();
        }

        /// <summary>
        /// Saves all data to persistent storage.
        /// </summary>
        public static void SaveAll()
        {
            SaveLoadService.SaveAll();
        }

        /// <summary>
        /// Saves all data asynchronously.
        /// </summary>
        public static async Task SaveAllAsync()
        {
            await SaveLoadService.SaveAllAsync();
        }

        /// <summary>
        /// Gets the last save time.
        /// </summary>
        public static DateTime LastSaveTime
        {
            get { return SaveLoadService.GetLastSaveTime(); }
        }
    }
}
