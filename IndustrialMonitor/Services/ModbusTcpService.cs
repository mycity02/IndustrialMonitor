using NModbus;
using System.Net.Sockets;

namespace IndustrialMonitor.Services;

public sealed class ModbusTcpService
{
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly int _timeout;
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private TcpClient? _tcpClient;
    private IModbusMaster? _master;

    public ModbusTcpService(string ipAddress, int port, int timeout = 2000)
    {
        _ipAddress = ipAddress;
        _port = port;
        _timeout = timeout;
    }

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

    private async Task<T> ExecuteAsync<T>(
        Func<IModbusMaster, Task<T>> command,
        CancellationToken cancellationToken)
    {
        await _commandGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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

    private async Task<IModbusMaster> GetMasterAsync(CancellationToken cancellationToken)
    {
        if (_master != null)
        {
            return _master;
        }

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_ipAddress, _port, cancellationToken)
            .ConfigureAwait(false);

        _master = new ModbusFactory().CreateMaster(_tcpClient);
        _master.Transport.ReadTimeout = _timeout;
        _master.Transport.WriteTimeout = _timeout;
        _master.Transport.Retries = 0;
        return _master;
    }

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
