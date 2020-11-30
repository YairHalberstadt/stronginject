using StrongInject.Modules;
using StrongInject.Samples.XamarinApp.Models;
using StrongInject.Samples.XamarinApp.Services;
using StrongInject.Samples.XamarinApp.ViewModels;
using StrongInject.Samples.XamarinApp.Views;
using System;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp
{
    [Register(typeof(AppShell))]
    [Register(typeof(ItemsViewModel))]
    [Register(typeof(ItemsPage))]
    [Register(typeof(AboutViewModel))]
    [Register(typeof(AboutPage))]
    [Register(typeof(LoginViewModel))]
    [Register(typeof(LoginPage))]
    [Register(typeof(ItemDetailViewModel))]
    [Register(typeof(ItemDetailPage), typeof(IViewOf<ItemDetailViewModel>))]
    [Register(typeof(NewItemViewModel))]
    [Register(typeof(NewItemPage), typeof(IViewOf<NewItemViewModel>))]
    [Register(typeof(NavigationService), Scope.SingleInstance, typeof(INavigationService))]
    [Register(typeof(MockDataStore), Scope.SingleInstance, typeof(IDataStore<Item>))]
    [Register(typeof(Browser), Scope.SingleInstance, typeof(IBrowser))]
    [RegisterModule(typeof(LazyModule))]
    public partial class Container : IContainer<AppShell>
    {
        [Factory(Scope.SingleInstance)] INavigationService<T> CreateNavigationService<T>(Lazy<Shell> shell, Func<IViewOf<T>> createView) where T : BaseViewModel
            => new NavigationService<T>(shell, createView);

        [Factory(Scope.SingleInstance)]
        IParameterizedNavigationService<T> CreateParameterizedNavigationService<T>(Lazy<Shell> shell, Func<T, IViewOf<T>> createView) where T : BaseViewModel
            => new ParameterizedNavigationService<T>(shell, createView);

        [Factory] Shell GetShell() => Shell.Current;
    }
}
