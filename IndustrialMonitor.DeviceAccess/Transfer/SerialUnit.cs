using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using NModbus;
using NModbus.Serial;
using System.IO.Ports;

namespace IndustrialMonitor.DeviceAccess.Transfer;

/// <summary>
/// Modbus RTU 串口连接。这里只配置和打开串口，协议报文交给 NModbus。
/// </summary>
internal sealed class SerialUnit : TransferObject
{
    private string _portName = string.Empty;
    private int _baudRate = 9600;
    private int _dataBits = 8;
    private Parity _parity = Parity.None;
    private StopBits _stopBits = StopBits.One;
    private SerialPort? _serialPort;

    internal override string ConnectionKey => $"RTU:{_portName.ToUpperInvariant()}";

    internal override Result Config(List<DevicePropEntity> properties)
    {
        Result commonResult = base.Config(properties);
        if (!commonResult.Status)
        {
            return commonResult;
        }

        try
        {
            _portName = GetProperty(properties, "PortName")
                ?? throw new InvalidOperationException("未配置串口名称");
            _baudRate = ParseInt(properties, "BaudRate", 9600);

            // 兼容旧数据库中的 DataBit/StopBit 和新界面中的 DataBits/StopBits。
            _dataBits = ParseInt(properties, ["DataBits", "DataBit"], 8);
            _parity = ParseEnum(properties, "Parity", Parity.None);
            _stopBits = ParseEnum(properties, ["StopBits", "StopBit"], StopBits.One);

            return new Result { Status = true, Msg = "串口参数配置成功" };
        }
        catch (Exception exception)
        {
            return new Result { Status = false, Msg = exception.Message };
        }
    }

    protected override Task<IModbusMaster> CreateMasterAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
        {
            ReadTimeout = Timeout,
            WriteTimeout = Timeout
        };
        _serialPort.Open();

        IModbusMaster master = new ModbusFactory().CreateRtuMaster(_serialPort);
        return Task.FromResult(master);
    }

    protected override void CloseConnection()
    {
        try
        {
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }
        }
        catch
        {
            // 串口可能已被 NModbus 释放，重复关闭是安全的。
        }
        finally
        {
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }

    private static int ParseInt(
        IEnumerable<DevicePropEntity> properties,
        string name,
        int defaultValue) => ParseInt(properties, [name], defaultValue);

    private static int ParseInt(
        IEnumerable<DevicePropEntity> properties,
        string[] names,
        int defaultValue)
    {
        string? value = GetProperty(properties, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return int.TryParse(value, out int number) && number > 0
            ? number
            : throw new InvalidOperationException($"串口参数 {names[0]} 无效");
    }

    private static TEnum ParseEnum<TEnum>(
        IEnumerable<DevicePropEntity> properties,
        string name,
        TEnum defaultValue) where TEnum : struct, Enum =>
        ParseEnum(properties, [name], defaultValue);

    private static TEnum ParseEnum<TEnum>(
        IEnumerable<DevicePropEntity> properties,
        string[] names,
        TEnum defaultValue) where TEnum : struct, Enum
    {
        string? value = GetProperty(properties, names);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return Enum.TryParse(value, true, out TEnum result)
            ? result
            : throw new InvalidOperationException($"串口参数 {names[0]} 无效");
    }
}