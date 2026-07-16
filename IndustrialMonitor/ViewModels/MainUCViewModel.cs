using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels;

/// <summary>
/// 管理六个固定 Modbus TCP 从站的轮询和保持寄存器写入。
/// </summary>
public sealed class MainUCViewModel : BindableBase, IDialogAware
{
    // Modbus TCP 模拟器的网络地址。
    private const string SimulatorIp = "127.0.0.1";
    // Modbus TCP 服务端口，标准端口通常为 502。
    private const int SimulatorPort = 502;
    // 页面固定监控的机组数量。
    private const int DeviceCount = 6;
    // 实际通信起始地址 0，对应界面显示的 40001。
    private const ushort FirstRegisterAddress = 0;
    // 每台机组连续读取、写入的保持寄存器数量。
    private const ushort RegisterCount = 4;
    // 两次轮询之间的等待时间，单位：毫秒。
    private const int RefreshInterval = 500;

    // 6 台机组卡片使用的图片；数组下标与从站号减 1 对应。
    private static readonly string[] DeviceImages =
    [
        "/Resources/Images/Components/air_compressor.png",
        "/Resources/Images/Components/air_compressor.png",
        "/Resources/Images/Components/can.png",
        "/Resources/Images/Components/dryer.png",
        "/Resources/Images/Components/dryer1.png",
        "/Resources/Images/Components/filter.png"
    ];

    // 供所有机组共用的 Modbus TCP 通信服务。
    private readonly ModbusTcpService _modbusService =
        new(SimulatorIp, SimulatorPort, timeout: 2000);
    // 保存所有机组的轮询任务，关闭页面时需等待它们结束。
    private readonly List<Task> _monitorTasks = [];
    // 向全部轮询任务发送停止信号的取消令牌源。
    private CancellationTokenSource _monitorCancellation = new();
    // 当前标记为通信异常的机组数量。
    private int _abnormalDeviceCount;

    public string Title => "Modbus 设备监控";
    // Prism 对话框关闭请求入口。
    public DialogCloseListener RequestClose { get; } = new();
    // 绑定到监控界面的机组集合；变化时页面会自动刷新。
    public ObservableCollection<DeviceModel> DeviceList { get; } = [];

    // 绑定到页面顶部“通信异常”指标的数量。
    public int AbnormalDeviceCount
    {
        get => _abnormalDeviceCount;
        private set => SetProperty(ref _abnormalDeviceCount, value);
    }

    // 允许 Prism 关闭当前监控对话框。
    public bool CanCloseDialog() => true;

    /// <summary>
    /// 对话框打开时调用：创建机组数据，并开始异步轮询。
    /// </summary>
    public void OnDialogOpened(IDialogParameters parameters)
    {
        LoadFixedDevices();
        StartMonitoring();
    }

    /// <summary>
    /// 对话框关闭时调用：停止轮询并释放 TCP 连接。
    /// </summary>
    public async void OnDialogClosed()
    {
        await StopMonitoringAsync();
    }

    /// <summary>
    /// 创建 6 台固定模拟机组及其变量、手动写入控件。
    /// </summary>
    private void LoadFixedDevices()
    {
        // 避免重复打开监控页时保留上一次创建的机组。
        DeviceList.Clear();

        // 从站号 1～6 分别代表 6 台独立机组。
        for (byte slaveId = 1; slaveId <= DeviceCount; slaveId++)
        {
            string deviceNum = $"SIM-{slaveId:00}";
            string deviceName = $"模拟设备 {slaveId}";

            DeviceList.Add(new DeviceModel
            {
                SlaveId = slaveId,
                DeviceNum = deviceNum,
                DeviceName = deviceName,
                DeviceImage = DeviceImages[slaveId - 1],
                WriteAction = WriteRegisterAsync,
                IsWarning = true,
                WarningMsg = "等待首次 Modbus 响应",
                DeviceVarList = new ObservableCollection<DeviceVarModel>(
                    CreateVariables(deviceNum, deviceName)),
                ManualControlList = new ObservableCollection<ManualControlModel>(
                    CreateWriteControls())
            });
        }

        RefreshAbnormalDeviceCount();
    }

    /// <summary>
    /// 为一台机组创建 4 个显示实时值的保持寄存器变量。
    /// </summary>
    private static IEnumerable<DeviceVarModel> CreateVariables(
        string deviceNum,
        string deviceName) =>
        Enumerable.Range(0, RegisterCount).Select(index => new DeviceVarModel
        {
            DeviceNum = deviceNum,
            DeviceName = deviceName,
            VarNum = $"{deviceNum}-R{index + 1:00}",
            VarName = $"保持寄存器 {index}",
            VarAddress = $"4{index + 1:0000}",
            VarType = "UInt16"
        });

    /// <summary>
    /// 为一台机组创建 4 个保持寄存器的手动写入控件。
    /// </summary>
    private static IEnumerable<ManualControlModel> CreateWriteControls() =>
        Enumerable.Range(0, RegisterCount).Select(index => new ManualControlModel
        {
            ControlName = $"写入 4{index + 1:0000}",
            ControlAddress = $"4{index + 1:0000}",
            ControlValue = "0"
        });

