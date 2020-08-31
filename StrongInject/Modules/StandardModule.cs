namespace StrongInject.Modules
{
    /// <summary>
    /// Provides registrations for the most common and least opinionated inbuilt modules.
    /// </summary>
    [RegisterModule(typeof(CollectionsModule))]
    [RegisterModule(typeof(LazyModule))]
    [RegisterModule(typeof(ValueTupleModule))]
    public class StandardModule
    {

    }
}
