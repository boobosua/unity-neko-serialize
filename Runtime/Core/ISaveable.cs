namespace NekoSerialize
{
    public interface ISaveable<T> where T : class, new()
    {
        string SaveKey { get; }
        void SaveData();
        // T LoadData();
    }
}
