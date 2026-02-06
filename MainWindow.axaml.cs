using Avalonia.Controls;
using Avalonia.Interactivity;

namespace cross.Views
{
    public partial class MainWindow : Window
    {
        // 侧边栏展开状态标记
        private bool _isSidebarOpen = true;
        public MainWindow()
        {
            InitializeComponent();
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

        // 数据管理
        private void DataManageButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取HomeView中选中的通讯类型
            switch (HomeView.SelectedCommType)
            {
                case "串口":
                    MainContentControl.Content = new SerialDataView();
                    break;
                case "以太网":
                    MainContentControl.Content = new EthernetDataView();
                    break;
                case "CAN":
                    MainContentControl.Content = new CANDataView();
                    break;
                default:
                    MainContentControl.Content = new SerialDataView(); // 默认串口
                    break;
            }
        }

        // 设置
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new SettingsView();
        }

        // 关于
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new AboutView();
        }
        #endregion




    }
}