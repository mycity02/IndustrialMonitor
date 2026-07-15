using IndustrialMonitor.Data;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
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

        containerRegistry.RegisterDialog<MainUCView>();
        containerRegistry.RegisterDialogWindow<DialogOuterWin>();
    }

}