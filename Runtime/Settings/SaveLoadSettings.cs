using UnityEngine;

namespace NekoSerialize
{
    [CreateAssetMenu(fileName = "SaveLoadSettings", menuName = "Neko Indie/Serialize/Save Load Settings")]
    public class SaveLoadSettings : ScriptableObject
    {
        [Header("Save Settings")]
        [field: SerializeField, Tooltip("The location where save data will be stored.")]
        public SaveLocation SaveLocation { get; private set; } = SaveLocation.PlayerPrefs;

        [field: SerializeField, Tooltip("The name of the file to save data to.")]
        public string FileName { get; private set; } = "GameData.json";

        [field: SerializeField, Tooltip("The name of the folder to save data to.")]
        public string FolderName { get; private set; } = "SaveData";

        [field: SerializeField, Tooltip("The key used to store data in PlayerPrefs.")]
        public string PlayerPrefsKey { get; private set; } = "GameSaveData";

        [Header("Security")]
        [field: SerializeField, Tooltip("Whether to use encryption for save data.")]
        public bool UseEncryption { get; private set; } = false;

        [field: SerializeField, Tooltip("The encryption key used to secure save data.")]
        public string EncryptionKey { get; private set; } = "DefaultEncryptionKey";

        [Header("Formatting")]
        [field: SerializeField, Tooltip("Whether to pretty print JSON data.")]
        public bool PrettyPrintJson { get; private set; } = true;

        [Header("Auto-Save")]
        [field: SerializeField, Tooltip("Whether to automatically save game data when the game loses focus.")]
        public bool AutoSaveOnFocusLost { get; private set; } = false;

        [field: SerializeField, Min(0f), Tooltip("The interval at which to automatically save game data.")]
        public float AutoSaveInterval { get; private set; } = 0f; // 0 for disable.
    }
}
