using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Input;

namespace cross.Mvvm
{
    /// <summary>
    /// 泛型RelayCommand（适配Avalonia，替代WPF的RelayCommand）
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // 核心逻辑保留：判断命令是否可执行
        public bool CanExecute(object parameter)
        {
            // 处理参数类型转换，避免强制转换报错
            if (parameter != null && !(parameter is T))
                return false;

            return _canExecute == null || _canExecute((T)parameter);
        }

        // 核心逻辑保留：执行命令
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute((T)parameter);
            }
        }

        // 修复核心：替换CommandManager，手动触发CanExecuteChanged
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 手动触发CanExecuteChanged事件（更新命令可执行状态）
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}
