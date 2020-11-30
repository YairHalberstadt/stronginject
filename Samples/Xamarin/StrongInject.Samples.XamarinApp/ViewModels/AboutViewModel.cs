using StrongInject.Samples.XamarinApp.Services;
using System.Windows.Input;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public AboutViewModel(IBrowser browser)
        {
            Title = "About";
            OpenWebCommand = new Command(async () => await browser.OpenAsync("https://aka.ms/xamarin-quickstart"));
        }

        public ICommand OpenWebCommand { get; }
    }
}