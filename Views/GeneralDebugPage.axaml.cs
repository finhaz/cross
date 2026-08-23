using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System;
using cross.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThingLing.Controls;

namespace cross.Views;

public partial class GeneralDebugPage : UserControl
{
    private readonly AppViewModel _globalVM = AppViewModel.Instance;

    public GeneralDebugPage()
    {
        InitializeComponent();
        // 整页 DataContext 直接指向通用调试页 ViewModel。
        var vm = _globalVM.ModbusSet;
        DataContext = vm;
        // 关键：在构造函数里（布局之前）就把 DataGrid 直接接到列表上，
        // 避免 XAML 松散绑定在 ContentControl 嵌套场景下取不到集合导致表格无行。
        dataGrodx.ItemsSource = vm.ModbusDataList;
    }

    private void setsure_click(object sender, RoutedEventArgs e)
    {
        try
        {
            var vm = _globalVM.ModbusSet;
            // 诊断：显示点击确定时面板里实际读到的值，定位"添加不行"是
            // 值没回传（TwoWay 绑定没生效）还是加了没刷新。
            string before = $"名称=[{vm.AddItemName}] 数量=[{vm.Snum}] 地址=[{vm.Sadd}] 类型=[{vm.ProtocolConfig.SelectedDataType}]";
            diag_text.Text = "[添加前] " + before;
            vm.setsurehander(sender);
            Setadd.IsVisible = false;
            diag_text.Text = "[添加成功] " + before + $" 当前条目总数={vm.ModbusDataList.Count}";
        }
        catch (Exception ex)
        {
            diag_text.Text = "[添加失败] " + ex.Message;
            _ = MessageBox.ShowAsync($"添加条目失败：{ex.Message}", "错误",
                MessageBoxButton.Ok, MessageBoxImage.Error);
        }
    }

    private void setcancel_click(object sender, RoutedEventArgs e)
    {
        Setadd.IsVisible = false;
    }

    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        Setadd.IsVisible = !Setadd.IsVisible;
    }

    private void AdvancedColumns_Click(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.IsAdvancedColumnsVisible = !_globalVM.ModbusSet.IsAdvancedColumnsVisible;
    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrodx.SelectedItem is not ModbusDataItem selected)
        {
            _ = MessageBox.ShowAsync("请先在表格中选中要删除的行！", "提示",
                MessageBoxButton.Ok, MessageBoxImage.Warning);
            return;
        }
        _globalVM.ModbusSet.ModbusDataList.Remove(selected);
    }

    private void ShowText_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;
        textBox.CaretIndex = textBox.Text?.Length ?? 0;
    }

    private async void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filters.Add(new FileDialogFilter { Name = "JSON配置文件", Extensions = { "json" } });
        saveFileDialog.Title = "导出Modbus配置";
        saveFileDialog.InitialFileName = $"ModbusConfig_{DateTime.Now:yyyyMMddHHmmss}.json";
        string? fileName = await saveFileDialog.ShowAsync(topLevel);
        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                var configList = _globalVM.ModbusSet.ModbusDataList.ToList();
                string json = JsonConvert.SerializeObject(configList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(fileName, json);
                _ = MessageBox.ShowAsync("导出成功！", "提示", MessageBoxButton.Ok, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.ShowAsync($"导出失败：{ex.Message}", "错误", MessageBoxButton.Ok, MessageBoxImage.Error);
            }
        }
    }

    private async void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel == null) return;
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filters.Add(new FileDialogFilter { Name = "JSON配置文件", Extensions = { "json" } });
        openFileDialog.Title = "导入Modbus配置";
        string[]? fileNames = await openFileDialog.ShowAsync(topLevel);
        if (fileNames != null && fileNames.Length > 0)
        {
            try
            {
                string json = File.ReadAllText(fileNames[0]);
                var configList = JsonConvert.DeserializeObject<List<ModbusDataItem>>(json);
                _globalVM.ModbusSet.ModbusDataList.Clear();
                foreach (var item in configList)
                {
                    _globalVM.ModbusSet.ModbusDataList.Add(item);
                }
                _ = MessageBox.ShowAsync("导入成功！", "提示", MessageBoxButton.Ok, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _ = MessageBox.ShowAsync($"导入失败：{ex.Message}", "错误", MessageBoxButton.Ok, MessageBoxImage.Error);
            }
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_LoadedD(sender);

        var vm = _globalVM.ModbusSet;
        dataGrodx.ItemsSource = vm.ModbusDataList;

        int listCount = vm.ModbusDataList?.Count ?? -1;
        string diag = $"[通用调试] 已切换到 ItemsControl 渲染 | 条目数={listCount}";
        if (listCount > 0)
        {
            var first = vm.ModbusDataList[0];
            diag += $"; 第一行: 序号={first.ID}, 名称={first.Name}, 地址={first.Addr}, 呈现类型={first.DisplayType}";
        }
        else
        {
            diag += "; 列表为空！";
        }
        Console.WriteLine(diag);
        if (diag_text != null)
            diag_text.Text = diag;
        if (show_text != null)
            show_text.Text = diag;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_UnLoadedD(sender);
    }

    public void Dispose()
    {
        Page_Unloaded(null!, null!);
    }

    private void cPro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem == null) return;
        string selectedProtocol = comboBox.SelectedItem.ToString()!;
        _globalVM.ModbusSet.InitProtocol(selectedProtocol);
    }

    // 点击"数值"单元格：读取该条目当前值
    private void NumCell_Tapped(object sender, TappedEventArgs e)
    {
        var tb = sender as TextBlock;
        if (tb?.DataContext is ModbusDataItem item)
        {
            _globalVM.ModbusSet.ReadItem(item);
        }
    }
}