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
        protected JsonSerializerSettings _jsonSettings;

        public SaveDataHandler(SaveLoadSettings settings)
        {
            _settings = settings;
            _jsonSettings = JsonSerializerUtils.GetSettings();
            _jsonSettings.Formatting = _settings.PrettyPrintJson ? Formatting.Indented : Formatting.None;
        }

        public abstract void WriteData(Dictionary<string, object> data);
        public abstract Dictionary<string, object> ReadData();
        public abstract void DeleteData();
        public abstract bool DataExists();

        /// <summary>
        /// Asynchronously saves the game data without blocking the main thread.
        /// </summary>
        public virtual async Task WriteDataAsync(Dictionary<string, object> data)
        {
            await Task.Run(() => WriteData(data));
        }

        /// <summary>
        /// Asynchronously loads the game data without blocking the main thread.
        /// </summary>
        public virtual async Task<Dictionary<string, object>> ReadDataAsync()
        {
            return await Task.Run(() => ReadData());
        }

        /// <summary>
        /// Serializes the given data object to a JSON string.
        /// </summary>
        protected string SerializeData(object data)
        {
            var json = JsonConvert.SerializeObject(data, _jsonSettings);

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

            return JsonConvert.DeserializeObject<T>(json, _jsonSettings) ?? default;
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
