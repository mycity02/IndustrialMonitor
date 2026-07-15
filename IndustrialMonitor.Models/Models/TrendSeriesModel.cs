using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;

namespace IndustrialMonitor.Models.Models;

public sealed class TrendSeriesModel
{
    private string _title = "新序列";
    private string _color = "DodgerBlue";
    private string _axisNum = string.Empty;

    public TrendSeriesModel()
    {
        Series = new LineSeries
        {
            Values = new ChartValues<double>(),
            Fill = Brushes.Transparent,
            StrokeThickness = 2
        };
        Title = _title;
        Color = _color;
    }

    public string DeviceNum { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            Series.Title = value;
        }
    }

    public string Color
    {
        get => _color;
        set
        {
            _color = value;
            if (ColorConverter.ConvertFromString(value) is System.Windows.Media.Color color)
            {
                Series.Stroke = new SolidColorBrush(color);
            }
        }
    }

    public string ANum
    {
        get => _axisNum;
        set
        {
            _axisNum = value;
            Series.ScalesYAt = Math.Max(0, AxisIndexFunc?.Invoke(value) ?? 0);
        }
    }

    public Func<string, int>? AxisIndexFunc { get; set; }
    public LineSeries Series { get; }
}