using Avalonia.Controls;
using Avalonia.Interactivity;
using cross.ViewModels;

namespace cross.Views;

public partial class ProIntroductionPage : UserControl
{
    private readonly AppViewModel _globalVM = AppViewModel.Instance;

    public ProIntroductionPage()
    {
        InitializeComponent();
        // 与 WPF 原版一致：整页 DataContext 指到全局 ViewModel
        DataContext = _globalVM;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // 无额外初始化；绑定由 XAML 完成
    }
}
