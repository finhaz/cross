using cross.database;
using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    public class TCPModbus : IProtocol
    {
        // 接收缓冲区（适配TCP响应长度）
        private readonly byte[] _revBuffer = new byte[512];

        // 单例模式（与串口版一致）
        private TCPModbus() { }
        private static readonly Lazy<TCPModbus> _instance = new Lazy<TCPModbus>(() => new TCPModbus());
        public static TCPModbus Instance => _instance.Value;

        #region 核心方法：构建标准Modbus TCP报文
        /// <summary>
        /// 构建MBAP报文头（严格遵循Modbus TCP标准，适配用户示例格式）
        /// </summary>
        /// <param name="transactionId">事务ID（默认0000）</param>
        /// <param name="unitId">单元ID（从站地址，默认01）</param>
        /// <param name="dataLength">功能码+后续数据的总长度</param>
        /// <returns>7字节MBAP头</returns>
        private byte[] BuildMBAPHeader(ushort transactionId = 0x0000, byte unitId = 0x01, ushort dataLength = 0x0000)
        {
            var mbap = new byte[7];
            // 1. 事务ID（2字节，大端序，默认0000）
            var transBytes = BitConverter.GetBytes(transactionId);
            Array.Reverse(transBytes);
            mbap[0] = transBytes[0];
            mbap[1] = transBytes[1];
            // 2. 协议ID（2字节，固定0000）
            mbap[2] = 0x00;
            mbap[3] = 0x00;
            // 3. 长度（2字节，大端序：= 单元ID后的字节数 = 功能码+数据长度）
            var lenBytes = BitConverter.GetBytes(dataLength);
            Array.Reverse(lenBytes);
            mbap[4] = lenBytes[0];
            mbap[5] = lenBytes[1];
            // 4. 单元ID（1字节，从站地址）
            mbap[6] = unitId;
            return mbap;
        }

        /// <summary>
        /// 转换short为大端序字节数组（Modbus标准）
        /// </summary>
        private byte[] ToBigEndianBytes(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// 转换int为大端序字节数组（Modbus标准）
        /// </summary>
        private byte[] ToBigEndianBytes(int value)
        {
            var bytes = BitConverter.GetBytes((ushort)value); // 数量最大支持65535，转ushort
            Array.Reverse(bytes);
            return bytes;
        }
        #endregion

        #region IProtocol接口实现（严格匹配串口版逻辑，仅适配TCP格式）
        public int MonitorRun(byte[] sendbf, bool brun, int addr = 0)
        {
            var order = brun ? 0xaa : 0x55;
            MonitorSet(sendbf, addr, order);
            // TCP发送长度：MBAP(7) + 数据段(6) = 13字节？不，用户示例是12字节，需按实际数据段长度计算
            return 12; // 动态返回有效字节长度
        }

        public int MonitorSet(byte[] sendbf, int tempsn, object value = null, object regtype = null, int regOccupyCount = 1)
        {
            // 1. 确定功能码（与串口版一致）
            byte functionCode = regtype switch
            {
                "线圈状态(RW)" => 0x05,
                "离散输入(RO)" => 0x02,
                "保持寄存器(RW)" => 0x06,
                "输入寄存器(RO)" => 0x04,
                _ => 0x06
            };

            if (functionCode != 0x05 && functionCode != 0x06)
                throw new ArgumentException("仅支持05(强制单线圈)和06(预置单寄存器)功能码");

            // 2. 处理写入值（与串口版一致）
            var sendValue = Convert.ToSingle(value);
            short svalue = (short)sendValue;
            if (functionCode == 0x05)
                svalue = svalue != 0 ? unchecked((short)0xFF00) : (short)0x0000; // 还原0xFF00，用unchecked避免编译错误

            // 3. 构建数据段（6字节：从站地址+功能码+地址+值）
            var dataSegment = new byte[6];
            dataSegment[0] = 0x01; // 从站地址（单元ID）
            dataSegment[1] = functionCode; // 功能码
            Array.Copy(ToBigEndianBytes((short)tempsn), 0, dataSegment, 2, 2); // 地址（大端序）
            Array.Copy(ToBigEndianBytes(svalue), 0, dataSegment, 4, 2); // 写入值（大端序）

            // 4. 构建MBAP头（长度字段=数据段长度6字节，因数据段已包含单元ID）
            var mbapHeader = BuildMBAPHeader(0x0000, 0x01, 0x0006); // 事务ID=0000，长度=06

            // 5. 拼接最终报文（MBAP头7字节 + 数据段6字节？不，用户示例是12字节，说明MBAP头实际是6字节，移除单元ID重复）
            // 适配用户示例格式：MBAP(6字节：事务ID+协议ID+长度) + 数据段(6字节：单元ID+功能码+地址+值)
            Array.Copy(mbapHeader, 0, sendbf, 0, 6); // 取MBAP前6字节（跳过单元ID，因数据段已包含）
            Array.Copy(dataSegment, 0, sendbf, 6, 6); // 数据段6字节（含单元ID）

            // 清空后续冗余字节
            Array.Clear(sendbf, 12, sendbf.Length - 12);

            return 12; // 总长度12字节（匹配用户示例格式）
        }

        public int MonitorGet(byte[] sendbf, int tempsn, object num = null, object regtype = null)
        {
            // 1. 确定功能码（与串口版一致）
            byte functionCode = regtype switch
            {
                "线圈状态(RW)" => 0x01,
                "离散输入(RO)" => 0x02,
                "保持寄存器(RW)" => 0x03,
                "输入寄存器(RO)" => 0x04,
                _ => 0x03
            };

            if (functionCode is not (0x01 or 0x02 or 0x03 or 0x04))
                throw new ArgumentException("仅支持01(线圈)/02(离散输入)/03(保持寄存器)/04(输入寄存器)功能码");

            // 2. 处理读取参数
            int readNum = Convert.ToInt32(num);
            if (readNum < 1 || readNum > 65535)
                throw new ArgumentOutOfRangeException(nameof(num), "读取数量范围：1-65535");

            // 3. 构建数据段（6字节：单元ID+功能码+起始地址+读取数量）
            var dataSegment = new byte[6];
            dataSegment[0] = 0x01; // 单元ID（从站地址）
            dataSegment[1] = functionCode; // 功能码
            Array.Copy(ToBigEndianBytes((short)tempsn), 0, dataSegment, 2, 2); // 起始地址（大端序）
            Array.Copy(ToBigEndianBytes(readNum), 0, dataSegment, 4, 2); // 读取数量（大端序）

            // 4. 构建MBAP头（适配用户示例：长度字段=6字节，对应数据段长度）
            var mbapHeader = BuildMBAPHeader(0x0000, 0x01, 0x0006);

            // 5. 拼接报文（MBAP前6字节 + 数据段6字节 = 12字节，匹配用户示例）
            Array.Copy(mbapHeader, 0, sendbf, 0, 6);
            Array.Copy(dataSegment, 0, sendbf, 6, 6);
            Array.Clear(sendbf, 12, sendbf.Length - 12);

            // 示例验证：当functionCode=0x03、tempsn=0、readNum=1时，报文为：
            // 00 00 00 00 00 06 01 03 00 00 00 01 → 完全匹配用户期望格式
            return 12;
        }

        public int MonitorCheck(byte[] buffer, object len = null)
        {
            int responseLen = Convert.ToInt32(len);
            // 最小合法响应长度：MBAP(6) + 功能码(1) = 7字节
            if (responseLen < 7) return 0x03;

            // 检查协议ID（必须为0000）
            ushort protocolId = BitConverter.ToUInt16(new[] { buffer[3], buffer[2] }, 0);
            if (protocolId != 0x0000) return 0x03;

            // 检查功能码（异常响应：功能码最高位=1）
            byte functionCode = buffer[7]; // 功能码在第8字节（索引7）
            if ((functionCode & 0x80) != 0) return 0x02; // 异常响应

            return 0x01; // 校验通过
        }

        public DataR MonitorSolve(byte[] buffer, object Readpos = null)
        {
            var data = new DataR();
            int dataOffset = 6; // 跳过MBAP前6字节，从第7字节开始解析

            // 功能码（数据段第2字节，索引6+1=7）
            data.COMMAND = buffer[dataOffset + 1];
            data.SN = Convert.ToInt32(Readpos);

            switch (data.COMMAND)
            {
                case 0x01: // 读线圈
                case 0x02: // 读离散输入
                    data.VALUE = buffer[dataOffset + 2]; // 数据从第9字节开始（索引8）
                    data.RWX = 1;
                    break;
                case 0x03: // 读保持寄存器
                case 0x04: // 读输入寄存器
                           // 数据长度字节（索引8）+ 寄存器值（大端序）
                    int byteCount = buffer[dataOffset + 2];
                    var valueBytes = buffer.Skip(dataOffset + 3).Take(byteCount).ToArray();
                    Array.Reverse(valueBytes); // 转小端序适配C#
                    data.VALUE = BitConverter.ToInt16(valueBytes, 0);
                    data.RWX = 1;
                    break;
                default: // 异常响应
                    data.SN = (buffer[dataOffset + 2] << 8) | buffer[dataOffset + 3];
                    data.RWX = 0;
                    break;
            }

            return data;
        }
        #endregion
    }
}
