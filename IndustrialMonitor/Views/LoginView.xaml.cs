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
    /// LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 直接关闭登录页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
