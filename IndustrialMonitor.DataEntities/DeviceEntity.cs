using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_Devices")]
public class DeviceEntity
{
    [Key]
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
}