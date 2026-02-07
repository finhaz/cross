using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using cross.Communication;
using cross.ViewModels;
using System;
using System.Collections.Generic;

namespace cross;

public partial class HomeView : UserControl
{
    // 静态全局变量：存储当前选中的通讯类型（默认串口）
    public static string SelectedCommType = "串口";
    public HomeView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        // 默认选中第一个选项（串口）
        CommTypeComboBox.SelectedIndex = 0;
    }
    // 选择框切换事件：更新全局变量
    private void CommTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        //if (CommTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        //{
            // 更新全局通讯类型
        //    SelectedCommType = selectedItem.Content.ToString() ?? "串口";
        //}
    }

    // 在窗口加载后执行
    private void UserControl_Loaded(object? sender, RoutedEventArgs e)
    {
        // 遍历ComboBox的项，打印每个项的Key/Value
        foreach (var item in CommTypeComboBox.Items)
        {
            if (item is KeyValuePair<string, CommunicationType> kvp)
            {
                Console.WriteLine($"项：{kvp.Key} → {kvp.Value}");
            }
        }
    }

}