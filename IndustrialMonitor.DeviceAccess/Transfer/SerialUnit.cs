using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess.Transfer
{
    /// <summary>
    /// 串口传输对象
    /// </summary>
    internal class SerialUnit : TransferObject
    {
        private static readonly object transLock = new object();

        SerialPort serialPort;
        public SerialUnit()
        {
            serialPort = new SerialPort();
            //serialPort.PortName = "COM1";
            //serialPort.DataBits= 8;

            //this.Tunit = serialPort;
        }

        /// <summary>
        /// 是否连接。默认未连接
        /// </summary>
        internal bool ConnectState { get; set; } = false;

        /// <summary>
        /// 串口属性配置
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        internal override Result Config(List<DevicePropEntity> props)
        {
            // 端口名称、波特率、数据位、校验位、停止位
            Result result = new Result();
            try
            {
                foreach (var item in props)
                {
                    object v = null;
                    PropertyInfo pi = serialPort.GetType().GetProperty(item.PropName.Trim(), BindingFlags.Public | BindingFlags.Instance);
                    if (pi == null)
                    {
                        continue;
                    }

                    Type propType = pi.PropertyType;//类型
                    if (propType.IsEnum)
                    {
                        v = Enum.Parse(propType, item.PropValue.Trim() as string);
                    }
                    else
                    {
                        v = Convert.ChangeType(item.PropValue.Trim(), propType);//转为指定类型
                    }

                    pi.SetValue(serialPort, v);//将值赋给属性
                }

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
        /// 连接
        /// </summary>
        /// <param name="trycount">重试次数</param>
        /// <returns></returns>
        internal override Result Connect(int trycount = 30)
        {
            lock (transLock)
            {
                Result result = new Result { Status = true, Msg = "打开成功" };
                try
                {
                    int count = 0;//连接次数
                    while (count < trycount)//如果连接次数<重试次数
                    {
                        if (serialPort.IsOpen)
                        {
                            break;
                        }

                        try
                        {
                            serialPort.Open();
                            break;
                        }
                        catch (System.IO.IOException)
                        {
                            Task.Delay(1).GetAwaiter().GetResult();
                            count++;
                        }
                    }
                    if (!serialPort.IsOpen)
                    {
                        throw new Exception("串口打开失败");
                    }

                    ConnectState = true;
                }
                catch (Exception e)
                {
                    result.Status = false;
                    result.Msg = e.Message;
                }

                return result;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        internal override void DisConnect()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }

            ConnectState = false;
        }

        /// <summary>
        /// 发送和接收数据
        /// </summary>
        /// <param name="req">发送报文</param>
        /// <param name="receiveLen">正常接收长度</param>
        /// <param name="errorLen">错误长度的字节个数</param>
        /// <param name="timeout">超时时间(毫秒)</param>
        /// <returns>响应的整个报文</returns>
        internal override ResultData<List<byte>> SendAndReceived(List<byte> req, int receiveLen, int errorLen, int timeout = 5000, Func<byte[], int> calcLen = null)
        {
            lock (transLock)
            {
                ResultData<List<byte>> result = new ResultData<List<byte>>();
                // 发送
                serialPort.Write(req.ToArray(), 0, req.Count);

                List<byte> respBytes = new List<byte>();//返回的所有字节数组
                try
                {
                    serialPort.ReadTimeout = timeout;
                    while (respBytes.Count < Math.Max(receiveLen, errorLen))
                    {
                        byte data = (byte)serialPort.ReadByte();//粘性
                        respBytes.Add(data);
                    }

                    // 异常：一定时间内没有拿到字节数据
                    result.Status = true;
                    result.Msg = "读取成功";
                }
                catch (TimeoutException)
                {
                    result.Status = false;
                    result.Msg = "接收报文超时";
                }
                catch (Exception e)
                {
                    // 异常：一定时间内没有拿到字节数据
                    result.Status = false;
                    result.Msg = e.Message;
                }
                finally
                {
                    result.Data = respBytes;
                }
                return result;
            }
        }
    }
}
