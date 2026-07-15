using System.Windows;
using System.Windows.Controls;

namespace IndustrialMonitor.Views;

public partial class MainUCView : UserControl
{
    public MainUCView()
    {
        InitializeComponent();
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    public void Min_Click(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this).WindowState = WindowState.Minimized;
    }
}