namespace IndustrialMonitor.DeviceAccess.Base;

public sealed class WriteDataInfo
{
    public string StartAddr { get; set; } = string.Empty;
    public Type ValueType { get; set; } = typeof(ushort);
    public byte[] WriteBytes { get; set; } = [];
}