using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IndustrialMonitor.Views
{
    /// <summary>
    /// DialogOuterWin.xaml 的交互逻辑
    /// </summary>
    public partial class DialogOuterWin : Window, IDialogWindow
    {
        public DialogOuterWin()
        {
            InitializeComponent();
        }

        public IDialogResult Result { get; set; }
    }
}
