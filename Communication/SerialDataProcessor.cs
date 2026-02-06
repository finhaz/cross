using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace cross.Communication
{
    /// <summary>
    /// 串口数据处理工具类（单例模式）
    /// 封装串口数据的格式化、解析等功能，支持自定义前缀
    /// </summary>
    public class SerialDataProcessor
    {
        // 单例实现：懒加载、线程安全
        private SerialDataProcessor() { }
        private static readonly Lazy<SerialDataProcessor> _instance = new Lazy<SerialDataProcessor>(() => new SerialDataProcessor());
        public static SerialDataProcessor Instance => _instance.Value;

        // 常量定义：统一管理分隔符和换行符（可根据需求改为配置项）
        private const string HexSeparator = " ";
        private const string NewLine = "\r\n";

        /// <summary>
        /// 通用方法：将串口字节数组格式化为十六进制字符串（支持自定义前缀、起始索引）
        /// </summary>
        /// <param name="buffer">待格式化的字节数组</param>
        /// <param name="length">从起始索引开始的有效数据长度</param>
        /// <param name="prefix">自定义前缀（如"TX:""RX:""发送："等）</param>
        /// <param name="startIndex">字节数组的起始索引（默认0，支持切片）</param>
        /// <param name="useUpperCase">是否使用大写十六进制（默认true）</param>
        /// <returns>格式化后的字符串</returns>
        /// <exception cref="ArgumentNullException">字节数组或前缀为空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">起始索引或长度越界时抛出</exception>
        public string FormatSerialDataToHexString(byte[] buffer, int length, string prefix, int startIndex = 0, bool useUpperCase = true)
        {
            // 1. 严格的参数校验，避免运行时异常
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "字节数组不能为空");
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentNullException(nameof(prefix), "自定义前缀不能为空或空字符串");
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"起始索引必须在0到{buffer.Length - 1}之间");
            if (length < 0 || startIndex + length > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length), "起始索引+有效长度超出字节数组范围");

            // 2. 无有效数据时，直接返回前缀+换行
            if (length == 0)
                return $"{prefix}{NewLine}";

            // 3. 使用StringBuilder高效拼接字符串（避免频繁创建字符串对象）
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(prefix);

            // 4. 格式化每个字节为两位十六进制（补零，控制大小写）
            string hexFormat = useUpperCase ? "X2" : "x2";
            for (int i = startIndex; i < startIndex + length; i++)
            {
                stringBuilder.Append(buffer[i].ToString(hexFormat));
                stringBuilder.Append(HexSeparator);
            }

            // 5. 移除最后一个多余的分隔符，添加换行符
            if (stringBuilder.Length > prefix.Length)
                stringBuilder.Length--; // 去掉最后一个空格
            stringBuilder.Append(NewLine);

            // 6. 返回最终格式化的字符串
            return stringBuilder.ToString();
        }

        // （可选）重载方法：简化无起始索引的调用（默认起始索引0）
        public string FormatSerialDataToHexString(byte[] buffer, int length, string prefix, bool useUpperCase = true)
        {
            return FormatSerialDataToHexString(buffer, length, prefix, 0, useUpperCase);
        }

        // 扩展方法示例：字节数组转ASCII字符串（保持类的功能完整性）
        public string ConvertToAsciiString(byte[] buffer, int length, int startIndex = 0)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0 || startIndex + length > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            return Encoding.ASCII.GetString(buffer, startIndex, length);
        }
    }
}
