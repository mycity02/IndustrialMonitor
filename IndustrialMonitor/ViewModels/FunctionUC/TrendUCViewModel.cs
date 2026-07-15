using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels.DialogWin;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace IndustrialMonitor.ViewModels.FunctionUC;

/// <summary>
/// 趋势页：管理趋势配置，并用一个 UI 定时器刷新全部曲线。
/// </summary>
public sealed class TrendUCViewModel : BindableBase
{
    private const int MaxPointCount = 30;

    private readonly MainUCViewModel _mainViewModel;
    private readonly IDataAccess _dataAccess;
    private readonly IWindowService _windowService;
    private readonly DispatcherTimer _refreshTimer;
    private TrendModel? _selectedTrend;

    public TrendUCViewModel(
        MainUCViewModel mainViewModel,
        IDataAccess dataAccess,
        IWindowService windowService)
    {
        _mainViewModel = mainViewModel;
        _dataAccess = dataAccess;
        _windowService = windowService;

        BrushList = typeof(Brushes).GetProperties()
            .Select(property => property.Name)
            .ToList();

        TrendList = LoadTrends();
        if (TrendList.Count == 0)
        {
            TrendList.Add(new TrendModel());
        }
        SelectedTrend = TrendList[0];

        AddTrendCommand = new DelegateCommand(AddTrend);
        DelTrendCommand = new DelegateCommand<TrendModel>(DeleteTrend);
        ShowAxisEditCommand = new DelegateCommand(ShowAxisEditor);
        ShowDeviceVarDialogCommand = new DelegateCommand(ShowVariableChooser);
        SaveTrendCommand = new DelegateCommand(SaveTrends);
        SaveToImageCommand = new DelegateCommand<Visual>(SaveToImage);

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _refreshTimer.Tick += (_, _) => RefreshCharts();
        _refreshTimer.Start();
    }

    public ObservableCollection<TrendModel> TrendList { get; }
    public IReadOnlyList<string> BrushList { get; }

    public TrendModel? SelectedTrend
    {
        get => _selectedTrend;
        set => SetProperty(ref _selectedTrend, value);
    }

    public DelegateCommand AddTrendCommand { get; }
    public DelegateCommand<TrendModel> DelTrendCommand { get; }
    public DelegateCommand ShowAxisEditCommand { get; }
    public DelegateCommand ShowDeviceVarDialogCommand { get; }
    public DelegateCommand SaveTrendCommand { get; }
    public DelegateCommand<Visual> SaveToImageCommand { get; }

    private void AddTrend()
    {
        var trend = new TrendModel();
        TrendList.Add(trend);
        SelectedTrend = trend;
    }

    private void DeleteTrend(TrendModel? trend)
    {
        if (trend == null || TrendList.Count <= 1)
        {
            return;
        }

        int nextIndex = Math.Max(0, TrendList.IndexOf(trend) - 1);
        bool wasSelected = ReferenceEquals(SelectedTrend, trend);
        TrendList.Remove(trend);
        if (wasSelected)
        {
            SelectedTrend = TrendList[nextIndex];
        }
    }

    private void ShowAxisEditor()
    {
        if (SelectedTrend == null)
        {
            return;
        }

        _windowService.ShowTrendAxisEditor(new TrendAxisEditWinViewModel
        {
            Trend = SelectedTrend,
            BrushList = BrushList
        });
    }

    private void ShowVariableChooser()
    {
        if (SelectedTrend == null)
        {
            return;
        }

        _windowService.ShowTrendVariableChooser(
            new TrendDeviceChooseWinViewModel(
                SelectedTrend,
                BrushList,
                _mainViewModel.DeviceList));
    }

    private void RefreshCharts()
    {
        List<DeviceModel> devices = _mainViewModel.DeviceList.ToList();
        string timeLabel = DateTime.Now.ToString("HH:mm:ss");

        foreach (TrendModel trend in TrendList)
        {
            IList<string>? labels = trend.XAxis[0].Labels;
            if (labels != null)
            {
                labels.Add(timeLabel);
                TrimToLatest(labels, MaxPointCount);
            }

            foreach (TrendSeriesModel series in trend.Series)
            {
                DeviceVarModel? variable = devices
                    .FirstOrDefault(device => device.DeviceNum == series.DeviceNum)?
                    .DeviceVarList
                    .FirstOrDefault(item => item.VarNum == series.VarNum);

                if (variable == null || !TryReadDouble(variable.ReadValue, out double value))
                {
                    continue;
                }

                series.Series.Values.Add(value);
                while (series.Series.Values.Count > MaxPointCount)
                {
                    series.Series.Values.RemoveAt(0);
                }
            }
        }
    }

