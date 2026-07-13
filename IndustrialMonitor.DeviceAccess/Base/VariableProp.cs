using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess.Base
{
    /// <summary>
    /// 变量属性
    /// </summary>
    public class VariableProp
    {
        /// <summary>
        /// 变量属性唯一编码
        /// </summary>
        public string VarNum { get; set; }

        /// <summary>
        /// 起始地址(modbus是绝对地址，其他不一定)
        /// </summary>
        public string VarAddr { get; set; }

        /// <summary>
        /// 类型 (ushort、bool)
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// 读取的字节个数(解析数据的时候需要)
        /// </summary>
        public int ReadByteCount { get; set; }

        /// <summary>
        /// 读取的字节数
        /// </summary>
        public byte[] ReadBytes { get; set; }
    }
}
