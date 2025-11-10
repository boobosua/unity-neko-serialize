using Newtonsoft.Json;

namespace NekoSerialize
{
    /// <summary>
    /// Utility class to configure JsonSerializer with Unity type converters
    /// </summary>
    public static class UnityJsonSettings
    {
        /// <summary>
        /// Creates JsonSerializerSettings with all Unity type converters configured
        /// </summary>
        /// <returns>JsonSerializerSettings with Unity converters</returns>
        public static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                // Prevents issues like: "Self referencing loop detected for property 'normalized' with type 'UnityEngine.Vector3'"
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // Add Unity type converters
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new QuaternionConverter());
            settings.Converters.Add(new TransformDataConverter());

            return settings;
        }

        /// <summary>
        /// Creates a JsonSerializer with all Unity type converters configured
        /// </summary>
        /// <returns>JsonSerializer with Unity converters</returns>
        public static JsonSerializer CreateSerializer()
        {
            var serializer = JsonSerializer.Create(CreateSettings());
            return serializer;
        }

        /// <summary>
        /// Serializes an object to JSON string using Unity converters
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>JSON string</returns>
        public static string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, CreateSettings());
        }

        /// <summary>
        /// Deserializes a JSON string to an object using Unity converters
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized object</returns>
        public static T DeserializeObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, CreateSettings());
        }

        /// <summary>
        /// Deserializes a JSON string to an object using Unity converters
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="type">Type to deserialize to</param>
        /// <returns>Deserialized object</returns>
        public static object DeserializeObject(string json, System.Type type)
        {
            return JsonConvert.DeserializeObject(json, type, CreateSettings());
        }
    }
}
