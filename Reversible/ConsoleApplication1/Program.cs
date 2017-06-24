/***
 * author:plen_wang
 * **/

using System;
using System.Text;
using System.Transactions;
using ReversibleLib;
using ReversibleLib.ResourceManagerObject;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //构造数据
            StringBuilder strbuilder = new StringBuilder();
            strbuilder.Append("0");//初始数据为0

            //资源管理器
            ReResourceManager<StringBuilder, StringBuilderCopy> strResource =
                new ReResourceManager<StringBuilder, StringBuilderCopy>(strbuilder, new StringBuilderCopy());
            strResource.Name = "0资源管理器";

            //开始进入可逆框架处理环境
            using (ReversibleManagerScope reversible = new ReversibleManagerScope(strResource))
            {
                try
                {
                    ReversibleManager.Current.PreviousEvent += new ReversibleManager.PhaseHanlder(Current_PreviousEvent);
                    ReversibleManager.Current.NextEvent += new ReversibleManager.PhaseHanlder(Current_NextEvent);
                    strbuilder.Append("1");//首次修改数据为01

                    //获取下一步操作的数据
                    StringBuilder strbuilder2 = (strResource as IReversibleGetResourceData<StringBuilder>).GetNextData();
                    //构造下一步操作的自定义资源管理器
                    ReResourceManager<StringBuilder, StringBuilderCopy> strResource2 =
                        new ReResourceManager<StringBuilder, StringBuilderCopy>(strbuilder2, new StringBuilderCopy());
                    strResource2.Name = "2资源管理器";
                    ReversibleManager.Current.Next<ReResourceManager<StringBuilder, StringBuilderCopy>>(
                        IsolationLevel.Serializable, strResource2);
                    strbuilder2.Append("2");//第二步修改数据为012

                    //返回上一步,也就是回滚对数据进行“2”设置的前一个状态
                    StringBuilder strbuilder3;
                    ReversibleManager.Current.Previous<StringBuilder>(out strbuilder3);//获取上一步使用的数据，这里应该是01

                    reversible.Completed();//提交所有操作
                    Console.WriteLine(strbuilder3);
                }
                catch (Exception err)
                { Console.WriteLine(err.Message); ReversibleManager.Current.RollBack(); }
            }

            Console.ReadLine();
        }

        static void Current_NextEvent(Transaction tran)
        {
            Console.WriteLine("下一步：" + tran.TransactionInformation.LocalIdentifier);
            Console.WriteLine("下一步：" + tran.TransactionInformation.DistributedIdentifier);
        }
        static void Current_PreviousEvent(Transaction tran)
        {
            Console.WriteLine("上一步：" + tran.TransactionInformation.LocalIdentifier);
            Console.WriteLine("上一步：" + tran.TransactionInformation.DistributedIdentifier);
        }
    }
}
