using System.Collections;
using NekoLib.Core;
using NekoLib.Logger;
using NekoLib.Utilities;
using UnityEngine;

namespace NekoSerialize
{
    public sealed class AutoSaveManager : PersistentSingleton<AutoSaveManager>
    {
        private SaveLoadSettings _settings;
        private Coroutine _autoSaveCoroutine;

        public void Initialize(SaveLoadSettings settings)
        {
            if (settings == null)
            {
                Log.Error("[AutoSaveManager] Provided settings are null. Initialization aborted.");
                return;
            }

            _settings = settings;

            if (_settings.AutoSaveInterval > 0f)
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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _settings != null && _settings.AutoSaveOnFocusLost)
            {
                NSR.SaveAll();
            }
        }
    }
}
