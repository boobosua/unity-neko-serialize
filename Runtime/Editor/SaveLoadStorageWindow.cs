#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NekoSerialize
{
    public class SaveLoadStorageWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<string, object> currentSaveData = new Dictionary<string, object>();
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> dictionaryFoldoutStates = new Dictionary<string, bool>();

        // Pagination
        private int currentPage = 0;
        private const int itemsPerPage = 25;

        [MenuItem("Tools/Neko Indie/Save Load Storage")]
        private static void OpenWindow()
        {
            GetWindow<SaveLoadStorageWindow>("Save Load Storage").Show();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnEditorUpdate()
        {
            if (Application.isPlaying && SaveLoadService.IsInitialized)
            {
                Refresh();
            }
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // EditorGUILayout.LabelField("Save Load Storage", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view and manage save data.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh")) Refresh();
            if (GUILayout.Button("Delete All")) DeleteAll();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (!SaveLoadService.IsInitialized)
            {
                EditorGUILayout.HelpBox("SaveLoadService is not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Display save data
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var dataList = new List<KeyValuePair<string, object>>(currentSaveData);
            int totalPages = Mathf.CeilToInt((float)dataList.Count / itemsPerPage);

            // Pagination controls
            if (totalPages > 1)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Page {currentPage + 1} of {totalPages}", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(currentPage <= 0);
                if (GUILayout.Button("Previous")) currentPage--;
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(currentPage >= totalPages - 1);
                if (GUILayout.Button("Next")) currentPage++;
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }

            // Calculate pagination range
            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, dataList.Count);

            // Ensure current page is valid
            if (startIndex >= dataList.Count && dataList.Count > 0)
            {
                currentPage = Mathf.Max(0, totalPages - 1);
                startIndex = currentPage * itemsPerPage;
                endIndex = Mathf.Min(startIndex + itemsPerPage, dataList.Count);
            }

            // Display paginated data
            for (int i = startIndex; i < endIndex; i++)
            {
                var kvp = dataList[i];
                if (kvp.Value == null) continue;

                // Rounded box style
                var boxStyle = new GUIStyle("helpBox");
                boxStyle.padding = new RectOffset(8, 8, 8, 8);
                boxStyle.margin = new RectOffset(4, 4, 2, 2);

                EditorGUILayout.BeginVertical(boxStyle);

                if (!foldoutStates.ContainsKey(kvp.Key))
                    foldoutStates[kvp.Key] = false;

                foldoutStates[kvp.Key] = EditorGUILayout.Foldout(foldoutStates[kvp.Key],
                    kvp.Key, true);

                if (foldoutStates[kvp.Key])
                {
                    EditorGUI.indentLevel++;
                    DisplayData(kvp.Value);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (currentSaveData.Count == 0)
                EditorGUILayout.LabelField("No save data found.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DisplayData(object obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                EditorGUI.indentLevel++;
                foreach (var kvp in jsonObj)
                {
                    string fieldName = ObjectNames.NicifyVariableName(kvp.Key);
                    object value = kvp.Value;

                    if (value == null)
                    {
                        EditorGUILayout.LabelField(fieldName + ":", "null");
                    }
                    else if (value.ToString().Contains("/") && value.ToString().Contains(":"))
                    {
                        // Handle DateTime strings directly
                        EditorGUILayout.LabelField(fieldName + ":", value.ToString());
                    }
                    else if (IsVector3(value))
                    {
                        var vector = ParseVector3(value);
                        EditorGUILayout.LabelField(fieldName + ":", $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})");
                    }
                    else if (IsVector2(value))
                    {
                        var vector = ParseVector2(value);
                        EditorGUILayout.LabelField(fieldName + ":", $"({vector.x:F3}, {vector.y:F3})");
                    }
                    else if (IsDictionary(value))
                    {
                        if (!IsEmptyDictionary(value))
                        {
                            string dictionaryKey = $"{fieldName}_dict_{EditorGUI.indentLevel}";
                            if (!dictionaryFoldoutStates.ContainsKey(dictionaryKey))
                                dictionaryFoldoutStates[dictionaryKey] = false;

                            dictionaryFoldoutStates[dictionaryKey] = EditorGUILayout.Foldout(
                                dictionaryFoldoutStates[dictionaryKey], fieldName);

                            if (dictionaryFoldoutStates[dictionaryKey])
                            {
                                DisplayDictionary(value);
                            }
                        }
                    }
                    else if (IsArray(value))
                    {
                        EditorGUILayout.LabelField(fieldName + ":", EditorStyles.boldLabel);
                        DisplayArray(value);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(fieldName + ":", value.ToString());
                    }
                }
                EditorGUI.indentLevel--;
            }
            catch (System.Exception)
            {
                // If JSON fails, show raw data
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Raw Data", obj.ToString());
                EditorGUI.indentLevel--;
            }
        }

        private bool IsVector2(object value)
        {
            if (value is Vector2) return true;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
                return jobj.ContainsKey("x") && jobj.ContainsKey("y") && !jobj.ContainsKey("z");
            return false;
        }

        private Vector2 ParseVector2(object value)
        {
            if (value is Vector2 vector2) return vector2;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        private bool IsVector3(object value)
        {
            if (value is Vector3) return true;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
                return jobj.ContainsKey("x") && jobj.ContainsKey("y") && jobj.ContainsKey("z") && !jobj.ContainsKey("w");
            return false;
        }

        private Vector3 ParseVector3(object value)
        {
            if (value is Vector3 vector3) return vector3;
            if (value is Newtonsoft.Json.Linq.JObject jobj)
            {
                float x = jobj["x"]?.ToObject<float>() ?? 0f;
                float y = jobj["y"]?.ToObject<float>() ?? 0f;
                float z = jobj["z"]?.ToObject<float>() ?? 0f;
                return new Vector3(x, y, z);
            }
            return Vector3.zero;
        }

        private bool IsDictionary(object value)
        {
            return value is System.Collections.IDictionary ||
                   value is Newtonsoft.Json.Linq.JObject;
        }

        private bool IsArray(object value)
        {
            if (value is System.Array) return true;
            if (value is System.Collections.IList) return true;
            if (value is Newtonsoft.Json.Linq.JArray) return true;
            return false;
        }

        private bool IsEmptyDictionary(object value)
        {
            try
            {
                if (value is Newtonsoft.Json.Linq.JObject jobj)
                {
                    return jobj.Count == 0;
                }
                else if (value is System.Collections.IDictionary dict)
                {
                    return dict.Count == 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void DisplayDictionary(object value)
        {
            EditorGUI.indentLevel++;
            try
            {
                if (value is Newtonsoft.Json.Linq.JObject jobj)
                {
                    foreach (var kvp in jobj)
                    {
                        EditorGUILayout.LabelField(kvp.Key + ":", kvp.Value?.ToString() ?? "null");
                    }
                }
                else if (value is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        EditorGUILayout.LabelField((entry.Key?.ToString() ?? "null") + ":", entry.Value?.ToString() ?? "null");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.LabelField("Error displaying dictionary:", e.Message);
            }
            EditorGUI.indentLevel--;
        }

        private void DisplayArray(object value)
        {
            EditorGUI.indentLevel++;
            try
            {
                if (value is Newtonsoft.Json.Linq.JArray jarray)
                {
                    for (int i = 0; i < jarray.Count; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", jarray[i]?.ToString() ?? "null");
                    }
                }
                else if (value is System.Collections.IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", list[i]?.ToString() ?? "null");
                    }
                }
                else if (value is System.Array array)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        EditorGUILayout.LabelField($"Element {i}", array.GetValue(i)?.ToString() ?? "null");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorGUILayout.LabelField("Error displaying array: " + e.Message);
            }
            EditorGUI.indentLevel--;
        }

        public void Refresh()
        {
            if (!Application.isPlaying || !SaveLoadService.IsInitialized) return;
            currentSaveData = SaveLoadService.GetAllSaveData();
            Repaint();
        }

        public void DeleteAll()
        {
            if (!Application.isPlaying || !SaveLoadService.IsInitialized) return;
            if (EditorUtility.DisplayDialog("Delete All", "Delete all save data and restart the game?", "Delete & Restart", "Cancel"))
            {
                // // Step 1: Delete all data completely
                // SaveLoadService.DeleteAllData(); // This calls s_dataHandler.DeleteSaveData() + s_saveData.Clear()

                // // Step 2: Also manually clear PlayerPrefs as backup (in case using PlayerPrefs)
                // if (SaveLoadService.GetSettings()?.SaveLocation == SaveLocation.PlayerPrefs)
                // {
                //     PlayerPrefs.DeleteAll();
                //     PlayerPrefs.Save();
                // }

                // currentSaveData.Clear();

                // // Step 3: Exit play mode
                // EditorApplication.isPlaying = false;

                // // Step 4: Re-enter play mode again (restart)
                // EditorApplication.delayCall += () =>
                // {
                //     EditorApplication.isPlaying = true;
                // };

                // Repaint();
            }
        }
    }
}

#endif