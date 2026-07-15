using System.Windows;

namespace IndustrialMonitor.Views;

public partial class LoginView : Window
{
    public LoginView()
    {
        InitializeComponent();
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}