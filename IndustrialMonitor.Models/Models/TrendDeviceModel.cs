namespace IndustrialMonitor.Models.Models;

public sealed class TrendDeviceModel
{
    public string DeviceName { get; set; } = string.Empty;
    public List<TrendDeviceVarModel> VarList { get; set; } = [];
}