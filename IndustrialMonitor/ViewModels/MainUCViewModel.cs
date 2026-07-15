using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DBAcess;
using IndustrialMonitor.DeviceAccess;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.DeviceAccess.Execute;
using IndustrialMonitor.Logger;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels;

/// <summary>
/// 主界面只负责三件事：页面导航、设备加载、设备轮询。
/// </summary>
public sealed class MainUCViewModel : BindableBase, IDialogAware
{
    private readonly IDataAccess _dataAccess;
    private readonly ILoggerService<MainUCViewModel> _logger;
    private readonly IWindowService _windowService;
    private readonly Communication _communication = Communication.CreateInstance();
    private readonly List<Task> _monitorTasks = [];
    private readonly DispatcherTimer _statusTimer;

    private CancellationTokenSource _monitorCancellation = new();
    private ObservableCollection<DeviceModel> _deviceList = [];
    private List<MenuModel> _menuList = [];
    private MenuModel? _selectedMenu;
    private MainPage _currentPage;
    private int _abnormalDeviceCount;

    public MainUCViewModel(
        IDataAccess dataAccess,
        ILoggerService<MainUCViewModel> logger,
        IWindowService windowService)
    {
        _dataAccess = dataAccess;
        _logger = logger;
        _windowService = windowService;

        OpenDeviceManageCommand = new DelegateCommand(OpenDeviceManager);
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += (_, _) =>
            AbnormalDeviceCount = DeviceList.Count(device => device.IsWarning);
    }

    public string Title => "工业设备监控";
    public DialogCloseListener RequestClose { get; } = new();
    public SysUserModel LoginUserModel { get; private set; } = new();

    public ObservableCollection<DeviceModel> DeviceList
    {
        get => _deviceList;
        private set => SetProperty(ref _deviceList, value);
    }

    public List<MenuModel> MenuList
    {
        get => _menuList;
        private set => SetProperty(ref _menuList, value);
    }

    public MenuModel? SelectedMenu
    {
        get => _selectedMenu;
        set
        {
            if (SetProperty(ref _selectedMenu, value) && value != null)
            {
                ShowPage(value.Page);
            }
        }
    }

    public MainPage CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public int AbnormalDeviceCount
    {
        get => _abnormalDeviceCount;
        private set => SetProperty(ref _abnormalDeviceCount, value);
    }

    public DelegateCommand OpenDeviceManageCommand { get; }

    public bool CanCloseDialog() => true;

    public void OnDialogOpened(IDialogParameters parameters)
    {
        LoginUserModel = parameters.GetValue<SysUserModel>("LoginUser") ?? new SysUserModel();
        LoadDevices();
        InitializeMenu();
        StartMonitoring();
        _logger.Info("主监控模块初始化完成");
    }

    public async void OnDialogClosed()
    {
        try
        {
            await StopMonitoringAsync();
        }
        catch (Exception exception)
        {
            _logger.Error("停止设备通信失败", exception);
        }
    }

    private void InitializeMenu()
    {
        MenuList =
        [
            new MenuModel { MenuName = "监控", MenuIcon = "\ue639", Page = MainPage.Monitor },
            new MenuModel { MenuName = "趋势", MenuIcon = "\ue61a", Page = MainPage.Trend },
            new MenuModel { MenuName = "监控数据", MenuIcon = "\ue703", Page = MainPage.MonitorData }
        ];
        SelectedMenu = MenuList[0];
    }

    private void ShowPage(MainPage page)
    {
        CurrentPage = page;
        _logger.Info($"切换到{SelectedMenu?.MenuName}模块");
    }

    private async void OpenDeviceManager()
    {
        try
        {
            if (!_windowService.ShowDeviceManager())
            {
                return;
            }

            await StopMonitoringAsync();
            LoadDevices();
            StartMonitoring();
            _logger.Info("设备配置已重新加载");
        }
        catch (Exception exception)
        {
            _logger.Error("重新加载设备配置失败", exception);
            _windowService.ShowError($"重新加载设备失败：{exception.Message}", "设备管理");
        }
    }

    private void LoadDevices()
    {
        List<DeviceEntity> devices = _dataAccess.GetDevices();
        List<DevicePropEntity> properties = _dataAccess.GetDeviceProps();
        List<DeviceVarEntity> variables = _dataAccess.GetDeviceVarList();
        List<ManualControlEntity> controls = _dataAccess.GetManualControlList();
        List<VarAlarmConfEntity> alarms = _dataAccess.GetVarAlarmConfList();

        DeviceList = new ObservableCollection<DeviceModel>(devices.Select(device => new DeviceModel
        {
            WriteAction = WriteBytesAsync,
            ComponentType = device.ComponentType,
            DeviceName = device.DeviceName,
            DeviceNum = device.DeviceNum,
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
                        Offset = variable.Offset,
                        Modulus = variable.Modulus,
                        VarType = variable.VarType,
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
        }));
    }

