using System.Threading.Tasks;

namespace StrongInject
{
    /// <summary>
    /// Implement this interface to inform StrongInject that a type will need initialization once it is instantiated before it is ready to be used.
    /// </summary>
    public interface IRequiresInitialization
    {
        void Initialize();
    }

    /// <summary>
    /// Implement this interface to inform StrongInject that a type will need asynchronous initialization once it is instantiated before it is ready to be used.
    /// </summary>
    public interface IRequiresAsyncInitialization
    {
        ValueTask InitializeAsync();
    }
}
