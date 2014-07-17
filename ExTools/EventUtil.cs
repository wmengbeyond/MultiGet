using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet.ExTools
{
    /// <summary>
    /// 事件派发器
    /// </summary>
    public sealed class EventUtil
    {
        /// <summary>
        /// 异步派发无参事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handle"></param>
        public static void DispatchEventAsync(object sender, EventHandler handle)
        {
            if (handle != null)
            {
                //异步多播事件
                Delegate[] dlgList = handle.GetInvocationList();
                if (dlgList != null)
                {
                    foreach (Delegate dlg in dlgList)
                    {
                        ((EventHandler)dlg).BeginInvoke(sender, EventArgs.Empty, null, null);
                    }
                }
                ////如果只一个订阅者，下面的代码不会报错，但如果有多个则会抛出“目标有且只能有一个”
                //handle.BeginInvoke(sender, EventArgs.Empty, null, null);
            }
        }

        /// <summary>
        /// 同步派发无参事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handle"></param>
        public static void DispatchEvent(object sender, EventHandler handle)
        {
            if (handle != null)
            {
                //同步多播事件，不管几个订阅者都不会报错
                handle(sender, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 异步派发有参事件
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="handle"></param>
        /// <param name="e">事件参数</param>
        public static void DispatchEventAsync<TEventArgs>(object sender, EventHandler<TEventArgs> handle, TEventArgs e)
            where TEventArgs : EventArgs
        {
            if (handle != null)
            {
                //异步多播事件
                Delegate[] dlgList = handle.GetInvocationList();
                if (dlgList != null)
                {
                    foreach (Delegate dlg in dlgList)
                    {
                        ((EventHandler<TEventArgs>)dlg).BeginInvoke(sender, e, null, null);
                    }
                }
                ////如果只一个订阅者，下面的代码不会报错，但如果有多个则会抛出“目标有且只能有一个”
                //handle.BeginInvoke(sender, e, null, null);
            }
        }

        /// <summary>
        /// 同步派发有参事件
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="handle"></param>
        /// <param name="e">事件参数</param>
        public static void DispatchEvent<TEventArgs>(object sender, EventHandler<TEventArgs> handle, TEventArgs e)
            where TEventArgs : EventArgs
        {
            if (handle != null && e != null)
            {
                //同步多播事件，不管几个订阅者都不会报错
                handle(sender, e);
            }
        }
    }
}
