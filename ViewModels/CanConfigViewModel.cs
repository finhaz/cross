using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using cross.Communication;
using cross.Interfaces;
using cross.Mvvm;

namespace cross.ViewModels
{
    /// <summary>
    /// CAN 配置 ViewModel（Avalonia 11 适配，基于 WPF CanConfigViewModel 迁移）。
    /// </summary>
    public class CanConfigViewModel : ObservableObject, IDisposable
    {
        private readonly SerialCommunication _serialComm;

        public SerialPortConfig PortConfig { get; } = new SerialPortConfig();

        #region 工作模式
        private int _selectedWorkMode = 0;
        public int SelectedWorkMode { get => _selectedWorkMode; set => SetProperty(ref _selectedWorkMode, value); }
        #endregion

        #region CAN 波特率
        private int _selectedCanBaud = 5; // 默认 500Kbps
        public int SelectedCanBaud { get => _selectedCanBaud; set => SetProperty(ref _selectedCanBaud, value); }
        #endregion

        #region 串口配置
        private int _selectedSerialBaud = 1; // 默认 9600
        public int SelectedSerialBaud { get => _selectedSerialBaud; set => SetProperty(ref _selectedSerialBaud, value); }

        private int _selectedDataBit = 0;
        public int SelectedDataBit { get => _selectedDataBit; set => SetProperty(ref _selectedDataBit, value); }

        private int _selectedStopBit = 1;
        public int SelectedStopBit { get => _selectedStopBit; set => SetProperty(ref _selectedStopBit, value); }

        private int _selectedParity = 0;
        public int SelectedParity { get => _selectedParity; set => SetProperty(ref _selectedParity, value); }

        private bool _isTransEnable = true;
        public bool IsTransEnable { get => _isTransEnable; set => SetProperty(ref _isTransEnable, value); }
        #endregion

        #region 帧格式 / 滤波器
        private int _selectedFrameFormat = 0;
        public int SelectedFrameFormat { get => _selectedFrameFormat; set => SetProperty(ref _selectedFrameFormat, value); }

        private string _stdId = "136";
        public string StdId { get => _stdId; set => SetProperty(ref _stdId, value); }

        private string _extId = "0";
        public string ExtId { get => _extId; set => SetProperty(ref _extId, value); }

        private bool _isFilterEnable = false;
        public bool IsFilterEnable { get => _isFilterEnable; set => SetProperty(ref _isFilterEnable, value); }

        private string _filterId = "0";
        public string FilterId { get => _filterId; set => SetProperty(ref _filterId, value); }

        private string _maskId = "0";
        public string MaskId { get => _maskId; set => SetProperty(ref _maskId, value); }
        #endregion

        #region 状态文本
        private string _atModuleStatus = "就绪，请先配置并打开串口";
        public string AtModuleStatus { get => _atModuleStatus; set => SetProperty(ref _atModuleStatus, value); }
        #endregion

        #region 下拉选项
        public List<CanBaudItem> CanBaudList { get; } = new()
        {
            new CanBaudItem { Value = 10000,  Text = "10Kbps"  },
            new CanBaudItem { Value = 20000,  Text = "20Kbps"  },
            new CanBaudItem { Value = 50000,  Text = "50Kbps"  },
            new CanBaudItem { Value = 100000, Text = "100Kbps" },
            new CanBaudItem { Value = 250000, Text = "250Kbps" },
            new CanBaudItem { Value = 500000, Text = "500Kbps" },
            new CanBaudItem { Value = 1000000, Text = "1Mbps"  }
        };

        public List<KeyValuePair<int, string>> WorkModeOptions { get; } = new()
        {
            new KeyValuePair<int, string>(0, "普通模式"),
            new KeyValuePair<int, string>(1, "回环模式"),
            new KeyValuePair<int, string>(2, "监听模式")
        };

        public List<KeyValuePair<int, string>> FrameFormatOptions { get; } = new()
        {
            new KeyValuePair<int, string>(0, "标准帧"),
            new KeyValuePair<int, string>(1, "扩展帧")
        };
        #endregion

        #region 串口事件
        // 预留标记（原有逻辑的等待状态，暂未启用）
        private StringBuilder _recvBuffer = new StringBuilder();
        private TaskCompletionSource<string> _atCommandTcs;
        private readonly object _lock = new object();
        #endregion

        public CanConfigViewModel()
        {
            try
            {
                _serialComm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
            }
            catch (InvalidOperationException)
            {
                CommunicationManager.Instance.CreateSerialInstance();
                _serialComm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
            }
            catch (Exception)
            {
                CommunicationManager.Instance.CreateSerialInstance();
                _serialComm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
            }

            _serialComm.Encoding = Encoding.Default;
            _serialComm.DataReceived += SerialComm_DataReceived;
            PortConfig.InitPortNames();
        }

        private void SerialComm_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (_lock)
            {
                string received = Encoding.Default.GetString(e.Buffer, e.LastIndex, e.BufferLength);
                _recvBuffer.Append(received);
                if (_atCommandTcs != null &&
                    (_recvBuffer.ToString().EndsWith("\r\nOK\r\n") ||
                     _recvBuffer.ToString().EndsWith("\r\nERROR\r\n")))
                {
                    var tcs = _atCommandTcs;
                    _atCommandTcs = null;
                    tcs.SetResult(_recvBuffer.ToString());
                    _recvBuffer.Clear();
                }
            }
        }

