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

            // 启动时默认进入首页，避免右侧内容区为空白
            MainContentControl.Content = new HomeView();
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
                case CommunicationType.FESerialPort:
                    MainContentControl.Content = new SerialConfigPage();
                    break;
                case CommunicationType.Ethernet:
                    MainContentControl.Content = new EthernetDataView();
                    break;
                case CommunicationType.CAN:
                    MainContentControl.Content = new CANConfigPage();
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

        // 通用调试界面：CAN通讯时进入DBC界面，FE串口通讯时进入设备控制界面，其余进入通用调试界面
        private void GeneralDebugButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_appViewModel.SelectedCommType)
            {
                case CommunicationType.CAN:
                    MainContentControl.Content = new DbcView();
                    break;
                case CommunicationType.FESerialPort:
                    MainContentControl.Content = new DeviceControlPage();
                    break;
                default:
                    MainContentControl.Content = new GeneralDebugPage();
                    break;
            }
        }

        private void ProfButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new ProIntroductionPage();
        }

        // 关于
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new AboutView();
        }
        #endregion




    }
}