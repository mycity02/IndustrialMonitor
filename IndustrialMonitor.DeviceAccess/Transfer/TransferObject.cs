using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess.Base;
using NModbus;

namespace IndustrialMonitor.DeviceAccess.Transfer;

/// <summary>
/// 一条可复用的物理通信连接，例如一个串口或一个 TCP 连接。
/// </summary>
internal abstract class TransferObject
{
    private readonly SemaphoreSlim _commandGate = new(1, 1);
    private IModbusMaster? _master;

    /// <summary>
    /// 同一物理连接的唯一标识。多个从站可以共享同一个串口或 TCP 连接。
    /// </summary>
    internal abstract string ConnectionKey { get; }

    protected int Timeout { get; private set; } = 5000;
    protected int RetryCount { get; private set; } = 3;

    /// <summary>
    /// 保存所有传输方式都会用到的超时和重试配置。
    /// </summary>
    internal virtual Result Config(List<DevicePropEntity> properties)
    {
        Timeout = ReadPositiveInt(properties, "Timeout", 5000);
        RetryCount = ReadPositiveInt(properties, "TryCount", 3);
        return new Result { Status = true, Msg = "通信参数配置成功" };
    }

    /// <summary>
    /// 在当前连接上执行一条 NModbus 指令。
    /// </summary>
    /// <remarks>
    /// 监控层仍然可以同时运行多个设备轮询任务，但 Modbus RTU 和普通的
    /// Modbus TCP 主站都是“一问一答”的。多个任务共享同一连接时必须在这里
    /// 排队，否则一个任务可能读到另一个任务的响应。不同连接拥有不同的门锁，
    /// 所以不同串口或不同 TCP 端点仍然可以真正并行通信。
    /// </remarks>
    internal async Task<ResultData<T>> UseMasterAsync<T>(
        Func<IModbusMaster, Task<T>> command,
        CancellationToken cancellationToken)
    {
        await _commandGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _master ??= await CreateMasterAsync(cancellationToken).ConfigureAwait(false);
            ConfigureNModbus(_master);

            T data = await command(_master).ConfigureAwait(false);
            return new ResultData<T>
            {
                Status = true,
                Msg = "通信成功",
                Data = data
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 取消属于正常的停止流程，交给上层轮询任务结束，不转换成通信错误。
            throw;
        }
        catch (Exception exception)
        {
            // 出错后丢弃连接。下一次轮询会自动重新创建，达到断线重连的效果。
            CloseConnectionCore();
            return new ResultData<T>
            {
                Status = false,
                Msg = GetReadableMessage(exception)
            };
        }
        finally
        {
            _commandGate.Release();
        }
    }

    /// <summary>
    /// 关闭连接。调用前应先停止所有设备轮询任务。
    /// </summary>
    internal async Task DisconnectAsync()
    {
        await _commandGate.WaitAsync().ConfigureAwait(false);
        try
        {
            CloseConnectionCore();
        }
        finally
        {
            _commandGate.Release();
        }
    }

    protected abstract Task<IModbusMaster> CreateMasterAsync(
        CancellationToken cancellationToken);

    protected abstract void CloseConnection();

    protected static string? GetProperty(
        IEnumerable<DevicePropEntity> properties,
        params string[] names) =>
        properties.FirstOrDefault(property => names.Any(name =>
            property.PropName.Equals(name, StringComparison.OrdinalIgnoreCase)))?.PropValue?.Trim();

    private void ConfigureNModbus(IModbusMaster master)
    {
        // NModbus 负责协议重试、CRC、MBAP 报文头以及响应校验。
        master.Transport.ReadTimeout = Timeout;
        master.Transport.WriteTimeout = Timeout;
        // 界面中的 TryCount 表示总尝试次数；NModbus 的 Retries 表示首次失败后的追加次数。
        master.Transport.Retries = Math.Max(0, RetryCount - 1);
        master.Transport.WaitToRetryMilliseconds = 50;
    }

    private void CloseConnectionCore()
    {
        try
        {
            _master?.Dispose();
        }
        catch
        {
            // 关闭阶段不覆盖真正的通信异常。
        }
        finally
        {
            _master = null;
            CloseConnection();
        }
    }

    private static int ReadPositiveInt(
        IEnumerable<DevicePropEntity> properties,
        string name,
        int defaultValue)
    {
        string? value = GetProperty(properties, name);
        return int.TryParse(value, out int number) && number > 0
            ? number
            : defaultValue;
    }

    private static string GetReadableMessage(Exception exception) =>
        exception switch
        {
            TimeoutException => "Modbus 通信超时",
            SlaveException slaveException => $"Modbus 从站返回异常：{slaveException.Message}",
            _ => exception.Message
        };
}