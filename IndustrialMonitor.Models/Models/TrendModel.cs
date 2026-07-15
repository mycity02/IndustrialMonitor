using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace IndustrialMonitor.Models.Models;

/// <summary>
/// 一张趋势图的配置和显示数据。
/// 该模型只保存状态，不负责创建后台线程。
/// </summary>
public sealed class TrendModel : BindableBase
{
    private ObservableCollection<TrendAxisModel> _axisList =
    [
        new TrendAxisModel { IsShowSeperator = true }
    ];

    private ObservableCollection<TrendSeriesModel> _series = [];
    private string _trendName = "新建趋势图";
    private bool _isShowLegend;
    private LegendLocation _legendLocation = LegendLocation.None;

    public TrendModel()
    {
        var xAxis = new Axis
        {
            Separator = new Separator { Step = 1, StrokeThickness = 0 },
            Labels = [],
            LabelsRotation = -45
        };
        XAxis.Add(xAxis);

        AddAxisCommand = new DelegateCommand(AddAxis);
        DeleteAxisCommand = new DelegateCommand<TrendAxisModel>(DeleteAxis);

        AttachCollections();
        RebuildAxes();
        RebuildSeries();
    }

    public string TNum { get; set; } = $"T{DateTime.Now:yyyyMMddHHmmssFFFF}";

    public string TrendName
    {
        get => _trendName;
        set => SetProperty(ref _trendName, value);
    }

    public bool IsShowLegend
    {
        get => _isShowLegend;
        set
        {
            if (SetProperty(ref _isShowLegend, value))
            {
                LegendLocation = value ? LegendLocation.Top : LegendLocation.None;
            }
        }
    }

    public LegendLocation LegendLocation
    {
        get => _legendLocation;
        private set => SetProperty(ref _legendLocation, value);
    }

    public ObservableCollection<TrendAxisModel> AxisList
    {
        get => _axisList;
        set
        {
            if (ReferenceEquals(_axisList, value))
            {
                return;
            }

            _axisList.CollectionChanged -= AxisListChanged;
            SetProperty(ref _axisList, value ?? []);
            _axisList.CollectionChanged += AxisListChanged;
            RebuildAxes();
            RefreshSeriesAxisIndexes();
        }
    }

    public ObservableCollection<TrendSeriesModel> Series
    {
        get => _series;
        set
        {
            if (ReferenceEquals(_series, value))
            {
                return;
            }

            _series.CollectionChanged -= SeriesChanged;
            SetProperty(ref _series, value ?? []);
            _series.CollectionChanged += SeriesChanged;
            RebuildSeries();
        }
    }

    public AxesCollection XAxis { get; } = [];
    public AxesCollection YAxis { get; } = [];
    public SeriesCollection ChartSeries { get; } = [];

    public DelegateCommand AddAxisCommand { get; }
    public DelegateCommand<TrendAxisModel> DeleteAxisCommand { get; }

    private void AttachCollections()
    {
        _axisList.CollectionChanged += AxisListChanged;
        _series.CollectionChanged += SeriesChanged;
    }

    private void AddAxis() => AxisList.Add(new TrendAxisModel());

    private void DeleteAxis(TrendAxisModel? axis)
    {
        if (axis == null || AxisList.Count <= 1 || !AxisList.Remove(axis))
        {
            return;
        }

        string fallbackAxis = AxisList[0].ANum;
        foreach (TrendSeriesModel series in Series.Where(item => item.ANum == axis.ANum))
        {
            series.ANum = fallbackAxis;
        }
    }

    private void AxisListChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildAxes();
        RefreshSeriesAxisIndexes();
    }

    private void SeriesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildSeries();

    private void RebuildAxes()
    {
        YAxis.Clear();
        foreach (TrendAxisModel axis in AxisList)
        {
            YAxis.Add(axis.Axis);
        }
    }

    private void RebuildSeries()
    {
        ChartSeries.Clear();
        foreach (TrendSeriesModel series in Series)
        {
            series.AxisIndexFunc = FindAxisIndex;
            series.ANum = series.ANum;
            ChartSeries.Add(series.Series);
        }
    }

    private void RefreshSeriesAxisIndexes()
    {
        foreach (TrendSeriesModel series in Series)
        {
            series.AxisIndexFunc = FindAxisIndex;
            series.ANum = series.ANum;
        }
    }

    private int FindAxisIndex(string axisNum)
    {
        int index = AxisList.ToList().FindIndex(axis => axis.ANum == axisNum);
        return index < 0 ? 0 : index;
    }
}