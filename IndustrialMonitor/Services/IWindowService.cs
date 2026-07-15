using IndustrialMonitor.Models.Models;
using IndustrialMonitor.ViewModels.DialogWin;

namespace IndustrialMonitor.Services;

public interface IWindowService
{
    bool ShowDeviceManager();
    void ShowVariableThresholds(DeviceModel device);
    void ShowTrendAxisEditor(TrendAxisEditWinViewModel viewModel);
    void ShowTrendVariableChooser(TrendDeviceChooseWinViewModel viewModel);
    string? ChooseSaveFile(string title, string filter, string defaultFileName);
    void CloseActiveDialog(bool result);
    void HideActiveWindow();
    void ShowInformation(string message, string title);
    void ShowError(string message, string title);
}