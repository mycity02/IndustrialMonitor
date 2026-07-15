namespace IndustrialMonitor.Models.Models;

public sealed class TrendDeviceVarModel : BindableBase
{
    private bool _isSelected;
    private string _axisNum = string.Empty;
    private string _color = "DodgerBlue";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DeviceNum { get; set; } = string.Empty;
    public string VarNum { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string VarType { get; set; } = string.Empty;

    public string ANum
    {
        get => _axisNum;
        set => SetProperty(ref _axisNum, value);
    }

    public string Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }
}