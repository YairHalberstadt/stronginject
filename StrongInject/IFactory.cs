using System.Threading.Tasks;

namespace StrongInject
{
    public interface IFactory<T>
    {
        ValueTask<T> CreateAsync();
    }
}
