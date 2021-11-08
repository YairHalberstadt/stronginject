using StrongInject.Samples.Wpf.Models;
using StrongInject.Samples.Wpf.ViewModels;

namespace StrongInject.Samples.Wpf
{
    [Register<MainWindow>]
    [Register<MainWindowViewModel>]
    [Register<UsersViewModel>]
    [Register<UserViewModel>(Scope.InstancePerDependency)]
    [Register<MockDatabase, IDatabase>(Scope.SingleInstance)]
    public partial class Container : IAsyncContainer<MainWindow>
    {
    }
}
