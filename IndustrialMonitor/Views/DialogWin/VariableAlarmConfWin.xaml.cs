using System.Windows;

namespace IndustrialMonitor.Views.DialogWin;

public partial class VariableAlarmConfWin : Window
{
    public VariableAlarmConfWin()
    {
        InitializeComponent();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}