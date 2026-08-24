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
    /// 严格复刻 WPF 版的 AT 指令读写流程。
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
        private int _selectedCanBaud = 3; // 默认 250Kbps（与 WPF 一致）
        public int SelectedCanBaud { get => _selectedCanBaud; set => SetProperty(ref _selectedCanBaud, value); }
        #endregion

        #region 串口配置
        // 注意：SerialPortConfig.BaudRates = {4800,9600,19200,38400,43000,56000,115200}
        //  默认 9600 = 索引 1
        private int _selectedSerialBaud = 1;
        public int SelectedSerialBaud { get => _selectedSerialBaud; set => SetProperty(ref _selectedSerialBaud, value); }

        //{6,7,8}
        //  默认 DataBit = 索引 1
        private int _selectedDataBit = 2;
        public int SelectedDataBit { get => _selectedDataBit; set => SetProperty(ref _selectedDataBit, value); }

        //{1,2}
        //  默认 StopBit = 索引 0
        private int _selectedStopBit = 0;
        public int SelectedStopBit { get => _selectedStopBit; set => SetProperty(ref _selectedStopBit, value); }

        // CAN 模块 AT 原始值：0=8位, 1=9位（内部存储，用于保存时下发 AT 指令）
        private int _dataBitAtValue = 0;
        public int DataBitAtValue { get => _dataBitAtValue; set => SetProperty(ref _dataBitAtValue, value); }

        // 显示文本（绑定到 CAN 主区 ComboBox 的 SelectedItem）
        private string _dataBitDisplay = "8位";
        public string DataBitDisplay
        {
            get => _dataBitDisplay;
            set
            {
                if (SetProperty(ref _dataBitDisplay, value))
                {
                    _dataBitAtValue = value switch { "8位" => 0, "9位" => 1, _ => 0 };
                    OnPropertyChanged(nameof(DataBitAtValue));
                }
            }
        }

        // CAN 模块 AT 原始值：0=1位, 1=1.5位, 2=2位
        private int _stopBitAtValue = 0;
        public int StopBitAtValue { get => _stopBitAtValue; set => SetProperty(ref _stopBitAtValue, value); }

        private string _stopBitDisplay = "1位";
        public string StopBitDisplay
        {
            get => _stopBitDisplay;
            set
            {
                if (SetProperty(ref _stopBitDisplay, value))
                {
                    _stopBitAtValue = value switch { "1位" => 0, "1.5位" => 1, "2位" => 2, _ => 0 };
                    OnPropertyChanged(nameof(StopBitAtValue));
                }
            }
        }

        // SerialPortConfig.ParityNames = {"无","奇校验","偶校验"}
        //  默认 "无" = 索引 0
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

        // CAN 模块 AT+USART_PARAM 返回的原始值：
        //   数据位：0=8, 1=9
        //   停止位：0=1, 1=1.5, 2=2
        //  列表长度/顺序恰好匹配这些索引，使 SelectedIndex 直接等于 AT 原始值。
        public List<string> DataBitOptions { get; } = new() { "8位", "9位" };
        public List<string> StopBitOptions { get; } = new() { "1位", "1.5位", "2位" };
        #endregion

        #region 串口事件 - 复刻 WPF 版的 _isWaitingATOk + 轮询模式
        private readonly StringBuilder _recvBuffer = new();
        private readonly object _lock = new object();
        private const int AtCmdTimeout = 1200;
        private bool _isWaitingATOk;
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

            _serialComm.Encoding = Encoding.ASCII;
            _serialComm.DataReceived += SerialComm_DataReceived;

            PortConfig.InitPortNames();
        }

        /// <summary>
        /// 串口数据接收（复刻 WPF 版）：仅在等待 AT 响应时积累数据
        /// </summary>
        private void SerialComm_DataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (_isWaitingATOk)
                {
                    string recvAscii = Encoding.ASCII.GetString(e.Buffer, e.LastIndex, e.BufferLength);
                    lock (_lock) { _recvBuffer.Append(recvAscii); }
                }
            }
            catch { /* 静默忽略解析异常 */ }
        }

        #region AT 指令发送 - 复刻 WPF 版
        /// <summary>
        /// 发送普通 AT 指令（进入配置模式、保存等），等待 OK/ERROR
        /// </summary>
        private async Task<bool> SendAtCmdAsync(string cmd)
        {
            lock (_lock) { _recvBuffer.Clear(); }
            byte[] sendBytes = Encoding.ASCII.GetBytes(cmd);
            _serialComm.Send(sendBytes, 0, sendBytes.Length);

            int waitMs = 0;
            while (waitMs < AtCmdTimeout)
            {
                await Task.Delay(50);
                waitMs += 50;
                lock (_lock)
                {
                    string recvStr = _recvBuffer.ToString();
                    if (recvStr.Contains("OK\r\n"))
                        return true;
                    if (recvStr.Contains("ERROR\r\n"))
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 发送查询类 AT 指令，返回以 + 开头的响应行
        /// </summary>
        private async Task<string> SendAtQueryCmdAsync(string cmd)
        {
            lock (_lock) { _recvBuffer.Clear(); }
            byte[] sendBytes = Encoding.ASCII.GetBytes(cmd);
            _serialComm.Send(sendBytes, 0, sendBytes.Length);

            int waitMs = 0;
            while (waitMs < AtCmdTimeout)
            {
                await Task.Delay(30);
                waitMs += 30;
                lock (_lock)
                {
                    string recvStr = _recvBuffer.ToString();
                    if (!string.IsNullOrEmpty(recvStr))
                    {
                        foreach (var line in recvStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (line.StartsWith("+"))
                                return line.Trim();
                        }
                        if (recvStr.Contains("ERROR"))
                            return "ERROR";
                    }
                }
            }
            return string.Empty;
        }

        private string ParseAtResultValue(string atResult)
        {
            if (string.IsNullOrEmpty(atResult) || !atResult.Contains(":") || atResult == "ERROR")
                return string.Empty;
            return atResult.Split(new[] { ':' }, 2)[1].Trim();
        }
        #endregion

        #region 进入/退出配置模式
        private bool EnterConfigMode()
        {
            _isWaitingATOk = true;
            bool ok = SendAtCmdAsync("AT+CG\r\n").GetAwaiter().GetResult();
            if (!ok)
            {
                _isWaitingATOk = false;
                return false;
            }
            return true;
        }

        private void ExitConfigMode()
        {
            _isWaitingATOk = true;
            SendAtCmdAsync("AT+ET\r\n").GetAwaiter().GetResult();
            _isWaitingATOk = false;
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
                    // CAN 模块 AT 原始值：0=8位, 1=9位
                    SelectedDataBits = _dataBitAtValue == 1 ? 9 : 8,
                    SelectedParityName = PortConfig.ParityNames[_selectedParity],
                    SelectedStopBit = _stopBitAtValue switch
                    {
                        0 => 1,    // 1 位
                        1 => 1,    // 1.5 位无法精确表示，退化为 1 位
                        2 => 2,    // 2 位
                        _ => 1
                    }
                };
                _serialComm.Open(cfg);
                _serialComm.Encoding = Encoding.ASCII;
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

        #region 读取配置 - 复刻 WPF 版
        public async Task ReadCanConfigAsync()
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "请先打开串口";
                return;
            }

            AtModuleStatus = "正在读取模块配置...";
            try
            {
                _isWaitingATOk = true;
                bool enterOk = await SendAtCmdAsync("AT+CG\r\n");
                if (!enterOk)
                {
                    AtModuleStatus = "❌ 读取失败：进入AT配置模式失败";
                    _isWaitingATOk = false;
                    return;
                }

                // 1. CAN工作模式
                string modeResult = await SendAtQueryCmdAsync("AT+CAN_MODE=?\r\n");
                if (!string.IsNullOrEmpty(modeResult) && modeResult != "ERROR")
                {
                    string modeVal = ParseAtResultValue(modeResult);
                    if (int.TryParse(modeVal, out int mode))
                        SelectedWorkMode = mode;
                }

                // 2. CAN波特率
                string baudResult = await SendAtQueryCmdAsync("AT+CAN_BAUD=?\r\n");
                if (!string.IsNullOrEmpty(baudResult) && baudResult != "ERROR")
                {
                    string baudVal = ParseAtResultValue(baudResult);
                    if (int.TryParse(baudVal, out int baud))
                    {
                        var match = CanBaudList.Find(x => x.Value == baud);
                        if (match != null)
                            SelectedCanBaud = CanBaudList.IndexOf(match);
                    }
                }

                // 3. 串口参数
                string usartResult = await SendAtQueryCmdAsync("AT+USART_PARAM=?\r\n");
                if (!string.IsNullOrEmpty(usartResult) && usartResult != "ERROR")
                {
                    ParseUsartParam(usartResult);
                }

                // 4. 帧格式
                string frameResult = await SendAtQueryCmdAsync("AT+CAN_FRAMEFORMAT=?\r\n");
                if (!string.IsNullOrEmpty(frameResult) && frameResult != "ERROR")
                {
                    ParseCanFrameFormat(frameResult);
                }

                // 5. 滤波器
                string filterResult = await SendAtQueryCmdAsync("AT+CAN_FILTER0=?\r\n");
                if (!string.IsNullOrEmpty(filterResult) && filterResult != "ERROR")
                {
                    ParseCanFilter(filterResult);
                }

                // 退出配置模式
                await SendAtCmdAsync("AT+ET\r\n");
                _isWaitingATOk = false;
                AtModuleStatus = "✅ 配置读取完成";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"❌ 读取异常：{ex.Message}";
                _isWaitingATOk = false;
            }
        }

        #region 专用解析方法
        /// <summary>
        /// +USART_PARAM:&lt;Baud&gt;,&lt;DataBit&gt;,&lt;StopBit&gt;,&lt;Parity&gt;
        /// </summary>
        private void ParseUsartParam(string usartResult)
        {
            string paramStr = ParseAtResultValue(usartResult);
            if (string.IsNullOrEmpty(paramStr)) return;

            string[] parts = paramStr.Split(',');
            if (parts.Length != 4) return;

            if (int.TryParse(parts[0], out int baud) && PortConfig.BaudRates.Contains(baud))
                SelectedSerialBaud = PortConfig.BaudRates.IndexOf(baud);

            // 直接赋值 AT 原始值：0=8位, 1=9位
            if (int.TryParse(parts[1], out int databit))
            {
                _dataBitAtValue = databit;
                OnPropertyChanged(nameof(DataBitAtValue));
                DataBitDisplay = databit == 1 ? "9位" : "8位";
                SelectedDataBit= databit == 1 ?  1 : 0;
            }

            // 直接赋值 AT 原始值：0=1位, 1=1.5位, 2=2位
            if (int.TryParse(parts[2], out int stopbit))
            {
                _stopBitAtValue = stopbit;
                OnPropertyChanged(nameof(StopBitAtValue));
                StopBitDisplay = stopbit switch { 0 => "1位", 1 => "1.5位", 2 => "2位", _ => "1位" };
            }

            if (int.TryParse(parts[3], out int parity))
                SelectedParity = parity;
        }

        private void ParseCanFrameFormat(string frameResult)
        {
            string paramStr = ParseAtResultValue(frameResult);
            if (string.IsNullOrEmpty(paramStr)) return;

            string[] parts = paramStr.Split(',');
            if (parts.Length != 4) return;

            if (int.TryParse(parts[0], out int enable))
                IsTransEnable = enable == 1;

            if (int.TryParse(parts[1], out int frameFormat))
                SelectedFrameFormat = frameFormat;

            StdId = parts[2].Trim();
            ExtId = parts[3].Trim();
        }

        private void ParseCanFilter(string filterResult)
        {
            string paramStr = ParseAtResultValue(filterResult);
            if (string.IsNullOrEmpty(paramStr)) return;

            string[] parts = paramStr.Split(',');
            if (parts.Length != 4) return;

            if (int.TryParse(parts[0], out int enable))
                IsFilterEnable = enable == 1;

            FilterId = parts[2].Trim();
            MaskId = parts[3].Trim();
        }
        #endregion
        #endregion

        #region 保存配置 - 复刻 WPF 版
        public async Task SaveCanConfigAsync()
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                AtModuleStatus = "请先打开串口";
                return;
            }

            AtModuleStatus = "正在写入配置到模块...";
            try
            {
                _isWaitingATOk = true;
                bool enterOk = await SendAtCmdAsync("AT+CG\r\n");
                if (!enterOk)
                {
                    AtModuleStatus = "❌ 保存失败：进入AT配置模式失败";
                    _isWaitingATOk = false;
                    return;
                }

                await SendAtCmdAsync($"AT+CAN_MODE={_selectedWorkMode}\r\n");
                int baud = _selectedCanBaud >= 0 && _selectedCanBaud < CanBaudList.Count
                    ? CanBaudList[_selectedCanBaud].Value : 500000;
                await SendAtCmdAsync($"AT+CAN_BAUD={baud}\r\n");

                int serialBaud = _selectedSerialBaud >= 0 && _selectedSerialBaud < PortConfig.BaudRates.Count
                    ? PortConfig.BaudRates[_selectedSerialBaud] : 9600;
                // dataBit/stopBit/parity 直接传 AT 原始值（0=8位/1=9位; 0=1/1=1.5/2=2; 0=无/1=奇/2=偶）
                await SendAtCmdAsync($"AT+USART_PARAM={serialBaud},{_selectedDataBit},{_selectedStopBit},{_selectedParity}\r\n");

                int transEn = _isTransEnable ? 1 : 0;
                await SendAtCmdAsync($"AT+CAN_FRAMEFORMAT={transEn},{_selectedFrameFormat},{_stdId},{_extId}\r\n");

                int filterEn = _isFilterEnable ? 1 : 0;
                await SendAtCmdAsync($"AT+CAN_FILTER0={filterEn},0,{_filterId},{_maskId}\r\n");

                await SendAtCmdAsync("AT+SAVE\r\n");
                await SendAtCmdAsync("AT+ET\r\n");
                _isWaitingATOk = false;
                AtModuleStatus = "✅ 配置保存成功，重启模块生效";
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"❌ 保存异常：{ex.Message}";
                _isWaitingATOk = false;
            }
        }
        #endregion

        #region 恢复默认 - 复刻 WPF 版
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
                _isWaitingATOk = true;
                bool enterOk = await SendAtCmdAsync("AT+CG\r\n");
                if (!enterOk)
                {
                    AtModuleStatus = "❌ 恢复失败：进入AT配置模式失败";
                    _isWaitingATOk = false;
                    return;
                }

                bool defOk = await SendAtCmdAsync("AT+DEFAULT\r\n");
                await SendAtCmdAsync("AT+ET\r\n");
                _isWaitingATOk = false;

                if (defOk)
                {
                    ResetDefaultValue();
                    AtModuleStatus = "✅ 出厂配置恢复完成";
                }
                else
                {
                    AtModuleStatus = "❌ 恢复出厂配置失败";
                }
            }
            catch (Exception ex)
            {
                AtModuleStatus = $"❌ 恢复异常：{ex.Message}";
                _isWaitingATOk = false;
            }
        }

        private void ResetDefaultValue()
        {
            _selectedWorkMode = 0; OnPropertyChanged(nameof(SelectedWorkMode));
            _selectedCanBaud = 3; OnPropertyChanged(nameof(SelectedCanBaud));
            _selectedSerialBaud = 1; OnPropertyChanged(nameof(SelectedSerialBaud));
            _selectedDataBit = 0; OnPropertyChanged(nameof(SelectedDataBit));
            _selectedStopBit = 0; OnPropertyChanged(nameof(SelectedStopBit));
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
