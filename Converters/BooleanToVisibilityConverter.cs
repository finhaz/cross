using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace cross.Converters
{
    /// <summary>
    /// 布尔值转IsVisible（替代Avalonia移除的内置转换器）
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // 核心逻辑：true→可见（true），false→隐藏（false）
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 处理空值/非布尔值，默认隐藏
            if (value is not bool boolValue)
                return false;

            // 支持反向转换（可选：parameter为"Reverse"则取反）
            if (parameter?.ToString() == "Reverse")
                return !boolValue;

            return boolValue;
        }

        // 反向转换（无需实现，返回DoNothing）
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return BindingOperations.DoNothing;
        }
    }
}
