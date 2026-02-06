using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    /// <summary>
    /// 模拟CAN协议AT指令ID解析工具类
    /// 严格遵循手册定义的32位ID位段规则
    /// </summary>
    public static class CanAtIdParser
    {
        /// <summary>
        /// 从4字节原始ID中解析出业务ID和帧类型
        /// </summary>
        /// <param name="rawIdBytes">4字节原始ID（高位在前，大端序）</param>
        /// <returns>解析结果对象</returns>
        public static CanIdInfo ParseRawId(byte[] rawIdBytes)
        {
            if (rawIdBytes == null || rawIdBytes.Length != 4)
                throw new ArgumentException("原始ID必须为4字节", nameof(rawIdBytes));

            // 将4字节大端序转换为32位整数
            uint rawId = (uint)(rawIdBytes[0] << 24 | rawIdBytes[1] << 16 | rawIdBytes[2] << 8 | rawIdBytes[3]);

            // 按手册位段拆分
            var info = new CanIdInfo
            {
                RawId = rawId,
                // 标准帧ID：高11位 (bit31-bit21)
                StandardFrameId = (ushort)(rawId >> 21 & 0x7FF),
                // 扩展帧ID：高11位 + 中间18位 (bit31-bit3)
                ExtendedFrameId = rawId >> 3 & 0x1FFFFFFF,
                // 扩展帧标识：bit2
                IsExtendedFrame = (rawId >> 2 & 0x01) == 1,
                // 远程帧标识：bit1
                IsRemoteFrame = (rawId >> 1 & 0x01) == 1,
                // Bit0固定为0
                Bit0 = (byte)(rawId & 0x01)
            };

            return info;
        }

        /// <summary>
        /// 将业务ID和帧类型打包为4字节原始ID（大端序）
        /// </summary>
        /// <param name="frameId">业务ID（标准帧11位，扩展帧29位）</param>
        /// <param name="isExtendedFrame">是否为扩展帧</param>
        /// <param name="isRemoteFrame">是否为远程帧</param>
        /// <returns>4字节原始ID（大端序）</returns>
        public static byte[] PackRawId(uint frameId, bool isExtendedFrame, bool isRemoteFrame)
        {
            uint rawId = 0;

            if (isExtendedFrame)
            {
                // 扩展帧：29位ID + 帧类型标识
                rawId |= (frameId & 0x1FFFFFFF) << 3;
            }
            else
            {
                // 标准帧：11位ID + 帧类型标识
                rawId |= (frameId & 0x7FF) << 21;
            }

            // 设置扩展帧和远程帧标识位
            rawId |= (isExtendedFrame ? 1U : 0U) << 2;
            rawId |= (isRemoteFrame ? 1U : 0U) << 1;
            rawId |= 0x00; // Bit0固定为0

            // 转换为大端序4字节数组
            return new[]
            {
            (byte)(rawId >> 24 & 0xFF),
            (byte)(rawId >> 16 & 0xFF),
            (byte)(rawId >> 8 & 0xFF),
            (byte)(rawId & 0xFF)
        };
        }

        /// <summary>
        /// 字节数组转十六进制字符串（带空格分隔，调试用）
        /// </summary>
        public static string BytesToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }
    }

    /// <summary>
    /// 解析后的CAN ID信息
    /// </summary>
    public class CanIdInfo
    {
        public uint RawId { get; set; }
        public ushort StandardFrameId { get; set; }
        public uint ExtendedFrameId { get; set; }
        public bool IsExtendedFrame { get; set; }
        public bool IsRemoteFrame { get; set; }
        public byte Bit0 { get; set; }
    }
}
