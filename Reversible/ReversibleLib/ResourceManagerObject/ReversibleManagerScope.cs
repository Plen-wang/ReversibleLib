/***
 * author:plen_wang
 * time:2012-06-10
 * **/

using System;
using System.Transactions;

namespace ReversibleLib.ResourceManagerObject
{

    /// <summary>
    /// 使代码成为可逆框架的事务性代码
    /// </summary>
    public class ReversibleManagerScope : IDisposable
    {
        /// <summary>
        /// 初始化ReversibleManagerScope新的实例
        /// </summary>
        public ReversibleManagerScope()
        {
            ReversibleManager._reversibleManager = new ReversibleManager();
        }
        /// <summary>
        /// 使用ReversibleManager对象构造ReversibleManagerScope使用范围对象
        /// </summary>
        /// <param name="manager">ReversibleManager实例</param>
        public ReversibleManagerScope(ReversibleManager manager)
        {
            ReversibleManager._reversibleManager = manager;
        }
        /// <summary>
        /// 使用自定义资源管理器构造ReversibleManagerScope包装的环境ReversibleManager.Current中的对象实例。
        /// </summary>
        /// <param name="source">IEnlistmentNotification资源管理器</param>
        public ReversibleManagerScope(IEnlistmentNotification source)
        {
            ReversibleManager._reversibleManager = new ReversibleManager(source);
        }
        /// <summary>
        /// 全局上下文ReversibleManager对象销毁
        /// </summary>
        public void Dispose()
        {
            ReversibleManager._reversibleManager = null;
        }
        /// <summary>
        /// 完成整个操作的提交。该操作将提交事务栈中的所有依赖事务
        /// </summary>
        public void Completed()
        {
            ReversibleManager.Current.Commit();
        }
    }
}
