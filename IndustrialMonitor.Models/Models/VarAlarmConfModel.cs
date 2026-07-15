namespace IndustrialMonitor.Models.Models;

public sealed class VarAlarmConfModel
{
    public string ConfNum { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string CompareValue { get; set; } = string.Empty;
    public string AlarmContent { get; set; } = string.Empty;
}