    private void StartMonitoring()
    {
        if (_monitorCancellation.IsCancellationRequested)
        {
            _monitorCancellation.Dispose();
            _monitorCancellation = new CancellationTokenSource();
        }

        CancellationToken token = _monitorCancellation.Token;
        foreach (DeviceModel device in DeviceList)
        {
            if (!TryPrepareDevice(device, out ExecuteObject? executeObject, out List<GroupAddress>? groups))
            {
                continue;
            }

            // 每台设备拥有独立轮询任务。任务之间可以并发运行；
            // 只有共享同一串口或 TCP 连接的 NModbus 指令会在连接层排队。
            _monitorTasks.Add(Task.Run(
                () => PollDeviceAsync(device, executeObject!, groups!, GetRefreshRate(device), token),
                token));
        }

        AbnormalDeviceCount = DeviceList.Count(device => device.IsWarning);
        _statusTimer.Start();
    }

    private bool TryPrepareDevice(
        DeviceModel device,
        out ExecuteObject? executeObject,
        out List<GroupAddress>? groups)
    {
        executeObject = null;
        groups = null;

        if (device.DevicePropList.Count == 0 || device.DeviceVarList.Count == 0)
        {
            _logger.Warn($"设备 {device.DeviceNum} 没有通信参数或监控变量，已跳过");
            return false;
        }

        var executeResult = _communication.GetExecuteObject(ToPropertyEntities(device));
        if (!executeResult.Status)
        {
            SetWarning(device, executeResult.Msg);
            _logger.Error($"设备 {device.DeviceNum} 通信初始化失败：{executeResult.Msg}");
            return false;
        }

        var variableProperties = new List<VariableProp>();
        foreach (DeviceVarModel variable in device.DeviceVarList)
        {
            Type? valueType = Type.GetType($"System.{variable.VarType}");
            if (valueType == null)
            {
                SetWarning(device, $"不支持的数据类型：{variable.VarType}");
                _logger.Error($"设备 {device.DeviceNum} 的变量 {variable.VarName} 数据类型无效");
                return false;
            }

            variableProperties.Add(new VariableProp
            {
                VarNum = variable.VarNum,
                VarAddr = variable.VarAddress,
                ValueType = valueType
            });
        }

        var groupResult = executeResult.Data.GroupAddress(variableProperties);
        if (!groupResult.Status)
        {
            SetWarning(device, groupResult.Msg);
            _logger.Error($"设备 {device.DeviceNum} 的变量地址无效：{groupResult.Msg}");
            return false;
        }

        executeObject = executeResult.Data;
        groups = groupResult.Data;
        return true;
    }

