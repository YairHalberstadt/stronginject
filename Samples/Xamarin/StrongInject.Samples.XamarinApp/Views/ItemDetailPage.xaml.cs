using StrongInject.Samples.XamarinApp.ViewModels;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Views
{
    public partial class ItemDetailPage : ContentPage, IViewOf<ItemDetailViewModel>
    {
        public ItemDetailPage(ItemDetailViewModel itemDetailViewModel)
        {
            InitializeComponent();
            BindingContext = itemDetailViewModel;
        }
    }
}