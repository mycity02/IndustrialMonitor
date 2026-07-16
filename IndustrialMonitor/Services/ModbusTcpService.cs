using NModbus;
using System.Net.Sockets;

namespace IndustrialMonitor.Services;

/// <summary>
/// 负责机组寄存器的 Modbus TCP 读写通信。
/// 所有机组轮询任务安全地共用一条 TCP 连接。
/// </summary>
public sealed class ModbusTcpService
{
    // 目标机组或本地 Modbus 模拟器的 IP 地址。
    private readonly string _ipAddress;
    // Modbus TCP 服务端口；标准端口通常为 502。
    private readonly int _port;
    // 单次读写操作的最大等待时间，单位：毫秒。
    private readonly int _timeout;
    // 同一条共享 TCP 连接每次只允许执行一条 Modbus 命令。
    // 防止多个机组轮询任务同时发送报文。
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    // 仅在首次通信需要连接时创建。
    private TcpClient? _tcpClient;
    // NModbus 主站对象，负责发送实际的 Modbus 请求。
    private IModbusMaster? _master;

    public ModbusTcpService(string ipAddress, int port, int timeout = 2000)
    {
        _ipAddress = ipAddress;
        _port = port;
        _timeout = timeout;
    }

    /// <summary>
    /// 异步读取一台机组的连续保持寄存器。
    /// 例如：从站号 1、起始地址 0、数量 4，对应界面显示的 40001-40004。
    /// </summary>
    /// <returns>读取到的寄存器值；第 0 项对应起始地址。</returns>
    public Task<ushort[]> ReadHoldingRegistersAsync(
        byte slaveId,
        ushort startAddress,
        ushort registerCount,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            master => master.ReadHoldingRegistersAsync(slaveId, startAddress, registerCount),
            cancellationToken);

    public async Task WriteSingleRegisterAsync(
        byte slaveId,
        ushort registerAddress,
        ushort value,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            async master =>
            {
                await master.WriteSingleRegisterAsync(slaveId, registerAddress, value)
                    .ConfigureAwait(false);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync()
    {
        await _commandGate.WaitAsync().ConfigureAwait(false);
        try
        {
            CloseConnection();
        }
        finally
        {
            _commandGate.Release();
        }
    }

    /// <summary>
    /// 所有读写操作的共同入口：将命令排队、获取连接，
    /// 并在通信失败后丢弃连接，使下一次请求能够重新连接。
    /// </summary>
    private async Task<T> ExecuteAsync<T>(
        Func<IModbusMaster, Task<T>> command,
        CancellationToken cancellationToken)
    {
        // 6 个轮询任务在这里排队，依次使用同一条连接。
        await _commandGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 已有主站则复用；首次访问时才建立连接。
            IModbusMaster master = await GetMasterAsync(cancellationToken).ConfigureAwait(false);
            return await command(master).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            CloseConnection();
            throw;
        }
        finally
        {
            _commandGate.Release();
        }
    }

    /// <summary>
    /// 获取可用的 NModbus 主站；首次调用时建立 TCP 连接。
    /// </summary>
    private async Task<IModbusMaster> GetMasterAsync(CancellationToken cancellationToken)
    {
        // 复用已有连接，避免每个轮询周期都重新连接。
        if (_master != null)
        {
            return _master;
        }

        // 创建 NModbus 主站之前，先打开 TCP 连接。
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_ipAddress, _port, cancellationToken)
            .ConfigureAwait(false);

        // 使用已连接的 TCP 客户端创建 NModbus 主站。
        _master = new ModbusFactory().CreateMaster(_tcpClient);
        // 应用读写超时；超时会视为通信失败。
        _master.Transport.ReadTimeout = _timeout;
        _master.Transport.WriteTimeout = _timeout;
        _master.Transport.Retries = 0;
        return _master;
    }

    /// <summary>
    /// 释放主站和 TCP 客户端；下一次命令会重新建立连接。
    /// </summary>
    private void CloseConnection()
    {
        try
        {
            _master?.Dispose();
        }
        catch
        {
        }
        finally
        {
            _master = null;
            _tcpClient?.Dispose();
            _tcpClient = null;
        }
    }
}
