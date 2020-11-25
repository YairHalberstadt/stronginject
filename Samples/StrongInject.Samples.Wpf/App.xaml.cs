using System.Windows;

namespace StrongInject.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            var container = new Container();
            MainWindow = (await container.ResolveAsync()).Value;

            // We never dispose of the container because its lifetime is the lifetime of the app.
            // Whilst we could hook into the Application Exit event, this will not be called if the app crashes or is forcefully terminated.
            // Therefore best practice is simply to make sure we don't rely on the container being disposed.
            // This will usually be the case as terminating the application frees up most resources.

            MainWindow.Show();
        }
    }
}
