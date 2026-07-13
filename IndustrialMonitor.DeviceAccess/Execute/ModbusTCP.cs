using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IndustrialMonitor.DeviceAccess.Execute
{
    /// <summary>
    /// ModbusTCP 执行对象
    /// </summary>
    public class ModbusTCP : ExecuteObject
    {
        /// <summary>
        /// 错误数据字典
        /// </summary>
        private static Dictionary<int, string> Errors = new Dictionary<int, string>
        {
            { 0x01, "非法功能码"},
            { 0x02, "非法数据地址"},
            { 0x03, "非法数据值"},
            { 0x04, "从站设备故障"},
            { 0x05, "确认，从站需要一个耗时操作"},
            { 0x06, "从站忙"},
            { 0x08, "存储奇偶性差错"},
            { 0x0A, "不可用网关路径"},
            { 0x0B, "网关目标设备响应失败"},
        };

        /// <summary>
        /// 匹配
        /// </summary>
        /// <param name="devicePropList">通信配置属性</param>
        /// <param name="transferObjList">匹配结果。匹配到的传输对象集合</param>
        internal override Result Match(List<DevicePropEntity> devicePropList, List<TransferObject> transferObjList)
        {
            this.DevicePropList = devicePropList;//属性赋值
            Result result = new Result();
            //["127.0.0.1","502"]
            var ps = devicePropList.Where(p => p.PropName == "IP" || p.PropName == "Port").Select(p => p.PropValue).ToList();

            //如果结果里面有，
            //如果没有，1、创建一个传输对象 2、将匹配条件赋给传输对象 3、赋给当前执行对象 4、将传输对象add到匹配结果
            var transferObj = transferObjList.FirstOrDefault(to => to.GetType().Name == "SocketTcpUnit"
            &&
            ps.All(s => to.Condition.Any(c => c == s))
            );

            if (transferObj == null)//没有匹配到传输对象
            {
                this.TransferObject = new SocketTcpUnit();//1、创建一个传输对象   3、赋给当前执行对象
                this.TransferObject.Condition = ps;//2、将匹配条件赋给传输对象

                this.TransferObject.Config(devicePropList);//设置属性
                transferObjList.Add(this.TransferObject);//4、将传输对象add到匹配结果
            }
            else//没有匹配到传输对象
            {
                this.TransferObject = transferObj;
            }

            result.Status = true;
            result.Msg = "没有错误";

            return result;
        }

        #region 读
        /// <summary>
        /// 读
        /// </summary>
        /// <param name="groupAddrList">变量集合</param>
        /// <returns></returns>
        public override Result Read(List<GroupAddress> groupAddrList)
        {
            Result result = new Result { Status = true, Msg = "读数据成功" };

            try
            {
                //1、基本数据验证
                var propIP = this.DevicePropList.FirstOrDefault(p => p.PropName == "IP");
                if (propIP == null)
                {
                    result.Status = false;
                    result.Msg = "未配置IP";
                    return result;
                }

                var propPort = this.DevicePropList.FirstOrDefault(p => p.PropName == "Port");
                if (propPort == null)//以前这里写的变量为propIP，现已更正。
                {
                    result.Status = false;
                    result.Msg = "未配置端口";
                    return result;
                }

                var propSlaveId = this.DevicePropList.FirstOrDefault(p => p.PropName == "SlaveId");

                byte slaveId = 0x01;//从站地址
                if (propSlaveId != null && !byte.TryParse(propSlaveId.PropValue, out slaveId))
                {
                    result.Status = false;
                    result.Msg = "从站地址配置错误";
                    return result;
                }

                //2、把分组后的数据读出来

                foreach (ModbusGroupAddress groupAddr in groupAddrList)
                {
                    int readByteLen = groupAddr.Length * 2;//读取的数据字节数长度

                    if (groupAddr.FuncCode == 0x01 || groupAddr.FuncCode == 0x02)//如果读线圈
                    {
                        readByteLen = (int)Math.Ceiling(groupAddr.Length * 1.0 / 8);//Ceiling 向上取整
                    }

                    //该组的字节数据集合
                    var byteList = Read(slaveId, (byte)groupAddr.FuncCode, (ushort)groupAddr.StartAddress, (byte)groupAddr.Length, (ushort)readByteLen);

                    foreach (var varProp in groupAddr.VarPropList)
                    {
                        if (groupAddr.FuncCode == 3 || groupAddr.FuncCode == 4)//解析从寄存器读取的
                        {
                            int startIndex = (int.Parse(varProp.VarAddr.Substring(1)) - 1 - groupAddr.StartAddress) * 2;//在byteList取数据的起始索引
                                                                                                                        //varProp.ReadBytes = byteList.GetRange(startIndex, varProp.ReadByteCount).ToArray();

                            //modbus 默认是大端 bitconverter默认是小端，因此要翻转
                            var resultList = byteList.GetRange(startIndex, varProp.ReadByteCount);
                            resultList.Reverse();
                            varProp.ReadBytes = resultList.ToArray();

                        }
                        if (groupAddr.FuncCode == 1 || groupAddr.FuncCode == 2)//解析从线圈读取的
                        {
                            List<byte> resultState = new List<byte>();//将读取读取线圈的字节放到这里

                            byteList.ForEach(b =>
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    resultState.Add((byte)((b & (1 << i)) >> i));
                                }
                            });

                            int startIndex = int.Parse(varProp.VarAddr.Substring(1)) - 1 - groupAddr.StartAddress;//在byteList取数据的起始索引
                            #region MyRegion
                            //var propEndian = DevicePropList.FirstOrDefault(dp => dp.PropName == "Endian");
                            //if (propEndian != null && propEndian.PropValue == "Big")
                            //{
                            //    resultState.Reverse();//如果是大端
                            //}
                            #endregion

                            varProp.ReadBytes = resultState.GetRange(startIndex, 1).ToArray();//读线圈就是一个字节
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Msg = "服务器繁忙";// ex.Message;
            }

            //从第0个读到第2个 起始地址:0  3个寄存器   3*2个字节  

            return result;
        }

        /// <summary>
        /// 读
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="funcCode">功能码</param>
        /// <param name="startAddr">起始地址(相对地址)</param>
        /// <param name="count">读多少个</param>
        /// <param name="respLen">需要读数据的字节个数</param>
        /// <returns>字节数组</returns>
        private List<byte> Read(byte slaveId, byte funcCode, ushort startAddr, ushort count, ushort respLen)
        {
            // 一、组建请求报文
            List<byte> dataBytes = this.CreateReadPDU(slaveId, funcCode, startAddr, count);
            // 二、拼接TCP报文头
            dataBytes = this.JointHeader(dataBytes);

            // 三、打开/检查通信组件的状态
            var prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "TryCount");
            int tryCount = 30;
            if (prop != null)
            {
                int.TryParse(prop.PropValue, out tryCount);
            }
            Result connectState = this.TransferObject.Connect(tryCount);
            if (!connectState.Status)
            {
                throw new Exception(connectState.Msg);
            }

            // 四、准备请求
            prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "Timeout");
            int timeout = 5000;
            if (prop != null)
            {
                int.TryParse(prop.PropValue, out timeout);
            }

            // 获取数据
            ResultData<List<byte>> readResult = this.TransferObject.SendAndReceived(
                dataBytes,
                6,// 报文头长度
                  //0,//len2 不需要，随便传
                timeout: timeout,    // 超时时间 
                   calcLen: CalcDataLength);
            if (!readResult.Status || readResult.Data.Count == 0)
            {
                throw new Exception("响应报文接收异常！" + readResult.Msg);
            }

            List<byte> respBytes = readResult.Data;
            respBytes.RemoveRange(0, 6);//移除6个字节

            // 五、检查异常报文
            if (respBytes[1] > 0x80)
            {
                byte errorCode = respBytes[2];
                throw new Exception(Errors.ContainsKey(errorCode) ? Errors[errorCode] : "自定义异常");
            }
            // 六、截取数据字节
            List<byte> datas = respBytes.GetRange(3, respBytes.Count - 3);
            return datas;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public override void DisConnect()
        {
            TransferObject?.DisConnect();
        }

        /// <summary>
        /// 拼接报文头
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>

        private List<byte> JointHeader(List<byte> bytes)
        {
            // 添加MBAP
            List<byte> byteList = new List<byte>();
            byteList.Add(0x00);// TransactionID 
            byteList.Add(0x00);// TransactionID   
            byteList.Add(0x00);
            byteList.Add(0x00); // Modbus标记
            byteList.Add((byte)(bytes.Count / 256));
            byteList.Add((byte)(bytes.Count % 256)); // 后续字节数
            byteList.AddRange(bytes);

            return byteList;
        }

        /// <summary>
        /// 根据数组获取后面数据长度
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>后面数据的长度</returns>
        private int CalcDataLength(byte[] bytes)
        {
            // 1、知道哪部分字节表示长度
            // 2、知道协议的字节序
            int length = 0;
            if (bytes != null && bytes.Length == 6)
            {
                length = BitConverter.ToUInt16([bytes[5], bytes[4]]);
            }
            return length;
        }

        /// <summary>
        /// 组装读报文(不包含验证码)
        /// </summary>
        /// <param name="slaveId">从站</param>
        /// <param name="funcCode">功能码</param>
        /// <param name="startAddr">起始地址(相对地址)</param>
        /// <param name="count">读寄存器/线圈个数</param>
        /// <returns></returns>
        private List<byte> CreateReadPDU(byte slaveId, byte funcCode, ushort startAddr, ushort count)
        {
            List<byte> datas = new List<byte>();
            datas.Add(slaveId);
            datas.Add(funcCode);

            //   //起始地址
            //byte[] bytes=   BitConverter.GetBytes(startAddr);//小端

            //   datas.Add(bytes[1]);
            //   datas.Add(bytes[0]);

            datas.Add((byte)(startAddr / 256));
            datas.Add((byte)(startAddr % 256));

            datas.Add((byte)(count / 256));
            datas.Add((byte)(count % 256));

            return datas;
        }

        /// <summary>
        /// 地址分组
        /// </summary>
        /// <param name="variableProps">变量集合</param>
        /// <returns>分组地址数据集合</returns>
        public override ResultData<List<GroupAddress>> GroupAddress(List<VariableProp> varList)
        {
            ResultData<List<GroupAddress>> result = new ResultData<List<GroupAddress>>();
            result.Data = new List<GroupAddress>();
            try
            {
                // N个打包地址
                List<ModbusGroupAddress> address = new List<ModbusGroupAddress>();//分组结果
                // 针对Modbus地址   :string "40001" "40011"
                foreach (var varProp in varList)//变量
                {
                    // 将"40001"地址解析成   03  01  02
                    // 目的将所有同类型功能码的地址整合在一起，不考虑长度
                    var resultAddr = this.AnalysisAddress(varProp);
                    if (!resultAddr.Status)
                    {
                        throw new Exception(resultAddr.Msg);
                    }

                    var maCurrent = resultAddr.Data;//modbus地址
                    //maCurrent.VarNum = item.VarNum;

                    var addr = address.FirstOrDefault(a => a.FuncCode == maCurrent.FuncCode);
                    if (addr == null)
                    {
                        maCurrent.VarPropList.Add(varProp);//将变量添加到该组
                        address.Add(maCurrent);
                    }
                    else//如果有
                    {
                        addr.VarPropList.Add(varProp);//将变量添加到该组
                        //当前起始地址>已有的起始地址
                        if (maCurrent.StartAddress > addr.StartAddress)
                        {
                            //2 6              1 5
                            if (maCurrent.StartAddress + maCurrent.Length > addr.StartAddress + addr.Length)
                            {
                                addr.Length = maCurrent.StartAddress - addr.StartAddress + maCurrent.Length;
                            }
                        }
                        //3 1              5 1
                        else if (maCurrent.StartAddress < addr.StartAddress) //当前起始地址<已有的起始地址
                        {
                            addr.Length += addr.StartAddress - maCurrent.StartAddress;
                            addr.StartAddress = maCurrent.StartAddress;
                        }
                    }
                }

                address.ForEach(a => { result.Data.Add(a); });
                //result.Data = address;
                result.Status = true;
                result.Msg = "没有错误";
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Msg = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 根据变量返回功能码、寄存器/线圈个数、起始逻辑地址、变量编码
        /// </summary>
        /// <param name="variable">变量</param>
        /// <param name="isWrite">是否是写</param>
        /// <returns></returns>
        private ResultData<ModbusGroupAddress> AnalysisAddress(VariableProp variable, bool isWrite = false)
        {
            ResultData<ModbusGroupAddress> result = new ResultData<ModbusGroupAddress>();
            try
            {
                ModbusGroupAddress ma = new ModbusGroupAddress();
                // 根据数据类型计算所需的寄存器数量
                int typeLen = Marshal.SizeOf(variable.ValueType);//字节个数
                variable.ReadByteCount = typeLen;//读取字节个数
                ma.Length = typeLen / 2;    //寄存器个数

                if (variable.VarAddr.StartsWith("0"))
                {
                    ma.FuncCode = 01;
                    variable.ReadByteCount = 1;//每个线圈一个字节就可以了
                    ma.Length = 1;//一个线圈

                    ma.FuncCode = isWrite ? 15 : 01;
                }
                else if (variable.VarAddr.StartsWith("1"))//只读
                {
                    ma.FuncCode = 02;
                    variable.ReadByteCount = 1;//每个线圈一个字节就可以了
                    ma.Length = 1;
                }
                else if (variable.VarAddr.StartsWith("3"))//只读
                {
                    ma.FuncCode = 04;
                }
                else if (variable.VarAddr.StartsWith("4"))
                {
                    ma.FuncCode = isWrite ? 16 : 03;
                }

                // 起始地址
                ma.StartAddress = int.Parse(variable.VarAddr.Substring(1)) - 1;//相对地址需要-1

                result.Status = true;
                result.Msg = "没有错误";
                result.Data = ma;
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Msg = ex.Message;
            }

            return result;
        }
        #endregion
    }
}
