using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public class JsonConfigLoader : IConfigLoader
    {
        public async ValueTask<Config> LoadConfig()
        {
            await using (var fileStream = File.OpenRead("config.json"))
            {
                return await JsonSerializer.DeserializeAsync<Config>(fileStream) ?? throw new JsonException("Invalid Config");
            }
        }
    }
}
