namespace IndustrialMonitor.DeviceAccess.Base;

public class ResultData<T>
{
    public bool Status { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T Data { get; set; } = default!;
}