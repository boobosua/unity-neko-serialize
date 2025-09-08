using UnityEngine;

namespace NekoSerialize
{
    public abstract class SaveableMonoBehaviour<T> : MonoBehaviour, ISaveableComponent<T> where T : class, new()
    {
        [Header("Save Settings")]
        [SerializeField] private string _saveKey;
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private bool _autoLoad = true;

        public string SaveKey => _saveKey;
        public bool AutoSave => _autoSave;
        public bool AutoLoad => _autoLoad;

        public abstract T GetSaveData();
        public abstract void LoadSaveData(T data);

        /// <summary>
        /// Manually save this component's data.
        /// </summary>
        public void Save()
        {
            NSR.Save(SaveKey, GetSaveData());
        }

        /// <summary>
        /// Manually load this component's data.
        /// </summary>
        public void Load()
        {
            if (NSR.Exists(SaveKey))
            {
                var data = NSR.Load<T>(SaveKey);
                LoadSaveData(data);
            }
        }

        protected virtual void Start()
        {
            SaveLoadManager.Instance.RegisterSaveableComponent(this);
        }

        protected virtual void OnDestroy()
        {
            if (SaveLoadManager.HasInstance)
            {
                SaveLoadManager.Instance.UnregisterSaveableComponent(this);
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(_saveKey))
            {
                _saveKey = $"{gameObject.name}_{GetType().Name}_{GetInstanceID()}";
            }
        }
#endif
    }
}
