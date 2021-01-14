using StrongInject.Samples.Wpf.Models;
using StrongInject.Samples.Wpf.ViewModels;

namespace StrongInject.Samples.Wpf
{
    [Register(typeof(MainWindow))]
    [Register(typeof(MainWindowViewModel))]
    [Register(typeof(UsersViewModel))]
    [Register(typeof(UserViewModel), Scope.InstancePerDependency)]
    [Register(typeof(MockDatabase), Scope.SingleInstance, typeof(IDatabase))]
    public partial class Container : IAsyncContainer<MainWindow>
    {
    }
}
