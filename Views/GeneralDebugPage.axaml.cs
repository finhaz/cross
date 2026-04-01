using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using cross.ViewModels;
using System;
using System.Collections.Generic;
using ThingLing.Controls;

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
        Setadd.Visibility = Visibility.Collapsed;
        _globalVM.ModbusSet.setsurehander(sender);
    }

    private void setcancel_click(object sender, RoutedEventArgs e)
    {
        Setadd.Visibility = Visibility.Collapsed;
    }


    private void ButtonAdd_Click(object sender, RoutedEventArgs e)
    {
        Setadd.Visibility = Visibility.Visible;
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

    private void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        // 打开保存文件对话框
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "JSON配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            Title = "导出Modbus配置",
            FileName = $"ModbusConfig_{DateTime.Now:yyyyMMddHHmmss}.json"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                // 方式1：如果是ObservableCollection<ModbusConfigItem>
                var configList = _globalVM.ModbusSet.ModbusDataList.ToList();
                string json = JsonConvert.SerializeObject(configList, Newtonsoft.Json.Formatting.Indented);

                //// 方式2：如果是DataTable
                //string json = JsonConvert.SerializeObject(_globalVM.ModbusSet.dtm, Newtonsoft.Json.Formatting.Indented);

                // 写入文件
                File.WriteAllText(saveFileDialog.FileName, json);
                MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        // 打开文件选择对话框
        var openFileDialog = new OpenFileDialog
        {
            Filter = "JSON配置文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            Title = "导入Modbus配置"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                string json = File.ReadAllText(openFileDialog.FileName);

                // 方式1：如果是ObservableCollection<ModbusConfigItem>
                var configList = JsonConvert.DeserializeObject<List<ModbusDataItem>>(json);
                _globalVM.ModbusSet.ModbusDataList.Clear(); // 清空原有数据
                foreach (var item in configList)
                {
                    _globalVM.ModbusSet.ModbusDataList.Add(item);
                }

                /*
                //方式2：如果是DataTable
                DataTable importedDt = JsonConvert.DeserializeObject<DataTable>(json);
                _globalVM.ModbusSet.dtm.Clear();
                foreach (DataRow row in importedDt.Rows)
                {
                     _globalVM.ModbusSet.dtm.ImportRow(row);
                }
                */
                MessageBox.ShowAsync("导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.ShowAsync($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_LoadedD(sender, e);

    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _globalVM.ModbusSet.Page_UnLoadedD(sender, e);
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