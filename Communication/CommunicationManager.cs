using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cross.ViewModels;

namespace cross.Communication
{

    public enum CommunicationType
    {
        SerialPort,    // 串口
        Ethernet,       // 网络
        CAN,
        IIC
    }
    /// <summary>
    /// 通讯管理器（单例，管理全局串口实例）
    /// </summary>
    public class CommunicationManager
    {
        private static readonly Lazy<CommunicationManager> _instance =
            new Lazy<CommunicationManager>(() => new CommunicationManager());
        public static CommunicationManager Instance => _instance.Value;

        // 当前通讯实例（串口/以太网）
        private ICommunication _currentComm;
        // 当前通讯类型
        private CommunicationType _currentType;

        // 创建串口实例
        public void CreateSerialInstance()
        {
            // 释放原有实例
            DisposeCurrentInstance();
            // 设置为当前通讯实例
            _currentComm = new SerialCommunication(); ;
            _currentType = CommunicationType.SerialPort;
        }

        // 创建以太网实例（区分TCP/UDP）
        public void CreateEthernetInstance()
        {
            // 释放原有实例
            DisposeCurrentInstance();
            // 先创建空的以太网实例（后续由EthernetConfigViewModel替换为TCP/UDP）
            _currentComm = new EmptyEthernetCommunication();
            _currentType = CommunicationType.Ethernet;
        }

        //获取当前通讯实例
        public ICommunication GetCurrentCommunication()
        {
            if (_currentComm == null)
                throw new InvalidOperationException("请先在MainPage选择通讯方式！");
            return _currentComm;
        }


        //供EthernetConfigViewModel替换具体协议实例（TCP/UDP）
        public void ReplaceEthernetInstance(ICommunication ethernetComm)
        {
            if (_currentType != CommunicationType.Ethernet)
                throw new InvalidOperationException("当前非以太网类型，无法替换");

            DisposeCurrentInstance();
            _currentComm = ethernetComm;
        }



        // 获取当前通讯类型
        public CommunicationType GetCurrentType() => _currentType;

        // 释放当前实例
        public void DisposeCurrentInstance()
        {
            _currentComm?.Dispose();
            _currentComm = null;
        }

    }
}
