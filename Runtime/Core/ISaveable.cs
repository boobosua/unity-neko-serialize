namespace NekoSerialize
{
    public interface ISaveable
    {
        string SaveKey { get; }
        object GetSaveData();
        void LoadSaveData(object data);
    }
}
