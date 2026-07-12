using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace IndustrialMonitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            // 启动页面
            return Container.Resolve<LoginView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册数据访问
            containerRegistry.Register<IDataAccess, DataAccess>();
            // 注册主窗体对话框
            containerRegistry.RegisterDialog<MainUCView>();
            
            containerRegistry.RegisterDialogWindow<DialogOuterWin>();
         }
    }

}
