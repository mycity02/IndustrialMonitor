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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IndustrialMonitor.Views
{
    /// <summary>
    /// MainUCView.xaml 的交互逻辑
    /// </summary>
    public partial class MainUCView : UserControl
    {
        public MainUCView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 关闭该页面等于直接退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Close_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        /// <summary>
        /// 最小化窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Min_Click(object sender, RoutedEventArgs e)
        {
            Window window = new Window(); //找到所属窗体
            window.WindowState = WindowState.Minimized;
        }
    }
}
