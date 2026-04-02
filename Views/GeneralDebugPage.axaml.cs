using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using cross.ViewModels;
using System;
using System.Collections.Generic;
using ThingLing.Controls;
using Newtonsoft.Json; // 必须添加，否则JsonConvert识别不到
using System.IO;
using System.Linq;
//using Microsoft.Win32;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace cross;

public partial class GeneralDebugPage : UserControl
{
    private AppViewModel _globalVM = AppViewModel.Instance;
    public GeneralDebugPage()
    {
        InitializeComponent();
        DataContext = _globalVM;
    }

    private void setsure_click(object sender, RoutedEventArgs e)
    {
        Setadd.IsVisible = false;
        _globalVM.ModbusSet.setsurehander(sender);
    }

    private void setcancel_click(object sender, RoutedEventArgs e)
    {
        Setadd.IsVisible = false;
    }


    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        Setadd.IsVisible = false;
    }

    /*
    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dataGrodx.SelectedItem is DataRowView selectedRowView)
        {
            _globalVM.ModbusSet.dtm.Rows.Remove(selectedRowView.Row);
        }
        else
        {
            MessageBox.Show("请指定行！");
        }
    }
    */
    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        // 多选删除：获取所有选中的ModbusDataItem
        var selectedItems = dataGrodx.SelectedItems.Cast<ModbusDataItem>().ToList();

        if (selectedItems.Count == 0)
        {
            MessageBox.ShowAsync("请指定行！");
            return;
        }

        // 逐个移除（避免遍历集合时修改集合导致异常）
        foreach (var item in selectedItems)
        {
            _globalVM.ModbusSet.ModbusDataList.Remove(item);
        }
    }


    private void ShowText_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        // 滚动到内容末尾（常用）
        //textBox.ScrollToEnd();
        textBox.CaretIndex = textBox.Text?.Length ?? 0;

        // 可选：滚动到顶部
        // textBox.ScrollToHome();

        // 可选：滚动到水平末尾
        // textBox.ScrollToRightEnd();
    }




    // 导出
    private async void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filters.Add(new FileDialogFilter { Name = "JSON配置文件", Extensions = { "json" } });
        saveFileDialog.Title = "导出Modbus配置";
        saveFileDialog.InitialFileName = $"ModbusConfig_{DateTime.Now:yyyyMMddHHmmss}.json";

        // 🔥 修复：自动获取父窗口，解决类型转换报错
        string fileName = await saveFileDialog.ShowAsync(TopLevel.GetTopLevel(this) as Window);

        if (!string.IsNullOrEmpty(fileName))
        {
            try
            {
                var configList = _globalVM.ModbusSet.ModbusDataList.ToList();
                string json = JsonConvert.SerializeObject(configList, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(fileName, json);
                MessageBox.ShowAsync("导出成功！", "提示", MessageBoxButton.Ok, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.ShowAsync($"导出失败：{ex.Message}", "错误", MessageBoxButton.Ok, MessageBoxImage.Error);
            }
        }
    }

    // 导入
    private async void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filters.Add(new FileDialogFilter { Name = "JSON配置文件", Extensions = { "json" } });
        openFileDialog.Title = "导入Modbus配置";

        // 🔥 修复：自动获取父窗口，解决类型转换报错
        string[] fileNames = await openFileDialog.ShowAsync(TopLevel.GetTopLevel(this) as Window);

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
                MessageBox.ShowAsync("导入成功！", "提示", MessageBoxButton.Ok, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.ShowAsync($"导入失败：{ex.Message}", "错误", MessageBoxButton.Ok, MessageBoxImage.Error);
            }
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_LoadedD(sender);

    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_UnLoadedD(sender);
    }

    // 释放资源
    public void Dispose()
    {
        Page_Unloaded(null, null);
    }

    private void cPro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem == null) return;

        string selectedProtocol = comboBox.SelectedItem.ToString();
        _globalVM.ModbusSet.InitProtocol(selectedProtocol);
    }

}