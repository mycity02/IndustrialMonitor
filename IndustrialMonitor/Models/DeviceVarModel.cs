namespace IndustrialMonitor.Models;

public sealed class DeviceVarModel : BindableBase
{
    private object _readValue = 0;

    public string VarNum { get; set; } = string.Empty;
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string VarAddress { get; set; } = string.Empty;
    public string VarType { get; set; } = "UInt16";

    public object ReadValue
    {
        get => _readValue;
        set => SetProperty(ref _readValue, value);
    }
}