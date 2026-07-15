namespace IndustrialMonitor.Models.Models;

public enum MainPage
{
    Monitor,
    Trend,
    MonitorData
}

public sealed class MenuModel
{
    public string MenuName { get; init; } = string.Empty;
    public string MenuIcon { get; init; } = string.Empty;
    public MainPage Page { get; init; }
}