# memory-reversibleLib （内存可逆框架）

* 内存级别的transaction事务的必要性
* transaction scope 事务范围上下文模式
* ReResourceManager 事务资源管理器
* ReversibleManagerScope 事务范围
* ReversibleManager 事务管理器
* 参考文章

## 内存级别的transaction事务的必要性
内存操作有着天然的性能优势，有时候在内存中进行适当的事务性操作是需要的。比如进行一个业务逻辑计算，当计算到最后一步的时候发现数据并不能满足业务检查要求，此时就需要将数据回滚到最初的状态。

## transaction scope 事务范围上下文模式
事务的使用其实非常适合上下文模式，在一个很明确的地方开启一个transaction scope，可读性比较强。
```
//开始进入可逆框架处理环境
            using (ReversibleManagerScope reversible = new ReversibleManagerScope(strResource))
```
## ReResourceManager 事务资源管理器
在事务的架构规范中，是需要事务资源管理器来协调各方资源进行两阶段提交的。
```
/***
 * author:plen_wang
 * time:2012-06-10
 * **/
using System.Transactions;

namespace ReversibleLib
{
    /// <summary>
    /// 可逆范围内的资源管理器。
    /// 可以使用该类对易失性资源进行事务范围内的管理。在事务操作范围内进行可逆操作。
    /// </summary>
    /// <typeparam name="T">需要管理的资源类型</typeparam>
    /// <typeparam name="Xcopy">资源在使用、恢复过程中的数据复制对象。</typeparam>
    public class ReResourceManager<T, Xcopy> : IEnlistmentNotification, IReversibleGetResourceData<T>
        where T : class, new()
        where Xcopy : class
    {
        /// <summary>
        /// 私有字段。资源的持久引用。
        /// </summary>
        T _commitfrontvalue;
        /// <summary>
        /// 私有字段。事务性操作数据对象。
        /// </summary>
        T _rollbackfrontvalue = new T();
        /// <summary>
        /// 保存数据复制对象。
        /// </summary>
        Xcopy _copy;
        /// <summary>
        /// 泛型约束需要，内部使用。
        /// </summary>
        public ReResourceManager() { }
        /// <summary>
        /// 资源管理器内部名称。便于追踪
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 重载默认构造函数，使用资源类型和数据复制对象初始化资源管理器。
        /// </summary>
        public ReResourceManager(T t, Xcopy icopy)
        {
            (icopy as IResourceCopy<T>).Copy(_rollbackfrontvalue, t);
            _commitfrontvalue = t;
            _copy = icopy;
        }

        #region IEnlistmentNotification 成员
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }
        public void Commit(Enlistment enlistment)
        {
            enlistment.Done();
        }
        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }
        public void Rollback(Enlistment enlistment)
        {
            (_copy as IResourceCopy<T>).Copy(_commitfrontvalue, _rollbackfrontvalue);//回滚事务
            enlistment.Done();
        }
        #endregion

        #region IReversibleGetResourceData<T> 成员
        T IReversibleGetResourceData<T>.GetPreviousData()
        {
            T result = new T();
            (_copy as IResourceCopy<T>).Copy(result, _rollbackfrontvalue);
            return result;
        }
        T IReversibleGetResourceData<T>.GetNextData()
        {
            T result = new T();
            (_copy as IResourceCopy<T>).Copy(result, _commitfrontvalue);
            return result;
        }
        #endregion
    }
}
```
## ReversibleManagerScope 事务范围
定义事务范围，使用起来比较方便。关键是如何保持上下文状态一致性。
```
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
```
## ReversibleManager 事务管理器
事务管理器与事务资源管理器进行协调。
```
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
```

## 参考文章
http://www.cnblogs.com/wangiqngpei557/archive/2012/06/24/2560576.html