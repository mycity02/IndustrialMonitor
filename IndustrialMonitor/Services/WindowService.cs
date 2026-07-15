using System.Windows;

namespace IndustrialMonitor.Services;

public sealed class WindowService : IWindowService
{
    public void HideActiveWindow() =>
        Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
            ?.Hide();
}