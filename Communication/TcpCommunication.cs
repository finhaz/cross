using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    // 确保类是 public（跨文件访问必须）
    public class TcpCommunication : ICommunication
    {
        #region 私有字段
        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private EthernetConfig _config;
        #endregion

        #region 公共事件（核心：AddLog 必须 public）
        // 实现接口的AddLog委托（不是事件，是普通委托属性）
        public Action<string> AddLog { get; set; }
        // 数据接收事件
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        #endregion

        #region 公共属性
        public bool IsConnected { get; private set; }
        public CommunicationConfig Config
        {
            get => _config;
            set => _config = (EthernetConfig)value;
        }
        #endregion

        #region 核心方法
        public void Open(CommunicationConfig config)
        {
            _config = (EthernetConfig)config;
            IsConnected = false;

            try
            {
                if (_config.TcpMode == "Server")
                {
                    // 服务器模式：绑定指定端口
                    _tcpListener = new TcpListener(IPAddress.Parse(_config.LocalIp), _config.LocalPort);
                    _tcpListener.Start();
                    _tcpListener.BeginAcceptTcpClient(OnTcpClientConnected, null);
                    IsConnected = true;
                    AddLog?.Invoke($"TCP服务器启动成功：{_config.LocalIp}:{_config.LocalPort}");
                }
                else
                {
                    // 客户端模式：自动分配端口（无参构造）
                    _tcpClient = new TcpClient(); // 关键：系统自动分配本地端口
                    _tcpClient.Connect(_config.RemoteIp, _config.RemotePort);
                    _networkStream = _tcpClient.GetStream();
                    IsConnected = true;

                    // 获取自动分配的本地端口
                    IPEndPoint localEP = (IPEndPoint)_tcpClient.Client.LocalEndPoint;
                    AddLog?.Invoke($"TCP客户端连接成功：远程 {_config.RemoteIp}:{_config.RemotePort} | 本地自动端口：{localEP.Port}");

                    // 开始接收数据
                    StartReceiveData();
                }
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"TCP打开失败：{ex.Message}");
                IsConnected = false;
            }
        }

        public void Close()
        {
            try
            {
                _networkStream?.Close();
                _tcpClient?.Close();
                _tcpListener?.Stop();
                IsConnected = false;
                AddLog?.Invoke("TCP连接已断开");
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"TCP断开失败：{ex.Message}");
            }
        }

        public void Send(byte[] data, int offset, int length)
        {
            if (!IsConnected || _networkStream == null)
                throw new InvalidOperationException("TCP未连接");

            try
            {
                _networkStream.Write(data, offset, length);
                AddLog?.Invoke($"TCP发送数据：{BitConverter.ToString(data, offset, length).Replace("-", " ")}");
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"TCP发送失败：{ex.Message}");
                throw;
            }
        }

        public string FormatSendData(byte[] data, int length)
        {
            return $"TX(TCP): {BitConverter.ToString(data, 0, length).Replace("-", " ")}";
        }
        #endregion

        #region 私有辅助方法
        private void OnTcpClientConnected(IAsyncResult ar)
        {
            try
            {
                if (_tcpListener == null) return;

                _tcpClient = _tcpListener.EndAcceptTcpClient(ar);
                _networkStream = _tcpClient.GetStream();
                _tcpListener.BeginAcceptTcpClient(OnTcpClientConnected, null); // 继续监听

                IPEndPoint clientEP = (IPEndPoint)_tcpClient.Client.RemoteEndPoint;
                AddLog?.Invoke($"客户端接入：{clientEP.Address}:{clientEP.Port}");

                StartReceiveData();
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"客户端接入失败：{ex.Message}");
            }
        }

        private void StartReceiveData()
        {
            byte[] buffer = new byte[1024];
            _networkStream?.BeginRead(buffer, 0, buffer.Length, (ar) =>
            {
                try
                {
                    int bytesRead = _networkStream.EndRead(ar);
                    if (bytesRead > 0)
                    {
                        DataReceived?.Invoke(this, new DataReceivedEventArgs(buffer, 0, bytesRead));
                        AddLog?.Invoke($"TCP接收数据：{BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ")}");
                        StartReceiveData(); // 继续接收
                    }
                    else
                    {
                        AddLog?.Invoke("TCP连接被对方关闭");
                        Close();
                    }
                }
                catch (Exception ex)
                {
                    AddLog?.Invoke($"TCP接收失败：{ex.Message}");
                }
            }, null);
        }
        #endregion

        #region 释放资源
        public void Dispose()
        {
            Close();
        }
        #endregion
    }
}