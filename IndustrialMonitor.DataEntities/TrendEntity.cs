using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_Trends")]
public class TrendEntity
{
    [Key]
    public string TrendNum { get; set; } = string.Empty;
    public string TrendName { get; set; } = string.Empty;
    public bool IsShowLegend { get; set; }
}