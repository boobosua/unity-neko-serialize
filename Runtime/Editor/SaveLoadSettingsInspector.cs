#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NekoSerialize
{
    [CustomEditor(typeof(SaveLoadSettings))]
    public class SaveLoadSettingsInspector : Editor
    {
        private SerializedProperty _saveLocationProp;
        private SerializedProperty _fileNameProp;
        private SerializedProperty _folderNameProp;
        private SerializedProperty _playerPrefsKeyProp;
        private SerializedProperty _autoSaveIntervalProp;
        private SerializedProperty _autoSaveOnPauseProp;
        private SerializedProperty _autoSaveOnFocusLostProp;
        private SerializedProperty _useEncryptionProp;
        private SerializedProperty _encryptionKeyProp;
        private SerializedProperty _prettyPrintJsonProp;

        private void OnEnable()
        {
            _saveLocationProp = serializedObject.FindProperty("<SaveLocation>k__BackingField");
            _fileNameProp = serializedObject.FindProperty("<FileName>k__BackingField");
            _folderNameProp = serializedObject.FindProperty("<FolderName>k__BackingField");
            _playerPrefsKeyProp = serializedObject.FindProperty("<PlayerPrefsKey>k__BackingField");
            _autoSaveIntervalProp = serializedObject.FindProperty("<AutoSaveInterval>k__BackingField");
            _autoSaveOnPauseProp = serializedObject.FindProperty("<AutoSaveOnPause>k__BackingField");
            _autoSaveOnFocusLostProp = serializedObject.FindProperty("<AutoSaveOnFocusLost>k__BackingField");
            _useEncryptionProp = serializedObject.FindProperty("<UseEncryption>k__BackingField");
            _encryptionKeyProp = serializedObject.FindProperty("<EncryptionKey>k__BackingField");
            _prettyPrintJsonProp = serializedObject.FindProperty("<PrettyPrintJson>k__BackingField");
        }

        public override void OnInspectorGUI()
        {
            // Don't draw the default inspector (this hides the script field)
            // base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);

            // Save Location
            EditorGUILayout.PropertyField(_saveLocationProp, new GUIContent("Save Location"));

            var saveLocation = (SaveLocation)_saveLocationProp.enumValueIndex;

            // Conditional settings based on save location
            if (saveLocation == SaveLocation.JsonFile)
            {
                EditorGUILayout.PropertyField(_fileNameProp, new GUIContent("File Name"));
                EditorGUILayout.PropertyField(_folderNameProp, new GUIContent("Folder Name"));
            }

            if (saveLocation == SaveLocation.PlayerPrefs)
            {
                EditorGUILayout.PropertyField(_playerPrefsKeyProp, new GUIContent("PlayerPrefs Key"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Save Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoSaveIntervalProp, new GUIContent("Auto Save Interval"));
            EditorGUILayout.PropertyField(_autoSaveOnPauseProp, new GUIContent("Auto Save On Pause"));
            EditorGUILayout.PropertyField(_autoSaveOnFocusLostProp, new GUIContent("Auto Save On Focus Lost"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Security", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useEncryptionProp, new GUIContent("Use Encryption"));

            if (_useEncryptionProp.boolValue)
            {
                EditorGUILayout.PropertyField(_encryptionKeyProp, new GUIContent("Encryption Key"));
                EditorGUILayout.HelpBox("Keep your encryption key secure! Consider using environment variables in production.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Formatting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_prettyPrintJsonProp, new GUIContent("Pretty Print JSON"));

            // Show hints based on save location
            EditorGUILayout.Space();
            string hint = saveLocation switch
            {
                SaveLocation.PlayerPrefs => "PlayerPrefs: Data saved to registry (Windows) or plist (Mac). Best for simple settings.",
                SaveLocation.JsonFile => "JSON File: Data saved to persistent data path as JSON. Best for complex save data.",
                _ => ""
            };

            if (!string.IsNullOrEmpty(hint))
            {
                EditorGUILayout.HelpBox(hint, MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif