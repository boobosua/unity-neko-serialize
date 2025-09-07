using System.Threading.Tasks;

namespace NekoSerialize
{
    public static class NSR
    {
        /// <summary>
        /// Saves the specified data to the save service.
        /// </summary>
        public static void Save<T>(string key, T data, bool autoSave = true)
        {
            SaveLoadService.Save(key, data, autoSave);
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
        /// Loads all data from persistent storage.
        /// </summary>
        public static void LoadAll()
        {
            SaveLoadService.LoadAll();
        }

        /// <summary>
        /// Saves all data asynchronously.
        /// </summary>
        public static async Task SaveAllAsync()
        {
            await SaveLoadService.SaveAllAsync();
        }

        /// <summary>
        /// Loads all data asynchronously.
        /// </summary>
        public static async Task LoadAllAsync()
        {
            await SaveLoadService.LoadAllAsync();
        }
    }
}
