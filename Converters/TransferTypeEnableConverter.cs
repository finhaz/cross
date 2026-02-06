using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ocean.Converters
{
    /// <summary>
    /// 基于NBit和SelectedOption控制传输类型下拉框选项是否启用的转换器
    /// </summary>
    public class TransferTypeEnableConverter : IMultiValueConverter
    {
        // 定义需要禁用的SelectedOption值列表
        private readonly List<string> _disabledSelectedOptions = new List<string>
        {
            "线圈状态(RW)",
            "离散输入(RO)"
        };

        // 定义SelectedOption触发禁用时，需要禁用的传输类型选项列表
        private readonly List<string> _disabledTransferTypes = new List<string>
        {
            "有符号整数",
            "无符号整数",
            "浮点数",
            "字节流"
        };

        // 浮点数选项文本（原有逻辑用）
        private readonly string _floatOptionText = "浮点数";

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // 1. 校验参数：需要3个参数（NBit、当前选项、SelectedOption）
            if (values.Count < 3 || values[0] == null || values[1] == null || values[2] == null)
                return true;

            // 2. 解析参数
            // 2.1 解析SelectedOption
            string selectedOption = values[2].ToString().Trim();
            // 2.2 解析当前传输类型选项
            string currentTransferType = values[1].ToString().Trim();
            // 2.3 解析NBit（处理转换失败，默认返回true）
            if (!int.TryParse(values[0].ToString(), out int nBit))
                return true;

            // 3. 新增逻辑：SelectedOption为指定值时，禁用目标传输类型
            if (_disabledSelectedOptions.Contains(selectedOption)
                && _disabledTransferTypes.Contains(currentTransferType))
            {
                return false; // 直接禁用
            }

            // 4. 原有逻辑：NBit<32时禁用浮点数
            if (nBit < 32 && currentTransferType.Equals(_floatOptionText, StringComparison.Ordinal))
            {
                return false; // 禁用浮点数
            }

            // 5. 其他情况启用
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("该转换器仅支持单向转换");
        }
    }
}
