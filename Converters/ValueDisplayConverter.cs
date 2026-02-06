using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace cross.Converters
{
    // 确保继承IMultiValueConverter（多值绑定必须用这个）
    public class ValueDisplayConverter : IMultiValueConverter
    {
        /// <summary>
        /// 多值转换：参数1=Value（数值），参数2=DisplayType（呈现类型）
        /// </summary>
        /// <param name="values">values[0] = Value，values[1] = DisplayType</param>
        /// <param name="targetType">目标类型（TextBlock.Text是string）</param>
        /// <param name="parameter">额外参数（未使用）</param>
        /// <param name="culture">文化信息</param>
        /// <returns>格式化后的显示字符串</returns>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // 1. 核心校验：参数数量不足/空值直接返回"0"
            if (values == null || values.Count < 2 || values[0] == null || values[1] == null
                || values[0] == AvaloniaProperty.UnsetValue)
            {
                return "0";
            }

            // 2. 提取呈现类型和原始值
            string displayType = values[1].ToString().Trim();
            object rawValue = values[0];

            try
            {
                switch (displayType)
                {
                    case "十进制整数":
                        // 安全转换为int，再转字符串（解决报错核心）
                        int decimalInt = System.Convert.ToInt32(rawValue);
                        return decimalInt.ToString(); // 返回string（object类型）

                    case "十六进制整数":
                        // 转十六进制（带0x前缀，大写）
                        int hexInt = System.Convert.ToInt32(rawValue);
                        return $"0x{hexInt:X}"; // 返回格式化字符串

                    case "浮点数":
                    default:
                        // 转浮点数，保留2位小数
                        double floatValue = System.Convert.ToDouble(rawValue);
                        return floatValue.ToString("0.00");
                }
            }
            catch (Exception ex)
            {
                // 转换失败返回"0"，也可返回异常提示（调试用）
                // return $"错误：{ex.Message}";
                return "0";
            }
        }

        /// <summary>
        /// 反向转换（Value列是只读的，直接返回null即可）
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 只读列无需反向转换，返回null避免报错
            return null;
        }
    }
}
