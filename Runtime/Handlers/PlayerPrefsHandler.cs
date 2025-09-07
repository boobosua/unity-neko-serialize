using System;
using System.Collections.Generic;
using UnityEngine;

namespace NekoSerialize
{
    /// <summary>
    /// Handles saving and loading data using PlayerPrefs.
    /// </summary>
    public class PlayerPrefsHandler : SaveDataHandler
    {
        public PlayerPrefsHandler(SaveLoadSettings settings) : base(settings) { }

        /// <summary>
        /// Saves the given data to PlayerPrefs.
        /// </summary>
        public override void SaveData(Dictionary<string, object> data)
        {
            try
            {
                var json = SerializeData(data);
                PlayerPrefs.SetString(_settings.PlayerPrefsKey, json);
                PlayerPrefs.Save();
                Debug.Log($"[PlayerPrefsHandler] Data saved to PlayerPrefs with key: {_settings.PlayerPrefsKey}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsHandler] Error saving data: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the saved data from PlayerPrefs.
        /// </summary>
        public override Dictionary<string, object> LoadData()
        {
            try
            {
                if (PlayerPrefs.HasKey(_settings.PlayerPrefsKey))
                {
                    var json = PlayerPrefs.GetString(_settings.PlayerPrefsKey);
                    var data = DeserializeData<Dictionary<string, object>>(json);
                    Debug.Log($"[PlayerPrefsHandler] Data loaded from PlayerPrefs with key: {_settings.PlayerPrefsKey}");
                    return data ?? new();
                }
                else
                {
                    Debug.LogWarning($"[PlayerPrefsHandler] No data found in PlayerPrefs save found, starting fresh.");
                    return new();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerPrefsHandler] Error loading data: {e.Message}");
                return new();
            }
        }

        /// <summary>
        /// Deletes the saved data from PlayerPrefs.
        /// </summary>
        public override void DeleteSaveData()
        {
            if (PlayerPrefs.HasKey(_settings.PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(_settings.PlayerPrefsKey);
                PlayerPrefs.Save();
                Debug.Log($"[PlayerPrefsHandler] Data deleted from PlayerPrefs.");
            }
        }

        /// <summary>
        /// Checks if there is saved data in PlayerPrefs.
        /// </summary>
        public override bool SaveDataExists()
        {
            return PlayerPrefs.HasKey(_settings.PlayerPrefsKey);
        }
    }
}
