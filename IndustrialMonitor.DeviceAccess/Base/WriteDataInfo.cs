using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess.Base
{
    /// <summary>
    /// 写数据信息
    /// </summary>
    public  class WriteDataInfo
    {
        /// <summary>
        /// 起始地址
        /// </summary>
        public string StartAddr { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// 写进去的字节数组
        /// </summary>
        public byte[] WriteBytes { get; set; }
    }
}
