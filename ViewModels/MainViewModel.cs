using Avalonia.Controls;
using cross.ViewModels;
using System;
using System.Windows.Input;
using Avalonia;

namespace cross.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region 私有字段
        private bool _isHamburgerOpen = true;
        private GridLength _sidebarWidth = new GridLength(240);
        #endregion

        #region 公共绑定属性
        public bool IsHamburgerOpen
        {
            get => _isHamburgerOpen;
            set
            {
                // 只有值发生变化时才更新，避免无效触发
                if (_isHamburgerOpen == value) return;
                _isHamburgerOpen = value;
                // 更新侧边栏宽度
                SidebarWidth = _isHamburgerOpen ? new GridLength(240) : new GridLength(0);
                // 通知UI属性变更
                OnPropertyChanged();
                OnPropertyChanged(nameof(MainContentText));
            }
        }

        public GridLength SidebarWidth
        {
            get => _sidebarWidth;
            set
            {
                if (_sidebarWidth == value) return;
                _sidebarWidth = value;
                OnPropertyChanged();
            }
        }

        public string MainContentText => IsHamburgerOpen
            ? "侧边栏已展开 → 点击左上角☰折叠"
            : "侧边栏已折叠 → 点击左上角☰展开";

        // 命令属性：保持public，支持XAML绑定
        public ICommand ToggleHamburgerCommand { get; }
        #endregion

        #region 构造函数
        public MainViewModel()
        {
            // 初始化命令，绑定执行方法
            ToggleHamburgerCommand = new RelayCommand(ToggleHamburger);
        }
        #endregion

        #region 核心业务逻辑
        private void ToggleHamburger()
        {
            // 调试用：点击按钮时，控制台输出日志，验证命令是否执行
            Console.WriteLine("命令执行成功：切换侧边栏状态");
            IsHamburgerOpen = !IsHamburgerOpen;
        }
        #endregion
    }

    /// <summary>
    /// 修复版 RelayCommand，兼容 Avalonia 所有版本
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _executeAction;
        private readonly Func<bool> _canExecuteFunc;

        public RelayCommand(Action executeAction, Func<bool> canExecuteFunc = null)
        {
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _canExecuteFunc = canExecuteFunc ?? (() => true);
        }

        // 关键修复：RequerySuggested 适配 Avalonia 命令系统
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecuteFunc();

        public void Execute(object? parameter) => _executeAction();
    }

    // 补充：.NET 命令管理器适配类（解决 Avalonia 命令不触发问题）
    public static class CommandManager
    {
        public static event EventHandler? RequerySuggested;
        public static void InvalidateRequerySuggested() => RequerySuggested?.Invoke(null, EventArgs.Empty);
    }
}