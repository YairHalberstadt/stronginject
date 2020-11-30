using StrongInject.Samples.XamarinApp.Models;
using StrongInject.Samples.XamarinApp.Services;
using StrongInject.Samples.XamarinApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        private Item? _selectedItem;
        private readonly IParameterizedNavigationService<ItemDetailViewModel> _navigationService;
        private readonly INavigationService<NewItemViewModel> _newItemNavigationService;
        private readonly Func<Item, ItemDetailViewModel> _createItemDetailViewModel;
        private readonly IDataStore<Item> _dataStore;

        public ObservableCollection<Item> Items { get; }
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<Item> ItemTapped { get; }

        public ItemsViewModel(
            IParameterizedNavigationService<ItemDetailViewModel> itemDetailNavigationService,
            INavigationService<NewItemViewModel> newItemNavigationService,
            Func<Item, ItemDetailViewModel> createItemDetailViewModel,
            IDataStore<Item> dataStore)
        {
            Title = "Browse";
            Items = new ObservableCollection<Item>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            ItemTapped = new Command<Item>(OnItemSelected);

            AddItemCommand = new Command(OnAddItem);
            _navigationService = itemDetailNavigationService;
            _newItemNavigationService = newItemNavigationService;
            _createItemDetailViewModel = createItemDetailViewModel;
            _dataStore = dataStore;
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await _dataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
        }

        public Item? SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                if (value is not null)
                    OnItemSelected(value);
            }
        }

        private async void OnAddItem(object obj)
        {
            await _newItemNavigationService.PushAsync();
        }

        async void OnItemSelected(Item item)
        {
            if (item == null)
                return;

            // This will push the ItemDetailPage onto the navigation stack
            await _navigationService.PushAsync(_createItemDetailViewModel(item));
        }
    }
}