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
    public class UdpCommunication : ICommunication
    {
        #region 私有字段
        private UdpClient _udpClient;
        private EthernetConfig _config;
        #endregion

        #region 公共事件（核心：AddLog 必须 public）
        public event Action<string> AddLog;
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
                if (_config.UdpMode == "Server")
                {
                    // 服务器模式：绑定指定端口
                    _udpClient = new UdpClient(_config.LocalPort);
                    IsConnected = true;
                    AddLog?.Invoke($"UDP服务器启动成功：本地端口 {_config.LocalPort}");
                }
                else
                {
                    // 客户端模式：自动分配端口
                    _udpClient = new UdpClient(); // 无参构造 = 自动分配端口
                    IsConnected = true;

                    IPEndPoint localEP = (IPEndPoint)_udpClient.Client.LocalEndPoint;
                    AddLog?.Invoke($"UDP客户端启动成功：本地自动端口 {localEP.Port}");
                }

                StartReceiveData();
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"UDP打开失败：{ex.Message}");
                IsConnected = false;
            }
        }

        public void Close()
        {
            try
            {
                _udpClient?.Close();
                IsConnected = false;
                AddLog?.Invoke("UDP连接已断开");
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"UDP断开失败：{ex.Message}");
            }
        }

        public void Send(byte[] data, int offset, int length)
        {
            if (!IsConnected || _udpClient == null)
                throw new InvalidOperationException("UDP未启动");

            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(_config.RemoteIp), _config.RemotePort);
                _udpClient.Send(data, length, remoteEP);
                AddLog?.Invoke($"UDP发送数据：{BitConverter.ToString(data, offset, length).Replace("-", " ")}");
            }
            catch (Exception ex)
            {
                AddLog?.Invoke($"UDP发送失败：{ex.Message}");
                throw;
            }
        }

        public string FormatSendData(byte[] data, int length)
        {
            return $"TX(UDP): {BitConverter.ToString(data, 0, length).Replace("-", " ")}";
        }
        #endregion

        #region 私有辅助方法
        private void StartReceiveData()
        {
            if (_udpClient == null) return;

            _udpClient.BeginReceive((ar) =>
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.EndReceive(ar, ref remoteEP);

                    if (data.Length > 0)
                    {
                        DataReceived?.Invoke(this, new DataReceivedEventArgs(data, 0, data.Length));
                        AddLog?.Invoke($"UDP接收数据 [{remoteEP}]: {BitConverter.ToString(data).Replace("-", " ")}");
                    }

                    StartReceiveData(); // 继续接收
                }
                catch (Exception ex)
                {
                    AddLog?.Invoke($"UDP接收失败：{ex.Message}");
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