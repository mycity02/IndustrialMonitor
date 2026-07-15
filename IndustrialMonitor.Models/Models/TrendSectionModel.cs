using LiveCharts.Wpf;
using System.Windows.Media;

namespace IndustrialMonitor.Models.Models;

public sealed class TrendSectionModel : BindableBase
{
    private double _value;
    private string _color = "Red";

    public TrendSectionModel()
    {
        Section = new AxisSection
        {
            Value = 0,
            Stroke = Brushes.Red,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 5, 5 }
        };
    }

    public double Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                Section.Value = value;
            }
        }
    }

    public string Color
    {
        get => _color;
        set
        {
            if (SetProperty(ref _color, value)
                && ColorConverter.ConvertFromString(value) is System.Windows.Media.Color color)
            {
                Section.Stroke = new SolidColorBrush(color);
            }
        }
    }

    public AxisSection Section { get; }
}