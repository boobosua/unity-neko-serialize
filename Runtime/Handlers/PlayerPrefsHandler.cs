using System;
using System.Collections.Generic;
using NekoLib.Core;
using NekoLib.Extensions;
using NekoLib.Logger;
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
        public override void WriteData(Dictionary<string, object> data)
        {
            try
            {
                var json = SerializeData(data);
                PlayerPrefs.SetString(_settings.PlayerPrefsKey, json);
                PlayerPrefs.Save();
                Log.Info($"[PlayerPrefsHandler] Data saved to PlayerPrefs with key: {_settings.PlayerPrefsKey.Colorize(Swatch.DE)}");
            }
            catch (Exception e)
            {
                Log.Error($"[PlayerPrefsHandler] Error saving data: {e.Message.Colorize(Swatch.VR)}");
            }
        }

        /// <summary>
        /// Loads the saved data from PlayerPrefs.
        /// </summary>
        public override Dictionary<string, object> ReadData()
        {
            try
            {
                if (PlayerPrefs.HasKey(_settings.PlayerPrefsKey))
                {
                    var json = PlayerPrefs.GetString(_settings.PlayerPrefsKey);
                    var data = DeserializeData<Dictionary<string, object>>(json);
                    Log.Info($"[PlayerPrefsHandler] Data loaded from PlayerPrefs with key: {_settings.PlayerPrefsKey.Colorize(Swatch.DE)}");
                    return data ?? new();
                }
                else
                {
                    Log.Warn($"[PlayerPrefsHandler] No data found in PlayerPrefs save found, starting fresh.");
                    return new();
                }
            }
            catch (Exception e)
            {
                Log.Error($"[PlayerPrefsHandler] Error loading data: {e.Message.Colorize(Swatch.VR)}");
                return new();
            }
        }

        /// <summary>
        /// Deletes the saved data from PlayerPrefs.
        /// </summary>
        public override void DeleteData()
        {
            if (PlayerPrefs.HasKey(_settings.PlayerPrefsKey))
            {
                PlayerPrefs.DeleteKey(_settings.PlayerPrefsKey);
                PlayerPrefs.Save();
                Log.Info($"[PlayerPrefsHandler] Data deleted from PlayerPrefs.");
            }
        }

        /// <summary>
        /// Checks if there is saved data in PlayerPrefs.
        /// </summary>
        public override bool DataExists()
        {
            return PlayerPrefs.HasKey(_settings.PlayerPrefsKey);
        }
    }
}
