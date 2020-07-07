using System.Threading.Tasks;

namespace StrongInject.Runtime
{
    public interface IRequiresInitialization
    {
        ValueTask InitializeAsync();
    }
}
