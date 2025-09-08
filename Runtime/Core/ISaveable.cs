namespace NekoSerialize
{
    public interface ISaveable<T> where T : class, new()
    {
        string SaveKey { get; }
        T GetSaveData();
        void LoadSaveData(T data);
    }
}
