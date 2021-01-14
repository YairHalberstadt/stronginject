using StrongInject.Samples.Wpf.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject.Samples.Wpf.ViewModels
{
    public class UserViewModel : INotifyPropertyChanged
    {
        public UserViewModel(User user, IDatabase database)
        {
            _firstName = user.FirstName;
            _lastName = user.LastName;
            _id = user.Id;
            _database = database;
        }

        public bool SaveChanges { get; set; } = true;

        private string _firstName;
        private string _lastName;
        private Guid _id;
        private readonly IDatabase _database;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName == value)
                {
                    return;
                }
                _firstName = value;
                RaisePropertyChanged(nameof(FirstName));
                RaisePropertyChanged(nameof(FullName));
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName == value)
                {
                    return;
                }
                _lastName = value;
                RaisePropertyChanged(nameof(LastName));
                RaisePropertyChanged(nameof(FullName));
            }
        }

        public string FullName => string.Concat(_firstName, " ", _lastName);

        private void RaisePropertyChanged(string propertyName)
        {
            if (SaveChanges)
            {
                _ = Save();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Task Delete() => _database.DeleteUser(_id);
        public Task Save() => _database.AddOrUpdateUser(new User(_id, FirstName, LastName));
    }
}
