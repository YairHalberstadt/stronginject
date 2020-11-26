using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public interface IConfigLoader
    {
        ValueTask<Config> LoadConfig();
    }
}
