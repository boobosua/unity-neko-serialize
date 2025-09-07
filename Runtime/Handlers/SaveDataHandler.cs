using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NekoSerialize
{
    public abstract class SaveDataHandler
    {
        protected SaveLoadSettings _settings;

        public SaveDataHandler(SaveLoadSettings settings)
        {
            _settings = settings;
        }

        public abstract void SaveData(Dictionary<string, object> data);
        public abstract Dictionary<string, object> LoadData();
        public abstract void DeleteSaveData();
        public abstract bool SaveDataExists();

        /// <summary>
        /// Asynchronously saves the game data without blocking the main thread.
        /// </summary>
        public virtual async Task SaveDataAsync(Dictionary<string, object> data)
        {
            await Task.Run(() => SaveData(data));
        }

        /// <summary>
        /// Asynchronously loads the game data without blocking the main thread.
        /// </summary>
        public virtual async Task<Dictionary<string, object>> LoadDataAsync()
        {
            return await Task.Run(() => LoadData());
        }

        /// <summary>
        /// Serializes the given data object to a JSON string.
        /// </summary>
        protected string SerializeData(object data)
        {
            var jsonSettings = UnityJsonSettings.CreateSettings();

            // Override formatting based on settings
            jsonSettings.Formatting = _settings.PrettyPrintJson ? Formatting.Indented : Formatting.None;
            jsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSettings.NullValueHandling = NullValueHandling.Ignore;

            var json = JsonConvert.SerializeObject(data, jsonSettings);

            if (_settings.UseEncryption)
            {
                json = EncryptString(json);
            }

            return json;
        }

        /// <summary>
        /// Deserializes the given JSON string to an object of type T.
        /// </summary>
        protected T DeserializeData<T>(string json)
        {
            if (_settings.UseEncryption)
            {
                json = DecryptString(json);
            }

            return JsonConvert.DeserializeObject<T>(json, UnityJsonSettings.CreateSettings());
        }

        /// <summary>
        /// Encrypts the given string using the specified encryption key.
        /// </summary>
        private string EncryptString(string text)
        {
            var key = _settings.EncryptionKey;
            var result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                result.Append((char)(text[i] ^ key[i % key.Length]));
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(result.ToString()));
        }

        /// <summary>
        /// Decrypts the given string using the specified encryption key.
        /// </summary>
        private string DecryptString(string encryptedText)
        {
            var key = _settings.EncryptionKey;
            var bytes = Convert.FromBase64String(encryptedText);
            var text = Encoding.UTF8.GetString(bytes);
            var result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                result.Append((char)(text[i] ^ key[i % key.Length]));
            }

            return result.ToString();
        }
    }
}
