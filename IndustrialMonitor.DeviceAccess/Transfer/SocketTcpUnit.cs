using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace IndustrialMonitor.DeviceAccess.Transfer
{
    /// <summary>
    /// 网口(TCP)传输对象
    /// </summary>
    internal class SocketTcpUnit : TransferObject
    {
        private Socket socket;

        public SocketTcpUnit()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        string ip;
        int port = 0;//端口号
        ManualResetEvent TimeoutObject = new ManualResetEvent(false);

        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        internal override Result Config(List<DevicePropEntity> props)
        {
            Result result = new Result { Status = true, Msg = "配置成功" };
            var propIP = props.FirstOrDefault(dp => dp.PropName == "IP");
            if (propIP == null)
            {
                result.Status = false;
                result.Msg = "没有配置IP";

                return result;
            }
            ip = propIP.PropValue;

            var propPort = props.FirstOrDefault(dp => dp.PropName == "Port");
            if (propPort == null)
            {
                result.Status = false;
                result.Msg = "没有配置端口号";

                return result;
            }
            if (!int.TryParse(propPort.PropValue, out port))
            {
                result.Status = false;
                result.Msg = "端口号配置错误";

                return result;
            }

            return result;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="trycount"></param>
        /// <returns></returns>
        internal override Result Connect(int trycount = 30)
        {
            lock (socketLock)
            {
                Result result = new Result { Status = true, Msg = "连接成功" };

                try
                {
                    if (socket == null)
                    {
                        // ProtocolType 可支持配置
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    int count = 0;
                    bool connectState = false;
                    TimeoutObject.Reset();
                    while (count < trycount)
                    {
                        if (!(!socket.Connected || (socket.Poll(200, SelectMode.SelectRead) && (socket.Available == 0))))
                        {
                            return result;
                        }
                        try
                        {
                            socket?.Close();
                            socket.Dispose();
                            socket = null;

                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            socket.BeginConnect(ip, port, callback =>
                            {
                                connectState = false;
                                var cbSocket = callback.AsyncState as Socket;
                                if (cbSocket != null)
                                {
                                    connectState = cbSocket.Connected;

                                    if (cbSocket.Connected)
                                        cbSocket.EndConnect(callback);

                                }
                                TimeoutObject.Set();
                            }, socket);
                            TimeoutObject.WaitOne(2000, false);
                            if (!connectState) throw new Exception();
                            else break;
                        }
                        catch (SocketException ex)
                        {
                            if (ex.SocketErrorCode == SocketError.TimedOut)// 连接超时
                                throw new Exception(ex.Message);
                        }
                        catch (Exception) { }
                    }
                    if (socket == null || !socket.Connected || ((socket.Poll(200, SelectMode.SelectRead) && (socket.Available == 0))))
                    {
                        throw new Exception("网络连接失败");
                    }
                }
                catch (Exception ex)
                {
                    result = new Result { Status = false, Msg = ex.Message };
                }

                return result;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        internal override void DisConnect()
        {
            try
            {
                if (socket?.Connected ?? false)
                {
                    socket?.Shutdown(SocketShutdown.Both);//正常关闭连接
                }
            }
            catch { }

            try
            {
                socket?.Close();
            }
            catch (Exception ex)
            {
            }
        }

        readonly object socketLock = new object();

        /// <summary>
        /// 发送和接收数据
        /// </summary>
        /// <param name="req">发送报文</param>
        /// <param name="headerLen">头长度</param>
        /// <param name="timeout"></param>
        /// <param name="calcLen">委托 计算数据长度</param>
        /// <returns></returns>
        internal override ResultData<List<byte>> SendAndReceived(List<byte> req, int headerLen,int len2=0, int timeout=5000, Func<byte[], int> calcLen=null)
        {
            lock (socketLock)
            {
                ResultData<List<byte>> result = new ResultData<List<byte>> { Status = true, Msg = "成功" };
                try
                {
                    socket.SendTimeout = timeout;
                    socket.ReceiveTimeout = timeout;

                    if (req != null)
                    {
                        socket.Send(req.ToArray(), 0, req.Count, SocketFlags.None);
                    }

                    // 获取报文头字节
                    byte[] data = new byte[headerLen];
                    socket.Receive(data, 0, headerLen, SocketFlags.None);
                    result.Data = new List<byte>(data);

                    int dataLen = 0;//后面数据长度
                    if (calcLen != null)
                    {
                        dataLen = calcLen(data);
                    }
                    if (dataLen == 0)
                        throw new Exception("获取数据长度失败");

                    // 剩余的报文字节
                    data = new byte[dataLen];
                    socket.Receive(data, 0, dataLen, SocketFlags.None);
                    result.Data.AddRange(data);//add进去
                }
                catch (SocketException se)
                {
                    result.Status = false;
                    if (se.SocketErrorCode == SocketError.TimedOut)
                    {
                        result.Msg = "未获取到响应数据，接收超时";
                    }
                    else
                    {
                        result.Msg = se.Message;
                    }
                }
                catch (Exception ex)
                {
                    result.Status = false;
                    result.Msg = ex.Message;
                }
                return result;
            }
        }
    }
}
