using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_DeviceProps")]
public class DevicePropEntity
{
    [Key]
    public int DevicePropId { get; set; }
    public string DeviceNum { get; set; } = string.Empty;
    public string PropName { get; set; } = string.Empty;
    public string PropValue { get; set; } = string.Empty;
}