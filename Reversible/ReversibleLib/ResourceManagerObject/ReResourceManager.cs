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
