using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace cross;

/// <summary>
/// WPF 兼容的 MessageBox 按钮枚举（Avalonia 11 未内置）。
/// </summary>
public enum MessageBoxButton { Ok, Cancel, YesNo, YesNoCancel, AbortRetryIgnore }

/// <summary>
/// WPF 兼容的 MessageBox 图标枚举（Avalonia 11 未内置）。
/// </summary>
public enum MessageBoxImage { None, Hand, Stop, Error, Question, Exclamation, Warning, Asterisk, Information }

/// <summary>
/// Avalonia 11 原生 MessageBox 辅助：自动从经典桌面应用生命周期取主窗口，
/// 替代 ThingLing.Controls.MessageBox（后者依赖 Avalonia 0.10，与本项目 11.3.11 冲突）。
/// </summary>
public static class DialogHelper
{
    private static Window? GetParentWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public static async Task<string?> ShowAsync(string message, string? title = null,
        MessageBoxButton button = MessageBoxButton.Ok, MessageBoxImage icon = MessageBoxImage.None)
    {
        var parent = GetParentWindow();
        if (parent is null) return null;

        var dialog = new Window
        {
            Title = title ?? "",
            Width = 420,
            Height = 180,
            MinWidth = 320,
            MinHeight = 120,
            CanResize = false,
        };

        var mainStack = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
        };

        var textBlock = new TextBlock
        {
            Text = message ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 6,
        };
        mainStack.Children.Add(textBlock);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 8, 0, 0),
        };

        string? result = null;

        var okBtn = new Button { Content = "确定", Width = 70, Margin = new Thickness(4) };
        okBtn.Click += (_, _) => { result = "Ok"; dialog.Close(result); };
        var cancelBtn = new Button { Content = "取消", Width = 70, Margin = new Thickness(4) };
        cancelBtn.Click += (_, _) => { result = "Cancel"; dialog.Close(result); };
        var yesBtn = new Button { Content = "是", Width = 70, Margin = new Thickness(4) };
        yesBtn.Click += (_, _) => { result = "Yes"; dialog.Close(result); };
        var noBtn = new Button { Content = "否", Width = 70, Margin = new Thickness(4) };
        noBtn.Click += (_, _) => { result = "No"; dialog.Close(result); };
        var abortBtn = new Button { Content = "中止", Width = 70, Margin = new Thickness(4) };
        abortBtn.Click += (_, _) => { result = "Abort"; dialog.Close(result); };
        var retryBtn = new Button { Content = "重试", Width = 70, Margin = new Thickness(4) };
        retryBtn.Click += (_, _) => { result = "Retry"; dialog.Close(result); };
        var ignoreBtn = new Button { Content = "忽略", Width = 70, Margin = new Thickness(4) };
        ignoreBtn.Click += (_, _) => { result = "Ignore"; dialog.Close(result); };

        switch (button)
        {
            case MessageBoxButton.Ok:
                btnPanel.Children.Add(okBtn);
                break;
            case MessageBoxButton.Cancel:
                btnPanel.Children.Add(okBtn);
                btnPanel.Children.Add(cancelBtn);
                break;
            case MessageBoxButton.YesNo:
                btnPanel.Children.Add(yesBtn);
                btnPanel.Children.Add(noBtn);
                break;
            case MessageBoxButton.YesNoCancel:
                btnPanel.Children.Add(yesBtn);
                btnPanel.Children.Add(noBtn);
                btnPanel.Children.Add(cancelBtn);
                break;
            case MessageBoxButton.AbortRetryIgnore:
                btnPanel.Children.Add(abortBtn);
                btnPanel.Children.Add(retryBtn);
                btnPanel.Children.Add(ignoreBtn);
                break;
        }

        mainStack.Children.Add(btnPanel);
        dialog.Content = mainStack;

        return await dialog.ShowDialog<string?>(parent);
    }
}
