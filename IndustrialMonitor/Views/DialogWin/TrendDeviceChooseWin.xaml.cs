using System.Windows;

namespace IndustrialMonitor.Views.DialogWin;

public partial class TrendDeviceChooseWin : Window
{
    public TrendDeviceChooseWin()
    {
        InitializeComponent();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}