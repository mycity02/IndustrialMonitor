using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Execute;
using IndustrialMonitor.DeviceAccess.Transfer;
using System.Text;

namespace IndustrialMonitor.DeviceAccess;

/// <summary>
/// 根据设备配置创建 Modbus 执行对象，并复用相同的物理连接。
/// </summary>
public sealed class Communication
{
    private static readonly Lazy<Communication> Instance = new(() => new Communication());
    private readonly Dictionary<string, TransferObject> _transfers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _transferLock = new();

    private Communication()
    {
    }

    public static Communication CreateInstance() => Instance.Value;

    /// <summary>
    /// 创建设备通信对象。协议选择在这里完成，不再让各个类相互“匹配”。
    /// </summary>
    public ResultData<ExecuteObject> GetExecuteObject(
        List<DevicePropEntity> deviceProperties)
    {
        string? protocol = GetProperty(deviceProperties, "Protocol");
        TransferObject candidate = protocol?.ToUpperInvariant() switch
        {
            "MODBUSRTU" => new SerialUnit(),
            "MODBUSTCP" => new SocketTcpUnit(),
            null or "" => null!,
            _ => null!
        };

        if (candidate == null)
        {
            return FailExecute(string.IsNullOrWhiteSpace(protocol)
                ? "没有配置通信协议"
                : $"不支持通信协议：{protocol}");
        }

        Result configResult = candidate.Config(deviceProperties);
        if (!configResult.Status)
        {
            return FailExecute(configResult.Msg);
        }

        TransferObject transfer;
        lock (_transferLock)
        {
            if (!_transfers.TryGetValue(candidate.ConnectionKey, out transfer!))
            {
                transfer = candidate;
                _transfers.Add(transfer.ConnectionKey, transfer);
            }
        }

        ExecuteObject executeObject = protocol!.Equals(
            "ModbusRTU",
            StringComparison.OrdinalIgnoreCase)
            ? new ModbusRTU(transfer, deviceProperties)
            : new ModbusTCP(transfer, deviceProperties);

        return new ResultData<ExecuteObject>
        {
            Status = true,
            Msg = "通信对象创建成功",
            Data = executeObject
        };
    }

    /// <summary>
    /// 关闭并清空全部共享连接。重新加载设备配置时会创建全新的连接。
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        TransferObject[] transfers;
        lock (_transferLock)
        {
            transfers = _transfers.Values.ToArray();
            _transfers.Clear();
        }

        foreach (TransferObject transfer in transfers)
        {
            await transfer.DisconnectAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 将通信层的小端字节转换成变量实际类型。
    /// </summary>
    public ResultData<object> ConvertType(byte[] valueBytes, Type type)
    {
        try
        {
            object value;
            if (type == typeof(bool)) value = valueBytes[0] != 0;
            else if (type == typeof(byte)) value = valueBytes[0];
            else if (type == typeof(sbyte)) value = unchecked((sbyte)valueBytes[0]);
            else if (type == typeof(short)) value = BitConverter.ToInt16(valueBytes);
            else if (type == typeof(ushort)) value = BitConverter.ToUInt16(valueBytes);
            else if (type == typeof(char)) value = BitConverter.ToChar(valueBytes);
            else if (type == typeof(int)) value = BitConverter.ToInt32(valueBytes);
            else if (type == typeof(uint)) value = BitConverter.ToUInt32(valueBytes);
            else if (type == typeof(float)) value = BitConverter.ToSingle(valueBytes);
            else if (type == typeof(long)) value = BitConverter.ToInt64(valueBytes);
            else if (type == typeof(ulong)) value = BitConverter.ToUInt64(valueBytes);
            else if (type == typeof(double)) value = BitConverter.ToDouble(valueBytes);
            else if (type == typeof(string)) value = Encoding.UTF8.GetString(valueBytes);
            else throw new InvalidOperationException($"不支持数据类型：{type.Name}");

            return new ResultData<object>
            {
                Status = true,
                Msg = "数据转换成功",
                Data = value
            };
        }
        catch (Exception exception)
        {
            return new ResultData<object>
            {
                Status = false,
                Msg = exception.Message
            };
        }
    }

    private static string? GetProperty(
        IEnumerable<DevicePropEntity> properties,
        string name) =>
        properties.FirstOrDefault(property =>
            property.PropName.Equals(name, StringComparison.OrdinalIgnoreCase))?.PropValue?.Trim();

    private static ResultData<ExecuteObject> FailExecute(string message) =>
        new()
        {
            Status = false,
            Msg = message
        };
}