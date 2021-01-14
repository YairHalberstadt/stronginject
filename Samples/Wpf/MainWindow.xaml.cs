using StrongInject.Samples.Wpf.ViewModels;
using System.Windows;

namespace StrongInject.Samples.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            MainWindowViewModel = mainWindowViewModel;
            InitializeComponent();
        }

        public MainWindowViewModel MainWindowViewModel { get; }
    }
}
