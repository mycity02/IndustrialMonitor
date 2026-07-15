using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Transfer;

namespace IndustrialMonitor.DeviceAccess.Execute;

/// <summary>
/// 设备协议执行对象。界面层只需要调用读、写和地址准备方法。
/// </summary>
public abstract class ExecuteObject
{
    private protected ExecuteObject(
        TransferObject transferObject,
        List<DevicePropEntity> deviceProperties)
    {
        TransferObject = transferObject;
        DeviceProperties = deviceProperties;
    }

    internal TransferObject TransferObject { get; }
    protected List<DevicePropEntity> DeviceProperties { get; }

    public abstract Task<Result> ReadAsync(
        List<GroupAddress> groups,
        CancellationToken cancellationToken);

    public abstract ResultData<List<GroupAddress>> GroupAddress(
        List<VariableProp> variables);

    public abstract Task<Result> WriteAsync(
        WriteDataInfo writeDataInfo,
        CancellationToken cancellationToken);
}