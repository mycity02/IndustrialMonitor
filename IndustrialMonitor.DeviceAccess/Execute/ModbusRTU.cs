using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Transfer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess.Execute
{
    /// <summary>
    /// ModbusRTU执行对象
    /// </summary>
    public class ModbusRTU : ExecuteObject
    {
        //IModbusSerialMaster master;
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
            //master = ModbusSerialMaster.CreateRtu(this.TransferObject.Tunit as SerialPort);

            this.DevicePropList = devicePropList;//属性赋值
            Result result = new Result();
            //["COM1"]
            var ps = devicePropList.Where(p => p.PropName == "PortName").Select(p => p.PropValue).ToList();

            //如果结果里面有，
            //如果没有，1、创建一个传输对象 2、将匹配条件赋给传输对象 3、赋给当前执行对象 4、将传输对象add到匹配结果
            var transferObj = transferObjList.FirstOrDefault(to => to.GetType().Name == "SerialUnit"
            &&
            ps.All(s => to.Condition.Any(c => c == s))
            );

            if (transferObj == null)//没有匹配到传输对象
            {
                this.TransferObject = new SerialUnit();//1、创建一个传输对象   3、赋给当前执行对象
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
            var prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "SlaveId");
            if (prop == null)
            {
                result.Status = false;
                result.Msg = "未配置从站地址";
                return result;
            }

            byte slaveId = 0x01;//从站地址
            if (!byte.TryParse(prop.PropValue, out slaveId))
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

                        #region 大小端
                        //var propEndian = DevicePropList.FirstOrDefault(dp => dp.PropName == "Endian");
                        //if (propEndian != null && propEndian.PropValue == "Big")
                        //{
                        //    resultState.Reverse();//如果是大端
                        //}
                        #endregion

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
                     

                        varProp.ReadBytes = resultState.GetRange(startIndex, 1).ToArray();//读线圈就是一个字节
                    }
                }
            }

                //从第0个读到第2个 起始地址:0  3个寄存器   3*2个字节  
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Msg =  ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public override void DisConnect()
        {
            TransferObject?.DisConnect();
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
            #region 如果调用第三方
            //Result connectState = this.TransferObject.Connect(tryCount);
            //if (funcCode == 1)
            //{ 
            //    master.ReadCoils(slaveId, startAddr, count); 
            //}
            //if (funcCode == 2)
            //{
            //    master.ReadInputs(slaveId, startAddr, count);
            //}
            //if (funcCode == 3)
            //{
            //    master.ReadHoldingRegisters(slaveId, startAddr, count);
            //}
            //if (funcCode == 4)
            //{
            //    master.ReadInputRegisters(slaveId, startAddr, count);
            //}
            #endregion

            // 一、组建请求报文
            List<byte> dataBytes = this.CreateReadPDU(slaveId, funcCode, startAddr, count);
            // 二、计算关拼接CRC校验码
            CRC16(dataBytes);//dataBytes 加了两个字节字节校验

            // 三、打开/检查通信组件的状态
            var prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "TryCount");
            int tryCount = 30;
            if (prop != null)
            {
                int.TryParse(prop.PropValue, out tryCount);
            }

            Result connectState = this.TransferObject.Connect(tryCount);
            if (connectState.Status)
            {
                // 四、发送请求报文
                ResultData<List<byte>> resp = this.TransferObject.SendAndReceived(
                    dataBytes, // 发送的请求报文 
                    respLen + 5,//正常接收报文长度
                    5); // 异常响应报文长度
                if (!resp.Status)
                {
                    throw new Exception(resp.Msg);
                }

                // 五、校验检查
                List<byte> crcValidation = resp.Data.GetRange(0, resp.Data.Count - 2);//从响应的所有报文里取出不包括校验位(2字节)的字节
                this.CRC16(crcValidation);
                if (!crcValidation.SequenceEqual(resp.Data))
                {
                    throw new Exception("CRC校验检查不匹配");
                    // CRC 校验失败
                }

                // 六、检查异常报文
                if (resp.Data[1] > 0x80)
                {
                    byte errorCode = resp.Data[2];//错误编码  0x01
                    throw new Exception(Errors[errorCode]);
                }

                // 七、解析 数据字节
                List<byte> datas = resp.Data.GetRange(3, resp.Data.Count - 5);
                return datas;
            }
            else
            {
                throw new Exception(connectState.Msg);
            }

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
        /// 计算CRC16校验码，并将校验码的低位、高位字节追加到原字节列表末尾
        /// </summary>
        /// <param name="value">待计算的字节列表（会被修改，追加校验码）</param>
        /// <param name="poly">CRC16多项式，默认0xA001（常用的Modbus CRC16多项式）</param>
        /// <param name="crcInit">CRC初始值，默认0xFFFF（Modbus CRC16初始值）</param>
        /// <returns>计算出的CRC16校验码（16位无符号整数）</returns>
        /// <exception cref="ArgumentException">当输入字节列表为空或null时抛出</exception>
        private void CRC16(List<byte> value, ushort poly = 0xA001, ushort crcInit = 0xFFFF)
        {
            if (value == null || !value.Any())
                throw new ArgumentException("");

            //运算
            ushort crc = crcInit;
            for (int i = 0; i < value.Count; i++)
            {
                crc = (ushort)(crc ^ (value[i]));
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ poly) : (ushort)(crc >> 1);
                }
            }
            byte hi = (byte)((crc & 0xFF00) >> 8);  //高位置
            byte lo = (byte)(crc & 0x00FF);         //低位置

            value.Add(lo);
            value.Add(hi);
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

                address.ForEach(a=> result.Data.Add(a));
                //foreach (ModbusGroupAddress modbusGroupAddress in address)
                //{
                //    result.Data.Add(modbusGroupAddress);
                //}

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

        #region 写
        /// <summary>
        /// 写方法
        /// </summary>
        /// <param name="writeDataInfo">要写的数据</param>
        /// <returns>结果</returns>
        public override Result Write(WriteDataInfo writeDataInfo)
        {
            Result result = new Result { Status = true, Msg = "写成功" };
            try
            {
                var modAddressResult = AnalysisAddress(new VariableProp { VarAddr = writeDataInfo.StartAddr, ValueType = writeDataInfo.ValueType }, true);
                if (!modAddressResult.Status)
                {
                    throw new Exception("数据错误");
                }

                var prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "SlaveId");
                if (prop == null)
                {
                    throw new Exception("未配置从站地址");
                }

                byte slaveId = 0x01;
                if (!byte.TryParse(prop.PropValue, out slaveId))
                {
                    throw new Exception("从站地址配置错误");
                }

                this.Write(slaveId, (byte)modAddressResult.Data.FuncCode, (ushort)modAddressResult.Data.StartAddress, writeDataInfo.WriteBytes);

            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Msg = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 写
        /// </summary>
        /// <param name="slaveId">从站地址</param>
        /// <param name="funcCode">功能码</param>
        /// <param name="startAddr">起始地址(相对地址)</param>
        /// <param name="writeBytes">要写进去的字节数</param>
        private void Write(byte slaveId, byte funcCode, ushort startAddr, byte[] writeBytes)
        {
            // 一、组建请求报文
            List<byte> dataBytes = this.CreateWritePDU(slaveId, funcCode, startAddr, writeBytes);
            // 二、计算关拼接CRC校验码
            CRC16(dataBytes);
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

            // 四、发送请求报文
            prop = this.DevicePropList.FirstOrDefault(p => p.PropName == "Timeout");
            int timeout = 5000;
            if (prop != null)
            {
                int.TryParse(prop.PropValue, out timeout);
            }

            ResultData<List<byte>> resp = this.TransferObject.SendAndReceived(
                dataBytes, // 发送的请求报文 
                8,//正常返回的字节数
                5,// 异常响应报文长度
                timeout);
            if (!resp.Status)
            {
                throw new Exception(resp.Msg);
            }


            // 五、校验检查
            List<byte> crcValidation = resp.Data.GetRange(0, resp.Data.Count - 2);
            this.CRC16(crcValidation);
            if (!crcValidation.SequenceEqual(resp.Data))
            {
                // CRC 校验失败
                throw new Exception("CRC校验检查不匹配");
            }

            // 六、检查异常报文
            if (resp.Data[1] > 0x80)
            {
                byte errorCode = resp.Data[2];
                throw new Exception(Errors.ContainsKey(errorCode) ? Errors[errorCode] : "自定义异常");
            }
        }

        /// <summary>
        /// 组装报文(不含验证)
        /// </summary>
        /// <param name="slaveId"></param>
        /// <param name="funcCode"></param>
        /// <param name="startAddr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private List<byte> CreateWritePDU(byte slaveId, byte funcCode, ushort startAddr, byte[] data)
        {
            List<byte> command = new List<byte>();
            command.Add(slaveId);
            command.Add(funcCode);
            command.Add(BitConverter.GetBytes(startAddr)[1]);
            command.Add(BitConverter.GetBytes(startAddr)[0]);

            if (funcCode == 0x10)// 写多寄存器
            {
                // 写寄存器数量
                command.Add(BitConverter.GetBytes(data.Length / 2)[1]);
                command.Add(BitConverter.GetBytes(data.Length / 2)[0]);
                // 要写入寄存器的字节数
                command.Add((byte)data.Length);
            }
            command.AddRange(data);//添加写入字节数组

            return command;
        }
        #endregion
    }
}
