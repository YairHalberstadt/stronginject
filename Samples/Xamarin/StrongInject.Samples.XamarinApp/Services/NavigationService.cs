using StrongInject.Samples.XamarinApp.ViewModels;
using StrongInject.Samples.XamarinApp.Views;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Lazy<Shell> _shell;
        protected INavigation Navigation => _shell.Value.Navigation;

        public NavigationService(Lazy<Shell> shell)
        {
            _shell = shell;
        }

        public Task PopAsync()
        {
            return Navigation.PopAsync();
        }

        public Task GoToAsync(string route)
        {
            return _shell.Value.GoToAsync(route);
        }
    }

    public class NavigationService<T> : NavigationService, INavigationService<T> where T : BaseViewModel
    {
        private readonly Func<IViewOf<T>> _createView;

        public NavigationService(Lazy<Shell> shell, Func<IViewOf<T>> createView) : base(shell)
        {
            _createView = createView;
        }

        public Task PushAsync()
        {
            return Navigation.PushAsync((Page)_createView());
        }

        public async Task PopAllAndPushAsync()
        {
            await Navigation.PopToRootAsync();
            await Navigation.PushAsync((Page)_createView());
        }
    }

    public class ParameterizedNavigationService<T> : NavigationService, IParameterizedNavigationService<T> where T : BaseViewModel
    {
        private readonly Func<T, IViewOf<T>> _createView;

        public ParameterizedNavigationService(Lazy<Shell> shell, Func<T, IViewOf<T>> createView) : base(shell)
        {
            _createView = createView;
        }

        public Task PushAsync(T viewModel)
        {
            return Navigation.PushAsync((Page)_createView(viewModel));
        }

        public async Task PopAllAndPushAsync(T viewModel)
        {
            await Navigation.PopToRootAsync();
            await Navigation.PushAsync((Page)_createView(viewModel));
        }
    }
}
