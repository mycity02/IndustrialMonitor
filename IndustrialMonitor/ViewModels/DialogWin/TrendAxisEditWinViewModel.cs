using IndustrialMonitor.Models.Models;

namespace IndustrialMonitor.ViewModels.DialogWin;

public sealed class TrendAxisEditWinViewModel
{
    public required TrendModel Trend { get; init; }
    public required IReadOnlyList<string> BrushList { get; init; }
}