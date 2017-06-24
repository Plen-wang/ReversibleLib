/***
 * author:plen_wang
 * time:2012-06-10
 * **/

using System;
using System.Collections.Generic;
using System.Transactions;

namespace ReversibleLib
{
    /// <summary>
    /// 可逆模块的入口。
    /// ReversibleManager对事务对象的封装，实现阶段性的事务提交和回滚。
    /// </summary>
    public class ReversibleManager
    {
        #region 上下文静态ReversibleManager实例
        /// <summary>
        /// 持有对可逆框架的对象引用
        /// </summary>
        internal static ReversibleManager _reversibleManager;
        /// <summary>
        /// 获取当前上下文中可逆框架
        /// </summary>
        public static ReversibleManager Current
        {
            get { return _reversibleManager; }
        }
        #endregion

        #region 构造对象
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ReversibleManager() { }
        /// <summary>
        /// 表示可提交的事务(主事务)
        /// </summary>
        private CommittableTransaction _commiTransaction;
        /// <summary>
        /// 支持两阶段提交协议的资源管理器(主资源管理器)
        /// </summary>
        private IEnlistmentNotification _resourceManager;
        /// <summary>
        /// 重载构造函数，使用自定义资源管理器构造可逆模块的开始。
        /// </summary>
        /// <param name="resource">IEnlistmentNotification接口对象</param>
        public ReversibleManager(IEnlistmentNotification resource)
        {
            _resourceManager = resource;
            InitLoad(IsolationLevel.Serializable);
        }
        /// <summary>
        /// 重载构造函数，使用自定义资源管理器、内部事务范围的事务隔离级别构造可逆模型的开始。
        /// </summary>
        /// <param name="resource">IEnlistmentNotification接口对象</param>
        /// <param name="isolationlevel">IsolationLevel枚举成员</param>
        public ReversibleManager(IEnlistmentNotification resource, IsolationLevel isolationlevel)
        {
            _resourceManager = resource;
            InitLoad(isolationlevel);
        }
        /// <summary>
        /// 事务初始化阶段的参数对象
        /// </summary>
        TransactionOptions _options;
        /// <summary>
        /// 重载构造函数，使用自定义资源管理器、内部事务范围的事务隔离级别、事务超时时间范围构造可逆模块的开始。
        /// </summary>
        /// <param name="resource">IEnlistmentNotification接口对象</param>
        /// <param name="isolationlevel">IsolationLevel枚举成员</param>
        /// <param name="span">TimeSpan时间范围</param>
        public ReversibleManager(IEnlistmentNotification resource, IsolationLevel isolationlevel, TimeSpan span)
        {
            _options = new TransactionOptions();
            _options.Timeout = span;
            InitLoad(isolationlevel);
        }
        /// <summary>
        /// 构造CommittableTransaction对象实例。
        /// </summary>
        /// <param name="level">事务隔离级别</param>
        private void InitLoad(IsolationLevel level)
        {
            if (_options == null)
                _options = new TransactionOptions();
            _options.IsolationLevel = level;
            _commiTransaction = new CommittableTransaction(_options);
            _commiTransaction.EnlistVolatile(_resourceManager, EnlistmentOptions.None);
            //作为事务栈的头开始整个可逆结构。
            _tranStack.Push(_commiTransaction);//压入事务栈
            _resourceStack.Push(_resourceManager);//压入资源栈
            //设置环境事务，让所有支持事务性感知框架的代码都能执行。
            Transaction.Current = _commiTransaction;
        }
        #endregion

        /// <summary>
        /// 事务栈，依次存放事务。
        /// </summary>
        private System.Collections.Generic.Stack<Transaction> _tranStack = new Stack<Transaction>();
        /// <summary>
        /// 资源栈，依次存放事务使用的资源。
        /// </summary>
        private System.Collections.Generic.Stack<IEnlistmentNotification> _resourceStack = new Stack<IEnlistmentNotification>();
        /// <summary>
        /// 阶段性事件委托
        /// </summary>
        /// <param name="tran">Transaction环境事务</param>
        public delegate void PhaseHanlder(System.Transactions.Transaction tran);
        /// <summary>
        /// 下一步事件
        /// </summary>
        public event PhaseHanlder NextEvent;
        /// <summary>
        /// 上一步事件
        /// </summary>
        public event PhaseHanlder PreviousEvent;
        /// <summary>
        /// 开始下一步操作
        /// </summary>
        /// <typeparam name="S">IEnlistmentNotification接口实现</typeparam>
        /// <param name="level">IsolationLevel事务的隔离级别(对全局事务处理设置)</param>
        /// <param name="source">下一步操作的自定义数据管理器</param>
        public void Next<S>(IsolationLevel level, S source)
            where S : class,IEnlistmentNotification, new()
        {
            Transaction tran = _tranStack.Peek();//获取事务栈的顶端事务
            if (tran == null)
                tran = Transaction.Current;//主事务
            DependentTransaction depentran = tran.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
            //将本次事务处理的资源管理器压入资源栈中
            depentran.EnlistVolatile(source, EnlistmentOptions.None);
            _tranStack.Push(depentran);
            _resourceStack.Push(source);
            //切换环境事务场景
            Transaction.Current = depentran;
            if (NextEvent != null)
                if (NextEvent.GetInvocationList().Length > 0)
                    NextEvent(Transaction.Current);
        }
        /// <summary>
        /// 返回上一步操作
        /// </summary>
        /// <typeparam name="T">需要接受的数据对象类型</typeparam>
        /// <param name="refadd">需要接受的数据对象引用</param>
        public void Previous<T>(out T refadd) where T : class,new()
        {
            Transaction tran = _tranStack.Pop();
            if (tran == null)//顶层事务
                Transaction.Current.Rollback();
            // tran.Rollback();//回滚本事务,将触发所有克隆事务的回滚。
            if (PreviousEvent != null)
                if (PreviousEvent.GetInvocationList().Length > 0)
                {
                    //设置上一步数据对象
                    refadd = (_resourceStack.Pop() as IReversibleGetResourceData<T>).GetPreviousData();
                    PreviousEvent(Transaction.Current);
                    return;
                }
            refadd = new T();//事务处理异常
        }
        /// <summary>
        /// 提交事物堆栈中的所有事物
        /// </summary>
        public void Commit()
        {
            if (Transaction.Current is DependentTransaction)
                (Transaction.Current as DependentTransaction).Complete();
            for (int i = 0; i < _tranStack.Count - 1; i++)
            {
                //依赖事务
                (_tranStack.Pop() as DependentTransaction).Complete();
            }
            //提交事务，主事务。必须进行克隆主体的提交才能完成所有阶段的操作。
            (_tranStack.Pop() as CommittableTransaction).Commit();
        }
        /// <summary>
        /// 回滚事物堆栈中的所有事物
        /// </summary>
        public void RollBack()
        {
            if (Transaction.Current is DependentTransaction)
                (Transaction.Current as DependentTransaction).Rollback();
            for (int i = 0; i < _tranStack.Count - 1; i++)
            {
                //依赖事务
                (_tranStack.Pop() as DependentTransaction).Rollback();
            }
            //提交事务，主事务。必须进行克隆主体的提交才能完成所有阶段的操作。
            (_tranStack.Pop() as CommittableTransaction).Rollback();
        }
    }
}
