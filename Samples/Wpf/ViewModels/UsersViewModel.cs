using StrongInject.Samples.Wpf.Commands;
using StrongInject.Samples.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace StrongInject.Samples.Wpf.ViewModels
{
    public class UsersViewModel : IRequiresAsyncInitialization, INotifyPropertyChanged
    {
        private readonly IDatabase _database;
        private readonly Func<User, UserViewModel> _createUserViewModel;
        private UserViewModel _userToAdd;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<UserViewModel> Users { get; set; } = null!;
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand AddCommand { get; set; }
        public UserViewModel? SelectedUser { get; set; }
        public UserViewModel UserToAdd
        {
            get => _userToAdd;
            private set
            {
                _userToAdd = value;
                RaisePropertyChanged(nameof(UserToAdd));
            }
        }

        public UsersViewModel(IDatabase database, Func<User, UserViewModel> createUserViewModel)
        {
            DeleteCommand = new RelayCommand(o => OnDelete(SelectedUser));
            AddCommand = new RelayCommand(OnAdd);
            _database = database;
            _createUserViewModel = createUserViewModel;
            _userToAdd = CreateNewUser();
        }

        private void OnDelete(UserViewModel? user)
        {
            if (user is not null)
            {
                Users.Remove(user);
                user.Delete();
            }
        }

        private void OnAdd(object? obj)
        {
            _ = UserToAdd.Save();
            Users.Add(UserToAdd);
            UserToAdd = CreateNewUser();
        }

        private UserViewModel CreateNewUser()
        {
            var newUser = _createUserViewModel(User.CreateNew("", ""));
            newUser.SaveChanges = false;
            return newUser;
        }

        public async ValueTask InitializeAsync()
        {
            var users = await _database.GetUsers();
            Users = new ObservableCollection<UserViewModel>(users.Select(_createUserViewModel));
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
