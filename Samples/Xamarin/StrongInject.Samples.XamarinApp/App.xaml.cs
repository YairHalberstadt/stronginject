using StrongInject.Samples.XamarinApp.Services;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            var container = new Container();
            var appShellOwned = container.Resolve();
            MainPage = appShellOwned.Value;
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
