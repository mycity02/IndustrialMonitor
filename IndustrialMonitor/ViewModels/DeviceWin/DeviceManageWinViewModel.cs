using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Logger;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace IndustrialMonitor.ViewModels.DeviceWin;

/// <summary>
/// 设备配置页。数据库仍使用属性列表，界面只暴露 RTU/TCP 的常用参数。
/// </summary>
public sealed class DeviceManageWinViewModel : BindableBase
{
    private static readonly HashSet<string> KnownComponentTypes =
    [
        "AirCompressorUC", "GasTankUC", "FreezeDryerUC", "AdsorptionDryerUC",
        "FilterUC", "HorizontalPipelineUC", "VerticalPipelineUC", "RAJointsUC", "TeeJointsUC"
    ];

    private readonly IDataAccess _dataAccess;
    private readonly ILoggerService<DeviceManageWinViewModel> _logger;
    private readonly IWindowService _windowService;
    private DeviceModel? _selectedDevice;
    private DeviceVarModel? _selectedVariable;
    private ManualControlModel? _selectedManualControl;
    private string _statusMessage = string.Empty;

    public DeviceManageWinViewModel(
        IDataAccess dataAccess,
        ILoggerService<DeviceManageWinViewModel> logger,
        IWindowService windowService)
    {
        _dataAccess = dataAccess;
        _logger = logger;
        _windowService = windowService;

        AddVariableCommand = new DelegateCommand(AddVariable);
        DeleteVariableCommand = new DelegateCommand(DeleteVariable);
        AddManualControlCommand = new DelegateCommand(AddManualControl);
        DeleteManualControlCommand = new DelegateCommand(DeleteManualControl);
        AlarmConfCommand = new DelegateCommand(OpenThresholdConfiguration);
        SaveDevicesCommand = new DelegateCommand(SaveDevices);
        CloseCommand = new DelegateCommand(() => _windowService.CloseActiveDialog(false));

        LoadDevices();
    }

    public IReadOnlyList<KeyValuePair<string, string>> ProtocolOptions { get; } =
    [
        new("ModbusRTU", "Modbus RTU（串口）"),
        new("ModbusTCP", "Modbus TCP（网络）")
    ];

    public IReadOnlyList<string> SerialPortOptions { get; } = GetSerialPortOptions();
    public IReadOnlyList<string> BaudRateOptions { get; } =
        ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200"];
    public IReadOnlyList<string> DataBitsOptions { get; } = ["7", "8"];
    public IReadOnlyList<string> ParityOptions { get; } = ["None", "Even", "Odd"];
    public IReadOnlyList<string> StopBitsOptions { get; } = ["One", "OnePointFive", "Two"];
    public ObservableCollection<DeviceModel> DeviceList { get; } = [];

