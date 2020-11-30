using StrongInject.Samples.XamarinApp.Services;
using StrongInject.Samples.XamarinApp.Views;
using System;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;

        public AppShell(Func<ItemsPage> itemsPage, Func<AboutPage> aboutPage, Func<LoginPage> loginPage, INavigationService navigationService)
        {
            InitializeComponent();
            ItemsPageTemplate = new(itemsPage);
            AboutPageTemplate = new(aboutPage);
            LoginPageTemplate = new(loginPage);
            BindingContext = this;
            _navigationService = navigationService;
        }

        public DataTemplate ItemsPageTemplate { get; }
        public DataTemplate AboutPageTemplate { get; }
        public DataTemplate LoginPageTemplate { get; }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await _navigationService.GoToAsync("//LoginPage");
        }
    }
}
