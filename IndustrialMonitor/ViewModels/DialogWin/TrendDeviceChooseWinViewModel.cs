using IndustrialMonitor.Models.Models;
using System.ComponentModel;

namespace IndustrialMonitor.ViewModels.DialogWin;

/// <summary>
/// 将设备变量勾选状态转换成趋势序列。
/// </summary>
public sealed class TrendDeviceChooseWinViewModel
{
    public TrendDeviceChooseWinViewModel(
        TrendModel trend,
        IReadOnlyList<string> brushList,
        IEnumerable<DeviceModel> devices)
    {
        Trend = trend;
        BrushList = brushList;
        ChooseDevicesList = devices
            .Where(device => device.DeviceVarList.Count > 0)
            .Select(device => new TrendDeviceModel
            {
                DeviceName = device.DeviceName,
                VarList = device.DeviceVarList
                    .Where(variable => variable.VarType != "Boolean")
                    .Select(variable => CreateVariable(device, variable))
                    .ToList()
            })
            .ToList();
    }

    public TrendModel Trend { get; }
    public List<TrendDeviceModel> ChooseDevicesList { get; }
    public IReadOnlyList<string> BrushList { get; }

    private TrendDeviceVarModel CreateVariable(DeviceModel device, DeviceVarModel variable)
    {
        TrendSeriesModel? selectedSeries = Trend.Series.FirstOrDefault(series =>
            series.DeviceNum == device.DeviceNum && series.VarNum == variable.VarNum);

        var model = new TrendDeviceVarModel
        {
            IsSelected = selectedSeries != null,
            DeviceNum = device.DeviceNum,
            VarNum = variable.VarNum,
            VarName = variable.VarName,
            VarType = variable.VarType,
            ANum = selectedSeries?.ANum ?? Trend.AxisList[0].ANum,
            Color = selectedSeries?.Color ?? BrushList[Random.Shared.Next(BrushList.Count)]
        };

        model.PropertyChanged += VariablePropertyChanged;
        return model;
    }

    private void VariablePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (sender is not TrendDeviceVarModel variable)
        {
            return;
        }

        if (eventArgs.PropertyName == nameof(TrendDeviceVarModel.IsSelected))
        {
            SetSeriesSelected(variable);
            return;
        }

        if (eventArgs.PropertyName is nameof(TrendDeviceVarModel.Color) or nameof(TrendDeviceVarModel.ANum))
        {
            TrendSeriesModel? series = FindSeries(variable);
            if (series != null)
            {
                series.Color = variable.Color;
                series.ANum = variable.ANum;
            }
        }
    }

    private void SetSeriesSelected(TrendDeviceVarModel variable)
    {
        TrendSeriesModel? existing = FindSeries(variable);
        if (variable.IsSelected)
        {
            if (existing != null)
            {
                return;
            }

            Trend.Series.Add(new TrendSeriesModel
            {
                ANum = variable.ANum,
                DeviceNum = variable.DeviceNum,
                VarNum = variable.VarNum,
                Title = variable.VarName,
                Color = variable.Color
            });
        }
        else if (existing != null)
        {
            Trend.Series.Remove(existing);
        }
    }

    private TrendSeriesModel? FindSeries(TrendDeviceVarModel variable) =>
        Trend.Series.FirstOrDefault(series =>
            series.DeviceNum == variable.DeviceNum && series.VarNum == variable.VarNum);
}