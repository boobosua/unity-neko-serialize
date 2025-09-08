#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NekoSerialize
{
    public class SaveLoadStorageWindow : EditorWindow
    {
        #region Window Management

        [MenuItem("Window/Neko Indie/Save Load Storage")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveLoadStorageWindow>("Save Load Storage");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private Dictionary<string, object> _editedData = new Dictionary<string, object>();
        private bool _hasUnsavedChanges = false;

        // Performance caching
        private Dictionary<string, object> _cachedSaveData;
        private float _lastRefreshTime;
        private const float REFRESH_COOLDOWN = 0.5f; // Prevent excessive refreshes

        // Data processing limits
        private const int MAX_EDITED_ITEMS = 50;
        private const int MAX_RENDER_DEPTH = 8;
        private const int MAX_ARRAY_ELEMENTS = 20;

        // Pagination
        private int _itemsPerPage = 10;
        private int _currentPage = 0;
        private string _searchFilter = "";

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _evenRowStyle;
        private GUIStyle _oddRowStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized = false;

        // Icons
        private Texture2D _checkIcon;
        private Texture2D _warningIcon;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            // Reset state on enable to prevent bugs after scene reload
            _cachedSaveData = null;
            _editedData = new Dictionary<string, object>();
            _foldoutStates = new Dictionary<string, bool>();
            _hasUnsavedChanges = false;
            _stylesInitialized = false;

            RefreshDataIfNeeded();
            LoadIcons();
        }

        private void OnDisable()
        {
            // Clean up to prevent memory leaks
            _cachedSaveData = null;
            _editedData?.Clear();
            _foldoutStates?.Clear();
        }

        private void OnGUI()
        {
            try
            {
                InitializeStyles();

                DrawHeader();
                DrawToolbar();

                if (!SaveLoadService.IsInitialized)
                {
                    EditorGUILayout.HelpBox("Save Load Service is not initialized. Click 'Initialize' to load data.", MessageType.Warning);
                    if (GUILayout.Button("Initialize"))
                    {
                        SaveLoadService.Initialize();
                        RefreshDataIfNeeded();
                    }
                    return;
                }

                DrawContent();
                DrawFooter();
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Critical error in Save Load Storage Window: {ex.Message}\n\nClick 'Refresh' to recover.", MessageType.Error);
                if (GUILayout.Button("Refresh", GUILayout.Height(30)))
                {
                    ForceRefreshData();
                }
                Debug.LogException(ex);
            }
        }

        #endregion

        #region GUI Drawing

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(_headerStyle))
            {
                GUILayout.Label("Save Load Storage Manager", _headerStyle);
                GUILayout.FlexibleSpace();

                if (_hasUnsavedChanges)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("● Unsaved Changes", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
            }

            GUILayout.Space(5);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Search
                GUILayout.Label("Search:", GUILayout.Width(50));
                var newSearch = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
                if (newSearch != _searchFilter)
                {
                    _searchFilter = newSearch;
                    _currentPage = 0;
                    // Clear cache when search changes to force refresh
                    _cachedSaveData = null;
                }

                GUILayout.FlexibleSpace();

                // Pagination
                GUILayout.Label("Items per page:", GUILayout.Width(90));
                _itemsPerPage = EditorGUILayout.IntSlider(_itemsPerPage, 5, 50, GUILayout.Width(100));

                GUILayout.Space(10);

                // Refresh button
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ForceRefreshData();
                }

                // Recovery button for when window gets bugged
                if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    ResetWindowState();
                }

                // Clear all button
                GUI.color = new Color(1f, 0.4f, 0.4f); // Light red, more visible
                if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Data",
                        "Are you sure you want to clear all save data? This action cannot be undone.",
                        "Yes, Clear All", "Cancel"))
                    {
                        SaveLoadService.DeleteAllData();
                        ForceRefreshData();
                    }
                }
                GUI.color = Color.white;
            }
        }

        private void DrawContent()
        {
            var saveData = GetCachedSaveData();
            if (saveData == null)
            {
                EditorGUILayout.HelpBox("Unable to load save data. Click 'Refresh' to try again.", MessageType.Warning);
                if (GUILayout.Button("Refresh"))
                {
                    ForceRefreshData();
                }
                return;
            }

            var filteredData = FilterData(saveData);
            var pagedData = GetPagedData(filteredData);

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                if (pagedData.Count == 0)
                {
                    DrawEmptyState();
                    return;
                }

                DrawDataItems(pagedData);
            }

            DrawPagination(filteredData.Count);
        }

        private void DrawDataItems(Dictionary<string, object> data)
        {
            var keys = data.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var value = data[key];

                // Zebra stripes
                var isEven = i % 2 == 0;
                var rowStyle = isEven ? _evenRowStyle : _oddRowStyle;

                using (new EditorGUILayout.VerticalScope(rowStyle))
                {
                    DrawDataItem(key, value, i);
                }

                GUILayout.Space(2);
            }
        }

        private void DrawDataItem(string key, object value, int index)
        {
            // Get foldout state
            if (!_foldoutStates.ContainsKey(key))
                _foldoutStates[key] = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Foldout with save status icon
                var isPersisted = SaveLoadService.IsDataPersisted(key);
                var icon = isPersisted ? _checkIcon : _warningIcon;
                var iconContent = new GUIContent(icon, isPersisted ? "Data is saved to persistent storage" : "Data is only in memory");

                GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(16));

                _foldoutStates[key] = EditorGUILayout.Foldout(_foldoutStates[key], $"{key} ({GetValueType(value)})", true);

                GUILayout.FlexibleSpace();

                // Delete button
                GUI.color = new Color(1f, 0.4f, 0.4f); // Light red, more visible
                if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
                {
                    if (EditorUtility.DisplayDialog("Delete Save Data",
                        $"Are you sure you want to delete save data for key '{key}'?",
                        "Delete", "Cancel"))
                    {
                        SaveLoadService.DeleteData(key);
                        ForceRefreshData();
                        return;
                    }
                }
                GUI.color = Color.white;
            }

            // Draw content if expanded
            if (_foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                DrawValueEditor(key, value);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawValueEditor(string key, object value)
        {
            try
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label($"Type: {GetValueType(value)}", EditorStyles.miniLabel);

                    EditorGUI.indentLevel++;
                    var newValue = DrawInspectorField(key, value, 0);
                    EditorGUI.indentLevel--;

                    if (!Equals(newValue, value))
                    {
                        // Prevent excessive edited data
                        if (_editedData.Count >= MAX_EDITED_ITEMS)
                        {
                            EditorGUILayout.HelpBox($"Maximum of {MAX_EDITED_ITEMS} edited items reached. Please apply or discard changes.", MessageType.Warning);
                            return;
                        }

                        _editedData[key] = newValue;
                        _hasUnsavedChanges = true;
                    }
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Error displaying data: {ex.Message}", MessageType.Error);
            }
        }

        private object DrawInspectorField(string fieldName, object value, int depth = 0)
        {
            // Prevent infinite recursion by limiting depth
            if (depth > MAX_RENDER_DEPTH)
            {
                EditorGUILayout.LabelField(fieldName, $"[Max depth {MAX_RENDER_DEPTH} reached]");
                return value;
            }

            if (value == null)
            {
                EditorGUILayout.LabelField(fieldName, "null");
                return value;
            }

            // Don't use _editedData for nested fields, only for top-level keys
            var currentValue = (depth == 0 && _editedData.ContainsKey(fieldName)) ? _editedData[fieldName] : value;

            try
            {
                // Handle different types like Unity Inspector
                switch (currentValue)
                {
                    case int intVal:
                        return EditorGUILayout.IntField(fieldName, intVal);

                    case float floatVal:
                        return EditorGUILayout.FloatField(fieldName, floatVal);

                    case double doubleVal:
                        return EditorGUILayout.DoubleField(fieldName, doubleVal);

                    case bool boolVal:
                        return EditorGUILayout.Toggle(fieldName, boolVal);

                    case string stringVal:
                        // Limit string length for performance
                        var displayStr = stringVal ?? "";
                        if (displayStr.Length > 1000)
                        {
                            EditorGUILayout.LabelField(fieldName, $"[Large string: {displayStr.Length} chars - Click to edit]");
                            if (GUILayout.Button("Edit Large String"))
                            {
                                // Could open a separate editor window for large strings
                                EditorGUIUtility.systemCopyBuffer = displayStr;
                                EditorUtility.DisplayDialog("Large String", "String copied to clipboard for editing in external editor.", "OK");
                            }
                            return stringVal;
                        }
                        return EditorGUILayout.TextField(fieldName, displayStr);

                    case Vector2 vec2Val:
                        return EditorGUILayout.Vector2Field(fieldName, vec2Val);

                    case Vector3 vec3Val:
                        return EditorGUILayout.Vector3Field(fieldName, vec3Val);

                    case Vector4 vec4Val:
                        return EditorGUILayout.Vector4Field(fieldName, vec4Val);

                    case Color colorVal:
                        return EditorGUILayout.ColorField(fieldName, colorVal);

                    case Rect rectVal:
                        return EditorGUILayout.RectField(fieldName, rectVal);

                    case AnimationCurve curveVal:
                        return EditorGUILayout.CurveField(fieldName, curveVal);

                    case JObject jObject:
                        return DrawJObjectFields(fieldName, jObject, depth + 1);

                    case JArray jArray:
                        return DrawJArrayFields(fieldName, jArray, depth + 1);

                    default:
                        // For complex objects, try to deserialize to a more specific type
                        if (value is JObject jObj)
                        {
                            return DrawJObjectFields(fieldName, jObj, depth + 1);
                        }
                        else
                        {
                            // Fallback: display as read-only with size info
                            var valueStr = currentValue.ToString();
                            if (valueStr.Length > 100)
                            {
                                valueStr = valueStr.Substring(0, 97) + "...";
                            }
                            EditorGUILayout.LabelField(fieldName, valueStr);
                            return currentValue;
                        }
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(fieldName, $"[Error: {ex.Message}]");
                return value;
            }
        }

        private object DrawJObjectFields(string parentName, JObject jObject, int depth = 0)
        {
            // Prevent infinite recursion
            if (depth > MAX_RENDER_DEPTH)
            {
                EditorGUILayout.LabelField(parentName, $"[Max depth {MAX_RENDER_DEPTH} reached for object]");
                return jObject;
            }

            var newJObject = new JObject();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(parentName, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                var propertyCount = 0;
                foreach (var property in jObject.Properties())
                {
                    // Limit number of properties to prevent UI freeze
                    if (propertyCount >= 20)
                    {
                        EditorGUILayout.LabelField("...", $"[{jObject.Properties().Count() - 20} more properties - expand individually]");
                        break;
                    }

                    var fieldName = property.Name;
                    var fieldValue = property.Value;

                    try
                    {
                        object newValue = DrawJTokenField(fieldName, fieldValue, depth + 1);
                        newJObject[fieldName] = JToken.FromObject(newValue);
                    }
                    catch (Exception ex)
                    {
                        EditorGUILayout.LabelField(fieldName, $"[Error: {ex.Message}]");
                        newJObject[fieldName] = fieldValue; // Keep original value on error
                    }

                    propertyCount++;
                }
                EditorGUI.indentLevel--;
            }

            return newJObject;
        }

        private object DrawJArrayFields(string parentName, JArray jArray, int depth = 0)
        {
            // Prevent infinite recursion
            if (depth > MAX_RENDER_DEPTH)
            {
                EditorGUILayout.LabelField(parentName, $"[Max depth {MAX_RENDER_DEPTH} reached for array]");
                return jArray;
            }

            var newJArray = new JArray();
            var elementCount = Mathf.Min(jArray.Count, MAX_ARRAY_ELEMENTS);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{parentName} (Array - {jArray.Count} items)", EditorStyles.boldLabel);

                if (jArray.Count > MAX_ARRAY_ELEMENTS)
                {
                    EditorGUILayout.HelpBox($"Showing first {MAX_ARRAY_ELEMENTS} of {jArray.Count} elements for performance.", MessageType.Info);
                }

                EditorGUI.indentLevel++;
                for (int i = 0; i < elementCount; i++)
                {
                    var element = jArray[i];
                    try
                    {
                        var newValue = DrawJTokenField($"Element {i}", element, depth + 1);
                        newJArray.Add(JToken.FromObject(newValue));
                    }
                    catch (Exception ex)
                    {
                        EditorGUILayout.LabelField($"Element {i}", $"[Error: {ex.Message}]");
                        newJArray.Add(element); // Keep original value on error
                    }
                }

                // Add remaining elements without modification
                for (int i = elementCount; i < jArray.Count; i++)
                {
                    newJArray.Add(jArray[i]);
                }

                EditorGUI.indentLevel--;
            }

            return newJArray;
        }

        private object DrawJTokenField(string fieldName, JToken token, int depth = 0)
        {
            // Prevent infinite recursion
            if (depth > MAX_RENDER_DEPTH)
            {
                EditorGUILayout.LabelField(fieldName, "[Max depth reached]");
                return token.Value<object>();
            }

            try
            {
                switch (token.Type)
                {
                    case JTokenType.Integer:
                        return EditorGUILayout.IntField(fieldName, token.Value<int>());

                    case JTokenType.Float:
                        return EditorGUILayout.FloatField(fieldName, token.Value<float>());

                    case JTokenType.Boolean:
                        return EditorGUILayout.Toggle(fieldName, token.Value<bool>());

                    case JTokenType.String:
                        var str = token.Value<string>() ?? "";
                        if (str.Length > 500)
                        {
                            EditorGUILayout.LabelField(fieldName, $"[Large string: {str.Length} chars]");
                            return str;
                        }
                        return EditorGUILayout.TextField(fieldName, str);

                    case JTokenType.Object:
                        return DrawJObjectFields(fieldName, (JObject)token, depth + 1);

                    case JTokenType.Array:
                        return DrawJArrayFields(fieldName, (JArray)token, depth + 1);

                    default:
                        var valueStr = token.ToString();
                        if (valueStr.Length > 100)
                        {
                            valueStr = valueStr.Substring(0, 97) + "...";
                        }
                        EditorGUILayout.LabelField(fieldName, valueStr);
                        return token.Value<object>();
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(fieldName, $"[Error: {ex.Message}]");
                return token.Value<object>();
            }
        }

        private void DrawEmptyState()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("No save data found", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Save some data in your game to see it here", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawPagination(int totalItems)
        {
            if (totalItems <= _itemsPerPage) return;

            var totalPages = Mathf.CeilToInt((float)totalItems / _itemsPerPage);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(_currentPage <= 0);
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    _currentPage--;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.Label($"Page {_currentPage + 1} of {totalPages}", EditorStyles.toolbarButton);

                EditorGUI.BeginDisabledGroup(_currentPage >= totalPages - 1);
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    _currentPage++;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                GUILayout.Label($"Total: {totalItems} items", EditorStyles.toolbarButton);
            }
        }

        private void DrawFooter()
        {
            if (!_hasUnsavedChanges) return;

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope(_boxStyle))
            {
                EditorGUILayout.HelpBox("You have unsaved changes. Click 'Apply Changes' to save and reload the scene.", MessageType.Warning);

                using (new EditorGUILayout.VerticalScope())
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("Apply Changes", _buttonStyle, GUILayout.Height(30)))
                    {
                        ApplyChanges();
                    }

                    GUI.color = Color.yellow;
                    if (GUILayout.Button("Discard Changes", GUILayout.Height(20)))
                    {
                        DiscardChanges();
                    }
                    GUI.color = Color.white;
                }
            }
        }

        #endregion

        #region Data Management

        private void RefreshDataIfNeeded()
        {
            var currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastRefreshTime < REFRESH_COOLDOWN)
                return;

            ForceRefreshData();
        }

        private void ForceRefreshData()
        {
            try
            {
                // Always reinitialize after scene changes
                if (!SaveLoadService.IsInitialized)
                {
                    SaveLoadService.Initialize();
                }

                // Clear cache to force refresh
                _cachedSaveData = null;

                if (_editedData == null)
                    _editedData = new Dictionary<string, object>();
                else
                    _editedData.Clear();

                if (_foldoutStates == null)
                    _foldoutStates = new Dictionary<string, bool>();
                else
                    _foldoutStates.Clear();

                _hasUnsavedChanges = false;
                _currentPage = 0;
                _lastRefreshTime = Time.realtimeSinceStartup;

                // Reset search and pagination
                _searchFilter = "";

                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Refresh Error", $"Failed to refresh data: {ex.Message}", "OK");

                // Reset to safe state on error
                _cachedSaveData = null;
                _editedData = new Dictionary<string, object>();
                _foldoutStates = new Dictionary<string, bool>();
                _hasUnsavedChanges = false;
            }
        }
        private Dictionary<string, object> GetCachedSaveData()
        {
            try
            {
                if (_cachedSaveData == null)
                {
                    _cachedSaveData = SaveLoadService.GetAllSaveData();
                }
                return _cachedSaveData;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private Dictionary<string, object> FilterData(Dictionary<string, object> data)
        {
            if (data == null) return new Dictionary<string, object>();

            if (string.IsNullOrEmpty(_searchFilter))
                return data;

            try
            {
                return data.Where(kvp => kvp.Key.ToLower().Contains(_searchFilter.ToLower()))
                          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return data; // Return unfiltered data on error
            }
        }

        private Dictionary<string, object> GetPagedData(Dictionary<string, object> data)
        {
            if (data == null) return new Dictionary<string, object>();

            try
            {
                var startIndex = _currentPage * _itemsPerPage;
                return data.Skip(startIndex).Take(_itemsPerPage).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return data; // Return all data on error
            }
        }

        private void ApplyChanges()
        {
            try
            {
                // Apply all edited data
                foreach (var kvp in _editedData)
                {
                    SaveLoadService.Save(kvp.Key, kvp.Value);
                }

                // Save to persistent storage
                SaveLoadService.SaveAll();

                // Reload scene
                if (EditorUtility.DisplayDialog("Apply Changes",
                    "Changes applied successfully. Reload the current scene to apply changes to game objects?",
                    "Reload Scene", "Skip"))
                {
                    // Check if we're in play mode and use appropriate scene loading method
                    if (Application.isPlaying)
                    {
                        // Use SceneManager for play mode
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        UnityEngine.SceneManagement.SceneManager.LoadScene(activeScene.name);
                    }
                    else
                    {
                        // Use EditorSceneManager for edit mode
                        EditorSceneManager.OpenScene(EditorSceneManager.GetActiveScene().path);
                    }
                }

                ForceRefreshData();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to apply changes: {ex.Message}", "OK");
            }
        }

        private new void DiscardChanges()
        {
            if (EditorUtility.DisplayDialog("Discard Changes",
                "Are you sure you want to discard all unsaved changes?",
                "Discard", "Cancel"))
            {
                ForceRefreshData();
            }
        }

        private void ResetWindowState()
        {
            try
            {
                // Complete reset of window state
                _cachedSaveData = null;
                _editedData = new Dictionary<string, object>();
                _foldoutStates = new Dictionary<string, bool>();
                _hasUnsavedChanges = false;
                _currentPage = 0;
                _searchFilter = "";
                _lastRefreshTime = 0f;
                _stylesInitialized = false;
                _checkIcon = null;
                _warningIcon = null;

                // Force reinitialization
                LoadIcons();
                ForceRefreshData();

                Debug.Log("[SaveLoadStorageWindow] Window state reset successfully");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Reset Error", $"Failed to reset window state: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Utilities

        private string GetValueType(object value)
        {
            if (value == null) return "null";
            if (value is JObject) return "Object";
            if (value is JArray) return "Array";
            return value.GetType().Name;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(10, 10, 10, 5)
            };

            _boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _evenRowStyle = new GUIStyle("box")
            {
                normal = { background = MakeTexture(2, 2, new Color(0.8f, 0.8f, 0.8f, 0.1f)) },
                padding = new RectOffset(8, 8, 4, 4)
            };

            _oddRowStyle = new GUIStyle("box")
            {
                normal = { background = MakeTexture(2, 2, new Color(0.9f, 0.9f, 0.9f, 0.1f)) },
                padding = new RectOffset(8, 8, 4, 4)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _stylesInitialized = true;
        }

        private void LoadIcons()
        {
            _checkIcon = EditorGUIUtility.IconContent("TestPassed").image as Texture2D;
            _warningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}

#endif