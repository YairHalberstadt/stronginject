using StrongInject.Samples.XamarinApp.Models;
using StrongInject.Samples.XamarinApp.ViewModels;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Views
{
    public partial class NewItemPage : ContentPage, IViewOf<NewItemViewModel>
    {
        public Item? Item { get; set; }

        public NewItemPage(NewItemViewModel newItemViewModel)
        {
            InitializeComponent();
            BindingContext = newItemViewModel;
        }
    }
}