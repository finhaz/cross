using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SomeNameSpace;
using cross.Communication;
using cross.database;
using cross.Interfaces;
using cross.Mvvm;

namespace cross.ViewModels
{
    /// <summary>
    /// 设备控制页面 ViewModel（Avalonia 11 适配，基于 WPF DeviceControlPageViewModel 迁移）。
    /// </summary>
    public class DeviceControlPageViewModel : ObservableObject
    {
        // 自定义运行界面的地址
        private string _raddText = "128";
        public string RaddText
        {
            get => _raddText;
            set => SetProperty(ref _raddText, value);
        }

        // 日志 TextBlock（跨线程安全读写）
        public TextBlock<string> SerialTextBlock { get; } = new TextBlock<string>();

        private string _bshowContent = "数据采集";
        public string BshowContent
        {
            get => _bshowContent;
            set => SetProperty(ref _bshowContent, value);
        }

        private string _protocolNum = "Modbus RTU协议";
        public string ProtocolNum
        {
            get => _protocolNum;
            set => SetProperty(ref _protocolNum, value);
        }

        public ICommand ButtonIncrease { get; }
        public ICommand SelectDatabaseFileCommand { get; }
        public ICommand LoadDatabaseTablesCommand { get; }
        public ICommand SaveDataToDbCommand { get; }

