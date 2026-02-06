using cross.Mvvm;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Avalonia.Threading;
using ThingLing.Controls;
using cross.Communication;
using cross.Interfaces;

namespace cross.ViewModels
{
    public class EthernetConfigViewModel : ObservableObject
    {
        // 原有字段补充UDP模式
        private string _udpMode = "Server"; // UDP Server/Client

        // 新增：LocalPort是否可用（仅服务器模式可用）
        private bool _isLocalPortEnabled;

        // 原有字段完全保留
        private string _selectedProtocol = "TCP";
        private string _tcpMode = "Server";
        private string _localIp = "127.0.0.1";
        private string _localPort = "8080";
        private string _remoteIp = "127.0.0.1";
        private string _remotePort = "8080";
        private string _dataLog = string.Empty;
        private string _sendData = string.Empty;

        // 当前网络通讯实例（TCP/UDP）
        private ICommunication _ethernetComm;

        // 原有属性保留，新增UDP模式属性
        public string UdpMode
        {
            get => _udpMode;
            set
            {
                _udpMode = value;
                OnPropertyChanged();
                UpdateLocalPortEnabledState(); // 更新LocalPort启用状态
            }
        }

        // 新增：LocalPort启用状态（绑定到UI）
        public bool IsLocalPortEnabled
        {
            get => _isLocalPortEnabled;
            set
            {
                _isLocalPortEnabled = value;
                OnPropertyChanged();
            }
        }