        #region AT 指令发送
        private async Task<string> SendAtCommandAsync(string cmd, int timeoutMs = 1500)
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "串口未连接";
                return string.Empty;
            }

            try
            {
                _atCommandTcs = new TaskCompletionSource<string>();
                _recvBuffer.Clear();

                byte[] bytes = Encoding.ASCII.GetBytes(cmd);
                _serialComm.Send(bytes, 0, bytes.Length);

                return await _atCommandTcs.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (TimeoutException)
            {
                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private async Task<bool> EnterConfigModeAsync()
        {
            string result = await SendAtCommandAsync("AT+CG\r\n", 1500);
            return result.Contains("OK", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ExitConfigModeAsync()
        {
            await SendAtCommandAsync("AT+ET\r\n", 1000);
        }
        #endregion

        #region 打开/关闭串口
        public bool OpenSerialPort()
        {
            if (_serialComm == null || _serialComm.IsConnected)
            {
                AtModuleStatus = "串口已打开";
                return false;
            }

            if (string.IsNullOrEmpty(PortConfig.SelectedPortName))
            {
                AtModuleStatus = "未选择串口";
                return false;
            }

            try
            {
                var cfg = new SerialConfig
                {
                    SelectedPortName = PortConfig.SelectedPortName,
                    SelectedBaudRate = PortConfig.BaudRates[_selectedSerialBaud],
                    SelectedDataBits = PortConfig.DataBits[_selectedDataBit],
                    SelectedParityName = PortConfig.ParityNames[_selectedParity],
                    SelectedStopBit = PortConfig.StopBitValues[_selectedStopBit]
                };
                _serialComm.Open(cfg);
                _serialComm.Encoding = Encoding.Default;
                AtModuleStatus = $"串口已打开: {PortConfig.SelectedPortName}";
                return true;
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"串口打开失败: {ex.Message}";
                return false;
            }
        }

        public void CloseSerialPort()
        {
            try
            {
                _serialComm?.Close();
                AtModuleStatus = "串口已关闭";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"串口关闭失败: {ex.Message}";
            }
        }
        #endregion

        #region 刷新串口列表
        public void RefreshPortNames()
        {
            PortConfig.InitPortNames();
        }
        #endregion

        #region 读取配置
        public async Task ReadCanConfigAsync()
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "请先打开串口";
                return;
            }

            AtModuleStatus = "正在读取 CAN 模块配置...";

            try
            {
                if (!await EnterConfigModeAsync())
                {
                    AtModuleStatus = "进入 AT 配置模式失败";
                    return;
                }

                await Task.Delay(50);
                await ReadAndApply("AT+CAN_MODE=?\r\n");
                await ReadAndApply("AT+CAN_BAUD=?\r\n");
                await ReadAndApply("AT+USART_PARAM=?\r\n");
                await ReadAndApply("AT+CAN_FRAMEFORMAT=?\r\n");
                await ReadAndApply("AT+CAN_FILTER0=?\r\n");

                await ExitConfigModeAsync();
                AtModuleStatus = "配置读取完成";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"读取异常: {ex.Message}";
            }
        }

        private async Task ReadAndApply(string cmd)
        {
            string result = await SendAtCommandAsync(cmd, 2000);
            if (string.IsNullOrEmpty(result)) return;
            string value = result.Replace("\r", "").Replace("\n", "").Trim();
            ApplyParsedValue(cmd, value);
        }

        private void ApplyParsedValue(string cmd, string value)
        {
            if (cmd.Contains("CAN_MODE"))
            {
                if (ParseIntFromResult(value, out int mode))
                    SetProperty(ref _selectedWorkMode, mode);
                return;
            }

            if (cmd.Contains("CAN_BAUD"))
            {
                if (ParseIntFromResult(value, out int baud))
                {
                    int idx = CanBaudList.FindIndex(x => x.Value == baud);
                    if (idx >= 0) SetProperty(ref _selectedCanBaud, idx);
                }
                return;
            }

            if (cmd.Contains("USART_PARAM"))
            {
                // 示例解析 "AT+USART_PARAM=115200,8,1,0"
                string[] parts = value.Split(new[] { '=', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[1], out int sb) && PortConfig.BaudRates.Contains(sb))
                        _selectedSerialBaud = PortConfig.BaudRates.IndexOf(sb);
                    if (int.TryParse(parts[2], out int db) && PortConfig.DataBits.Contains(db))
                        _selectedDataBit = PortConfig.DataBits.IndexOf(db);
                    if (int.TryParse(parts[3], out int stpb))
                        _selectedStopBit = stpb - 1;
                    if (int.TryParse(parts[4], out int par))
                        _selectedParity = par;
                }
                OnPropertyChanged(nameof(_selectedSerialBaud));
                OnPropertyChanged(nameof(_selectedDataBit));
                OnPropertyChanged(nameof(_selectedStopBit));
                OnPropertyChanged(nameof(_selectedParity));
                return;
            }

            if (cmd.Contains("CAN_FRAMEFORMAT"))
            {
                string[] parts = value.Split(new[] { '=', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[1], out int trans))
                        SetProperty(ref _isTransEnable, trans != 0);
                    if (int.TryParse(parts[2], out int ff))
                        SetProperty(ref _selectedFrameFormat, ff);
                    if (parts.Length > 2) SetProperty(ref _stdId, parts[3]);
                    if (parts.Length > 3) SetProperty(ref _extId, parts[4]);
                }
                return;
            }

            if (cmd.Contains("CAN_FILTER0"))
            {
                string[] parts = value.Split(new[] { '=', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[1], out int fe))
                        SetProperty(ref _isFilterEnable, fe != 0);
                    if (parts.Length > 3) SetProperty(ref _filterId, parts[3]);
                    if (parts.Length > 4) SetProperty(ref _maskId, parts[4]);
                }
            }
        }
        #endregion

        #region 保存配置
        public async Task SaveCanConfigAsync()
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "请先打开串口";
                return;
            }

            AtModuleStatus = "正在保存 CAN 配置...";

            try
            {
                if (!await EnterConfigModeAsync())
                {
                    AtModuleStatus = "进入 AT 配置模式失败";
                    return;
                }

                await Task.Delay(50);
                await SendAtCommandAsync($"AT+CAN_MODE={_selectedWorkMode}\r\n");
                int baud = _selectedCanBaud >= 0 && _selectedCanBaud < CanBaudList.Count
                    ? CanBaudList[_selectedCanBaud].Value : 500000;
                await SendAtCommandAsync($"AT+CAN_BAUD={baud}\r\n");

                int serialBaud = _selectedSerialBaud >= 0 && _selectedSerialBaud < PortConfig.BaudRates.Count
                    ? PortConfig.BaudRates[_selectedSerialBaud] : 9600;
                int dataBit = _selectedDataBit >= 0 && _selectedDataBit < PortConfig.DataBits.Count
                    ? PortConfig.DataBits[_selectedDataBit] : 8;
                await SendAtCommandAsync($"AT+USART_PARAM={serialBaud},{dataBit},{_selectedStopBit + 1},{_selectedParity}\r\n");

                int transEn = _isTransEnable ? 1 : 0;
                await SendAtCommandAsync($"AT+CAN_FRAMEFORMAT={transEn},{_selectedFrameFormat},{_stdId},{_extId}\r\n");

                int filterEn = _isFilterEnable ? 1 : 0;
                await SendAtCommandAsync($"AT+CAN_FILTER0={filterEn},0,{_filterId},{_maskId}\r\n");

                await SendAtCommandAsync("AT+SAVE\r\n");
                await ExitConfigModeAsync();
                AtModuleStatus = "配置保存成功，重启模块生效";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"保存异常: {ex.Message}";
            }
        }
        #endregion

        #region 恢复默认
        public async Task RestoreDefaultAsync()
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "请先打开串口";
                return;
            }

            AtModuleStatus = "正在恢复出厂配置...";

            try
            {
                if (!await EnterConfigModeAsync())
                {
                    AtModuleStatus = "进入 AT 配置模式失败";
                    return;
                }

                await SendAtCommandAsync("AT+DEFAULT\r\n");
                await ExitConfigModeAsync();

                ResetDefaults();
                AtModuleStatus = "出厂配置恢复完成";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"恢复异常: {ex.Message}";
            }
        }

        private void ResetDefaults()
        {
            _selectedWorkMode = 0; OnPropertyChanged(nameof(SelectedWorkMode));
            _selectedCanBaud = 5; OnPropertyChanged(nameof(SelectedCanBaud));
            _selectedSerialBaud = 1; OnPropertyChanged(nameof(SelectedSerialBaud));
            _selectedDataBit = 0; OnPropertyChanged(nameof(SelectedDataBit));
            _selectedStopBit = 1; OnPropertyChanged(nameof(SelectedStopBit));
            _selectedParity = 0; OnPropertyChanged(nameof(SelectedParity));
            _isTransEnable = true; OnPropertyChanged(nameof(IsTransEnable));
            _selectedFrameFormat = 0; OnPropertyChanged(nameof(SelectedFrameFormat));
            _stdId = "136"; OnPropertyChanged(nameof(StdId));
            _extId = "0"; OnPropertyChanged(nameof(ExtId));
            _isFilterEnable = false; OnPropertyChanged(nameof(IsFilterEnable));
            _filterId = "0"; OnPropertyChanged(nameof(FilterId));
            _maskId = "0"; OnPropertyChanged(nameof(MaskId));
        }
        #endregion

        private bool ParseIntFromResult(string result, out int value)
        {
            string[] parts = result.Split(new[] { '=', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && int.TryParse(parts[1], out value))
                return true;
            value = 0;
            return false;
        }

        public void Dispose()
        {
            if (_serialComm != null)
                _serialComm.DataReceived -= SerialComm_DataReceived;
        }
    }

    public class CanBaudItem
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}
