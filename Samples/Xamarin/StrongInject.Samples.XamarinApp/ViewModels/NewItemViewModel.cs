using StrongInject.Samples.XamarinApp.Models;
using StrongInject.Samples.XamarinApp.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.ViewModels
{
    public class NewItemViewModel : BaseViewModel
    {
        private string _text = "";
        private string _description = "";
        private readonly IDataStore<Item> _dataStore;
        private readonly INavigationService _navigationService;

        public NewItemViewModel(IDataStore<Item> dataStore, INavigationService navigationService)
        {
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();
            _dataStore = dataStore;
            _navigationService = navigationService;
        }

        private bool ValidateSave()
        {
            return !string.IsNullOrWhiteSpace(_text)
                && !string.IsNullOrWhiteSpace(_description);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            await _navigationService.PopAsync();
        }

        private async void OnSave()
        {
            Item newItem = new Item(
                id: Guid.NewGuid().ToString(),
                text:  Text,
                description: Description
            );

            await _dataStore.AddItemAsync(newItem);
            await _navigationService.PopAsync();
        }
    }
}
