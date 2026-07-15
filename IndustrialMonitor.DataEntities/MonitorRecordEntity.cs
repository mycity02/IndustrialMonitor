using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_MonitorRecords")]
public class MonitorRecordEntity
{
    [Key]
    public int RecordId { get; set; }
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public decimal RecordValue { get; set; }
    public DateTime RecordTime { get; set; } = DateTime.Now;
    public string Account { get; set; } = string.Empty;
}