using System.Collections.ObjectModel;

namespace IndustrialMonitor.Models;

/// <summary>
/// 一台模拟设备的从站地址、图片、连接状态、实时值和写入命令。
/// </summary>
public sealed class DeviceModel : BindableBase
{
    private bool _isWarning;
    private string _warningMsg = string.Empty;

    public DeviceModel()
    {
        ManualControlCommand = new DelegateCommand<ManualControlModel>(ExecuteManualControl);
    }

    public byte SlaveId { get; set; }
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceImage { get; set; } = string.Empty;
    public bool HasDeviceImage => !string.IsNullOrWhiteSpace(DeviceImage);
    public ObservableCollection<DeviceVarModel> DeviceVarList { get; set; } = [];
    public ObservableCollection<ManualControlModel> ManualControlList { get; set; } = [];

    public bool IsWarning
    {
        get => _isWarning;
        set => SetProperty(ref _isWarning, value);
    }

    public string WarningMsg
    {
        get => _warningMsg;
        set => SetProperty(ref _warningMsg, value);
    }

    public Func<DeviceModel, string, ushort, Task>? WriteAction { get; set; }
    public DelegateCommand<ManualControlModel> ManualControlCommand { get; }

    private async void ExecuteManualControl(ManualControlModel? control)
    {
        if (control == null || string.IsNullOrWhiteSpace(control.ControlAddress))
        {
            return;
        }

        if (!ushort.TryParse(control.ControlValue, out ushort value))
        {
            IsWarning = true;
            WarningMsg = "写入值必须是 0 到 65535 的整数";
            return;
        }

        if (WriteAction == null)
        {
            return;
        }

        try
        {
            await WriteAction(this, control.ControlAddress, value);
        }
        catch (OperationCanceledException)
        {
            // 关闭监控时取消写入属于正常流程。
        }
        catch (Exception exception)
        {
            IsWarning = true;
            WarningMsg = exception.Message;
        }
    }
}
