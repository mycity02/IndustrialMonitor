using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using NModbus;
using System.Net.Sockets;

namespace IndustrialMonitor.DeviceAccess.Transfer;

/// <summary>
/// Modbus TCP 网络连接。MBAP 报文头和响应解析全部由 NModbus 完成。
/// </summary>
internal sealed class SocketTcpUnit : TransferObject
{
    private string _ipAddress = string.Empty;
    private int _port;
    private TcpClient? _tcpClient;

    internal override string ConnectionKey => $"TCP:{_ipAddress}:{_port}";

    internal override Result Config(List<DevicePropEntity> properties)
    {
        Result commonResult = base.Config(properties);
        if (!commonResult.Status)
        {
            return commonResult;
        }

        _ipAddress = GetProperty(properties, "IP") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_ipAddress))
        {
            return new Result { Status = false, Msg = "未配置设备 IP" };
        }

        string? portText = GetProperty(properties, "Port");
        if (!int.TryParse(portText, out _port) || _port is < 1 or > 65535)
        {
            return new Result { Status = false, Msg = "TCP 端口配置错误" };
        }

        return new Result { Status = true, Msg = "TCP 参数配置成功" };
    }

    protected override async Task<IModbusMaster> CreateMasterAsync(
        CancellationToken cancellationToken)
    {
        _tcpClient = new TcpClient
        {
            ReceiveTimeout = Timeout,
            SendTimeout = Timeout
        };

        await _tcpClient.ConnectAsync(_ipAddress, _port, cancellationToken)
            .ConfigureAwait(false);

        return new ModbusFactory().CreateMaster(_tcpClient);
    }

    protected override void CloseConnection()
    {
        try
        {
            _tcpClient?.Close();
        }
        catch
        {
            // 连接可能已经被远端关闭。
        }
        finally
        {
            _tcpClient?.Dispose();
            _tcpClient = null;
        }
    }
}