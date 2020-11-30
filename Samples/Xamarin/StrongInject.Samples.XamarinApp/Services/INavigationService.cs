using StrongInject.Samples.XamarinApp.ViewModels;
using System.Threading.Tasks;

namespace StrongInject.Samples.XamarinApp.Services
{
    public interface INavigationService
    {
        Task PopAsync();
        Task GoToAsync(string route);
    }

    public interface INavigationService<T> : INavigationService where T : BaseViewModel
    {
        Task PushAsync();
        Task PopAllAndPushAsync();
    }

    public interface IParameterizedNavigationService<T> : INavigationService where T : BaseViewModel
    {
        Task PushAsync(T viewModel);
        Task PopAllAndPushAsync(T viewModel);
    }
}
