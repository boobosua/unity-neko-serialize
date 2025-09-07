using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NekoLib.Core;
using NekoLib.Utilities;

namespace NekoSerialize
{
    public sealed class SaveLoadManager : PersistentSingleton<SaveLoadManager>
    {
        [SerializeField] private SaveLoadSettings _settings;
        private readonly List<ISaveableComponent> _saveableComponents = new();
        private Coroutine _autoSaveCoroutine;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (_settings == null)
            {
                _settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");
            }
        }
#endif

        private void Start()
        {
            if (_settings != null && _settings.AutoSaveInterval > 0f)
            {
                StartAutoSave(_settings);
            }
        }

        /// <summary>
        /// Registers a saveable component with the manager.
        /// </summary>
        public void RegisterSaveableComponent(ISaveableComponent saveable)
        {
            if (!_saveableComponents.Contains(saveable))
            {
                _saveableComponents.Add(saveable);
            }
        }

        /// <summary>
        /// Unregisters a saveable component from the manager.
        /// </summary>
        public void UnregisterSaveableComponent(ISaveableComponent saveable)
        {
            if (_saveableComponents.Contains(saveable))
            {
                _saveableComponents.Remove(saveable);
            }
        }

        /// <summary>
        /// Triggers save for all registered components via SaveLoadService.
        /// </summary>
        public void SaveAllComponents()
        {
            foreach (var component in _saveableComponents)
            {
                if (component.AutoSave)
                {
                    var data = component.GetSaveData();
                    NSR.Save(component.SaveKey, data);
                }
            }

            NSR.SaveAll();
        }

        /// <summary>
        /// Starts the auto-save coroutine.
        /// </summary>
        private void StartAutoSave(SaveLoadSettings settings)
        {
            StopAutoSave();
            _autoSaveCoroutine = StartCoroutine(AutoSaveRoutine(settings));
        }

        /// <summary>
        /// Stops the auto-save coroutine if it's running.
        /// </summary>
        private void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }
        }

        /// <summary>
        /// Auto-save routine that runs in the background.
        /// </summary>
        private IEnumerator AutoSaveRoutine(SaveLoadSettings settings)
        {
            while (true)
            {
                yield return Utils.GetWaitForSeconds(settings.AutoSaveInterval);
                SaveAllComponents();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _settings != null && _settings.AutoSaveOnPause)
            {
                SaveAllComponents();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _settings != null && _settings.AutoSaveOnFocusLost)
            {
                SaveAllComponents();
            }
        }

        protected override void OnApplicationQuit()
        {
            StopAutoSave();
            SaveAllComponents();
            base.OnApplicationQuit();
        }
    }
}
