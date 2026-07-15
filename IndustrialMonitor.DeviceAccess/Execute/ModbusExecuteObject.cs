using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Transfer;
using NModbus;

namespace IndustrialMonitor.DeviceAccess.Execute;

/// <summary>
/// RTU 和 TCP 共用的 Modbus 读写逻辑。
/// </summary>
/// <remarks>
/// 两种协议的功能码、寄存器地址和数据解析完全相同，区别只在物理连接。
/// 因此公共逻辑只保留一份，RTU/TCP 子类只用于说明使用了哪种连接。
/// </remarks>
public abstract class ModbusExecuteObject : ExecuteObject
{
    private protected ModbusExecuteObject(
        TransferObject transferObject,
        List<DevicePropEntity> deviceProperties)
        : base(transferObject, deviceProperties)
    {
    }

    public override async Task<Result> ReadAsync(
        List<GroupAddress> groups,
        CancellationToken cancellationToken)
    {
        ResultData<byte> slaveResult = GetSlaveId();
        if (!slaveResult.Status)
        {
            return Fail(slaveResult.Msg);
        }

        foreach (ModbusGroupAddress group in groups.Cast<ModbusGroupAddress>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            Result readResult = group.FuncCode switch
            {
                1 => await ReadBitsAsync(
                    group,
                    master => master.ReadCoilsAsync(
                        slaveResult.Data,
                        checked((ushort)group.StartAddress),
                        checked((ushort)group.Length)),
                    cancellationToken).ConfigureAwait(false),

                2 => await ReadBitsAsync(
                    group,
                    master => master.ReadInputsAsync(
                        slaveResult.Data,
                        checked((ushort)group.StartAddress),
                        checked((ushort)group.Length)),
                    cancellationToken).ConfigureAwait(false),

                3 => await ReadRegistersAsync(
                    group,
                    master => master.ReadHoldingRegistersAsync(
                        slaveResult.Data,
                        checked((ushort)group.StartAddress),
                        checked((ushort)group.Length)),
                    cancellationToken).ConfigureAwait(false),

                4 => await ReadRegistersAsync(
                    group,
                    master => master.ReadInputRegistersAsync(
                        slaveResult.Data,
                        checked((ushort)group.StartAddress),
                        checked((ushort)group.Length)),
                    cancellationToken).ConfigureAwait(false),

                _ => Fail($"不支持的 Modbus 功能码：{group.FuncCode}")
            };

            if (!readResult.Status)
            {
                return readResult;
            }
        }

        return Success("读取 Modbus 数据成功");
    }

    /// <summary>
    /// 把界面使用的 00001、10001、30001、40001 地址转换为 Modbus 相对地址。
    /// </summary>
    public override ResultData<List<GroupAddress>> GroupAddress(
        List<VariableProp> variables)
    {
        try
        {
            var groups = new List<GroupAddress>();

            // 本学习项目设备和变量较少，一项变量对应一次清晰的读取请求。
            // 这样比跨地址合并更容易理解，也不会因地址跨度过大超过协议限制。
            foreach (VariableProp variable in variables)
            {
                ModbusGroupAddress group = ParseAddress(variable);
                group.VarPropList.Add(variable);
                groups.Add(group);
            }

            return new ResultData<List<GroupAddress>>
            {
                Status = true,
                Msg = "Modbus 地址准备成功",
                Data = groups
            };
        }
        catch (Exception exception)
        {
            return new ResultData<List<GroupAddress>>
            {
                Status = false,
                Msg = exception.Message,
                Data = []
            };
        }
    }

    public override async Task<Result> WriteAsync(
        WriteDataInfo writeDataInfo,
        CancellationToken cancellationToken)
    {
        ResultData<byte> slaveResult = GetSlaveId();
        if (!slaveResult.Status)
        {
            return Fail(slaveResult.Msg);
        }

        try
        {
            ModbusGroupAddress address = ParseAddress(new VariableProp
            {
                VarAddr = writeDataInfo.StartAddr,
                ValueType = writeDataInfo.ValueType
            });

            if (writeDataInfo.StartAddr.StartsWith('0'))
            {
                bool value = writeDataInfo.WriteBytes.Any(item => item != 0);
                ResultData<bool> result = await TransferObject.UseMasterAsync(
                    async master =>
                    {
                        await master.WriteSingleCoilAsync(
                            slaveResult.Data,
                            checked((ushort)address.StartAddress),
                            value).ConfigureAwait(false);
                        return true;
                    },
                    cancellationToken).ConfigureAwait(false);

                return result.Status ? Success("线圈写入成功") : Fail(result.Msg);
            }

            if (!writeDataInfo.StartAddr.StartsWith('4'))
            {
                return Fail("10001 和 30001 区域是只读地址，不能写入");
            }

            ushort[] registers = ToRegisters(writeDataInfo.WriteBytes);
            ResultData<bool> registerResult = await TransferObject.UseMasterAsync(
                async master =>
                {
                    if (registers.Length == 1)
                    {
                        await master.WriteSingleRegisterAsync(
                            slaveResult.Data,
                            checked((ushort)address.StartAddress),
                            registers[0]).ConfigureAwait(false);
                    }
                    else
                    {
                        await master.WriteMultipleRegistersAsync(
                            slaveResult.Data,
                            checked((ushort)address.StartAddress),
                            registers).ConfigureAwait(false);
                    }

                    return true;
                },
                cancellationToken).ConfigureAwait(false);

            return registerResult.Status
                ? Success("保持寄存器写入成功")
                : Fail(registerResult.Msg);
        }
        catch (Exception exception)
        {
            return Fail(exception.Message);
        }
    }

