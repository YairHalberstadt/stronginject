using System.Threading.Tasks;

namespace StrongInject.Samples.XamarinApp.Services
{
    public class Browser : IBrowser
    {
        public Task OpenAsync(string uri) => Xamarin.Essentials.Browser.OpenAsync(uri);
    }
}
