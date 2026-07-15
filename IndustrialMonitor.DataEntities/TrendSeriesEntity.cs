using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_TrendSerieses")]
public class TrendSeriesEntity
{
    [Key]
    public int SeriesId { get; set; }
    public string DeviceNum { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;
    public string TrendNum { get; set; } = string.Empty;
    public string ANum { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}