    /// <summary>
    /// 为每台机组启动一个独立的异步轮询任务。
    /// </summary>
    private void StartMonitoring()
    {
        CancellationToken token = _monitorCancellation.Token;
        foreach (DeviceModel device in DeviceList)
        {
            // 当前机组开始执行“读数据 → 更新界面 → 等待 → 再读”的循环。
            _monitorTasks.Add(PollDeviceAsync(device, token));
        }
    }

    /// <summary>
    /// 持续读取一台机组的保持寄存器，并更新到监控界面。
    /// </summary>
    private async Task PollDeviceAsync(DeviceModel device, CancellationToken token)
    {
        try
        {
            // 除非页面关闭并取消 token，否则持续轮询。
            while (true)
            {
                try
                {
                    // 读取当前机组地址 0～3，即界面中的 40001～40004。
                    ushort[] values = await _modbusService.ReadHoldingRegistersAsync(
                        device.SlaveId,
                        FirstRegisterAddress,
                        RegisterCount,
                        token).ConfigureAwait(false);
                    // 切回 UI 线程，更新界面。
                    await RunOnUiAsync(
                        () => ApplyReadValues(device, values),
                        token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await RunOnUiAsync(
                        () => SetCommunicationError(device, exception.Message),
                        token).ConfigureAwait(false);
                }

                // 异步等待 500ms，不会阻塞界面线程。
                await Task.Delay(RefreshInterval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 关闭主界面时正常停止轮询。
        }
    }

    /// <summary>
    /// 将读取到的寄存器值写入绑定变量，并更新通信正常状态。
    /// </summary>
    private void ApplyReadValues(DeviceModel device, IReadOnlyList<ushort> values)
    {
        int valueCount = Math.Min(values.Count, device.DeviceVarList.Count);
        for (int index = 0; index < valueCount; index++)
        {
            device.DeviceVarList[index].ReadValue = values[index];
        }

        device.IsWarning = false;
        device.WarningMsg = string.Empty;
        RefreshAbnormalDeviceCount();
    }

    /// <summary>
    /// 将用户输入的值写入指定机组的一个保持寄存器。
    /// </summary>
    private async Task WriteRegisterAsync(
        DeviceModel device,
        string displayAddress,
        ushort value)
    {
        try
        {
            // 将显示地址（如 40001）转换为零基通信地址（如 0）。
            ushort registerAddress = ParseHoldingRegisterAddress(displayAddress);
            await _modbusService.WriteSingleRegisterAsync(
                device.SlaveId,
                registerAddress,
                value,
                _monitorCancellation.Token).ConfigureAwait(false);

            await RunOnUiAsync(() =>
            {
                device.IsWarning = false;
                device.WarningMsg = string.Empty;
                RefreshAbnormalDeviceCount();
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_monitorCancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RunOnUiAsync(
                () => SetCommunicationError(device, exception.Message),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 校验保持寄存器显示地址，并转换为 Modbus 实际通信地址。
    /// </summary>
    private static ushort ParseHoldingRegisterAddress(string address)
    {
        if (address.Length < 2
            || address[0] != '4'
            || !int.TryParse(address[1..], out int displayAddress)
            || displayAddress is < 1 or > 65536)
        {
            throw new InvalidOperationException($"保持寄存器地址无效：{address}");
        }

        return (ushort)(displayAddress - 1);
    }

    /// <summary>
    /// 标记机组通信异常，记录错误信息并重新统计异常数。
    /// </summary>
    private void SetCommunicationError(DeviceModel device, string message)
    {
        device.IsWarning = true;
        device.WarningMsg = message;
        RefreshAbnormalDeviceCount();
    }

    /// <summary>
    /// 统计全部处于通信异常状态的机组数量。
    /// </summary>
    private void RefreshAbnormalDeviceCount()
    {
        AbnormalDeviceCount = DeviceList.Count(device => device.IsWarning);
    }

    /// <summary>
    /// 确保操作在 WPF UI 线程执行，避免后台线程直接刷新界面。
    /// </summary>
    private static Task RunOnUiAsync(Action action, CancellationToken cancellationToken)
    {
        Dispatcher? dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(
            action,
            DispatcherPriority.DataBind,
            cancellationToken).Task;
    }

    /// <summary>
    /// 取消全部轮询，等待任务退出，再关闭共享 TCP 连接。
    /// </summary>
    private async Task StopMonitoringAsync()
    {
        // 通知正在读取或等待的全部轮询任务停止。
        _monitorCancellation.Cancel();

        try
        {
            // 等待所有机组轮询任务都真正结束。
            await Task.WhenAll(_monitorTasks);
        }
        catch (OperationCanceledException)
        {
            // 正常停止。
        }

        _monitorTasks.Clear();
        // 轮询结束后再断开共享连接。
        await _modbusService.DisconnectAsync();
        _monitorCancellation.Dispose();
        _monitorCancellation = new CancellationTokenSource();
    }
}