        // 数据库 / 数据表
        public int SelectIndex { get; set; }
        public string NewValue { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public bool IsShowing { get; set; }
        public int RunNum { get; set; }

        private readonly byte[] _sendbf = new byte[128];
        private int _sn;

        private DispatcherTimer? _dataTimer;
        private readonly object _logLock = new object();

        private IProtocol? _currentProtocol;
        private ICommunication? _comm;

        public DeviceControlPageViewModel()
        {
            ButtonIncrease = new RelayCommand<object>(
                _ => ButtonIncreaseAction(),
                _ => true
            );

            SelectDatabaseFileCommand = new RelayCommand<object>(
                _ => SelectDatabaseFile(),
                _ => true
            );

            LoadDatabaseTablesCommand = new RelayCommand<object>(
                _ => LoadDatabaseTables(),
                _ => true
            );

            SaveDataToDbCommand = new RelayCommand<object>(
                _ => SaveDataToDb(),
                _ => true
            );

            InitTimer();
        }

        /// <summary>
        /// 由 code-behind 传入运行参数页的单元格映射
        /// </summary>
        public void SetShowCellMap(
            Dictionary<int, Dictionary<int, Avalonia.Controls.TextBlock>>? map)
        {
            ShowCellMap = map;
        }

        /// <summary>
        /// 数据表加载完成事件（code-behind 订阅后填充 ListBox）
        /// </summary>
        public event Action? TablesLoaded;

        // ========== 数据库绑定属性 ==========
        private string _dbFilePath = string.Empty;
        public string DbFilePath
        {
            get => _dbFilePath;
            set => SetProperty(ref _dbFilePath, value);
        }

        private DataTable _dtrun = new DataTable();
        public DataTable Dtrun
        {
            get => _dtrun;
            set => SetProperty(ref _dtrun, value);
        }

        private DataTable _dtset = new DataTable();
        public DataTable Dtset
        {
            get => _dtset;
            set => SetProperty(ref _dtset, value);
        }

        private DataTable _dtfactor = new DataTable();
        public DataTable Dtfactor
        {
            get => _dtfactor;
            set => SetProperty(ref _dtfactor, value);
        }

        // DataGrid 绑定用 ObservableCollection<Dictionary<string, object>>，
        // Avalonia 11 DataGrid 不认 DataTable.DefaultView，改用字典集合
        // 每次列名/行数变化都创建新实例强制刷新
        private ObservableCollection<object>? _dtrunRows;
        public ObservableCollection<object>? DtrunRows
        {
            get => _dtrunRows;
            set => SetProperty(ref _dtrunRows, value);
        }

        private ObservableCollection<object>? _dtsetRows;
        public ObservableCollection<object>? DtsetRows
        {
            get => _dtsetRows;
            set => SetProperty(ref _dtsetRows, value);
        }

        private ObservableCollection<object>? _dtfactorRows;
        public ObservableCollection<object>? DtfactorRows
        {
            get => _dtfactorRows;
            set => SetProperty(ref _dtfactorRows, value);
        }

        private IDatabaseOperation? _dbOperation;

        // 运行参数页单元映射：行索引 → (列索引 → TextBlock)
        // 供 HandleSerialData 更新 UI 时直接使用
        internal Dictionary<int, Dictionary<int, Avalonia.Controls.TextBlock>>? ShowCellMap;

        // ========== 协议切换 ==========
        public void InitProtocol(string protocolNum)
        {
            _currentProtocol = protocolNum switch
            {
                "FE协议" => COMFE.Instance,
                "Modbus RTU协议" => COMModbus.Instance,
                "Modbus TCP协议" => TCPModbus.Instance,
                _ => throw new ArgumentException($"不支持的协议类型：{protocolNum}")
            };
        }

        // ========== 运行 / 停止控制 ==========
        public void RunStopControl(int addr, bool brun)
        {
            if (_currentProtocol == null)
            {
                AppendLog("错误：请先选择协议！");
                return;
            }
            if (_comm == null || !_comm.IsConnected)
            {
                AppendLog("错误：请先打开串口！");
                return;
            }

            try
            {
                int sendNum = _currentProtocol.MonitorRun(_sendbf, brun, addr);
                _sn = addr;
                IsRunning = brun;

                _comm.Send(_sendbf, 0, sendNum);

                string txt = SerialDataProcessor.Instance.FormatSerialDataToHexString(
                    _sendbf, sendNum, "TX:", true);
                AppendLog(txt);
            }
            catch (Exception ex)
            {
                AppendLog($"运行控制异常：{ex.Message}");
            }
        }

        // ========== 设定按钮处理 ==========
        public async void MButtonSet(string table, int index)
        {
            DataTable? target = table switch
            {
                "PARAMETER_FACTOR" => Dtfactor,
                _ => Dtset
            };

            if (target == null || target.Rows.Count <= index || index < 0)
            {
                await DialogHelper.ShowAsync("选中行无效！");
                return;
            }

            try
            {
                string y = target.Rows[0][0].ToString();
                int offset = Convert.ToInt32(y);
                int tempsn = index + offset;

                string val = (table == "PARAMETER_FACTOR")
                    ? target.Rows[index][2].ToString()
                    : target.Rows[index][5].ToString();
                float value = Convert.ToSingle(val);

                if (_dbOperation != null)
                {
                    _dbOperation.DataBase_SET_Save(table, value, (byte)tempsn);
                }

                if (_comm == null || !_comm.IsConnected)
                {
                    await DialogHelper.ShowAsync("请打开串口！", "提示");
                    return;
                }

                if (_currentProtocol == null)
                {
                    await DialogHelper.ShowAsync("请先选择协议！", "提示");
                    return;
                }

                int sendNum = _currentProtocol.MonitorSet(_sendbf, tempsn, value);
                _comm.Send(_sendbf, 0, sendNum);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowAsync($"设定失败：{ex.Message}", "错误");
            }
        }

        // ========== 数据采集按钮 ==========
        private async void ButtonIncreaseAction()
        {
            if (_comm == null || !_comm.IsConnected)
            {
                await DialogHelper.ShowAsync("请打开串口！", "提示");
                return;
            }

            IsShowing = !IsShowing;
            if (IsShowing)
            {
                BshowContent = "停止采集";
                StartTimer();
            }
            else
            {
                BshowContent = "数据采集";
                StopTimer();
            }
        }

        // ========== 通讯事件 ==========
        public void Page_LoadedD(object sender)
        {
            try
            {
                _comm = CommunicationManager.Instance.GetCurrentCommunication();
                if (_comm != null)
                {
                    _comm.DataReceived += HandleSerialDataWrapper;
                }
            }
            catch (InvalidOperationException)
            {
                CommunicationManager.Instance.CreateSerialInstance();
                _comm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
                _comm.DataReceived += HandleSerialDataWrapper;
            }
            catch (Exception)
            {
                CommunicationManager.Instance.CreateSerialInstance();
                _comm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
                _comm.DataReceived += HandleSerialDataWrapper;
            }
        }

        public void Page_UnloadedD(object sender)
        {
            if (_comm != null)
                _comm.DataReceived -= HandleSerialDataWrapper;

            StopTimer();
            _dataTimer = null;
        }

        private void HandleSerialDataWrapper(object sender, DataReceivedEventArgs e)
        {
            HandleSerialData(e.Buffer, e.LastIndex, e.BufferLength);
        }

        // ========== 串口数据处理 ==========
        public void HandleSerialData(byte[] gbuffer, int gbLast, int bufferLen)
        {
            byte[] buffer = new byte[200];
            int checkResult;

            string str = SerialDataProcessor.Instance.FormatSerialDataToHexString(
                gbuffer, bufferLen, "RX:", gbLast, true);
            AppendLog(str);

            Array.Copy(gbuffer, gbLast, buffer, 0, bufferLen);

            try
            {
                if (_currentProtocol == null)
                    return;

                checkResult = _currentProtocol.MonitorCheck(buffer, bufferLen);
            }
            catch
            {
                AppendLog("RX:解析异常");
                return;
            }

            if (checkResult == 1)
            {
                if (_currentProtocol == null) return;

                try
                {
                    DataR datax = _currentProtocol.MonitorSolve(buffer, _sn - 1);
                    if (datax.RWX == 1 && Dtrun != null && Dtrun.Rows.Count > datax.SN)
                    {
                        int colIdx = 5;
                        Dtrun.Rows[datax.SN][colIdx] = datax.VALUE;

                        // 更新 UI 单元格（如果已加载）
                        UpdateShowCell(datax.SN, colIdx, datax.VALUE);
                    }
                }
                catch
                {
                    AppendLog("RX:解析异常");
                }
            }
            else
            {
                AppendLog("RX:Wrong");
            }
        }

        // ========== 定时器 ==========
        private void InitTimer()
        {
            _dataTimer = new DispatcherTimer();
            _dataTimer.Tick += DataTimer_Tick;
            _dataTimer.Interval = TimeSpan.FromSeconds(10);
        }

        private void DataTimer_Tick(object? sender, EventArgs e)
        {
            ShowStop();
        }

        public void StartTimer()
        {
            if (_dataTimer != null && !_dataTimer.IsEnabled)
                _dataTimer.Start();
        }

        public void StopTimer()
        {
            if (_dataTimer != null && _dataTimer.IsEnabled)
                _dataTimer.Stop();
        }

        private void ShowStop()
        {
            if (!IsShowing || _currentProtocol == null || _comm == null) return;

            try
            {
                if (_sn == RunNum)
                    _sn = 0;

                int sendNum = _currentProtocol.MonitorGet(_sendbf, _sn, 1);
                _sn++;

                _comm.Send(_sendbf, 0, sendNum);

                string str = SerialDataProcessor.Instance.FormatSerialDataToHexString(
                    _sendbf, sendNum, "TX:", 0, true);
                AppendLog(str);
            }
            catch (Exception ex)
            {
                AppendLog($"采集异常：{ex.Message}");
            }
        }

        // ========== 日志 ==========
        private void AppendLog(string txt)
        {
            lock (_logLock)
            {
                SerialTextBlock.Text += txt + "\r\n";
            }
        }

        // ========== 数据库操作 ==========
        private async void SelectDatabaseFile()
        {
            try
            {
                var topLevel = (Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop)
                    ? desktop.MainWindow : null;
                if (topLevel == null) return;

                var dialog = new OpenFileDialog
                {
                    Title = "选择数据库文件",
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "数据库文件", Extensions = { "db", "mdb", "accdb" } }
                    }
                };

                string[]? result = await dialog.ShowAsync(topLevel);
                if (result != null && result.Length > 0)
                {
                    DbFilePath = result[0];
                    InitDbOperation(DbFilePath);
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowAsync($"选择文件失败：{ex.Message}", "错误");
            }
        }

