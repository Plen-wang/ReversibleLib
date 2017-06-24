/***
 * author:plen_wang
 * time:2012-06-10
 * **/
using System;
using System.Collections.Generic;
using System.Text;

namespace ReversibleLib
{
    /// <summary>
    /// 事务性资源管理器中的资源拷贝，不同的资源类型存在多种拷贝形式。
    /// </summary>
    public interface IResourceCopy<T>
    {
        void Copy(T t1, T t2);
    }
}
