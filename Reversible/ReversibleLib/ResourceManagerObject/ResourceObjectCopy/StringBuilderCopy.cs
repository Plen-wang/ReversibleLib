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
    /// StringBuilder类型的数据Copy对象，需要实现IResourceCopy泛型接口。
    /// </summary>
    public class StringBuilderCopy : IResourceCopy<StringBuilder>
    {
        #region IResourceCopy<StringBuilder> 成员
        public void Copy(StringBuilder t1, StringBuilder t2)
        {
            t1.Remove(0, t1.Length);
            t1.Append(t2.ToString());
        }
        #endregion
    }
}
