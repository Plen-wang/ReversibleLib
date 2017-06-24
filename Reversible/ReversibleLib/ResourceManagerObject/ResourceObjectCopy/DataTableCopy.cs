/***
 * author:plen_wang
 * time:2012-06-10
 * **/
using System.Data;

namespace ReversibleLib
{
    /// <summary>
    /// DataTable类型的数据Copy对象，需要实现IResourceCopy泛型接口。
    /// </summary>
    public class DataTableCopy : IResourceCopy<DataTable>
    {
        #region  IResourceCopy<DataTable> 成员
        void IResourceCopy<DataTable>.Copy(DataTable t1, DataTable t2)
        {

        }
        #endregion
    }
}
