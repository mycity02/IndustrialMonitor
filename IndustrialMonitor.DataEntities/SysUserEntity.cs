using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_SysUsers")]
public class SysUserEntity
{
    [Key]
    public int UserId { get; set; }
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}