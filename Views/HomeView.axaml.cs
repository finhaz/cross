using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace cross;

public partial class HomeView : UserControl
{
    // 静态全局变量：存储当前选中的通讯类型（默认串口）
    public static string SelectedCommType = "串口";
    public HomeView()
    {
        InitializeComponent();
        // 默认选中第一个选项（串口）
        CommTypeComboBox.SelectedIndex = 0;
    }
    // 选择框切换事件：更新全局变量
    private void CommTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (CommTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            // 更新全局通讯类型
            SelectedCommType = selectedItem.Content.ToString() ?? "串口";
        }
    }

}