        // 原有属性完全保留
        // 重写原有属性的变更逻辑（补充LocalPort状态更新）
        public string SelectedProtocol
        {
            get => _selectedProtocol;
            set
            {
                _selectedProtocol = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTcpSelected));
                OnPropertyChanged(nameof(IsRemoteConfigVisible));
                UpdateLocalPortEnabledState(); // 更新LocalPort启用状态
            }
        }

        public string TcpMode
        {
            get => _tcpMode;
            set
            {
                _tcpMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRemoteConfigVisible));
                UpdateLocalPortEnabledState(); // 更新LocalPort启用状态
            }
        }


        public string LocalIp
        {
            get => _localIp;
            set { _localIp = value; OnPropertyChanged(); }
        }

        public string LocalPort
        {
            get => _localPort;
            set { _localPort = value; OnPropertyChanged(); }
        }

        public string RemoteIp
        {
            get => _remoteIp;
            set { _remoteIp = value; OnPropertyChanged(); }
        }

        public string RemotePort
        {
            get => _remotePort;
            set { _remotePort = value; OnPropertyChanged(); }
        }

        public string DataLog
        {
            get => _dataLog;
            set { _dataLog = value; OnPropertyChanged(); }
        }

        public string SendData
        {
            get => _sendData;
            set { _sendData = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _ethernetComm?.IsConnected ?? false;
            set
            {
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectButtonText));
            }
        }

        public string ConnectButtonText => IsConnected ? "断开" : "连接";
        public bool IsTcpSelected => SelectedProtocol == "TCP";
        public bool IsRemoteConfigVisible =>
            (SelectedProtocol == "TCP" && TcpMode == "Client") ||
            SelectedProtocol == "UDP";

        // 原有命令保留
        public ICommand ConnectCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SendDataCommand { get; }
        public ICommand ClearLogCommand { get; }

        public EthernetConfigViewModel()
        {
            // 初始化时创建默认协议实例（TCP）
            CreateEthernetProtocolInstance();
            UpdateLocalPortEnabledState(); // 初始状态

            // 原有命令初始化保留
            ConnectCommand = new RelayCommand<object>(ExecuteConnect);
            SaveCommand = new RelayCommand<object>(ExecuteSave);
            SendDataCommand = new RelayCommand<object>(ExecuteSendData);
            ClearLogCommand = new RelayCommand<object>(ExecuteClearLog);
        }

        // 核心：更新LocalPort启用状态（仅服务器模式可用）
        private void UpdateLocalPortEnabledState()
        {
            LocalPort = "8080";
            if (SelectedProtocol == "TCP")
            {
                // TCP：仅服务器模式启用LocalPort
                IsLocalPortEnabled = TcpMode == "Server";
            }
            else if (SelectedProtocol == "UDP")
            {
                // UDP：仅服务器模式启用LocalPort
                IsLocalPortEnabled = UdpMode == "Server";
            }
            else
            {
                IsLocalPortEnabled = false;
            }

            // 客户端模式下，清空LocalPort输入框并提示
            if (!IsLocalPortEnabled)
            {
                LocalPort = "自动分配";
            }
        }

        // 改造CreateEthernetProtocolInstance（绑定日志委托）
        private void CreateEthernetProtocolInstance()
        {
            try
            {
                // 释放原有实例
                _ethernetComm?.Dispose();

                if (SelectedProtocol == "TCP")
                {
                    var tcpComm = new TcpCommunication();
                    // 绑定 AddLog 事件（此时不会报错）
                    tcpComm.AddLog = AddLog;
                    _ethernetComm = tcpComm;
                }
                else
                {
                    var udpComm = new UdpCommunication();
                    udpComm.AddLog += AddLog; // 绑定 AddLog 事件
                    _ethernetComm = udpComm;
                }

                CommunicationManager.Instance.ReplaceEthernetInstance(_ethernetComm);
                _ethernetComm.DataReceived += OnEthernetDataReceived;
                AddLog($"已切换到 {SelectedProtocol} 协议");
            }
            catch (Exception ex)
            {
                AddLog($"切换协议失败：{ex.Message}");
            }
        }



        // 连接/断开逻辑（原有逻辑适配）
        private void ExecuteConnect(object _)
        {
            try
            {
                if (IsConnected)
                {
                    _ethernetComm.Close();
                    AddLog($"已断开{SelectedProtocol}连接");
                }
                else
                {
                    int tLocalPort = 8080; 
                    if (IsLocalPortEnabled)
                    {
                        tLocalPort=int.Parse(LocalPort);
                    }

                    // 构建配置
                    var config = new EthernetConfig
                    {
                        Type = CommunicationType.Ethernet,
                        SelectedProtocol = SelectedProtocol,
                        TcpMode = TcpMode,
                        LocalIp = LocalIp,
                        LocalPort = tLocalPort,
                        RemoteIp = RemoteIp,
                        RemotePort = int.Parse(RemotePort)
                    };

                    // 打开连接
                    _ethernetComm.Open(config);
                    AddLog($"{SelectedProtocol}({TcpMode})连接成功");
                }
                IsConnected = _ethernetComm.IsConnected;
            }
            catch (Exception ex)
            {
                AddLog($"{SelectedProtocol}连接失败：{ex.Message}");
                IsConnected = false;
            }
        }

        // 发送数据逻辑（原有逻辑适配）
        private void ExecuteSendData(object _)
        {
            if (string.IsNullOrEmpty(SendData))
            {
                AddLog("发送失败：请输入要发送的数据");
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(SendData);
                _ethernetComm.Send(data, 0, data.Length);
                AddLog($"发送{SelectedProtocol}数据：{SendData}");
                SendData = string.Empty;
            }
            catch (Exception ex)
            {
                AddLog($"发送失败：{ex.Message}");
            }
        }

        // 数据接收处理
        private void OnEthernetDataReceived(object sender, DataReceivedEventArgs e)
        {
            string data = Encoding.UTF8.GetString(e.Buffer, e.LastIndex, e.BufferLength);
            AddLog($"接收{SelectedProtocol}数据：{data}");
        }

        // 原有方法完全保留
        private void ExecuteSave(object _)
        {
            try
            {
                AddLog("网络通讯配置已保存");
                MessageBox.ShowAsync("配置保存成功！", "提示");
            }
            catch (Exception ex)
            {
                AddLog($"保存失败：{ex.Message}");
            }
        }

        private void ExecuteClearLog(object _)
        {
            DataLog = string.Empty;
        }

        private void AddLog(string message)
        {
            // 核心修复：直接使用全局静态的Dispatcher.UIThread
            Dispatcher.UIThread.Invoke(() =>
            {
                // 原有日志逻辑完全保留，无任何改动
                DataLog += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            });
        }


    }  

}