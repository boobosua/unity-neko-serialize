using System.Collections;
using UnityEngine;
using NekoLib.Core;
using NekoLib.Utilities;

namespace NekoSerialize
{
    public sealed class SaveLoadManager : PersistentSingleton<SaveLoadManager>
    {
        private SaveLoadSettings _settings;
        private Coroutine _autoSaveCoroutine;

        public void Initialize(SaveLoadSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[SaveLoadManager] Provided settings are null. Initialization aborted.");
                return;
            }

            _settings = settings;

            if (_settings != null && _settings.AutoSaveInterval > 0f)
            {
                StartAutoSave(_settings);
            }
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
                NSR.SaveAll();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _settings != null && _settings.AutoSaveOnPause)
            {
                NSR.SaveAll();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _settings != null && _settings.AutoSaveOnFocusLost)
            {
                NSR.SaveAll();
            }
        }
    }
}
