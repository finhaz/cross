using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace cross.Mvvm
{

    /// <summary>
    /// 实现属性变更通知的基础类，用于MVVM架构中绑定数据模型与视图
    /// 所有需要实现属性双向绑定的实体类/ViewModel类均可继承此类
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性值发生变更时触发的多播事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更通知的核心方法，通知所有订阅者指定属性已变更
        /// </summary>
        /// <param name="propertyName">发生变更的属性名称，无需手动传入，编译器会通过特性自动获取调用方名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 通用属性赋值方法，统一处理【值校验+字段赋值+属性变更通知】逻辑
        /// 仅当值发生实际变化时才执行赋值并触发通知，避免无效的UI刷新和性能损耗
        /// </summary>
        /// <typeparam name="T">属性的泛型类型，适配任意值类型/引用类型</typeparam>
        /// <param name="field">需要赋值的类私有字段（引用传递，直接修改原字段）</param>
        /// <param name="value">属性的目标赋值</param>
        /// <param name="propertyName">发生变更的属性名称，编译器自动传入，无需手动指定</param>
        /// <returns>赋值结果：True=值发生变化并完成赋值和通知，False=新旧值一致未做任何操作</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            // 校验新旧值是否一致，值类型/引用类型均能正确判断相等性
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            // 赋值并触发通知
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
