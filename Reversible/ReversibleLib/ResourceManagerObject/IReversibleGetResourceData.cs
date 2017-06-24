/***
 * author:plen_wang
 * time:2012-06-10
 * **/

namespace ReversibleLib
{
    /// <summary>
    /// 在可逆框架中用来获取“上一步”、“下一步”的数据。
    /// </summary>
    /// <typeparam name="T">数据对象类型</typeparam>
    public interface IReversibleGetResourceData<T> where T : class
    {
        /// <summary>
        /// 获取放入时的数据。
        /// 对应的“上一步”操作的数据。
        /// </summary>
        /// <returns>泛型对象T</returns>
        T GetPreviousData();
        /// <summary>
        /// 获取经过事物处理后的数据。
        /// 对应的“下一步”操作的数据
        /// </summary>
        /// <returns>泛型对象T</returns>
        T GetNextData();
    }
}
