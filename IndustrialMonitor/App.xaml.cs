using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Logger;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using IndustrialMonitor.ViewModels.FunctionUC;
using IndustrialMonitor.Views;
using System.Windows;

namespace IndustrialMonitor;

public partial class App : PrismApplication
{
    protected override Window CreateShell() => Container.Resolve<LoginView>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IDataAccess, DataAccess>();
        containerRegistry.RegisterSingleton<IWindowService, WindowService>();
        containerRegistry.RegisterSingleton<MainUCViewModel>();
        containerRegistry.RegisterSingleton<TrendUCViewModel>();
        containerRegistry.Register(typeof(ILoggerService<>), typeof(NLogLoggerService<>));

        containerRegistry.RegisterDialog<MainUCView>();
        containerRegistry.RegisterDialogWindow<DialogOuterWin>();
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        try
        {
            MonitorRecordBuffer.Flush(Container.Resolve<IDataAccess>());
        }
        finally
        {
            base.OnExit(eventArgs);
        }
    }
}