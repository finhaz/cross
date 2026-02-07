using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using cross.ViewModels;
using Avalonia.Controls;
using Avalonia.Platform;
using System.Threading.Tasks;
using Avalonia.Interactivity; // 用于RoutedEventArgs

namespace cross;

public partial class DbcView: UserControl
{
    // 获取ViewModel实例
    private DbcViewModel _viewModel => DataContext as DbcViewModel;
    public DbcView()
    {
        InitializeComponent();
    }


    private async void BtnLoadDbc_Click(object sender, RoutedEventArgs e)
    {
        // 创建Avalonia的OpenFileDialog实例
        var openFileDialog = new OpenFileDialog();

        // 配置文件筛选器
        openFileDialog.Filters.Add(new FileDialogFilter
        {
            Name = "DBC文件",
            Extensions = { "dbc" }
        });
        openFileDialog.Filters.Add(new FileDialogFilter
        {
            Name = "所有文件",
            Extensions = { "*" }
        });

        // 设置对话框标题
        openFileDialog.Title = "选择DBC描述文件";

        // 获取当前窗口作为父窗口
        var window = TopLevel.GetTopLevel((Control)sender) as Window;
        // 使用 await 调用异步的 ShowAsync 方法（现在方法是 async 的，不会报错）
        var selectedFiles = await openFileDialog.ShowAsync(window);

        // 判断是否选择了文件
        if (selectedFiles != null && selectedFiles.Length > 0)
        {
            _viewModel.LoadDbcFile(selectedFiles[0]);
        }
    }

    // ✅ 只新增这一个方法，仅此一行代码
    private void BtnSendCanFrame_Click(object sender, RoutedEventArgs e)
    {
        var vm = this.DataContext as ViewModels.DbcViewModel;
        vm?.SendCanFrameByDbc();
    }

    // 修改后的【进入AT指令模式】按钮事件 - 核心：下发指令+标记等待，不立即弹窗
    private void BtnEnterATMode_Click(object sender, RoutedEventArgs e)
    {
        var vm = this.DataContext as ViewModels.DbcViewModel;
        vm?.EnterATMode();
    }
}