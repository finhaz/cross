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
    // 布尔值转按钮文字的转换器：true→隐藏高级列，false→显示高级列
    public class BoolToButtonTextConverter : IValueConverter
    {
        // 正向转换（bool→文字）
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 判断传入的是否是bool值
            if (value is bool isVisible)
            {
                return isVisible ? "隐藏高级列" : "显示高级列";
            }
            // 非bool值时默认显示
            return "显示高级列";
        }

        // 反向转换（用不到，返回null即可）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
