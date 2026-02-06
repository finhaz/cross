using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    /// <summary>
    /// 串口通讯类（实例化管理，替代原CommonRes静态串口）
    /// </summary>
    public class SerialCommunication : ICommunication
    {
        private readonly SerialPort _serialPort;
        private readonly byte[] _gbuffer = new byte[4096];
        private int _gbIndex = 0;

        // 新增：暴露串口编码属性（适配原有逻辑）
        public Encoding Encoding
        {
            get => _serialPort.Encoding;
            set => _serialPort.Encoding = value;
        }

        // 数据接收事件（替代原CommonRes的CurrentDataHandler）
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        // 串口是否打开
        public bool IsConnected => _serialPort.IsOpen;

        // 串口无需Config，空实现（不影响原有逻辑）
        public CommunicationConfig Config { get; set; }

        // 构造函数：初始化串口实例
        public SerialCommunication()
        {
            _serialPort = new SerialPort();
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        /// <summary>
        /// 打开串口（核心：接收SerialConfig配置）
        /// </summary>
        public void Open(CommunicationConfig config)
        {
            if (config is not SerialConfig serialConfig)
                throw new ArgumentException("配置类型必须为SerialConfig");

            // 配置串口参数（完全复用原有逻辑）
            _serialPort.PortName = serialConfig.SelectedPortName;
            _serialPort.BaudRate = serialConfig.SelectedBaudRate;
            _serialPort.DataBits = serialConfig.SelectedDataBits;

            // 校验位配置
            _serialPort.Parity = serialConfig.SelectedParityName switch
            {
                "无" => Parity.None,
                "奇校验" => Parity.Odd,
                "偶校验" => Parity.Even,
                _ => Parity.None
            };

            // 停止位配置
            _serialPort.StopBits = serialConfig.SelectedStopBit switch
            {
                0 => StopBits.None,
                1 => StopBits.One,
                2 => StopBits.Two,
                _ => StopBits.One
            };

            // 打开串口
            if (!_serialPort.IsOpen)
                _serialPort.Open();
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
        }

        /// <summary>
        /// 发送数据（与原逻辑一致）
        /// </summary>
        public void Send(byte[] data, int offset, int length)
        {
            if (!IsConnected)
                throw new InvalidOperationException("串口未打开");

            _serialPort.Write(data, offset, length);
        }

        /// <summary>
        /// 格式化发送数据（复用原有方法）
        /// </summary>
        public string FormatSendData(byte[] data, int length)
        {
            return SerialDataProcessor.Instance.FormatSerialDataToHexString(data, length, "TX:", true);
        }

        /// <summary>
        /// 串口数据接收逻辑（原CommonRes的核心逻辑）
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bufferLen = 0;
            int gbLast = _gbIndex;

            try
            {
                bufferLen = _serialPort.Read(_gbuffer, _gbIndex, _gbuffer.Length - _gbIndex);
                _gbIndex += bufferLen;
                if (_gbIndex >= _gbuffer.Length)
                    _gbIndex -= _gbuffer.Length;
            }
            catch
            {
                return;
            }

            // 触发接收事件
            DataReceived?.Invoke(this, new DataReceivedEventArgs(_gbuffer, gbLast, bufferLen));
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Close();
            _serialPort.Dispose();
        }
    }
}
