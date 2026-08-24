using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup;
using cross.ViewModels;

namespace cross.Views
{
    public partial class DeviceControlPage : UserControl
    {
        private DeviceControlPageViewModel _vm;

        // 运行参数页：行索引 → (列索引 → TextBlock)
        private readonly Dictionary<int, Dictionary<int, TextBlock>> _showCellMap = new();

        public DeviceControlPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = new DeviceControlPageViewModel();
            DataContext = _vm;

            _vm.Page_LoadedD(sender);

            // 数据表加载完成 → 为 ListBox 填充预构建的 StackPanel 项
            _vm.TablesLoaded += OnTablesLoaded;

            // 刷新协议选项
            CbPro.ItemsSource = new string[]
            {
                "FE协议", "Modbus RTU协议", "Modbus TCP协议"
            };
            CbPro.SelectedItem = _vm.ProtocolNum;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_vm != null)
                _vm.TablesLoaded -= OnTablesLoaded;

            _vm.Page_UnloadedD(sender);
            _vm = null;
        }

        /// <summary>
        /// 数据表加载完成 → 为每个 ListBox 创建 StackPanel 项并填充
        /// </summary>
        private void OnTablesLoaded()
        {
            _showCellMap.Clear();
            BuildListBox(DataShowList, _vm.DtrunRows, _vm.Dtrun, _showCellMap);
            BuildListBox(DataSetList, _vm.DtsetRows, _vm.Dtset, null);
            BuildListBox(DataFactorList, _vm.DtfactorRows, _vm.Dtfactor, null);

            _vm.SetShowCellMap(_showCellMap);
        }

        /// <summary>
        /// 为 ListBox 创建 StackPanel 项：每行一个 StackPanel
        ///   顶部一行列头 + 下方内容行，每列一个 TextBlock
        /// </summary>
        private void BuildListBox(ListBox listBox, ObservableCollection<object>? rows,
                                  DataTable table,
                                  Dictionary<int, Dictionary<int, TextBlock>>? cellMap)
        {
            if (rows == null || rows.Count == 0)
            {
                listBox.ItemsSource = new List<object>();
                return;
            }

            var columnNames = table.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName).ToList();
            int colCount = columnNames.Count;

            // 列宽：根据列数自适应
            int cellWidth = Math.Max(50, Math.Min(200, 800 / Math.Max(colCount, 1)));

            var items = new ObservableCollection<object>();

            for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
            {
                var rowDict = (IDictionary<string, object>)rows[rowIdx];
                var outerPanel = new StackPanel { Orientation = Orientation.Vertical };

                // 列头行：用 Border 包裹提供 Padding
                var headerRow = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(
                        new Avalonia.Media.Color(0xFF, 0x37, 0x41, 0x51)),
                    Padding = new Thickness(4, 2, 4, 2),
                    Margin = new Thickness(0, 4, 0, 2)
                };
                var headerContent = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (string colName in columnNames)
                {
                    var headerText = new TextBlock
                    {
                        Text = colName,
                        FontSize = 11,
                        Foreground = Avalonia.Media.Brushes.White,
                        Margin = new Thickness(2),
                        Width = cellWidth,
                        MinWidth = 40
                    };
                    headerContent.Children.Add(headerText);
                }
                headerRow.Child = headerContent;
                outerPanel.Children.Add(headerRow);

                // 内容行
                var contentRow = new StackPanel { Orientation = Orientation.Horizontal };
                var cellDict = new Dictionary<int, TextBlock>();

                for (int colIdx = 0; colIdx < columnNames.Count; colIdx++)
                {
                    string colName = columnNames[colIdx];
                    object? val = rowDict.ContainsKey(colName)
                        ? rowDict[colName]
                        : string.Empty;

                    var cellText = new TextBlock
                    {
                        Text = val?.ToString() ?? string.Empty,
                        FontSize = 11,
                        Margin = new Thickness(2),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Width = cellWidth,
                        MinWidth = 40
                    };
                    contentRow.Children.Add(cellText);

                    if (cellMap != null)
                        cellDict[colIdx] = cellText;
                }
                outerPanel.Children.Add(contentRow);

                if (cellMap != null)
                    cellMap[rowIdx] = cellDict;

                items.Add(outerPanel);
            }

            listBox.ItemsSource = items;
        }

        #region 运行 / 停止
        private void BtRun_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(_vm.RaddText, out int addr))
            {
                _vm.RunStopControl(addr, true);
                StatusText.Text = "系统正在运行";
                StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
            }
            else
            {
                StatusText.Text = "地址格式错误";
            }
        }

        private void BtStop_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(_vm.RaddText, out int addr))
            {
                _vm.RunStopControl(addr, false);
                StatusText.Text = "系统停止运行";
                StatusText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
            }
        }
        #endregion

        #region 协议选择
        private void CbPro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem == null) return;

            string selectedProtocol = comboBox.SelectedItem.ToString() ?? string.Empty;
            try
            {
                _vm.InitProtocol(selectedProtocol);
            }
            catch (ArgumentException ex)
            {
                _ = DialogHelper.ShowAsync($"协议切换失败：{ex.Message}", "错误");
            }
        }
        #endregion

        #region ListBox 选中行跟踪
        private void DataShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 运行参数页不需要显示索引
        }

        private void DataSetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DebugSelectIdx.Text = DataSetList.SelectedIndex.ToString();
        }

        private void DataFactorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FactorSelectIdx.Text = DataFactorList.SelectedIndex.ToString();
        }
        #endregion

        #region 设定按钮
        private void BtDebugSet_Click(object sender, RoutedEventArgs e)
        {
            int idx = DataSetList.SelectedIndex;
            _vm.MButtonSet("PARAMETER_SET", idx);
        }

        private void BtFactorSet_Click(object sender, RoutedEventArgs e)
        {
            int idx = DataFactorList.SelectedIndex;
            _vm.MButtonSet("PARAMETER_FACTOR", idx);
        }
        #endregion

        #region 日志滚动
        private void ShowText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ShowTextBox.Text != null && ShowTextBox.Text.Length > 0)
                ShowTextBox.CaretIndex = ShowTextBox.Text.Length;
        }
        #endregion
    }
}