    private async Task PollDeviceAsync(
        DeviceModel device,
        ExecuteObject executeObject,
        List<GroupAddress> groups,
        int refreshRate,
        CancellationToken token)
    {
        try
        {
            while (true)
            {
                await Task.Delay(refreshRate, token).ConfigureAwait(false);

                Result readResult = await executeObject
                    .ReadAsync(groups, token)
                    .ConfigureAwait(false);

                if (!readResult.Status)
                {
                    if (!device.IsWarning || device.WarningMsg != readResult.Msg)
                    {
                        _logger.Error($"设备 {device.DeviceNum} 读取失败：{readResult.Msg}");
                    }

                    await RunOnUiAsync(
                        () => SetWarning(device, readResult.Msg),
                        token).ConfigureAwait(false);
                    continue;
                }

                // WPF 绑定对象只在 UI 线程更新，通信仍在后台任务中进行。
                await RunOnUiAsync(
                    () => ProcessReadValues(device, groups),
                    token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 正常停止。
        }
        catch (Exception exception)
        {
            await RunOnUiAsync(
                () => SetWarning(device, exception.Message),
                CancellationToken.None).ConfigureAwait(false);
            _logger.Error($"设备 {device.DeviceNum} 轮询异常", exception);
        }
    }

    private void ProcessReadValues(DeviceModel device, IEnumerable<GroupAddress> groups)
    {
        bool wasWarning = device.IsWarning;
        string previousWarning = device.WarningMsg;
        string? currentWarning = null;

        foreach (VariableProp variableProperty in groups.SelectMany(group => group.VarPropList))
        {
            var convertResult = _communication.ConvertType(
                variableProperty.ReadBytes,
                variableProperty.ValueType);

            if (!convertResult.Status)
            {
                currentWarning ??= convertResult.Msg;
                _logger.Error($"设备 {device.DeviceNum} 数据转换失败：{convertResult.Msg}");
                continue;
            }

            DeviceVarModel variable = device.DeviceVarList.First(item =>
                item.VarNum == variableProperty.VarNum);
            object oldValue = variable.ReadValue;
            variable.ReadValue = ScaleValue(convertResult.Data, variable);

            currentWarning ??= FindThresholdWarning(variable);
            SaveChangedValue(device, variable, oldValue);
        }

        if (currentWarning == null)
        {
            if (wasWarning)
            {
                _logger.Info($"设备 {device.DeviceNum} 通信恢复正常");
            }
            device.IsWarning = false;
            device.WarningMsg = string.Empty;
            return;
        }

        if (!wasWarning || previousWarning != currentWarning)
        {
            _logger.Warn($"设备 {device.DeviceNum}：{currentWarning}");
        }
        SetWarning(device, currentWarning);
    }

    private static object ScaleValue(object rawValue, DeviceVarModel variable)
    {
        if (variable.VarType == "Boolean")
        {
            return rawValue;
        }

        double number = Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);
        return number * variable.Modulus + variable.Offset;
    }

    private static string? FindThresholdWarning(DeviceVarModel variable)
    {
        if (!TryConvertDouble(variable.ReadValue, out double currentValue))
        {
            return null;
        }

        foreach (VarAlarmConfModel alarm in variable.VarAlarmConfList)
        {
            if (TryConvertDouble(alarm.CompareValue, out double limit)
                && Compare(currentValue, limit, alarm.Operator))
            {
                return alarm.AlarmContent;
            }
        }
        return null;
    }

    private static bool Compare(double value, double limit, string comparisonOperator) =>
        comparisonOperator switch
        {
            ">" => value > limit,
            ">=" => value >= limit,
            "<" => value < limit,
            "<=" => value <= limit,
            "==" or "=" => value == limit,
            "!=" or "<>" => value != limit,
            _ => false
        };

    private static bool TryConvertDouble(object? value, out double result)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || double.TryParse(text, out result);
    }

    private void SaveChangedValue(DeviceModel device, DeviceVarModel variable, object oldValue)
    {
        if (variable.VarType != "UInt16" || Equals(variable.ReadValue, oldValue))
        {
            return;
        }

        MonitorRecordBuffer.Add(_dataAccess, new MonitorRecordEntity
        {
            DeviceNum = device.DeviceNum,
            DeviceName = device.DeviceName,
            VarNum = variable.VarNum,
            VarName = variable.VarName,
            RecordValue = Convert.ToDecimal(variable.ReadValue),
            Account = LoginUserModel.Account
        });
    }

    private async Task WriteBytesAsync(
        DeviceModel device,
        string address,
        byte[] writeBytes)
    {
        ResultData<ExecuteObject> executeResult =
            _communication.GetExecuteObject(ToPropertyEntities(device));

        if (!executeResult.Status)
        {
            SetWarning(device, executeResult.Msg);
            _logger.Error($"设备 {device.DeviceNum} 写入前通信初始化失败：{executeResult.Msg}");
            return;
        }

        Result writeResult = await executeResult.Data.WriteAsync(
            new WriteDataInfo
            {
                StartAddr = address,
                ValueType = typeof(ushort),
                WriteBytes = writeBytes
            },
            _monitorCancellation.Token);

        if (!writeResult.Status)
        {
            SetWarning(device, writeResult.Msg);
            _logger.Error($"设备 {device.DeviceNum} 写入失败：{writeResult.Msg}");
        }
    }

    private static int GetRefreshRate(DeviceModel device)
    {
        string? configuredValue = device.DevicePropList.FirstOrDefault(property =>
            property.PropName.Equals("RefreshRate", StringComparison.OrdinalIgnoreCase))?.PropValue;
        return int.TryParse(configuredValue, out int refreshRate) && refreshRate > 0
            ? refreshRate
            : 500;
    }
    private static List<DevicePropEntity> ToPropertyEntities(DeviceModel device) =>
        device.DevicePropList.Select(property => new DevicePropEntity
        {
            PropName = property.PropName,
            PropValue = property.PropValue
        }).ToList();

    private static void SetWarning(DeviceModel device, string message)
    {
        device.IsWarning = true;
        device.WarningMsg = message;
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
        _statusTimer.Stop();
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

        // 所有轮询任务停止后再统一关闭共享连接，避免某台设备提前关闭
        // 仍被其他设备使用的串口或 TCP 连接。
        await _communication.DisconnectAllAsync();

        _monitorCancellation.Dispose();
        _monitorCancellation = new CancellationTokenSource();
    }
}