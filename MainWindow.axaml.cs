using Avalonia.Controls;
using Avalonia.Interactivity;
using cross.Communication;
using cross.ViewModels;

namespace cross.Views
{
    public partial class MainWindow : Window
    {
        // 侧边栏展开状态标记
        private bool _isSidebarOpen = true;
        private AppViewModel _appViewModel;
        public MainWindow()
        {
            InitializeComponent();
            // 获取全局AppViewModel
            _appViewModel = AppViewModel.Instance;
            DataContext = _appViewModel;
        }

        #region 汉堡按钮：折叠/展开侧边栏
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarOpen = !_isSidebarOpen;
            SidebarBorder.Width = _isSidebarOpen ? 240 : 0;
        }
        #endregion

        #region 侧边栏按钮：切换子页面
        // 首页
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new HomeView();
        }

        // 通讯配置界面
        private void DataManageButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_appViewModel.SelectedCommType)
            {
                case CommunicationType.SerialPort:
                    MainContentControl.Content = new SerialConfigPage();
                    break;
                case CommunicationType.Ethernet:
                    MainContentControl.Content = new EthernetDataView();
                    break;
                case CommunicationType.CAN:
                    MainContentControl.Content = new CANDataView();
                    break;
                default:
                    MainContentControl.Content = new SerialConfigPage();
                    break;
            }
        }

        // 设置
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new SettingsView();
        }

        // 通用调试界面
        private void GeneralDebugButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new GeneralDebugPage();
        }

        private void DBCButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new DbcView();
        }

        private void ProfButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new ProIntroductionPage();
        }

        private void DeviceControlButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new TextBlock { Text = "设备控制界面（开发中）",
                FontSize = 24, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        }

        // 关于
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new AboutView();
        }
        #endregion




    }
}