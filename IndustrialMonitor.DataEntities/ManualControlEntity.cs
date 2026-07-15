using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_ManualControls")]
public class ManualControlEntity
{
    [Key]
    public int ControlId { get; set; }
    public string DeviceNum { get; set; } = string.Empty;
    public string ControlName { get; set; } = string.Empty;
    public string ControlAddress { get; set; } = string.Empty;
    public string ControlValue { get; set; } = string.Empty;
}