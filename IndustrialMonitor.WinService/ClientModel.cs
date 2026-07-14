using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.WinService
{
    /// <summary>
    /// 客户端模型
    /// </summary>
    public class ClientModel
    {
        /// <summary>
        /// 客户端ID
        /// </summary>
        public ushort ClientId { get; set; }

        /// <summary>
        /// socket
        /// </summary>
        public Socket Client { get; set; }

        /// <summary>
        /// 通讯配置集合
        /// </summary>
        public List<string[]> PropList { get; set; } = new List<string[]>();

        /// <summary>
        /// 变量集合
        /// </summary>
        public List<string[]> VarList { get; set; } = new List<string[]>();

        /// <summary>
        /// 有效期
        /// </summary>
        public DateTime LifeTime { get; set; }// 服务接收客户

        /// <summary>
        /// 值字典 。比如，[变量编号1:数据1,变量编号2:数据2]
        /// </summary>
        public Dictionary<string, byte[]> Values { get; set; } = new Dictionary<string, byte[]>();
    }
}
