using System.Collections.ObjectModel;

namespace IndustrialMonitor.Models.Models;

public sealed class DeviceVarModel : BindableBase
{
    private object _readValue = 0;

    public DeviceVarModel()
    {
        AddConfCommand = new DelegateCommand(() =>
            VarAlarmConfList.Add(new VarAlarmConfModel
            {
                Operator = "<",
                ConfNum = $"C{DateTime.Now:yyyyMMddHHmmssFFF}"
            }));
        DeleteConfCommand = new DelegateCommand<VarAlarmConfModel>(configuration =>
        {
            if (configuration != null)
            {
                VarAlarmConfList.Remove(configuration);
            }
        });
    }

    public string VarNum { get; set; } = string.Empty;
    public string DeviceNum { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string VarName { get; set; } = string.Empty;
    public string VarAddress { get; set; } = string.Empty;

    public object ReadValue
    {
        get => _readValue;
        set => SetProperty(ref _readValue, value);
    }

    public double Offset { get; set; }
    public double Modulus { get; set; } = 1;
    public string VarType { get; set; } = "UInt16";
    public ObservableCollection<VarAlarmConfModel> VarAlarmConfList { get; set; } = [];

    public DelegateCommand AddConfCommand { get; }
    public DelegateCommand<VarAlarmConfModel> DeleteConfCommand { get; }
}