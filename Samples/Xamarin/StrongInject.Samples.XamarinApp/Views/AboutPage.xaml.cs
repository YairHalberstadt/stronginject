using StrongInject.Samples.XamarinApp.ViewModels;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage(AboutViewModel aboutViewModel)
        {
            InitializeComponent();
            BindingContext = aboutViewModel;
        }
    }
}