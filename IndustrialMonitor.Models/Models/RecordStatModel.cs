namespace IndustrialMonitor.Models.Models;

/// <summary>
/// 按设备变量汇总后的历史监控数据。
/// </summary>
public class RecordStatModel
{
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string AvgValue { get; set; } = string.Empty;
    public decimal MaxValue { get; set; }
    public decimal MinValue { get; set; }
    public string LastTime { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}