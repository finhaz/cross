using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using cross.Communication;
using cross.Interfaces;
using cross.Mvvm;

namespace cross.ViewModels
{
    /// <summary>
    /// 串口配置 ViewModel（Avalonia 11 适配，基于 WPF SerialConfigViewModel 迁移）。
    /// </summary>
    public class SerialConfigViewModel : ObservableObject
    {
        public SerialPortConfig PortConfig { get; } = new SerialPortConfig();
        public event Action? ScrollToEndRequested;

        public SerialCommunication? _serialComm;

        #region 控件状态
        private bool _isConfigEnabled = true;
        public bool IsConfigEnabled
        {
            get => _isConfigEnabled;
            set => SetProperty(ref _isConfigEnabled, value);
        }
        #endregion

        #region 复选框相关
        private bool _isHexSend;
        public bool IsHexSend { get => _isHexSend; set => SetProperty(ref _isHexSend, value); }

        private bool _isAsciiView;
        public bool IsAsciiView { get => _isAsciiView; set => SetProperty(ref _isAsciiView, value); }

        private bool _isAutoSend;
        public bool IsAutoSend
        {
            get => _isAutoSend;
            set
            {
                if (SetProperty(ref _isAutoSend, value))
                    OnAutoSendStateChanged(value);
            }
        }

        private bool _isHexView;
        public bool IsHexView { get => _isHexView; set => SetProperty(ref _isHexView, value); }

        private bool _isAdvantechCmdChecked;
        public bool IsAdvantechCmdChecked
        {
            get => _isAdvantechCmdChecked;
            set
            {
                SetProperty(ref _isAdvantechCmdChecked, value);
                if (IsAsciiView) Update16ViewText();
            }
        }
        #endregion

        #region 文本绑定
        private string _tbSendText = "0";
        public string TbSendText
        {
            get => _tbSendText;
            set
            {
                SetProperty(ref _tbSendText, value);
                tbSend_TextChanged();
            }
        }

        private string _tbReceiveText = string.Empty;
        public string TbReceiveText { get => _tbReceiveText; set => SetProperty(ref _tbReceiveText, value); }

        private string _tb16ViewText = string.Empty;
        public string Tb16ViewText { get => _tb16ViewText; set => SetProperty(ref _tb16ViewText, value); }

        private string _tbIntervalTimeText = string.Empty;
        public string TbIntervalTimeText { get => _tbIntervalTimeText; set => SetProperty(ref _tbIntervalTimeText, value); }

        private string _tbComStateText = "0";
        public string TbComStateText { get => _tbComStateText; set => SetProperty(ref _tbComStateText, value); }

        private string _receiveCount = "0";
        public string ReceiveCount { get => _receiveCount; set => SetProperty(ref _receiveCount, value); }

        private string _sendCount = "0";
        public string SendCount { get => _sendCount; set => SetProperty(ref _sendCount, value); }
        #endregion

        #region 可见性（用 IsVisible 布尔值替代 WPF Visibility）
        private bool _tbkIntervalTimeVisible = true;
        public bool TbkIntervalTimeVisible { get => _tbkIntervalTimeVisible; set => SetProperty(ref _tbkIntervalTimeVisible, value); }

        private bool _tbIntervalTimeVisible = true;
        public bool TbIntervalTimeVisible { get => _tbIntervalTimeVisible; set => SetProperty(ref _tbIntervalTimeVisible, value); }

        private bool _tb16ViewVisible = false;
        public bool Tb16ViewVisible { get => _tb16ViewVisible; set => SetProperty(ref _tb16ViewVisible, value); }

        private bool _expandVisible = false;
        public bool ExpandVisible { get => _expandVisible; set => SetProperty(ref _expandVisible, value); }
        #endregion

        #region 串口状态指示颜色
        private IBrush _comStateColor = Brushes.Red;
        public IBrush ComStateColor
        {
            get => _comStateColor;
            set => SetProperty(ref _comStateColor, value);
        }
        #endregion

        #region 按钮内容
        private object _openComButtonContent = "打开串口";
        public object OpenComButtonContent
        {
            get => _openComButtonContent;
            set => SetProperty(ref _openComButtonContent, value);
        }
        #endregion

        private DispatcherTimer _autoSendTimer = new DispatcherTimer();

        public SerialConfigViewModel()
        {
            _autoSendTimer.Interval = TimeSpan.FromSeconds(1);
            _autoSendTimer.Tick += AutoSendTimer_Tick;
        }

        #region 16 进制处理
        public string ByteArrayToHexString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(b.ToString("X2") + " ");
            return sb.ToString().TrimEnd();
        }

