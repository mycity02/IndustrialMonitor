using IndustrialMonitor.Models.Models;

namespace IndustrialMonitor.ViewModels;

public sealed class AlarmConfViewModel
{
    public IReadOnlyList<AlarmConfOperatorModel> OperatorList { get; } =
    [
        new() { OperatorName = "大于", OperatorSymbol = ">" },
        new() { OperatorName = "小于", OperatorSymbol = "<" },
        new() { OperatorName = "等于", OperatorSymbol = "==" },
        new() { OperatorName = "大于等于", OperatorSymbol = ">=" },
        new() { OperatorName = "小于等于", OperatorSymbol = "<=" },
        new() { OperatorName = "不等于", OperatorSymbol = "!=" }
    ];
}