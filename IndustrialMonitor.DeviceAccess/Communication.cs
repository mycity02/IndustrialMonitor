using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Execute;
using IndustrialMonitor.DeviceAccess.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialMonitor.DeviceAccess
{
    /// <summary>
    /// 通信类。提供通信执行对象
    /// </summary>
    public class Communication
    {
        //单例
        #region 单例处理
        private static Communication _instance;
        private static object _lock = new object();

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <returns></returns>
        public static Communication CreateInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = new Communication();
                }
            }
            return _instance;
        }
        #endregion

        /// <summary>
        /// 匹配结果
        /// </summary>

        private List<TransferObject> TransferObjects = new List<TransferObject>();
        //监控 串口 Connect COM1
        //远程启动 串口 Connect COM1
        //监控 远程启动不是同一个通信(Communication)对象

        /// <summary>
        /// 根据设备属性获取通信执行对象
        /// </summary>
        /// <param name="devicePropList">设备属性</param>
        /// <returns>通信执行对象</returns>
        public ResultData<ExecuteObject> GetExecuteObject(List<DevicePropEntity> devicePropList)
        {
            ResultData<ExecuteObject> result = new ResultData<ExecuteObject>();//返回结果

            var protocol = devicePropList.FirstOrDefault(p => p.PropName == "Protocol");
            if (protocol == null)
            {
                result.Status = false;//有错
                result.Msg = "没有配置协议";

                return result;
            }

            Type type = Assembly.Load("IndustrialMonitor.DeviceAccess").GetType($"IndustrialMonitor.DeviceAccess.Execute.{protocol.PropValue}");
            if (type == null)
            {
                result.Status = false;//有错
                result.Msg = "没有找到执行对象";

                return result;
            }

            ExecuteObject eo = Activator.CreateInstance(type) as ExecuteObject;

            Result reaultMatch = eo.Match(devicePropList, TransferObjects);
            if (!reaultMatch.Status)
            {
                result.Status = false;
                result.Msg = reaultMatch.Msg;

                return result;
            }

            result.Status = true;
            result.Msg = "没有错误";
            result.Data = eo;

            return result;
        }

        /// <summary>
        /// 将字节数组转成Type
        /// </summary>
        /// <param name="valueBytes">字节数组</param>
        /// <param name="type">类型</param>
        /// <returns>结果</returns>
        public ResultData<object> ConvertType(byte[] valueBytes, Type type)
        {
            ResultData<object> result = new ResultData<object> { Status = true, Msg = "转换数据成功" };

            try
            {
                if (type == typeof(bool))
                {
                    result.Data = (valueBytes[0] == 0x01);
                }
                else if (type == typeof(string))
                {
                    result.Data = Encoding.UTF8.GetString(valueBytes);
                }
                else
                {
                    // 这里不需要字节调整  默认小端
                    Type tBitConverter = typeof(BitConverter);
                    MethodInfo method = tBitConverter.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(mi => mi.ReturnType == type && mi.GetParameters().Length == 2) as MethodInfo;
                    if (method == null)
                    {
                        throw new Exception("未找到匹配的数据类型转换方法");
                    }

                    result.Data = method?.Invoke(tBitConverter, new object[] { valueBytes.ToArray(), 0 });
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
