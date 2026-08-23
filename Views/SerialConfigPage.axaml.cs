using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using cross.Communication;
using cross.ViewModels;

namespace cross.Views
{
    public partial class SerialConfigPage : UserControl
    {
        private readonly AppViewModel _globalVM = AppViewModel.Instance;
        private SerialConfigViewModel _vm => _globalVM.SerialConfig;

        public SerialConfigPage()
        {
            InitializeComponent();
            DataContext = _globalVM;

            // 接收滚动到底部
            _globalVM.SerialConfig.ScrollToEndRequested += () =>
            {
                if (tbReceive is not null)
                    tbReceive.CaretIndex = tbReceive.Text?.Length ?? 0;
            };

            // 扩展面板默认隐藏
            _globalVM.SerialConfig.ExpandVisible = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.Page_Loaded(sender, e);
            RefreshComStateIndicator();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _vm.Page_Unloaded(sender, e);
        }

        private void RefreshComStateIndicator()
        {
            // Ellipse 颜色通过 ComStateColor 绑定，此处无需显式更新
            if (comState != null)
                comState.Fill = _vm.ComStateColor;
        }

        #region 串口按钮
        private void btOpenCom_Click(object sender, RoutedEventArgs e)
        {
            _vm.btOpenCom_Click();
            RefreshComStateIndicator();
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            _vm.btSend_Event(tbSend?.Text ?? string.Empty, ck16Send?.IsChecked == true);
        }

        private void btClearView_Click(object sender, RoutedEventArgs e)
        {
            _vm.ReceiveCount = "0";
            _vm.TbReceiveText = string.Empty;
        }

        private async void btSaveDisplay_Click(object sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            await _vm.SaveReceiveData(window);
        }

        private void btSearchPort_Click(object sender, RoutedEventArgs e)
        {
            _vm.PortConfig.InitPortNames();
        }
        #endregion

        #region 复选框
        private void ck16View_Click(object sender, RoutedEventArgs e)
        {
            if (ck16View?.IsChecked == true)
            {
                byte[] bytes = Encoding.Default.GetBytes(_vm.TbReceiveText);
                _vm.TbReceiveText = _vm.ByteArrayToHexString(bytes);
            }
            else
            {
                byte[] bytes = _vm.HexStringToByteArray(_vm.TbReceiveText.Replace(" ", ""));
                _vm.TbReceiveText = Encoding.Default.GetString(bytes);
            }
        }

        private void ck16Send_Click(object sender, RoutedEventArgs e)
        {
            _vm.Toggle16View(ck16Send?.IsChecked == true);
        }

        private void ckAsciiView_Click(object sender, RoutedEventArgs e)
        {
            _vm.Toggle16View(ckAsciiView?.IsChecked == true);
        }

        private void ckAdvantechCmd_Click(object sender, RoutedEventArgs e)
        {
            if (ckAsciiView?.IsChecked == true)
                _vm.Toggle16View(true);
            else if (ck16Send?.IsChecked == true)
                _vm.Toggle16View(true);
        }

        private void ckAutoSend_Click(object sender, RoutedEventArgs e)
        {
            // 由 VM 的 IsAutoSend setter 内的 OnAutoSendStateChanged 处理
            // 但需要在 UI 线程同步显示/隐藏间隔控件
            bool isOn = ckAutoSend?.IsChecked == true;
            _vm.TbkIntervalTimeVisible = isOn;
            _vm.TbIntervalTimeVisible = isOn;
        }
        #endregion

        #region 发送区
        private void tbSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.tbSend_TextChanged();
        }

        private void tbIntervalTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 间隔时间修改时仅当自动发送已开启才重启动定时器
            if (ckAutoSend?.IsChecked == true &&
                !string.IsNullOrEmpty(tbIntervalTime?.Text) &&
                double.TryParse(tbIntervalTime.Text, out double v) && v > 0)
            {
                // IsAutoSend setter 已通过 OnAutoSendStateChanged 启动定时器；
                // 如需刷新 Interval，临时关闭再开启触发一次 setter
                _vm.IsAutoSend = false;
                _vm.IsAutoSend = true;
            }
        }
        #endregion

        #region 扩展面板
        private void btExpend_Click(object sender, RoutedEventArgs e)
        {
            _vm.ExpandVisible = !_vm.ExpandVisible;

            if (_vm.ExpandVisible && (expandItems?.Items?.Count ?? 0) == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(2) };
                    var cb = new CheckBox { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Width = 24 };
                    row.Children.Add(cb);
                    var tb = new TextBox { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Margin = new Thickness(0, 2) };
                    row.Children.Add(tb);
                    var btn = new Button { Content = $"发送{i}", Width = 40, Margin = new Thickness(0, 2) };
                    btn.Click += (s, e2) =>
                    {
                        _vm.btSend_Event(tb.Text ?? string.Empty, cb.IsChecked == true);
                    };
                    row.Children.Add(btn);
                    expandItems.Items.Add(row);
                }
            }
        }
        #endregion
    }
}
