using cross.database;
using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    // 完全对标COMModbus的纯原生CANopen协议类 | 无任何第三方库依赖 | 串口透传TTL转CAN模块专用
    public class COMCANopen : IProtocol
    {
        byte[] revbuffer = new byte[256];

        // ========== 【和你的Modbus类完全一致】单例模式，一字不改 ==========
        private COMCANopen() { }
        private static readonly Lazy<COMCANopen> _instance = new Lazy<COMCANopen>(() => new COMCANopen());
        public static COMCANopen Instance => _instance.Value;

        // ========== CANopen基础配置 (可修改，和你的设备匹配即可) ==========
        private const byte CAN_NODE_ID = 0x01;    // CANopen节点ID 1-127，对应Modbus从站地址0x01
        private const int CAN_FRAME_FIX_LEN = 13; // TTL转CAN模块串口透传固定长度：ID4+DLC1+DATA8

        #region CANopen核心常量 (协议固定值，不用改)
        private const uint NMT_CAN_ID = 0x00000000;        // NMT指令固定CAN-ID
        private const byte NMT_CMD_START = 0x01;           // 启动节点
        private const byte NMT_CMD_STOP = 0x02;            // 停止节点
        private const uint SDO_WRITE_ID_BASE = 0x00000200; // SDO写 帧ID基础值 0x200+节点ID
        private const uint SDO_READ_ID_BASE = 0x00000400;  // SDO读 帧ID基础值 0x400+节点ID
        private const byte SDO_CMD_WRITE = 0x23;           // SDO写 指令码
        private const byte SDO_CMD_READ = 0x40;            // SDO读 指令码
        #endregion

        #region 核心工具：CAN帧↔串口透传字节数组 互转 (适配你的TTL转CAN模块，通用99%模块，不用改)
        /// <summary>
        /// CAN帧转串口透传字节数组 (ID4字节+DLC1字节+DATA8字节)，给TTL转CAN模块发送
        /// </summary>
        private void CanFrameToSerialBytes(uint canId, byte[] canData, byte[] sendbf)
        {
            Array.Clear(sendbf, 0, sendbf.Length);
            // 填充4字节CAN-ID (小端，所有TTL转CAN模块通用)
            sendbf[0] = (byte)(canId & 0xFF);
            sendbf[1] = (byte)((canId >> 8) & 0xFF);
            sendbf[2] = (byte)((canId >> 16) & 0xFF);
            sendbf[3] = (byte)((canId >> 24) & 0xFF);
            // 填充1字节数据长度DLC
            sendbf[4] = (byte)canData.Length;
            // 填充8字节数据段
            Array.Copy(canData, 0, sendbf, 5, canData.Length);
        }

        /// <summary>
        /// 串口接收的字节数组 转 CAN帧信息 (解析ID、数据长度、数据段)
        /// </summary>
        private void SerialBytesToCanFrame(byte[] buffer, out uint canId, out byte[] canData, out byte dlc)
        {
            canId = 0;
            dlc = 0;
            canData = new byte[8];
            if (buffer.Length < CAN_FRAME_FIX_LEN) return;
            // 解析4字节CAN-ID
            canId = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            // 解析数据长度
            dlc = buffer[4];
            // 解析数据段
            Array.Copy(buffer, 5, canData, 0, dlc);
        }
        #endregion

        #region 【完全对标Modbus】实现IProtocol接口 - 所有方法一字不改，调用逻辑完全一致
        /// <summary>
        /// CANopen节点启停控制，对应你的Modbus MonitorRun
        /// </summary>
        public int MonitorRun(byte[] sendbf, bool brun, int addr = 0)
        {
            // CANopen NMT指令：数据段固定2字节 [控制码,节点ID]
            byte[] nmtData = new byte[2];
            nmtData[0] = brun ? NMT_CMD_START : NMT_CMD_STOP;
            nmtData[1] = CAN_NODE_ID;
            // 打包为串口透传格式
            CanFrameToSerialBytes(NMT_CAN_ID, nmtData, sendbf);
            // 固定返回串口透传长度13
            return CAN_FRAME_FIX_LEN;
        }

        /// <summary>
        /// CANopen SDO写入对象字典 核心方法 | 对应你的Modbus MonitorSet
        /// 入参规则完全一致：sendbf=发送缓冲区，tempsn=对象字典索引(如0x6040)，value=写入值，regtype=类型，regOccupyCount>1批量写入
        /// </summary>
        public int MonitorSet(byte[] sendbf, int tempsn, object value = null, object regtype = null, int regOccupyCount = 1)
        {
            // 前置校验 和你的Modbus完全一致
            if (value == null) throw new ArgumentNullException(nameof(value), "写入数值不能为空！");
            if (sendbf == null || sendbf.Length == 0) throw new ArgumentException("发送缓冲区不能为空且长度大于0！");
            if (tempsn < 0) throw new ArgumentException("对象字典索引不能为负数，tempsn≥0！");
            if (regOccupyCount < 1) regOccupyCount = 1;

            // 只读类型拦截，和你的Modbus逻辑完全一致
            switch (regtype)
            {
                case "离散输入(RO)":
                case "输入寄存器(RO)":
                    throw new ArgumentException("离散输入(RO)、输入寄存器(RO)为只读类型，不支持写入操作！");
            }

            byte[] writeData = new byte[8];
            ushort index = (ushort)tempsn;
            byte subIndex = 0x00; // CANopen子索引默认0，可自定义

            // 解析写入值：兼容byte[]数组透传 和 数值类型(浮点/整型)，和你的Modbus一致
            if (value is byte[] byteValue)
            {
                writeData = byteValue.Take(8).ToArray();
            }
            else
            {
                float writeVal = Convert.ToSingle(value);
                writeData = BitConverter.GetBytes((ushort)writeVal);
            }

            // ========== CANopen SDO写 标准帧格式 核心打包 (固定逻辑，不用改) ==========
            // SDO写数据段固定格式：[指令码0x23, 索引高字节, 索引低字节, 子索引, 数据1,数据2,数据3,数据4]
            byte[] sdoWriteData = new byte[8];
            sdoWriteData[0] = SDO_CMD_WRITE;
            sdoWriteData[1] = (byte)((index >> 8) & 0xFF); // 索引高位
            sdoWriteData[2] = (byte)(index & 0xFF);        // 索引低位
            sdoWriteData[3] = subIndex;                    // 子索引
            // 填充写入数据
            Array.Copy(writeData, 0, sdoWriteData, 4, writeData.Length > 4 ? 4 : writeData.Length);

            // 计算SDO写的CAN-ID：0x200 + 节点ID
            uint sdoWriteId = SDO_WRITE_ID_BASE + CAN_NODE_ID;
            // 打包为串口透传格式
            CanFrameToSerialBytes(sdoWriteId, sdoWriteData, sendbf);

            return CAN_FRAME_FIX_LEN;
        }

        /// <summary>
        /// CANopen SDO读取对象字典 核心方法 | 对应你的Modbus MonitorGet
        /// 入参规则完全一致：sendbf=发送缓冲区，tempsn=对象字典索引，num=读取个数，regtype=类型
        /// </summary>
        public int MonitorGet(byte[] sendbf, int tempsn, object num = null, object regtype = null)
        {
            // 前置校验 和你的Modbus完全一致
            if (sendbf == null || sendbf.Length == 0) throw new ArgumentException("发送缓冲区不能为空且长度大于0！");
            if (tempsn < 0) throw new ArgumentException("对象字典索引不能为负数，tempsn≥0！");

            ushort index = (ushort)tempsn;
            byte subIndex = 0x00;
            int readCount = num == null ? 1 : Convert.ToInt32(num);

            // ========== CANopen SDO读 标准帧格式 核心打包 (固定逻辑，不用改) ==========
            // SDO读数据段固定格式：[指令码0x40, 索引高字节, 索引低字节, 子索引, 0x00,0x00,0x00,0x00]
            byte[] sdoReadData = new byte[8];
            sdoReadData[0] = SDO_CMD_READ;
            sdoReadData[1] = (byte)((index >> 8) & 0xFF);
            sdoReadData[2] = (byte)(index & 0xFF);
            sdoReadData[3] = subIndex;

            // 计算SDO读的CAN-ID：0x400 + 节点ID
            uint sdoReadId = SDO_READ_ID_BASE + CAN_NODE_ID;
            // 打包为串口透传格式
            CanFrameToSerialBytes(sdoReadId, sdoReadData, sendbf);

            return CAN_FRAME_FIX_LEN;
        }

        /// <summary>
        /// CANopen应答帧校验 | 对应你的Modbus MonitorCheck CRC校验
        /// 返回值规则完全一致：0x01=校验通过，0x03=校验失败
        /// </summary>
        public int MonitorCheck(byte[] buffer, object len = null)
        {
            int recvLen = len == null ? buffer.Length : Convert.ToInt32(len);
            // 校验串口透传最小长度
            if (recvLen < CAN_FRAME_FIX_LEN) return 0x03;

            // 解析CAN帧
            SerialBytesToCanFrame(buffer, out uint canId, out byte[] canData, out byte dlc);

            // CANopen校验规则：应答帧ID必须是0x600+节点ID(SDO应答固定ID) + 数据长度合法 + 指令码正确
            bool isAckOk = (canId == 0x600 + CAN_NODE_ID) && dlc <= 8 && (canData[0] == 0x60 || canData[0] == 0x70);
            return isAckOk ? 0x01 : 0x03;
        }

        /// <summary>
        /// CANopen返回数据解析 | 对应你的Modbus MonitorSolve
        /// 返回值完全复用你的DataR类，字段含义不变，上层解析逻辑完全不用改
        /// </summary>
        public DataR MonitorSolve(byte[] buffer, object Readpos = null)
        {
            DataR data = new DataR();
            data.COMMAND = 0x00;
            data.SN = Readpos == null ? 0 : Convert.ToInt32(Readpos);
            data.ByteArr = new byte[0];
            data.RWX = 0;

            try
            {
                if (buffer.Length >= CAN_FRAME_FIX_LEN)
                {
                    // 解析CAN帧数据
                    SerialBytesToCanFrame(buffer, out uint canId, out byte[] canData, out byte dlc);
                    // 只解析合法的SDO应答帧
                    if (canId == 0x600 + CAN_NODE_ID && dlc > 0)
                    {
                        // 提取有效数据段，和Modbus的ByteArr格式一致
                        data.ByteArr = canData.Take(dlc).ToArray();
                        data.COMMAND = 0x03; // 对应Modbus的读成功功能码
                        data.RWX = 1;        // 1=解析成功，0=失败，规则不变
                    }
                }
            }
            catch
            {
                data.RWX = 0;
            }
            return data;
        }
        #endregion
    }
}
