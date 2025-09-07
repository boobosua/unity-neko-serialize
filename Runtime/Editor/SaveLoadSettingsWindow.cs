#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace NekoSerialize
{
    public class SaveLoadSettingsWindow : EditorWindow
    {
        private SaveLoadSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Neko Indie/Save Load Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveLoadSettingsWindow>("Save Load Settings");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");

            if (_settings != null)
            {
                _serializedSettings = new SerializedObject(_settings);
            }
        }

        private void CreateDefaultSettings()
        {
            _settings = CreateInstance<SaveLoadSettings>();

            // Create the directory structure for library use
            string pluginPath = "Assets/Plugins";
            string nekoSerializePath = "Assets/Plugins/NekoSerialize";
            string resourcesPath = "Assets/Plugins/NekoSerialize/Resources";

            if (!AssetDatabase.IsValidFolder(pluginPath))
                AssetDatabase.CreateFolder("Assets", "Plugins");

            if (!AssetDatabase.IsValidFolder(nekoSerializePath))
                AssetDatabase.CreateFolder("Assets/Plugins", "NekoSerialize");

            if (!AssetDatabase.IsValidFolder(resourcesPath))
                AssetDatabase.CreateFolder("Assets/Plugins/NekoSerialize", "Resources");

            // Save the settings asset
            string assetPath = "Assets/Plugins/NekoSerialize/Resources/SaveLoadSettings.asset";
            AssetDatabase.CreateAsset(_settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SaveLoadSettings] Created default settings at: {assetPath}");
        }

        private void ReloadScene()
        {
            if (Application.isPlaying)
            {
                var currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.name);
            }
        }

        private void OnGUI()
        {
            if (_settings == null || _serializedSettings == null)
            {
                EditorGUILayout.HelpBox("SaveLoadSettings not found. Create settings for this project.", MessageType.Warning);
                EditorGUILayout.Space();

                if (GUILayout.Button("Create New Save Load Settings", GUILayout.Height(30)))
                {
                    CreateDefaultSettings();
                    LoadSettings();
                }
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(5);

            // Settings UI
            _serializedSettings.Update();

            DrawSettingsSection();

            if (_serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();

                // Refresh the service if it's initialized
                if (Application.isPlaying)
                {
                    SaveLoadService.RefreshSettings();
                }
            }

            EditorGUILayout.Space();
            DrawUtilityButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);

            // Save Location
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<SaveLocation>k__BackingField"), new GUIContent("Save Location"));

            var saveLocation = (SaveLocation)_serializedSettings.FindProperty("<SaveLocation>k__BackingField").enumValueIndex;

            // Conditional settings based on save location
            if (saveLocation == SaveLocation.JsonFile)
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<FileName>k__BackingField"), new GUIContent("File Name"));
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<FolderName>k__BackingField"), new GUIContent("Folder Name"));
            }

            if (saveLocation == SaveLocation.PlayerPrefs)
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<PlayerPrefsKey>k__BackingField"), new GUIContent("PlayerPrefs Key"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Save Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<AutoSaveInterval>k__BackingField"), new GUIContent("Auto Save Interval"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<AutoSaveOnPause>k__BackingField"), new GUIContent("Auto Save On Pause"));
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<AutoSaveOnFocusLost>k__BackingField"), new GUIContent("Auto Save On Focus Lost"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Security", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<UseEncryption>k__BackingField"), new GUIContent("Use Encryption"));

            if (_settings.UseEncryption)
            {
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<EncryptionKey>k__BackingField"), new GUIContent("Encryption Key"));
                EditorGUILayout.HelpBox("Keep your encryption key secure! Consider using environment variables in production.", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Formatting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedSettings.FindProperty("<PrettyPrintJson>k__BackingField"), new GUIContent("Pretty Print JSON"));
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open Settings File"))
            {
                Selection.activeObject = _settings;
                EditorGUIUtility.PingObject(_settings);
            }

            if (GUILayout.Button("Refresh from File"))
            {
                LoadSettings();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Save All"))
                {
                    NSR.SaveAll();
                }

                if (GUILayout.Button("Load All"))
                {
                    NSR.LoadAll();
                    ReloadScene();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Delete All & Reload"))
                {
                    if (EditorUtility.DisplayDialog("Delete All Save Data",
                        "Are you sure you want to delete all save data and reload the scene? This cannot be undone.",
                        "Delete & Reload", "Cancel"))
                    {
                        NSR.ClearAll();
                        ReloadScene();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime controls are available when the game is playing.", MessageType.Info);
            }
        }
    }
}
#endif