    private async Task<Result> ReadBitsAsync(
        ModbusGroupAddress group,
        Func<IModbusMaster, Task<bool[]>> command,
        CancellationToken cancellationToken)
    {
        ResultData<bool[]> result = await TransferObject.UseMasterAsync(
            command,
            cancellationToken).ConfigureAwait(false);

        if (!result.Status)
        {
            return Fail(result.Msg);
        }

        foreach (VariableProp variable in group.VarPropList)
        {
            int relativeAddress = ParseRelativeAddress(variable.VarAddr);
            int index = relativeAddress - group.StartAddress;
            variable.ReadBytes = [result.Data[index] ? (byte)1 : (byte)0];
        }

        return Success("位数据读取成功");
    }

    private async Task<Result> ReadRegistersAsync(
        ModbusGroupAddress group,
        Func<IModbusMaster, Task<ushort[]>> command,
        CancellationToken cancellationToken)
    {
        ResultData<ushort[]> result = await TransferObject.UseMasterAsync(
            command,
            cancellationToken).ConfigureAwait(false);

        if (!result.Status)
        {
            return Fail(result.Msg);
        }

        byte[] networkBytes = RegistersToNetworkBytes(result.Data);
        foreach (VariableProp variable in group.VarPropList)
        {
            int relativeAddress = ParseRelativeAddress(variable.VarAddr);
            int startIndex = (relativeAddress - group.StartAddress) * 2;

            byte[] valueBytes = networkBytes
                .Skip(startIndex)
                .Take(variable.ReadByteCount)
                .ToArray();

            if (valueBytes.Length != variable.ReadByteCount)
            {
                return Fail($"变量 {variable.VarAddr} 返回的数据长度不足");
            }

            // Modbus 每个寄存器按大端传输，BitConverter 在 Windows 上按小端解析。
            // 翻转后，ConvertType 就能直接得到正确的数值。
            Array.Reverse(valueBytes);
            variable.ReadBytes = valueBytes;
        }

        return Success("寄存器读取成功");
    }

    private ResultData<byte> GetSlaveId()
    {
        string? value = DeviceProperties.FirstOrDefault(property =>
            property.PropName.Equals("SlaveId", StringComparison.OrdinalIgnoreCase))?.PropValue;

        return byte.TryParse(value, out byte slaveId) && slaveId is >= 1 and <= 247
            ? new ResultData<byte>
            {
                Status = true,
                Msg = "从站地址有效",
                Data = slaveId
            }
            : new ResultData<byte>
            {
                Status = false,
                Msg = "从站地址应为 1 到 247"
            };
    }

    private static ModbusGroupAddress ParseAddress(VariableProp variable)
    {
        if (string.IsNullOrWhiteSpace(variable.VarAddr) || variable.VarAddr.Length < 2)
        {
            throw new InvalidOperationException("Modbus 变量地址不能为空");
        }

        int relativeAddress = ParseRelativeAddress(variable.VarAddr);
        int byteCount = GetByteCount(variable.ValueType);
        char area = variable.VarAddr[0];

        if (area is '0' or '1')
        {
            variable.ReadByteCount = 1;
            return new ModbusGroupAddress
            {
                FuncCode = area == '0' ? 1 : 2,
                StartAddress = relativeAddress,
                Length = 1
            };
        }

        if (area is not ('3' or '4'))
        {
            throw new InvalidOperationException(
                $"地址 {variable.VarAddr} 必须以 0、1、3 或 4 开头");
        }

        if (byteCount % 2 != 0)
        {
            throw new InvalidOperationException(
                $"寄存器变量 {variable.VarAddr} 的类型 {variable.ValueType.Name} 不是偶数字节类型");
        }

        variable.ReadByteCount = byteCount;
        return new ModbusGroupAddress
        {
            FuncCode = area == '3' ? 4 : 3,
            StartAddress = relativeAddress,
            Length = byteCount / 2
        };
    }

    private static int ParseRelativeAddress(string address)
    {
        if (!int.TryParse(address[1..], out int displayAddress) || displayAddress <= 0)
        {
            throw new InvalidOperationException($"Modbus 地址 {address} 无效");
        }

        int relativeAddress = displayAddress - 1;
        if (relativeAddress > ushort.MaxValue)
        {
            throw new InvalidOperationException($"Modbus 地址 {address} 超出范围");
        }

        return relativeAddress;
    }

    private static int GetByteCount(Type type)
    {
        if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte)) return 1;
        if (type == typeof(short) || type == typeof(ushort) || type == typeof(char)) return 2;
        if (type == typeof(int) || type == typeof(uint) || type == typeof(float)) return 4;
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double)) return 8;

        throw new InvalidOperationException($"暂不支持 Modbus 数据类型：{type.Name}");
    }

    private static byte[] RegistersToNetworkBytes(IEnumerable<ushort> registers) =>
        registers.SelectMany(register => new[]
        {
            (byte)(register >> 8),
            (byte)(register & 0xFF)
        }).ToArray();

    private static ushort[] ToRegisters(byte[] networkBytes)
    {
        if (networkBytes.Length == 0 || networkBytes.Length % 2 != 0)
        {
            throw new InvalidOperationException("写入寄存器的数据必须是非空的偶数字节");
        }

        var registers = new ushort[networkBytes.Length / 2];
        for (int index = 0; index < registers.Length; index++)
        {
            registers[index] = (ushort)(
                (networkBytes[index * 2] << 8) |
                networkBytes[index * 2 + 1]);
        }

        return registers;
    }

    private static Result Success(string message) =>
        new() { Status = true, Msg = message };

    private static Result Fail(string message) =>
        new() { Status = false, Msg = message };
}