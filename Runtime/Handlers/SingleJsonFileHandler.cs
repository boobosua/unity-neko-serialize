using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Handles saving and loading data to a single JSON file.
    /// </summary>
    public class SingleJsonFileHandler : SaveDataHandler
    {
        public SingleJsonFileHandler(SaveLoadSettings settings) : base(settings) { }

        private string SavePath => Path.Combine(Application.persistentDataPath, _settings.FileName);

        /// <summary>
        /// Saves the given data to a JSON file.
        /// </summary>
        public override void SaveData(Dictionary<string, object> data)
        {
            try
            {
                var json = SerializeData(data);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SingleJsonFileHandler] Data saved to: {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SingleJsonFileHandler] Error saving data: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the saved data from a JSON file.
        /// </summary>
        public override Dictionary<string, object> LoadData()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    var data = DeserializeData<Dictionary<string, object>>(json);
                    Debug.Log($"[SingleJsonFileHandler] Data loaded from: {SavePath}");
                    return data ?? new();
                }
                else
                {
                    Debug.LogWarning($"[SingleJsonFileHandler] No save file found, starting fresh.");
                    return new();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SingleJsonFileHandler] Error loading data: {e.Message}");
                return new();
            }
        }

        /// <summary>
        /// Deletes the saved data file.
        /// </summary>
        public override void DeleteSaveData()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log($"[SingleJsonFileHandler] Save file deleted.");
            }
        }

        /// <summary>
        /// Checks if the save data file exists.
        /// </summary>
        public override bool SaveDataExists()
        {
            return File.Exists(SavePath);
        }
    }
}
