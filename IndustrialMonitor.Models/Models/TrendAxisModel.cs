using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;

namespace IndustrialMonitor.Models.Models;

/// <summary>
/// 趋势纵轴。SectionList 是唯一数据源，图表预警线会自动同步。
/// </summary>
public sealed class TrendAxisModel : BindableBase
{
    private string _title = "新纵轴";
    private bool _isShowTitle;
    private double _minimum;
    private double _maximum = 100;
    private bool _isShowSeperator;
    private string _labelFormater = "00";
    private string _position = "Left";
    private string _sectionValues = "<未配置>";
    private ObservableCollection<TrendSectionModel> _sectionList = [];

    public TrendAxisModel()
    {
        Axis = new Axis
        {
            MinValue = 0,
            MaxValue = 100,
            LabelFormatter = value => value.ToString("00"),
            Separator = new Separator
            {
                Step = 20,
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 245, 245, 245))
            }
        };

        AddSectionCommand = new DelegateCommand(() => SectionList.Add(new TrendSectionModel()));
        DeleteSectionCommand = new DelegateCommand<TrendSectionModel>(section =>
        {
            if (section != null)
            {
                SectionList.Remove(section);
            }
        });

        AttachSections(_sectionList);
        RebuildSections();
    }

    public string ANum { get; set; } = $"A{DateTime.Now:yyyyMMddHHmmssFFFF}";

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                Axis.Title = IsShowTitle ? value : string.Empty;
            }
        }
    }

    public bool IsShowTitle
    {
        get => _isShowTitle;
        set
        {
            if (SetProperty(ref _isShowTitle, value))
            {
                Axis.Title = value ? Title : string.Empty;
            }
        }
    }

    public double Minimum
    {
        get => _minimum;
        set
        {
            if (SetProperty(ref _minimum, value))
            {
                Axis.MinValue = value;
            }
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            if (SetProperty(ref _maximum, value))
            {
                Axis.MaxValue = value;
            }
        }
    }

    public bool IsShowSeperator
    {
        get => _isShowSeperator;
        set
        {
            if (SetProperty(ref _isShowSeperator, value))
            {
                Axis.Separator.StrokeThickness = value ? 1 : 0;
            }
        }
    }

    public string LabelFormater
    {
        get => _labelFormater;
        set
        {
            if (SetProperty(ref _labelFormater, value))
            {
                Axis.LabelFormatter = number => number.ToString(value);
            }
        }
    }

    public string Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                Axis.Position = value == "Right"
                    ? LiveCharts.AxisPosition.RightTop
                    : LiveCharts.AxisPosition.LeftBottom;
            }
        }
    }

    public string SectionValues
    {
        get => _sectionValues;
        private set => SetProperty(ref _sectionValues, value);
    }

    public ObservableCollection<TrendSectionModel> SectionList
    {
        get => _sectionList;
        set
        {
            if (ReferenceEquals(_sectionList, value))
            {
                return;
            }

            DetachSections(_sectionList);
            SetProperty(ref _sectionList, value ?? []);
            AttachSections(_sectionList);
            RebuildSections();
        }
    }

    public Axis Axis { get; }
    public DelegateCommand AddSectionCommand { get; }
    public DelegateCommand<TrendSectionModel> DeleteSectionCommand { get; }

    private void AttachSections(ObservableCollection<TrendSectionModel> sections)
    {
        sections.CollectionChanged += SectionsChanged;
        foreach (TrendSectionModel section in sections)
        {
            section.PropertyChanged += SectionPropertyChanged;
        }
    }

    private void DetachSections(ObservableCollection<TrendSectionModel> sections)
    {
        sections.CollectionChanged -= SectionsChanged;
        foreach (TrendSectionModel section in sections)
        {
            section.PropertyChanged -= SectionPropertyChanged;
        }
    }

    private void SectionsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems != null)
        {
            foreach (TrendSectionModel section in eventArgs.OldItems)
            {
                section.PropertyChanged -= SectionPropertyChanged;
            }
        }
        if (eventArgs.NewItems != null)
        {
            foreach (TrendSectionModel section in eventArgs.NewItems)
            {
                section.PropertyChanged += SectionPropertyChanged;
            }
        }
        RebuildSections();
    }

    private void SectionPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        SectionValues = FormatSectionValues();
        Axis.Model?.Chart?.Updater.Run(false, true);
    }

    private void RebuildSections()
    {
        Axis.Sections.Clear();
        foreach (TrendSectionModel section in SectionList)
        {
            Axis.Sections.Add(section.Section);
        }
        SectionValues = FormatSectionValues();
        Axis.Model?.Chart?.Updater.Run(false, true);
    }

    private string FormatSectionValues() =>
        SectionList.Count == 0
            ? "<未配置>"
            : string.Join(",", SectionList.Select(section => section.Value));
}