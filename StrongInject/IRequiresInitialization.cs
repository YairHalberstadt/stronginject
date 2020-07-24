using System.Threading.Tasks;

namespace StrongInject
{
    public interface IRequiresAsyncInitialization
    {
        ValueTask InitializeAsync();
    }
}
