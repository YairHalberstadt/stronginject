using StrongInject.Modules;
using StrongInject.Samples.XamarinApp.Models;
using StrongInject.Samples.XamarinApp.Services;
using StrongInject.Samples.XamarinApp.ViewModels;
using StrongInject.Samples.XamarinApp.Views;
using System;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp
{
    [Register<AppShell>]
    [Register<ItemsViewModel>]
    [Register<ItemsPage>]
    [Register<AboutViewModel>]
    [Register<AboutPage>]
    [Register<LoginViewModel>]
    [Register<LoginPage>]
    [Register<ItemDetailViewModel>]
    [Register<ItemDetailPage, IViewOf<ItemDetailViewModel>>]
    [Register<NewItemViewModel>]
    [Register<NewItemPage, IViewOf<NewItemViewModel>>]
    [Register<NavigationService, INavigationService>(Scope.SingleInstance)]
    [Register<MockDataStore, IDataStore<Item>>(Scope.SingleInstance)]
    [Register<Browser, IBrowser>(Scope.SingleInstance)]
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
