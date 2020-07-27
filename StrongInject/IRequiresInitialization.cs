using System.Threading.Tasks;

namespace StrongInject
{
    public interface IRequiresInitialization
    {
        void Initialize();
    }

    public interface IRequiresAsyncInitialization
    {
        ValueTask InitializeAsync();
    }
}