    private static bool TryReadDouble(object value, out double result)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || double.TryParse(text, out result);
    }

    private static void TrimToLatest(IList<string> values, int maxCount)
    {
        while (values.Count > maxCount)
        {
            values.RemoveAt(0);
        }
    }

    private ObservableCollection<TrendModel> LoadTrends()
    {
        List<TrendEntity> trends = _dataAccess.GetTrends();
        List<TrendAxisEntity> axes = _dataAccess.GetTrendAxises();
        List<TrendSectionEntity> sections = _dataAccess.GetTrendSections();
        List<TrendSeriesEntity> series = _dataAccess.GetTrendSerieses();

        return new ObservableCollection<TrendModel>(trends.Select(trend =>
        {
            var model = new TrendModel
            {
                TNum = trend.TrendNum,
                TrendName = trend.TrendName,
                IsShowLegend = trend.IsShowLegend
            };

            var axisModels = new ObservableCollection<TrendAxisModel>(
                axes.Where(axis => axis.TrendNum == trend.TrendNum)
                    .Select(axis => new TrendAxisModel
                    {
                        ANum = axis.AxisNum,
                        Title = axis.Title,
                        IsShowTitle = axis.IsShowTitle,
                        Minimum = axis.Minimum,
                        Maximum = axis.Maximum,
                        IsShowSeperator = axis.IsShowSeperator,
                        LabelFormater = axis.LabelFormater,
                        Position = axis.Position,
                        SectionList = new ObservableCollection<TrendSectionModel>(
                            sections.Where(section => section.AxisNum == axis.AxisNum)
                                .Select(section => new TrendSectionModel
                                {
                                    Value = section.Value,
                                    Color = section.Color
                                }))
                    }));

            if (axisModels.Count > 0)
            {
                model.AxisList = axisModels;
            }

            model.Series = new ObservableCollection<TrendSeriesModel>(
                series.Where(item => item.TrendNum == trend.TrendNum)
                    .Select(item => new TrendSeriesModel
                    {
                        DeviceNum = item.DeviceNum,
                        VarNum = item.VarNum,
                        Title = item.Title,
                        Color = string.IsNullOrWhiteSpace(item.Color) ? "DodgerBlue" : item.Color,
                        ANum = item.ANum
                    }));

            return model;
        }));
    }

    private void SaveTrends()
    {
        var trends = new List<TrendEntity>();
        var axes = new List<TrendAxisEntity>();
        var sections = new List<TrendSectionEntity>();
        var series = new List<TrendSeriesEntity>();

        foreach (TrendModel trend in TrendList)
        {
            trends.Add(new TrendEntity
            {
                TrendNum = trend.TNum,
                TrendName = trend.TrendName,
                IsShowLegend = trend.IsShowLegend
            });

            foreach (TrendAxisModel axis in trend.AxisList)
            {
                axes.Add(new TrendAxisEntity
                {
                    AxisNum = axis.ANum,
                    TrendNum = trend.TNum,
                    Title = axis.Title,
                    IsShowTitle = axis.IsShowTitle,
                    Minimum = axis.Minimum,
                    Maximum = axis.Maximum,
                    IsShowSeperator = axis.IsShowSeperator,
                    LabelFormater = axis.LabelFormater,
                    Position = axis.Position
                });

                sections.AddRange(axis.SectionList.Select(section => new TrendSectionEntity
                {
                    AxisNum = axis.ANum,
                    Value = section.Value,
                    Color = section.Color
                }));
            }

            series.AddRange(trend.Series.Select(item => new TrendSeriesEntity
            {
                DeviceNum = item.DeviceNum,
                VarNum = item.VarNum,
                Title = item.Title,
                Color = item.Color,
                ANum = item.ANum,
                TrendNum = trend.TNum
            }));
        }

        try
        {
            _dataAccess.SaveTrend(trends, axes, sections, series);
            _windowService.ShowInformation("趋势配置已保存", "趋势");
        }
        catch (Exception exception)
        {
            _windowService.ShowError($"保存失败：{exception.Message}", "趋势");
        }
    }

    private void SaveToImage(Visual? visual)
    {
        if (visual == null)
        {
            return;
        }

        string? fileName = _windowService.ChooseSaveFile(
            "导出趋势图",
            "PNG 图片 (*.png)|*.png",
            $"Trend-{DateTime.Now:yyyyMMdd-HHmmss}.png");

        if (fileName != null)
        {
            CreateBitmapFromVisual(visual, fileName);
        }
    }

    private static void CreateBitmapFromVisual(Visual target, string fileName)
    {
        Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
        int width = (int)Math.Ceiling(bounds.Width);
        int height = (int)Math.Ceiling(bounds.Height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var drawingVisual = new DrawingVisual();
        using (DrawingContext context = drawingVisual.RenderOpen())
        {
            context.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size));
        }
        bitmap.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(fileName);
        encoder.Save(stream);
    }
}