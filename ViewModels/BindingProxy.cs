using Avalonia;
using Avalonia.Controls;

namespace cross.ViewModels
{
    /// <summary>
    /// Avalonia 官方标准 BindingProxy（替代 WPF Freezable 代理）
    /// 解决：DataGrid 列 / 非可视化元素 拿不到 DataContext 的问题
    /// </summary>
    public class BindingProxy : Control
    {
        // 绑定数据属性（Avalonia 标准属性）
        public static readonly StyledProperty<object?> DataProperty =
            AvaloniaProperty.Register<BindingProxy, object?>(nameof(Data));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}