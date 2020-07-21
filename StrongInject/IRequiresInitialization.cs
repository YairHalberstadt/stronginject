using System.Threading.Tasks;

namespace StrongInject
{
    public interface IRequiresInitialization
    {
        ValueTask InitializeAsync();
    }
}
