using Avalonia;
using Avalonia.Controls;
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

namespace cross;

public partial class GeneralDebugPage : UserControl
{
    private readonly AppViewModel _globalVM = AppViewModel.Instance;

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
        Setadd.IsVisible = !Setadd.IsVisible;
    }

    private void ButtonDelete_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = dataGrodx.SelectedItems.Cast<ModbusDataItem>().ToList();
        if (selectedItems.Count == 0)
        {
            _ = MessageBox.ShowAsync("请指定行！", "提示", MessageBoxButton.Ok, MessageBoxImage.Warning);
            return;
        }
        foreach (var item in selectedItems)
        {
            _globalVM.ModbusSet.ModbusDataList.Remove(item);
        }
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
}