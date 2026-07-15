using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Views.DeviceWin;
using IndustrialMonitor.Views.DialogWin;
using Microsoft.Win32;
using System.Windows;

namespace IndustrialMonitor.Services;

public sealed class WindowService : IWindowService
{
    public bool ShowDeviceManager() => ShowDialog(new DeviceManageWin()) == true;

    public void ShowVariableThresholds(DeviceModel device) =>
        ShowDialog(new VariableAlarmConfWin { DataContext = device });

    public string? ChooseSaveFile(string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            RestoreDirectory = true,
            FileName = defaultFileName
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void CloseActiveDialog(bool result)
    {
        Window? window = GetActiveWindow();
        if (window != null)
        {
            window.DialogResult = result;
        }
    }

    public void HideActiveWindow() => GetActiveWindow()?.Hide();

    public void ShowInformation(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    private static bool? ShowDialog(Window dialog)
    {
        Window? owner = GetActiveWindow(dialog);
        if (owner != null)
        {
            dialog.Owner = owner;
        }
        return dialog.ShowDialog();
    }

    private static Window? GetActiveWindow(Window? excludedWindow = null) =>
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive && window != excludedWindow);
}