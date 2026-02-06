using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Communication
{
    /// <summary>
    /// 模拟CAN协议的AT指令帧生成器
    /// 严格遵循手册定义的帧格式：头 + 4字节ID + 数据长度 + 数据 + 尾
    /// </summary>
    public static class CanAtFrameBuilder
    {
        // 固定帧头（AT）
        private static readonly byte[] FrameHeader = { 0x41, 0x54 };
        // 固定帧尾（\r\n）
        private static readonly byte[] FrameTail = { 0x0D, 0x0A };

        /// <summary>
        /// 生成完整的AT指令帧
        /// </summary>
        /// <param name="frameId">业务ID（标准帧11位，扩展帧29位）</param>
        /// <param name="isExtendedFrame">是否为扩展帧</param>
        /// <param name="isRemoteFrame">是否为远程帧</param>
        /// <param name="data">数据内容（远程帧时必须为null或空）</param>
        /// <returns>完整的AT指令帧字节数组</returns>
        public static byte[] BuildFrame(uint frameId, bool isExtendedFrame, bool isRemoteFrame, byte[] data = null)
        {
            // 远程帧数据长度必须为0
            if (isRemoteFrame && data != null && data.Length > 0)
                throw new ArgumentException("远程帧不能包含数据", nameof(data));

            // 打包4字节原始ID
            byte[] rawId = CanAtIdParser.PackRawId(frameId, isExtendedFrame, isRemoteFrame);
            // 数据长度（远程帧时为0）
            byte dataLength = (byte)(data?.Length ?? 0);

            // 拼接完整帧
            List<byte> frame = new List<byte>();
            frame.AddRange(FrameHeader);
            frame.AddRange(rawId);
            frame.Add(dataLength);
            if (data != null && data.Length > 0)
                frame.AddRange(data);
            frame.AddRange(FrameTail);

            return frame.ToArray();
        }

        /// <summary>
        /// 解析收到的AT指令帧
        /// </summary>
        /// <param name="frame">完整的AT指令帧字节数组</param>
        /// <returns>解析后的帧信息</returns>
        public static CanAtFrameInfo ParseFrame(byte[] frame)
        {
            if (frame == null || frame.Length < 8) // 最小帧长度：头(2) + ID(4) + 长度(1) + 尾(2) -1
                throw new ArgumentException("帧长度无效", nameof(frame));

            // 验证帧头
            if (frame[0] != FrameHeader[0] || frame[1] != FrameHeader[1])
                throw new ArgumentException("帧头无效", nameof(frame));

            // 验证帧尾
            if (frame[frame.Length - 2] != FrameTail[0] || frame[frame.Length - 1] != FrameTail[1])
                throw new ArgumentException("帧尾无效", nameof(frame));

            // 提取各部分
            byte[] rawId = new byte[4];
            Array.Copy(frame, 2, rawId, 0, 4);
            byte dataLength = frame[6];
            byte[] data = new byte[dataLength];
            if (dataLength > 0)
                Array.Copy(frame, 7, data, 0, dataLength);

            // 解析ID
            CanIdInfo idInfo = CanAtIdParser.ParseRawId(rawId);

            return new CanAtFrameInfo
            {
                FrameHeader = FrameHeader,
                RawId = rawId,
                IdInfo = idInfo,
                DataLength = dataLength,
                Data = data,
                FrameTail = FrameTail
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
    /// 解析后的AT指令帧信息
    /// </summary>
    public class CanAtFrameInfo
    {
        public byte[] FrameHeader { get; set; }
        public byte[] RawId { get; set; }
        public CanIdInfo IdInfo { get; set; }
        public byte DataLength { get; set; }
        public byte[] Data { get; set; }
        public byte[] FrameTail { get; set; }
    }

}
