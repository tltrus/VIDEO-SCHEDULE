using System.Windows;
using VideoSchedule_WPF.ViewModels;

namespace VideoSchedule_WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Создаем ViewModel и устанавливаем как DataContext
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.SaveSettingsAndCleanup();
        }
    }
}