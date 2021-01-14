using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrongInject.Samples.Wpf.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel(UsersViewModel usersViewModel)
        {
            UsersViewModel = usersViewModel;
        }

        public UsersViewModel UsersViewModel { get; }
    }
}
