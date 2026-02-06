using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using cross.Mvvm;

namespace cross.Communication
{
    /// <summary>
    /// 协议配置类：集中整合所有Modbus/FE协议相关的配置选项和信息
    /// </summary>
    public class ProtocolConfig : ObservableObject
    {
        #region 下拉框选项集合（静态/默认选项）
        /// <summary>
        /// 数据类型选项（线圈状态、离散输入等）
        /// </summary>
        public ObservableCollection<string> DataTypeOptions { get; set; } = new ObservableCollection<string>
        { "线圈状态(RW)", "离散输入(RO)", "保持寄存器(RW)", "输入寄存器(RO)" };

        /// <summary>
        /// 协议类型选项（Modbus RTU、FE、Modbus TCP）
        /// </summary>
        public ObservableCollection<string> ProtocolTypeOptions { get; set; } = new ObservableCollection<string>
        { "Modbus RTU协议", "FE协议","Modbus TCP协议" };

        /// <summary>
        /// 传输类型选项（有符号整数、无符号整数等）
        /// </summary>
        public ObservableCollection<string> TransferTypeOptions { get; set; } = new ObservableCollection<string>
        { "有符号整数", "无符号整数", "浮点数", "字节流","位数据" };

        /// <summary>
        /// 显示类型选项（浮点数、位数据等）
        /// </summary>
        public ObservableCollection<string> DisplayTypeOptions { get; set; } = new ObservableCollection<string>
        { "浮点数", "位数据", "十进制整数", "十六进制整数","字节流","字符串" };
        #endregion

        #region 协议详情信息列表
        /// <summary>
        /// 协议详情信息集合（存储各协议的帧结构、特性等）
        /// </summary>
        private ObservableCollection<ProtocolInfo> _protocolDetailList = new ObservableCollection<ProtocolInfo>
        {
            new ProtocolInfo
            {
                Name = "Modbus RTU协议",
                FrameStructure = "地址域(1字节) + 功能码(1字节) + 数据域(N字节) + CRC校验(2字节)",
                Transport = "RTU/ASCII/TCP三种，RTU为二进制格式，效率更高",
                Features = "常用功能码：03（读保持寄存器）、06（写单个寄存器）"
            },
            new ProtocolInfo
            {
                Name = "Modbus TCP协议",
                FrameStructure = "MBAP报文头(7字节) + 功能码(1字节) + 数据域(N字节)（无CRC校验）",
                Transport = "基于TCP/IP网络传输，默认端口502，字节序为大端序（网络字节序）",
                Features = "常用功能码：03（读保持寄存器）、06（写单个寄存器）、16（写多个寄存器）；MBAP头包含事务标识、协议标识、长度、单元标识"
            },
            new ProtocolInfo
            {
                Name = "FE协议",
                FrameStructure = "帧头(2字节: 0xFE 0x01) + 设备地址(1字节) + 数据长度(2字节) + 数据域(N字节) + 校验和(1字节)",
                Transport = "基于串口的私有协议，波特率默认9600，8N1格式",
                Features = "支持广播帧，数据域采用小端序存储"
            }
        };

        public ObservableCollection<ProtocolInfo> ProtocolDetailList
        {
            get => _protocolDetailList;
            set => SetProperty(ref _protocolDetailList, value);
        }
        #endregion

        #region 可选扩展：选中项属性（如果需要绑定下拉框选中值）
        // 如果你需要绑定下拉框的选中值，可添加以下属性（按需扩展）
        private string _selectedProtocolType = "Modbus RTU协议";
        /// <summary>
        /// 选中的协议类型（默认Modbus RTU）
        /// </summary>
        public string SelectedProtocolType
        {
            get => _selectedProtocolType;
            set
            {
                if (SetProperty(ref _selectedProtocolType, value))
                {
                    // 关键：当选中的协议名称变更时，主动通知SelectedProtocol属性变更
                    OnPropertyChanged(nameof(SelectedProtocol));
                }
            }
        }

        private string _selectedDataType = "保持寄存器(RW)";
        /// <summary>
        /// 选中的数据类型（默认保持寄存器）
        /// </summary>
        public string SelectedDataType
        {
            get => _selectedDataType;
            set => SetProperty(ref _selectedDataType, value);
        }


        // 筛选后的当前协议（核心属性）
        public ProtocolInfo SelectedProtocol
        {
            get
            {
                // 根据选中的协议名称，从ProtocolList中筛选对应对象
                return ProtocolDetailList.FirstOrDefault(p => p.Name == SelectedProtocolType);
            }
        }

        private string _dSelectedTransferType = "无符号整数";
        public string DSelectedTransferType
        {
            get => _dSelectedTransferType;
            set => SetProperty(ref _dSelectedTransferType, value);
        }

        private string _dSelectedDisplayType = "浮点数";
        public string DSelectedDisplayType
        {
            get => _dSelectedDisplayType;
            set => SetProperty(ref _dSelectedDisplayType, value);
        }



        #endregion

        #region 可选扩展：业务逻辑方法
        /// <summary>
        /// 选中协议类型变更后的联动逻辑（比如过滤协议详情、加载对应配置）
        /// </summary>
        /// <param name="protocolTypeName">选中的协议名称</param>
        private void OnProtocolTypeChanged(string protocolTypeName)
        {
            // 示例：根据选中的协议名称，筛选ProtocolDetailList（可选）
            // 如需动态更新，可先清空再添加对应协议的信息
            // _protocolDetailList.Clear();
            // var targetProtocol = GetProtocolInfoByName(protocolTypeName);
            // if (targetProtocol != null) _protocolDetailList.Add(targetProtocol);
        }

        /// <summary>
        /// 根据协议名称获取协议详情
        /// </summary>
        /// <param name="name">协议名称</param>
        /// <returns>协议详情</returns>
        public ProtocolInfo? GetProtocolInfoByName(string name)
        {
            return ProtocolDetailList.FirstOrDefault(p => p.Name == name);
        }
        #endregion
    }
}
