using IndustrialMonitor.Models.Models;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels.FunctionUC;

/// <summary>
/// 实时趋势页：每秒读取一个设备变量，并保留最近 60 个点。
/// Modbus 通信仍由 MainUCViewModel 负责，这里只读取已经更新好的 ReadValue。
/// </summary>
public sealed class TrendUCViewModel : BindableBase
{
    private const int MaxPointCount = 60;

    private readonly MainUCViewModel _mainViewModel;
    private readonly DispatcherTimer _sampleTimer;
    private readonly ChartValues<double> _values = [];
    private readonly LineSeries _lineSeries;

    private ObservableCollection<DeviceVarModel> _variables = [];
    private DeviceModel? _selectedDevice;
    private DeviceVarModel? _selectedVariable;
    private bool _isRunning;
    private string _currentValueText = "--";
    private string _statusText = "请选择设备和变量";

    public TrendUCViewModel(MainUCViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        _lineSeries = new LineSeries
        {
            Title = "实时值",
            Values = _values,
            Fill = Brushes.Transparent,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 2,
            PointGeometrySize = 5,
            LineSmoothness = 0.2
        };
        ChartSeries = new SeriesCollection { _lineSeries };

        ToggleSamplingCommand = new DelegateCommand(ToggleSampling);
        ClearCommand = new DelegateCommand(ClearChart);

        _sampleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _sampleTimer.Tick += (_, _) => SampleCurrentValue();
        _sampleTimer.Start();

        Devices.CollectionChanged += DevicesChanged;
        EnsureSelectedDevice();
    }

    public ObservableCollection<DeviceModel> Devices => _mainViewModel.DeviceList;
    public ObservableCollection<string> TimeLabels { get; } = [];
    public SeriesCollection ChartSeries { get; }

    public ObservableCollection<DeviceVarModel> Variables
    {
        get => _variables;
        private set => SetProperty(ref _variables, value);
    }

    public DeviceModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetProperty(ref _selectedDevice, value))
            {
                return;
            }

            Variables = value?.DeviceVarList ?? [];
            SelectedVariable = Variables.FirstOrDefault();
        }
    }

    public DeviceVarModel? SelectedVariable
    {
        get => _selectedVariable;
        set
        {
            if (!SetProperty(ref _selectedVariable, value))
            {
                return;
            }

            IsRunning = false;
            _lineSeries.Title = value?.VarName ?? "实时值";
            RaisePropertyChanged(nameof(ChartTitle));
            ClearChart();
            StatusText = value == null ? "当前设备没有监控变量" : "点击开始显示实时曲线";
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RaisePropertyChanged(nameof(RunButtonText));
            }
        }
    }

    public string RunButtonText => IsRunning ? "暂停" : "开始";
    public string ChartTitle => SelectedVariable == null ? "实时趋势" : $"{SelectedVariable.VarName}实时趋势";

    public string CurrentValueText
    {
        get => _currentValueText;
        private set => SetProperty(ref _currentValueText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public DelegateCommand ToggleSamplingCommand { get; }
    public DelegateCommand ClearCommand { get; }

    private void DevicesChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs) =>
        EnsureSelectedDevice();

    private void EnsureSelectedDevice()
    {
        if (SelectedDevice == null || !Devices.Contains(SelectedDevice))
        {
            SelectedDevice = Devices.FirstOrDefault();
        }
    }

    private void ToggleSampling()
    {
        if (SelectedVariable == null)
        {
            StatusText = "请先选择设备和变量";
            return;
        }

        IsRunning = !IsRunning;
        StatusText = IsRunning ? "正在每秒采样" : "已暂停";
    }

    private void SampleCurrentValue()
    {
        if (!IsRunning || SelectedVariable == null)
        {
            return;
        }

        if (!TryReadDouble(SelectedVariable.ReadValue, out double value))
        {
            StatusText = "当前变量值不是数字";
            return;
        }

        _values.Add(value);
        TimeLabels.Add(DateTime.Now.ToString("HH:mm:ss"));
        CurrentValueText = value.ToString("0.###", CultureInfo.InvariantCulture);
        TrimOldPoints();
    }

    private void ClearChart()
    {
        _values.Clear();
        TimeLabels.Clear();
        CurrentValueText = "--";
    }

    private void TrimOldPoints()
    {
        while (_values.Count > MaxPointCount)
        {
            _values.RemoveAt(0);
            TimeLabels.RemoveAt(0);
        }
    }

    private static bool TryReadDouble(object value, out double result)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || double.TryParse(text, out result);
    }
}
