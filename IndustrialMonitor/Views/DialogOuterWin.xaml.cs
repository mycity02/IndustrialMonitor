using System.Windows;

namespace IndustrialMonitor.Views;

public partial class DialogOuterWin : Window, IDialogWindow
{
    public DialogOuterWin()
    {
        InitializeComponent();
    }

    public IDialogResult Result { get; set; } = null!;
}