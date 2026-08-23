using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using cross.ViewModels;

namespace cross.Views
{
    public partial class CANConfigPage : UserControl
    {
        private CanConfigViewModel _viewModel;

        public CANConfigPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = new CanConfigViewModel();
            DataContext = _viewModel;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void BtnSerialConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SerialConfigWindow(_viewModel);
            var parent = TopLevel.GetTopLevel(this);
            dialog.ShowDialog(parent as Avalonia.Controls.Window);
        }

        private async void BtnRead_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.ReadCanConfigAsync();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveCanConfigAsync();
        }

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.RestoreDefaultAsync();
        }
    }
}