        private async void LoadDatabaseTables()
        {
            if (string.IsNullOrEmpty(DbFilePath) || _dbOperation == null)
            {
                await DialogHelper.ShowAsync("请先选择有效的数据库文件！");
                return;
            }

            try
            {
                if (!_dbOperation.IsTableExist("PARAMETER_RUN"))
                {
                    await DialogHelper.ShowAsync("表 PARAMETER_RUN 不存在！");
                    return;
                }

                DataTable loadedRun = _dbOperation.GetDBTable("PARAMETER_RUN");
                DataTable loadedSet = _dbOperation.GetDBTable("PARAMETER_SET");
                DataTable loadedFactor = _dbOperation.GetDBTable("PARAMETER_FACTOR");

                // 保持底层 DataTable 不变（用于 MButtonSet / MonitorSolve 读取）
                CopyDataTable(loadedRun, Dtrun);
                CopyDataTable(loadedSet, Dtset);
                CopyDataTable(loadedFactor, Dtfactor);

                // DataGrid 绑定 ObservableCollection<Dictionary<string, object>>
                // 新建集合实例强制刷新（Avalonia 11 DataGrid 对 DataView 不支持）
                DtrunRows = DataTableToRows(Dtrun);
                DtsetRows = DataTableToRows(Dtset);
                DtfactorRows = DataTableToRows(Dtfactor);

                RunNum = Dtrun.Rows.Count;

                TablesLoaded?.Invoke();

                await DialogHelper.ShowAsync(
                    $"数据表读取成功！运行参数 {DtrunRows.Count} 行，" +
                    $"调试参数 {DtsetRows.Count} 行，修正系数 {DtfactorRows.Count} 行", "提示");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowAsync($"读取失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 将 DataTable 转成 DataGrid 可绑定的集合：每行一个 ExpandoObject
        /// （Avalonia 11 Binding 不支持 Dictionary 索引器，改用 ExpandoObject
        /// 通过 IDynamicMetaObjectProvider 解析动态属性）
        /// </summary>
        private ObservableCollection<object> DataTableToRows(DataTable table)
        {
            var result = new ObservableCollection<object>();
            foreach (DataRow row in table.Rows)
            {
                ExpandoObject expando = new();
                var dict = (IDictionary<string, object>)expando;
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col] is DBNull ? string.Empty : row[col];
                }
                result.Add(expando);
            }
            return result;
        }

        private void CopyDataTable(DataTable source, DataTable target)
        {
            target.Clear();
            foreach (DataColumn col in source.Columns)
            {
                if (!target.Columns.Contains(col.ColumnName))
                    target.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataRow row in source.Rows)
            {
                target.ImportRow(row);
            }
        }

        private async void SaveDataToDb()
        {
            if (string.IsNullOrEmpty(DbFilePath) || _dbOperation == null)
            {
                await DialogHelper.ShowAsync("请先选择有效的数据库文件！");
                return;
            }

            try
            {
                if (Dtrun != null && Dtrun.Rows.Count > 0)
                    _dbOperation.UpdateDBTable(Dtrun, "PARAMETER_RUN");
                if (Dtset != null && Dtset.Rows.Count > 0)
                    _dbOperation.UpdateDBTable(Dtset, "PARAMETER_SET");
                if (Dtfactor != null && Dtfactor.Rows.Count > 0)
                    _dbOperation.UpdateDBTable(Dtfactor, "PARAMETER_FACTOR");

                await DialogHelper.ShowAsync("所有数据已成功保存到数据库！", "提示");
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowAsync($"保存失败：{ex.Message}", "错误");
            }
        }

        private void UpdateShowCell(int rowIdx, int colIdx, object value)
        {
            if (ShowCellMap == null || rowIdx < 0 || rowIdx >= ShowCellMap.Count)
                return;

            if (!ShowCellMap.TryGetValue(rowIdx, out var cells))
                return;

            if (!cells.TryGetValue(colIdx, out var cell))
                return;

            UiDispatcherHelper.ExecuteOnUiThread(() =>
            {
                cell.Text = value?.ToString() ?? string.Empty;
            });
        }

        private void InitDbOperation(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            string connStr;

            switch (ext)
            {
                case ".db":
                    connStr = $"Data Source={filePath};Version=3;";
                    _dbOperation = new DB_SQLlite(connStr);
                    break;
                case ".mdb":
                    connStr = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};DBQ={filePath};";
                    _dbOperation = new DB_Access(connStr);
                    break;
                case ".accdb":
                    connStr = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};DBQ={filePath};";
                    _dbOperation = new DB_Access(connStr);
                    break;
                default:
                    throw new NotSupportedException($"不支持的数据库类型：{ext}");
            }
        }
    }
}
