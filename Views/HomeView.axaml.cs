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
        if (CommTypeComboBox != null)
        {
            switch (CommTypeComboBox.SelectedValue)
            {
                case CommunicationType.SerialPort:
                    // 选择串口：创建串口实例（仅创建，不打开/不绑定事件）
                    CommunicationManager.Instance.CreateSerialInstance();
                    break;
                case CommunicationType.CAN:
                    // 针对TTL转CAN，创建串口实例（仅创建，不打开/不绑定事件）
                    CommunicationManager.Instance.CreateSerialInstance();
                    break;
                case CommunicationType.Ethernet:
                    // 仅创建网络通讯空实例，不指定TCP/UDP
                    CommunicationManager.Instance.CreateEthernetInstance();
                    break;
                case CommunicationType.IIC:
                    // 预留，暂不处理
                    break;
            }
        }
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