    public DeviceModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetProperty(ref _selectedDevice, value))
            {
                return;
            }

            SelectedVariable = null;
            SelectedManualControl = null;
            if (value != null)
            {
                EnsureCommunicationDefaults(value);
            }
            RaiseCommunicationPropertiesChanged();
        }
    }

    public DeviceVarModel? SelectedVariable
    {
        get => _selectedVariable;
        set => SetProperty(ref _selectedVariable, value);
    }

    public ManualControlModel? SelectedManualControl
    {
        get => _selectedManualControl;
        set => SetProperty(ref _selectedManualControl, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CommunicationProtocol
    {
        get => GetSelectedProperty("Protocol", "ModbusRTU");
        set
        {
            if (SelectedDevice == null || CommunicationProtocol == value)
            {
                return;
            }

            if (value == "ModbusTCP")
            {
                ApplyTcpDefaults(SelectedDevice);
            }
            else
            {
                ApplyRtuDefaults(SelectedDevice);
            }
            RaiseCommunicationPropertiesChanged();
        }
    }

    public string PortName
    {
        get => GetSelectedProperty("PortName", SerialPortOptions[0]);
        set => SetSelectedProperty("PortName", value);
    }

    public string BaudRate
    {
        get => GetSelectedProperty("BaudRate", "9600");
        set => SetSelectedProperty("BaudRate", value);
    }

    public string DataBits
    {
        get => GetSelectedProperty("DataBits", "8");
        set => SetSelectedProperty("DataBits", value);
    }

    public string Parity
    {
        get => GetSelectedProperty("Parity", "None");
        set => SetSelectedProperty("Parity", value);
    }

    public string StopBits
    {
        get => GetSelectedProperty("StopBits", "One");
        set => SetSelectedProperty("StopBits", value);
    }

    public string IpAddress
    {
        get => GetSelectedProperty("IP", "127.0.0.1");
        set => SetSelectedProperty("IP", value);
    }

    public string TcpPort
    {
        get => GetSelectedProperty("Port", "502");
        set => SetSelectedProperty("Port", value);
    }

    public string SlaveId
    {
        get => GetSelectedProperty("SlaveId", "1");
        set => SetSelectedProperty("SlaveId", value);
    }

    public string Timeout
    {
        get => GetSelectedProperty("Timeout", "5000");
        set => SetSelectedProperty("Timeout", value);
    }

    public string TryCount
    {
        get => GetSelectedProperty("TryCount", "3");
        set => SetSelectedProperty("TryCount", value);
    }

    public string RefreshRate
    {
        get => GetSelectedProperty("RefreshRate", "500");
        set => SetSelectedProperty("RefreshRate", value);
    }

    public DelegateCommand AddVariableCommand { get; }
    public DelegateCommand DeleteVariableCommand { get; }
    public DelegateCommand AddManualControlCommand { get; }
    public DelegateCommand DeleteManualControlCommand { get; }
    public DelegateCommand AlarmConfCommand { get; }
    public DelegateCommand SaveDevicesCommand { get; }
    public DelegateCommand CloseCommand { get; }

    private void LoadDevices()
    {
        List<DeviceEntity> devices = _dataAccess.GetDevices();
        List<DevicePropEntity> properties = _dataAccess.GetDeviceProps();
        List<DeviceVarEntity> variables = _dataAccess.GetDeviceVarList();
        List<ManualControlEntity> controls = _dataAccess.GetManualControlList();
        List<VarAlarmConfEntity> alarms = _dataAccess.GetVarAlarmConfList();

        foreach (DeviceEntity device in devices)
        {
            DeviceList.Add(new DeviceModel
            {
                DeviceNum = device.DeviceNum,
                DeviceName = device.DeviceName,
                ComponentType = ResolveComponentType(device.ComponentType, device.DeviceName),
                DevicePropList = new ObservableCollection<DevicePropModel>(
                    properties.Where(property => property.DeviceNum == device.DeviceNum)
                        .Select(property => new DevicePropModel
                        {
                            PropName = property.PropName,
                            PropValue = property.PropValue
                        })),
                DeviceVarList = new ObservableCollection<DeviceVarModel>(
                    variables.Where(variable => variable.DeviceNum == device.DeviceNum)
                        .Select(variable => new DeviceVarModel
                        {
                            DeviceNum = variable.DeviceNum,
                            DeviceName = device.DeviceName,
                            VarNum = variable.VarNum,
                            VarName = variable.VarName,
                            VarAddress = variable.VarAddress,
                            VarType = variable.VarType,
                            Modulus = variable.Modulus,
                            Offset = variable.Offset,
                            VarAlarmConfList = new ObservableCollection<VarAlarmConfModel>(
                                alarms.Where(alarm => alarm.VarNum == variable.VarNum)
                                    .Select(alarm => new VarAlarmConfModel
                                    {
                                        ConfNum = alarm.ConfNum,
                                        Operator = alarm.Operator,
                                        CompareValue = alarm.CompareValue,
                                        AlarmContent = alarm.AlarmContent
                                    }))
                        })),
                ManualControlList = new ObservableCollection<ManualControlModel>(
                    controls.Where(control => control.DeviceNum == device.DeviceNum)
                        .Select(control => new ManualControlModel
                        {
                            ControlName = control.ControlName,
                            ControlAddress = control.ControlAddress,
                            ControlValue = control.ControlValue
                        }))
            });
        }

        foreach (DeviceModel device in DeviceList)
        {
            EnsureCommunicationDefaults(device);
        }
        SelectedDevice = DeviceList.FirstOrDefault();
    }

    private static string ResolveComponentType(string componentType, string deviceName)
    {
        if (KnownComponentTypes.Contains(componentType)) return componentType;
        if (deviceName.Contains("空压")) return "AirCompressorUC";
        if (deviceName.Contains("气罐") || deviceName.Contains("储气")) return "GasTankUC";
        if (deviceName.Contains("冷冻") || deviceName.Contains("冷干")) return "FreezeDryerUC";
        if (deviceName.Contains("吸附")) return "AdsorptionDryerUC";
        if (deviceName.Contains("过滤")) return "FilterUC";
        if (deviceName.Contains("水平管")) return "HorizontalPipelineUC";
        if (deviceName.Contains("垂直管")) return "VerticalPipelineUC";
        if (deviceName.Contains("直角") || deviceName.Contains("弯头")) return "RAJointsUC";
        if (deviceName.Contains("三通")) return "TeeJointsUC";
        return "Device";
    }

    private static IReadOnlyList<string> GetSerialPortOptions()
    {
        string[] ports = SerialPort.GetPortNames().OrderBy(port => port).ToArray();
        return ports.Length == 0 ? ["COM1"] : ports;
    }

    private string GetSelectedProperty(string name, string defaultValue) =>
        SelectedDevice == null ? defaultValue : GetDeviceProperty(SelectedDevice, name, defaultValue);

    private void SetSelectedProperty(
        string name,
        string value,
        [CallerMemberName] string? propertyName = null)
    {
        if (SelectedDevice == null)
        {
            return;
        }

        SetDeviceProperty(SelectedDevice, name, value);
        RaisePropertyChanged(propertyName);
    }

    private static string GetDeviceProperty(
        DeviceModel device,
        string name,
        string defaultValue = "") =>
        device.DevicePropList.FirstOrDefault(property =>
            property.PropName.Equals(name, StringComparison.OrdinalIgnoreCase))?.PropValue ?? defaultValue;

    private static void SetDeviceProperty(DeviceModel device, string name, string value)
    {
        List<DevicePropModel> matches = device.DevicePropList.Where(property =>
            property.PropName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (matches.Count == 0)
        {
            device.DevicePropList.Add(new DevicePropModel { PropName = name, PropValue = value });
            return;
        }

        matches[0].PropName = name;
        matches[0].PropValue = value;
        foreach (DevicePropModel duplicate in matches.Skip(1))
        {
            device.DevicePropList.Remove(duplicate);
        }
    }

    private static void SetDefaultProperty(DeviceModel device, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(GetDeviceProperty(device, name)))
        {
            SetDeviceProperty(device, name, value);
        }
    }

    private static void RemoveDeviceProperties(DeviceModel device, params string[] names)
    {
        List<DevicePropModel> obsoleteProperties = device.DevicePropList.Where(property =>
            names.Any(name => property.PropName.Equals(name, StringComparison.OrdinalIgnoreCase))).ToList();
        foreach (DevicePropModel property in obsoleteProperties)
        {
            device.DevicePropList.Remove(property);
        }
    }

    private static void EnsureCommunicationDefaults(DeviceModel device)
    {
        string protocol = GetDeviceProperty(device, "Protocol") == "ModbusTCP"
            ? "ModbusTCP"
            : "ModbusRTU";
        if (protocol == "ModbusTCP")
        {
            ApplyTcpDefaults(device, removeRtuProperties: false);
        }
        else
        {
            ApplyRtuDefaults(device, removeTcpProperties: false);
        }
    }

    private static void ApplyRtuDefaults(DeviceModel device, bool removeTcpProperties = true)
    {
        if (removeTcpProperties)
        {
            RemoveDeviceProperties(device, "IP", "Port");
        }
        SetDeviceProperty(device, "Protocol", "ModbusRTU");
        SetDefaultProperty(device, "PortName", SerialPort.GetPortNames().FirstOrDefault() ?? "COM1");
        SetDefaultProperty(device, "BaudRate", "9600");
        SetDefaultProperty(device, "DataBits", "8");
        SetDefaultProperty(device, "Parity", "None");
        SetDefaultProperty(device, "StopBits", "One");
        SetCommonDefaults(device);
    }

    private static void ApplyTcpDefaults(DeviceModel device, bool removeRtuProperties = true)
    {
        if (removeRtuProperties)
        {
            RemoveDeviceProperties(device, "PortName", "BaudRate", "DataBit", "DataBits", "Parity", "StopBit", "StopBits");
        }
        SetDeviceProperty(device, "Protocol", "ModbusTCP");
        SetDefaultProperty(device, "IP", "127.0.0.1");
        SetDefaultProperty(device, "Port", "502");
        SetCommonDefaults(device);
    }

    private static void SetCommonDefaults(DeviceModel device)
    {
        SetDefaultProperty(device, "SlaveId", "1");
        SetDefaultProperty(device, "Timeout", "5000");
        SetDefaultProperty(device, "TryCount", "3");
        SetDefaultProperty(device, "RefreshRate", "500");
    }

    private void RaiseCommunicationPropertiesChanged()
    {
        RaisePropertyChanged(nameof(CommunicationProtocol));
        RaisePropertyChanged(nameof(PortName));
        RaisePropertyChanged(nameof(BaudRate));
        RaisePropertyChanged(nameof(DataBits));
        RaisePropertyChanged(nameof(Parity));
        RaisePropertyChanged(nameof(StopBits));
        RaisePropertyChanged(nameof(IpAddress));
        RaisePropertyChanged(nameof(TcpPort));
        RaisePropertyChanged(nameof(SlaveId));
        RaisePropertyChanged(nameof(Timeout));
        RaisePropertyChanged(nameof(TryCount));
        RaisePropertyChanged(nameof(RefreshRate));
    }

    private void AddVariable()
    {
        if (SelectedDevice == null)
        {
            return;
        }

        var variable = new DeviceVarModel
        {
            VarNum = $"V{DateTime.Now:yyyyMMddHHmmssfff}",
            DeviceNum = SelectedDevice.DeviceNum,
            DeviceName = SelectedDevice.DeviceName,
            VarName = "新变量",
            VarAddress = "40001",
            VarType = "UInt16"
        };
        SelectedDevice.DeviceVarList.Add(variable);
        SelectedVariable = variable;
    }

    private void DeleteVariable()
    {
        if (SelectedDevice != null && SelectedVariable != null)
        {
            SelectedDevice.DeviceVarList.Remove(SelectedVariable);
            SelectedVariable = null;
        }
    }

    private void AddManualControl()
    {
        if (SelectedDevice == null)
        {
            return;
        }

        var control = new ManualControlModel
        {
            ControlName = "控制",
            ControlAddress = "40001",
            ControlValue = "0"
        };
        SelectedDevice.ManualControlList.Add(control);
        SelectedManualControl = control;
    }

    private void DeleteManualControl()
    {
        if (SelectedDevice != null && SelectedManualControl != null)
        {
            SelectedDevice.ManualControlList.Remove(SelectedManualControl);
            SelectedManualControl = null;
        }
    }

    private void OpenThresholdConfiguration()
    {
        if (SelectedDevice != null)
        {
            _windowService.ShowVariableThresholds(SelectedDevice);
        }
    }

    private void SaveDevices()
    {
        StatusMessage = string.Empty;
        if (!Validate(out string message))
        {
            StatusMessage = message;
            _logger.Warn($"设备配置校验失败：{message}");
            return;
        }

        List<DeviceEntity> devices = DeviceList.Select(device => new DeviceEntity
        {
            DeviceNum = device.DeviceNum.Trim(),
            DeviceName = device.DeviceName.Trim(),
            ComponentType = ResolveComponentType(device.ComponentType, device.DeviceName)
        }).ToList();
        var properties = new List<DevicePropEntity>();
        var variables = new List<DeviceVarEntity>();
        var controls = new List<ManualControlEntity>();
        var alarms = new List<VarAlarmConfEntity>();

        foreach (DeviceModel device in DeviceList)
        {
            properties.AddRange(device.DevicePropList
                .Where(property => !string.IsNullOrWhiteSpace(property.PropName)
                    && !string.IsNullOrWhiteSpace(property.PropValue))
                .Select(property => new DevicePropEntity
                {
                    DeviceNum = device.DeviceNum.Trim(),
                    PropName = property.PropName.Trim(),
                    PropValue = property.PropValue.Trim()
                }));

            foreach (DeviceVarModel variable in device.DeviceVarList)
            {
                variables.Add(new DeviceVarEntity
                {
                    VarNum = variable.VarNum.Trim(),
                    DeviceNum = device.DeviceNum.Trim(),
                    VarName = variable.VarName.Trim(),
                    VarAddress = variable.VarAddress.Trim(),
                    VarType = variable.VarType.Trim(),
                    Modulus = variable.Modulus,
                    Offset = variable.Offset
                });
                alarms.AddRange(variable.VarAlarmConfList.Select(alarm => new VarAlarmConfEntity
                {
                    ConfNum = alarm.ConfNum,
                    VarNum = variable.VarNum.Trim(),
                    Operator = alarm.Operator,
                    CompareValue = alarm.CompareValue,
                    AlarmContent = alarm.AlarmContent
                }));
            }

            controls.AddRange(device.ManualControlList.Select(control => new ManualControlEntity
            {
                DeviceNum = device.DeviceNum.Trim(),
                ControlName = control.ControlName,
                ControlAddress = control.ControlAddress,
                ControlValue = control.ControlValue
            }));
        }

        if (!_dataAccess.SaveDevice(devices, properties, variables, controls, alarms))
        {
            StatusMessage = "保存失败，请检查数据库连接和日志。";
            _logger.Error("设备配置保存失败");
            return;
        }

        _logger.Info($"设备配置保存成功：{devices.Count} 台设备，{variables.Count} 个变量");
        _windowService.CloseActiveDialog(true);
    }

    private bool Validate(out string message)
    {
        message = string.Empty;
        if (DeviceList.Count == 0)
        {
            message = "请至少保留一台设备。";
            return false;
        }
        if (DeviceList.Any(device => string.IsNullOrWhiteSpace(device.DeviceNum)
            || string.IsNullOrWhiteSpace(device.DeviceName)))
        {
            message = "设备编号和设备名称不能为空。";
            return false;
        }
        if (DeviceList.GroupBy(device => device.DeviceNum.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            message = "设备编号不能重复。";
            return false;
        }

        foreach (DeviceModel device in DeviceList)
        {
            if (!ValidateCommunication(device, out message))
            {
                return false;
            }
            foreach (DeviceVarModel variable in device.DeviceVarList)
            {
                if (string.IsNullOrWhiteSpace(variable.VarNum)
                    || string.IsNullOrWhiteSpace(variable.VarName)
                    || string.IsNullOrWhiteSpace(variable.VarAddress)
                    || Type.GetType($"System.{variable.VarType.Trim()}") == null)
                {
                    message = $"设备“{device.DeviceName}”存在未填写完整或类型无效的变量。";
                    return false;
                }
            }
        }

        if (DeviceList.SelectMany(device => device.DeviceVarList)
            .GroupBy(variable => variable.VarNum.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            message = "变量编号不能重复。";
            return false;
        }
        return true;
    }

    private static bool ValidateCommunication(DeviceModel device, out string message)
    {
        message = string.Empty;
        string protocol = GetDeviceProperty(device, "Protocol");
        if (protocol == "ModbusRTU")
        {
            if (string.IsNullOrWhiteSpace(GetDeviceProperty(device, "PortName")))
            {
                message = $"设备“{device.DeviceName}”未选择串口。";
                return false;
            }
            if (!int.TryParse(GetDeviceProperty(device, "BaudRate"), out int baudRate) || baudRate <= 0
                || !int.TryParse(GetDeviceProperty(device, "DataBits"), out int dataBits) || dataBits is < 5 or > 8
                || !Enum.TryParse(GetDeviceProperty(device, "Parity"), out Parity _)
                || !Enum.TryParse(GetDeviceProperty(device, "StopBits"), out System.IO.Ports.StopBits stopBits)
                || stopBits == System.IO.Ports.StopBits.None)
            {
                message = $"设备“{device.DeviceName}”的串口参数无效。";
                return false;
            }
        }
        else if (protocol == "ModbusTCP")
        {
            if (string.IsNullOrWhiteSpace(GetDeviceProperty(device, "IP"))
                || !int.TryParse(GetDeviceProperty(device, "Port"), out int port)
                || port is < 1 or > 65535)
            {
                message = $"设备“{device.DeviceName}”的 IP 或 TCP 端口无效。";
                return false;
            }
        }
        else
        {
            message = $"设备“{device.DeviceName}”的通信协议无效。";
            return false;
        }

        if (!byte.TryParse(GetDeviceProperty(device, "SlaveId"), out byte slaveId)
            || slaveId is < 1 or > 247)
        {
            message = $"设备“{device.DeviceName}”的从站地址应为 1 到 247。";
            return false;
        }
        if (!int.TryParse(GetDeviceProperty(device, "Timeout"), out int timeout) || timeout <= 0
            || !int.TryParse(GetDeviceProperty(device, "TryCount"), out int tryCount) || tryCount <= 0
            || !int.TryParse(GetDeviceProperty(device, "RefreshRate"), out int refreshRate) || refreshRate <= 0)
        {
            message = $"设备“{device.DeviceName}”的超时、重试次数或采集周期无效。";
            return false;
        }
        return true;
    }
}