        public byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "").ToUpper();
            if (s.Length % 2 != 0)
                s = s.Substring(0, s.Length - 1) + "0" + s.Substring(s.Length - 1);

            byte[] buffer = new byte[s.Length / 2];
            try
            {
                for (int i = 0; i < s.Length; i += 2)
                    buffer[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
                return buffer;
            }
            catch
            {
                return new byte[] { 0xE4 };
            }
        }

        public string StringToHexString(string s)
        {
            s = s.Replace(" ", "");
            var buffer = new StringBuilder();
            foreach (char c in s)
            {
                buffer.Append(((int)c).ToString("X2"));
            }
            return buffer.ToString();
        }
        #endregion

        #region 16 进制预览
        public void Toggle16View(bool isHex)
        {
            if (isHex)
            {
                Tb16ViewVisible = true;
                IsAsciiView = true;
                Update16ViewText();
            }
            else
            {
                Tb16ViewVisible = false;
                Tb16ViewText = string.Empty;
            }
        }

        private void Update16ViewText()
        {
            string hexString = StringToHexString(TbSendText);
            var sb = new StringBuilder();
            for (int i = 0; i < hexString.Length; i += 2)
            {
                sb.Append(hexString.Substring(i, 2));
                sb.Append(" ");
            }
            if (IsAdvantechCmdChecked)
                sb.Append("0D 0A ");
            Tb16ViewText = sb.ToString().TrimEnd();
        }
        #endregion

        #region 发送
        public void btSend_Event(string strSend, bool hexState)
        {
            if (_serialComm == null || !_serialComm.IsConnected)
            {
                TbComStateText = "串口未开";
                _ = DialogHelper.ShowAsync("串口没有打开，请检查！", "提示");
                return;
            }

            try
            {
                if (!hexState)
                {
                    byte[] sendData = Encoding.Default.GetBytes(strSend);
                    _serialComm.Send(sendData, 0, sendData.Length);
                    SendCount = (Convert.ToInt32(SendCount) + sendData.Length).ToString();
                    if (IsAdvantechCmdChecked)
                    {
                        byte[] crlf = HexStringToByteArray("0D 0A");
                        _serialComm.Send(crlf, 0, 2);
                        SendCount = (Convert.ToInt32(SendCount) + 2).ToString();
                    }
                }
                else
                {
                    byte[] sendHex = HexStringToByteArray(strSend);
                    _serialComm.Send(sendHex, 0, sendHex.Length);
                    SendCount = (Convert.ToInt32(SendCount) + sendHex.Length).ToString();
                }
            }
            catch (Exception ex)
            {
                _ = DialogHelper.ShowAsync($"发送失败：{ex.Message}", "错误");
            }
        }
        #endregion

        #region 打开 / 关闭串口
        public void btOpenCom_Click()
        {
            if (_serialComm == null)
            {
                _ = DialogHelper.ShowAsync("请先在首页选择串口通讯！", "提示");
                return;
            }

            if (_serialComm.IsConnected)
            {
                _serialComm.Close();
                IsConfigEnabled = true;
                OpenComButtonContent = "打开串口";
                TbComStateText = PortConfig.SelectedPortName + "已关闭";
                ComStateColor = Brushes.Red;
            }
            else
            {
                if (string.IsNullOrEmpty(PortConfig.SelectedPortName))
                {
                    _ = DialogHelper.ShowAsync("未选择串口！", "提示");
                    return;
                }
                try
                {
                    var cfg = new SerialConfig
                    {
                        SelectedPortName = PortConfig.SelectedPortName,
                        SelectedBaudRate = PortConfig.SelectedBaudRate,
                        SelectedParityName = PortConfig.SelectedParityName,
                        SelectedStopBit = PortConfig.SelectedStopBit,
                        SelectedDataBits = PortConfig.SelectedDataBit
                    };
                    _serialComm.Open(cfg);
                    _serialComm.Encoding = Encoding.Default;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("占用") || ex.Message.Contains("busy"))
                    {
                        TbComStateText = PortConfig.SelectedPortName + "串口被占用！";
                        _ = DialogHelper.ShowAsync("串口被占用！", "错误");
                    }
                    else
                    {
                        TbComStateText = "串口打开失败：" + ex.Message;
                        _ = DialogHelper.ShowAsync("串口打开失败：" + ex.Message, "错误");
                    }
                    return;
                }

                IsConfigEnabled = false;
                OpenComButtonContent = "关闭串口";
                TbComStateText = $"{PortConfig.SelectedPortName},{PortConfig.SelectedBaudRate}," +
                    $"{PortConfig.SelectedParityName},{PortConfig.SelectedDataBit},{PortConfig.SelectedStopBit}";
                ComStateColor = Brushes.Green;
            }
        }
        #endregion

        #region 自动发送
        private void OnAutoSendStateChanged(bool isAuto)
        {
            if (isAuto)
            {
                if (_serialComm == null || !_serialComm.IsConnected)
                {
                    _ = DialogHelper.ShowAsync("串口未开！", "提示");
                    IsAutoSend = false;
                    return;
                }
                if (string.IsNullOrEmpty(TbIntervalTimeText) ||
                    double.TryParse(TbIntervalTimeText, out double v) && v == 0)
                {
                    _ = DialogHelper.ShowAsync("请设置有效的发送间隔（秒）！", "提示");
                    IsAutoSend = false;
                    return;
                }
                _autoSendTimer.Interval = TimeSpan.FromSeconds(Convert.ToDouble(TbIntervalTimeText));
                _autoSendTimer.Start();
            }
            else
            {
                _autoSendTimer.Stop();
            }
        }

        private void AutoSendTimer_Tick(object? sender, EventArgs e)
        {
            if (_serialComm != null && _serialComm.IsConnected)
                btSend_Event(TbSendText, IsHexSend);
        }
        #endregion

        #region 接收数据处理
        public void DebugSerialDataHandler(object sender, DataReceivedEventArgs e)
        {
            byte[] buf = new byte[e.BufferLength];
            Array.Copy(e.Buffer, e.LastIndex, buf, 0, e.BufferLength);

            string abc;
            if (IsHexView)
            {
                abc = ByteArrayToHexString(buf);
                if (abc.Length >= 4 && abc.Substring(abc.Length - 3, 3).Trim() == "0D")
                    abc += "\n";
            }
            else
            {
                abc = Encoding.Default.GetString(buf);
            }

            UiDispatcherHelper.ExecuteOnUiThread(() =>
            {
                TbReceiveText += abc;
                ReceiveCount = (Convert.ToInt32(ReceiveCount) + e.BufferLength).ToString();
                ScrollToEndRequested?.Invoke();
            });
        }

        public void tbSend_TextChanged()
        {
            if (IsAsciiView)
            {
                var sb = new StringBuilder();
                foreach (char c in TbSendText)
                {
                    if (char.IsWhiteSpace(c)) continue;
                    int code = (int)c;
                    sb.Append(code >= 0 && code <= 127 ? $"{code:X2} " : "[NA] ");
                }
                if (IsAdvantechCmdChecked)
                    sb.Append("0D ");
                Tb16ViewText = sb.ToString().TrimEnd();
            }
        }
        #endregion

        #region 保存接收数据到文件
        public async Task SaveReceiveData(Window? parent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TbReceiveText))
                {
                    await DialogHelper.ShowAsync("没有可保存的串口数据！", "提示",
                        MessageBoxButton.Ok, MessageBoxImage.Information);
                    return;
                }

                if (parent == null) return;

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "保存串口接收数据",
                    InitialFileName = $"串口接收数据_{DateTime.Now:yyyyMMddHHmmss}.txt",
                };
                saveFileDialog.Filters.Add(new FileDialogFilter
                {
                    Name = "文本文件",
                    Extensions = { "txt" }
                });
                saveFileDialog.Filters.Add(new FileDialogFilter
                {
                    Name = "所有文件",
                    Extensions = { "*" }
                });

                string? fileName = await saveFileDialog.ShowAsync(parent);
                if (string.IsNullOrEmpty(fileName)) return;

                File.WriteAllText(fileName, TbReceiveText, Encoding.UTF8);
                await DialogHelper.ShowAsync("数据已成功保存！", "保存成功",
                    MessageBoxButton.Ok, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowAsync($"保存失败：{ex.Message}", "错误",
                    MessageBoxButton.Ok, MessageBoxImage.Error);
            }
        }
        #endregion

        #region 页面加载 / 卸载
        public void Page_Loaded(object sender, EventArgs e)
        {
            PortConfig.InitPortNames();

            // 兜底：如果进入配置页时通讯管理器还没有串口实例
            // （例如用户直接点击配置按钮，首页 ComboBox 未触发 SelectionChanged），
            // 则立即创建一个。默认 SelectedCommType 为 SerialPort，与用户预期一致。
            if (_serialComm == null)
            {
                try
                {
                    CommunicationManager.Instance.CreateSerialInstance();
                }
                catch { /* 创建失败后续 try 会 catch */ }
            }

            try
            {
                _serialComm = (SerialCommunication)CommunicationManager.Instance.GetCurrentCommunication();
                _serialComm.Encoding = Encoding.Default;
            }
            catch (InvalidOperationException ex)
            {
                TbComStateText = "未初始化串口：" + ex.Message;
                return;
            }
            catch (Exception ex)
            {
                TbComStateText = "串口初始化异常：" + ex.Message;
                return;
            }

            if (!_serialComm.IsConnected)
            {
                IsConfigEnabled = true;
                OpenComButtonContent = "打开串口";
                TbComStateText = PortConfig.SelectedPortName + "已关闭";
                ComStateColor = Brushes.Red;
            }
            else
            {
                IsConfigEnabled = false;
                OpenComButtonContent = "关闭串口";
                TbComStateText = $"{PortConfig.SelectedPortName},{PortConfig.SelectedBaudRate}," +
                    $"{PortConfig.SelectedParityName},{PortConfig.SelectedDataBit},{PortConfig.SelectedStopBit}";
                ComStateColor = Brushes.Green;
            }

            try
            {
                _serialComm.DataReceived += DebugSerialDataHandler;
            }
            catch { /* 已订阅时忽略 */ }
        }

        public void Page_Unloaded(object sender, EventArgs e)
        {
            _autoSendTimer.Stop();
            if (_serialComm != null)
                _serialComm.DataReceived -= DebugSerialDataHandler;
        }
        #endregion
    }
}
