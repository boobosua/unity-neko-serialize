namespace NekoSerialize
{
    public interface ISaveableComponent
    {
        bool AutoSave { get; }
        bool AutoLoad { get; }
        void Save();
        void Load();
    }

    public interface ISaveableComponent<T> : ISaveableComponent, ISaveable<T> where T : class, new()
    {

    }
}
