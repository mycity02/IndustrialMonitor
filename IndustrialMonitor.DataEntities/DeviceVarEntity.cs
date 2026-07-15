using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IndustrialMonitor.DataEntities;

[Table("monitor_DeviceVars")]
public class DeviceVarEntity
{
    [Key]
    public string VarNum { get; set; } = string.Empty;
    public string DeviceNum { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string VarAddress { get; set; } = string.Empty;
    public string VarType { get; set; } = string.Empty;
    public double Offset { get; set; }
    public double Modulus { get; set; } = 1;
}