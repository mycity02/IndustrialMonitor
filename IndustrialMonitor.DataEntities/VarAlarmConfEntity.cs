using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

/// <summary>
/// 监控变量的阈值判断配置。保留原表名以兼容已有数据。
/// </summary>
[Table("monitor_VarAlarmConfs")]
public class VarAlarmConfEntity
{
    [Key]
    public string ConfNum { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string CompareValue { get; set; } = string.Empty;
    public string AlarmContent { get; set; } = string.Empty;
}