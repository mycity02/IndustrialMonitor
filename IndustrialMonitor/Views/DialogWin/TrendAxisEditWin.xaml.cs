using System.Windows;

namespace IndustrialMonitor.Views.DialogWin;

public partial class TrendAxisEditWin : Window
{
    public TrendAxisEditWin()
    {
        InitializeComponent();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}