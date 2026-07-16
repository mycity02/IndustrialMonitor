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
    private const string SimulatorIp = "127.0.0.1";
    private const int SimulatorPort = 502;
    private const int DeviceCount = 6;
    private const ushort FirstRegisterAddress = 0;
    private const ushort RegisterCount = 4;
    private const int RefreshInterval = 500;

    private static readonly string[] DeviceImages =
    [
        "/Resources/Images/Components/air_compressor.png",
        "/Resources/Images/Components/air_compressor.png",
        "/Resources/Images/Components/can.png",
        "/Resources/Images/Components/dryer.png",
        "/Resources/Images/Components/dryer1.png",
        "/Resources/Images/Components/filter.png"
    ];

    private readonly ModbusTcpService _modbusService =
        new(SimulatorIp, SimulatorPort, timeout: 2000);
    private readonly List<Task> _monitorTasks = [];
    private CancellationTokenSource _monitorCancellation = new();
    private int _abnormalDeviceCount;

    public string Title => "Modbus 设备监控";
    public DialogCloseListener RequestClose { get; } = new();
    public ObservableCollection<DeviceModel> DeviceList { get; } = [];

    public int AbnormalDeviceCount
    {
        get => _abnormalDeviceCount;
        private set => SetProperty(ref _abnormalDeviceCount, value);
    }

    public bool CanCloseDialog() => true;

    public void OnDialogOpened(IDialogParameters parameters)
    {
        LoadFixedDevices();
        StartMonitoring();
    }

    public async void OnDialogClosed()
    {
        await StopMonitoringAsync();
    }

    private void LoadFixedDevices()
    {
        DeviceList.Clear();

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

    private static IEnumerable<ManualControlModel> CreateWriteControls() =>
        Enumerable.Range(0, RegisterCount).Select(index => new ManualControlModel
        {
            ControlName = $"写入 4{index + 1:0000}",
            ControlAddress = $"4{index + 1:0000}",
            ControlValue = "0"
        });

    private void StartMonitoring()
    {
        CancellationToken token = _monitorCancellation.Token;
        foreach (DeviceModel device in DeviceList)
        {
            _monitorTasks.Add(PollDeviceAsync(device, token));
        }
    }

    private async Task PollDeviceAsync(DeviceModel device, CancellationToken token)
    {
        try
        {
            while (true)
            {
                try
                {
                    ushort[] values = await _modbusService.ReadHoldingRegistersAsync(
                        device.SlaveId,
                        FirstRegisterAddress,
                        RegisterCount,
                        token).ConfigureAwait(false);

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

                await Task.Delay(RefreshInterval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 关闭主界面时正常停止轮询。
        }
    }

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

    private async Task WriteRegisterAsync(
        DeviceModel device,
        string displayAddress,
        ushort value)
    {
        try
        {
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

    private void SetCommunicationError(DeviceModel device, string message)
    {
        device.IsWarning = true;
        device.WarningMsg = message;
        RefreshAbnormalDeviceCount();
    }

    private void RefreshAbnormalDeviceCount()
    {
        AbnormalDeviceCount = DeviceList.Count(device => device.IsWarning);
    }

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

    private async Task StopMonitoringAsync()
    {
        _monitorCancellation.Cancel();

        try
        {
            await Task.WhenAll(_monitorTasks);
        }
        catch (OperationCanceledException)
        {
            // 正常停止。
        }

        _monitorTasks.Clear();
        await _modbusService.DisconnectAsync();
        _monitorCancellation.Dispose();
        _monitorCancellation = new CancellationTokenSource();
    }
}
