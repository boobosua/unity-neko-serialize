using System;
using System.Collections.Generic;
using System.IO;
using NekoLib.Core;
using NekoLib.Extensions;
using NekoLib.Logger;
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
                Log.Info($"[SingleJsonFileHandler] Data saved to: {SavePath.Colorize(Swatch.DE)}");
            }
            catch (Exception e)
            {
                Log.Error($"[SingleJsonFileHandler] Error saving data: {e.Message.Colorize(Swatch.VR)}");
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
                    Log.Info($"[SingleJsonFileHandler] Data loaded from: {SavePath.Colorize(Swatch.DE)}");
                    return data ?? new();
                }
                else
                {
                    Log.Warn($"[SingleJsonFileHandler] No save file found, starting fresh.");
                    return new();
                }
            }
            catch (Exception e)
            {
                Log.Error($"[SingleJsonFileHandler] Error loading data: {e.Message.Colorize(Swatch.VR)}");
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
                Log.Info($"[SingleJsonFileHandler] Save file deleted.");
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
