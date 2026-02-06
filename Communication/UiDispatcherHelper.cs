using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
// 第一步：添加必需的命名空间
using Avalonia.Threading;

namespace cross.Communication
{
    /// <summary>
    /// WPF UI调度工具类：提供线程安全的控件更新方法
    /// </summary>
    public static class UiDispatcherHelper
    {
        /// <summary>
        /// 线程安全地执行UI操作（同步）
        /// </summary>
        /// <param name="action">要执行的UI操作（如更新控件属性）</param>
        public static void ExecuteOnUiThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            // 核心修复：Avalonia官方推荐的调度器获取方式
            // 优先UI线程调度器 → 兜底当前线程调度器
            var dispatcher = Dispatcher.UIThread;

            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// 线程安全地执行UI操作（异步，不阻塞调用线程）
        /// </summary>
        /// <param name="action">要执行的UI操作</param>
        public static void ExecuteOnUiThreadAsync(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var dispatcher = Dispatcher.UIThread;

            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                dispatcher.Post(action);
            }
        }
    }
}
