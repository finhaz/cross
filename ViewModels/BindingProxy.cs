using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace cross.ViewModels
{
    /*
     * BindingProxy（绑定代理）是 WPF 中专门解决非可视化元素无法绑定 DataContext 的一种 “数据桥接” 方式，本质是：
        它是一个继承自Freezable的自定义类（WPF 内置的基类）；
        作用是 “中转数据”—— 把上层的DataContext（比如包含ModbusSet的 ViewModel）“存” 在自己的属性里；
        让原本拿不到 DataContext 的逻辑对象（比如 DataGridColumn），能通过StaticResource找到这个 Proxy，间接拿到需要的数据。
     */
    // 继承Freezable是关键，赋予它“保留绑定”的特性
    public class BindingProxy : Freezable
    {
        // 必须重写的方法，创建Freezable实例
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        // 定义一个Data属性，用来“存”我们需要传递的数据（比如ModbusSet）
        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // 注册依赖属性（WPF绑定必须用依赖属性/INotifyPropertyChanged）
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
    }
}
