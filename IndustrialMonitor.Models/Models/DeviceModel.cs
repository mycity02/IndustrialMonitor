using System.Collections.ObjectModel;

namespace IndustrialMonitor.Models.Models;

/// <summary>
/// 一台设备在界面中的实时状态。
/// </summary>
public sealed class DeviceModel : BindableBase
{
    private string _deviceNum = string.Empty;
    private string _deviceName = string.Empty;
    private string _componentType = string.Empty;
    private bool _isWarning;
    private string _warningMsg = string.Empty;

    public DeviceModel()
    {
        ManualControlCommand = new DelegateCommand<ManualControlModel>(ExecuteManualControl);
    }

    public string DeviceNum
    {
        get => _deviceNum;
        set => SetProperty(ref _deviceNum, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set
        {
            if (SetProperty(ref _deviceName, value))
            {
                RaisePropertyChanged(nameof(DeviceImage));
                RaisePropertyChanged(nameof(HasDeviceImage));
            }
        }
    }

    public string ComponentType
    {
        get => _componentType;
        set
        {
            if (SetProperty(ref _componentType, value))
            {
                RaisePropertyChanged(nameof(DeviceImage));
                RaisePropertyChanged(nameof(HasDeviceImage));
            }
        }
    }

    public string? DeviceImage
    {
        get
        {
            string? fileName = ResolveImageFile();
            return fileName == null
                ? null
                : $"pack://application:,,,/IndustrialMonitor.CommonResource;component/Images/Components/{fileName}";
        }
    }

    public bool HasDeviceImage => DeviceImage != null;
    public ObservableCollection<DevicePropModel> DevicePropList { get; set; } = [];
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

    public Func<DeviceModel, string, byte[], Task>? WriteAction { get; set; }
    public DelegateCommand<ManualControlModel> ManualControlCommand { get; }

    private string? ResolveImageFile()
    {
        string key = $"{ComponentType} {DeviceName}".ToLowerInvariant();

        if (key.Contains("aircompressor") || key.Contains("空压")) return "air_compressor.png";
        if (key.Contains("gastank") || key.Contains("气罐") || key.Contains("储气")) return "can.png";
        if (key.Contains("freezedryer") || key.Contains("冷冻") || key.Contains("冷干")) return "dryer.png";
        if (key.Contains("adsorptiondryer") || key.Contains("吸附")) return "dryer1.png";
        if (key.Contains("filter") || key.Contains("过滤")) return "filter.png";
        if (key.Contains("horizontalpipeline") || key.Contains("水平管")) return "horizontalpipe.png";
        if (key.Contains("verticalpipeline") || key.Contains("垂直管")) return "verticalpipe.png";
        if (key.Contains("rajoints") || key.Contains("直角") || key.Contains("弯头")) return "rt_joints.png";
        if (key.Contains("teejoints") || key.Contains("三通")) return "tee_joints.png";

        return null;
    }

    private async void ExecuteManualControl(ManualControlModel? control)
    {
        if (control == null || string.IsNullOrWhiteSpace(control.ControlAddress))
        {
            return;
        }

        if (!short.TryParse(control.ControlValue, out short value))
        {
            IsWarning = true;
            WarningMsg = "控制值必须是 16 位整数";
            return;
        }

        byte[] writeBytes = BitConverter.GetBytes(value);
        Array.Reverse(writeBytes);
        if (WriteAction == null)
        {
            return;
        }

        try
        {
            // 命令方法是 UI 事件边界，因此这里可以等待后台 Modbus 写入，
            // 同时不会阻塞 WPF 界面。
            await WriteAction(this, control.ControlAddress, writeBytes);
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