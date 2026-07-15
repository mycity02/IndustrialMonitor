using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Transfer;

namespace IndustrialMonitor.DeviceAccess.Execute;

/// <summary>
/// Modbus TCP 设备。读写实现位于 <see cref="ModbusExecuteObject"/>，
/// 本类只表明它使用网络传输。
/// </summary>
public sealed class ModbusTCP : ModbusExecuteObject
{
    internal ModbusTCP(
        TransferObject transferObject,
        List<DevicePropEntity> deviceProperties)
        : base(transferObject, deviceProperties)
    {
    }
}