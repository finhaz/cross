using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace cross;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        // 找到TextBlock（先给TextBlock命名：x:Name="MainText"）
        var mainText = this.FindControl<TextBlock>("MainText");
        if (mainText != null)
        {
            // 强制设置字体，优先级最高
            mainText.FontFamily = new FontFamily("SimSun");
            //mainText.FontFamilyFallback = new FontFamilyFallback(); // 清空回退字体
        }
    }
}