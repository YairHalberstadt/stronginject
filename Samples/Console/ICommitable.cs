namespace StrongInject.Samples.ConsoleApp
{
    public interface ICommitable<TKey, TValue>
    {
        TKey Key { get; }
        TValue Value { get; }
        void Commit();
    }
}
