namespace IndustrialMonitor.Models.Models;

public sealed class ManualControlModel
{
    public string ControlName { get; set; } = string.Empty;
    public string ControlAddress { get; set; } = string.Empty;
    public string ControlValue { get; set; } = string.Empty;
}