using IndustrialMonitor.Helper;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Views.DialogWin;
using Platform.Helper;
using Platform.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Controls;

namespace IndustrialMonitor.ViewModels
{
    public class MainUCViewModel : BindableBase, IDialogAware
    {
        public SysUserModel LoginUserModel { get; set; }
        public string Title { get; set; } = "主界面";
        public DialogCloseListener RequestClose { get; } = new();

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 接收对话框传递过来的参数
            LoginUserModel = parameters.GetValue<SysUserModel>("LoginUser");

            InitMenu();
        }



        #region 左侧菜单
        private UserControl _viewFunc;//右侧功能对象

        public UserControl ViewFunc
        {
            get { return _viewFunc; }
            set
            {
                SetProperty(ref _viewFunc, value);
            }
        }

        private List<MenuModel> _menuList = new();

        public List<MenuModel> MenuList
        {
            get => _menuList;
            set => SetProperty(ref _menuList, value);
        }

        /// <summary>
        /// 左侧菜单初始化
        /// </summary>
        private void InitMenu()
        {
            //_loggerService.Info("左侧初始化菜单123");
            MenuList =
                [
                    new MenuModel{  IsSelected=true, MenuName="监控", MenuIcon="\ue639",TargetFunc="MonitorUC"},
                    new MenuModel{  MenuName="趋势", MenuIcon="\ue61a",TargetFunc="TrendUC"},
                    new MenuModel{  MenuName="报警", MenuIcon="\ue60b",TargetFunc="AlarmUC"},
                    new MenuModel{  MenuName="报表", MenuIcon="\ue703",TargetFunc="ReportUC"},
                    new MenuModel{  MenuName="配置", MenuIcon="\ue60f",TargetFunc="SettingsUC"},
                ];

            DoSwitchFunc(MenuList[0]); //默认第一个功能
        }

        //切换功能命令
        public DelegateCommand<MenuModel> SwitchFuncCommand => new DelegateCommand<MenuModel>(DoSwitchFunc);

        /// <summary>
        /// 切换功能
        /// </summary>
        /// <param name="menuModel"></param>
        private void DoSwitchFunc(MenuModel menuModel)
        {
            //1、只针对该功能实现
            //2、代码优化（封装） 后面 弹出窗体，在窗体上面有操作 dialogresult

            //如果不是管理员，并且不是监控功能
            if (!LoginUserModel.IsAdmin && menuModel.TargetFunc != "MonitorUC")
            {
                MenuList[0].IsSelected = true;//没有权限，默认还是选择监控功能
                RightRemindWin rightRemindWin = new RightRemindWin();
                bool? dialogResult = rightRemindWin.ShowDialog();
                //if (ActionHelper.ExecuteAndResult<object>("ShowRight", null))
                //{
                //    //重新登录
                //    DoReLogin();
                //}
                DoReLogin();
            }
            else
            {
                //和之前点击的功能是一样的，就不用再执行
                if (ViewFunc != null && ViewFunc.GetType().Name == menuModel.TargetFunc)
                {
                    return;
                }
                Type type = Assembly.Load("IndustrialMonitor").GetType("IndustrialMonitor.Views.FunctionUC." + menuModel.TargetFunc)!;
                ViewFunc = Activator.CreateInstance(type) as UserControl;
            }
        }
        #endregion

        /// <summary>
        /// 重新登录
        /// </summary>
        private void DoReLogin()
        {
            Process.Start("IndustrialMonitor.exe");
            App.Current.Shutdown();
        }
    }
}
