namespace NekoSerialize
{
    public interface ISaveableComponent : ISaveable
    {
        bool AutoSave { get; }
        bool AutoLoad { get; }
    }
}
