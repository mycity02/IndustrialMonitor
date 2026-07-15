namespace IndustrialMonitor.DeviceAccess.Base;

public sealed class VariableProp
{
    public string VarNum { get; set; } = string.Empty;
    public string VarAddr { get; set; } = string.Empty;
    public Type ValueType { get; set; } = typeof(ushort);
    public int ReadByteCount { get; set; }
    public byte[] ReadBytes { get; set; } = [];
}