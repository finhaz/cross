using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using cross.ViewModels;

namespace cross.Views
{
    public partial class SerialConfigWindow : Window
    {
        private readonly CanConfigViewModel _viewModel;

        public SerialConfigWindow()
        {
            InitializeComponent();
        }

        public SerialConfigWindow(CanConfigViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void BtnRefreshPort_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshPortNames();
        }

        private async void BtnOpenPort_Click(object sender, RoutedEventArgs e)
        {
            bool ok = _viewModel.OpenSerialPort();
            string msg = ok ? "打开成功" : "打开失败：" + _viewModel.AtModuleStatus;
            await DialogHelper.ShowAsync(msg, "提示");
        }

        private async void BtnClosePort_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CloseSerialPort();
            await DialogHelper.ShowAsync(_viewModel.AtModuleStatus, "提示");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
