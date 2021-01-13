using StrongInject.Samples.XamarinApp.Services;
using StrongInject.Samples.XamarinApp.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        public Command LoginCommand { get; }

        public LoginViewModel(INavigationService navigationService)
        {
            LoginCommand = new Command(OnLoginClicked);
            _navigationService = navigationService;
        }

        private async void OnLoginClicked(object obj)
        {
            // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
            await _navigationService.GoToAsync($"//{nameof(AboutPage)}");
        }
    }
}
