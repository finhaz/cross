using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

//数据控件其实很好理解，它就是把UI控件中存储的数据提取出来,
//好让ViewModel可以通过修改数据来控制UI变化；
//当然，为了更好的控制UI变化，数据控件里还得包含一点管理UI的属性。

namespace cross.Mvvm
{
    public class Control<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // 数据内容
        public T _DataContent;
        public T DataContent 
        { 
            get { return _DataContent; } 
            set { _DataContent = value; OnPropertyChanged(); } 
        }

        /*
        // 可见性
        public IsVisible _Visibility;
        public IsVisible Visibility 
        { 
            get { return _Visibility; } 
            set { _Visibility = value; OnPropertyChanged(); } 
        }
        */
        // 只读状态
        public bool _IsReadOnly;
        public bool IsReadOnly 
        { 
            get { return _IsReadOnly; } 
            set { _IsReadOnly = value; OnPropertyChanged(); } 
        }
        // 启用状态
        public bool _IsEnabled;
        public bool IsEnabled 
        { 
            get { return _IsEnabled; } 
            set { _IsEnabled = value; OnPropertyChanged(); } 
        }
        // 触发属性变更通知（优化空值调用）
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TextBlock<T> : Control<T>
    {
        public T _Text;
        public T Text
        {
            get { return _Text; }
            set
            {
                _Text = value;
                OnPropertyChanged();
            }
        }
    }

    public class TextBox<T> : Control<T>
    {
        public Action<T> TextChangeCallBack = null;

        public T _Text;
        public T Text
        {
            get { return _Text; }
            set
            {
                _Text = value;
                if (TextChangeCallBack != null)
                {
                    TextChangeCallBack(_Text);
                }
                OnPropertyChanged();
            }
        }
    }

    /*
    public class MyCommand : ICommand
    {
        private readonly Action<object> execAction;
        private readonly Func<object, bool> changeFunc;

        public MyCommand(Action<object> execAction, Func<object, bool> changeFunc)
        {
            this.execAction = execAction ?? throw new ArgumentNullException(nameof(execAction));
            this.changeFunc = changeFunc ?? (o => true); // 默认可执行
        }

        //public event EventHandler CanExecuteChanged;
        // 关联CommandManager，实现自动重新查询
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public bool CanExecute(object parameter)
        {
            return this.changeFunc.Invoke(parameter);
        }

        public void Execute(object parameter)
        {
            this.execAction.Invoke(parameter);
        }

        //public bool CanExecute(object parameter) => changeFunc(parameter);

        //public void Execute(object parameter) => execAction(parameter);

        // 手动触发方法（非WPF框架使用）
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested(); // WPF中也可用此方法手动刷新
        }

    }



    public class NotifyObject: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    */





}
