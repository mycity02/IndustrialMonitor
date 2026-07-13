using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess.Transfer
{
    /// <summary>
    /// 传输对象。和PLC发送/接收报文
    /// </summary>
    internal abstract class TransferObject
    {
        //匹配条件
        //["COM1"]
        //["127.0.0.1","502"]
        internal List<string> Condition = new List<string>();

        //public object Tunit { get; set; }//传输单元(第三方需要)  串口：serialport

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="trycount">重试次数</param>
        /// <returns></returns>
        internal virtual Result Connect(int trycount = 30)
        {
            return new Result();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        internal virtual void DisConnect() { }

        /// <summary>
        /// 发送和接收数据
        /// </summary>
        /// <param name="req">发送报文</param>
        /// <param name="len1">预留参数</param>
        /// <param name="len2">预留参数</param>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <param name="calcLen">预留委托参数</param>
        /// <returns>响应的整个报文</returns>
        internal virtual ResultData<List<byte>> SendAndReceived(List<byte> req, int len1=0, int len2=0, int timeout = 5000, Func<byte[], int> calcLen=null)
        {
            return new ResultData<List<byte>>();
        }

        /// <summary>
        /// 属性配置
        /// </summary>
        /// <param name="props">设备属性</param>
        /// <returns></returns>
        internal virtual Result Config(List<DevicePropEntity> props)
        {
            return new Result();
        }
    }
}
