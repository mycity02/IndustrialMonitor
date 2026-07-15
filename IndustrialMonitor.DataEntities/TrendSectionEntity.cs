using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_TrendSections")]
public class TrendSectionEntity
{
    [Key]
    public int SectionId { get; set; }
    public string AxisNum { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Color { get; set; } = "Red";
}