using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_TrendAxises")]
public class TrendAxisEntity
{
    [Key]
    public string AxisNum { get; set; } = string.Empty;
    public string TrendNum { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsShowTitle { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 100;
    public bool IsShowSeperator { get; set; }
    public string LabelFormater { get; set; } = "00";
    public string Position { get; set; } = string.Empty;
}