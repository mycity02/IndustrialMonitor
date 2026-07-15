using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Transfer;

namespace IndustrialMonitor.DeviceAccess.Execute;

/// <summary>
/// Modbus RTU 设备。读写实现位于 <see cref="ModbusExecuteObject"/>，
/// 本类只表明它使用串口传输。
/// </summary>
public sealed class ModbusRTU : ModbusExecuteObject
{
    internal ModbusRTU(
        TransferObject transferObject,
        List<DevicePropEntity> deviceProperties)
        : base(transferObject, deviceProperties)
    {
    }
}