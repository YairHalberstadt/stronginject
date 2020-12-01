using StrongInject.Samples.XamarinApp.ViewModels;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Views
{
    public partial class ItemsPage : ContentPage
    {
        readonly ItemsViewModel _viewModel;

        public ItemsPage(ItemsViewModel itemsViewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = itemsViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }
    }
}