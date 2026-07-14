using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace IndustrialMonitor.ViewModels
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public class LoginViewModel : BindableBase
    {
        private readonly IDataAccess _dataAccess;
        private readonly IDialogService _dialogService;
        public SysUserModel SysUser { get; set; } = new();
        public LoginViewModel(IDataAccess dataAccess, IDialogService dialogService) 
        {
            _dataAccess = dataAccess;
            _dialogService = dialogService;
        }

        #region 登录结果
        private string _loginErrorMsg;

        public string LoginErrorMsg
        {
            get => _loginErrorMsg;
            set
            {
                SetProperty(ref _loginErrorMsg, value);
            }
        }
        #endregion

        #region 登录
        public DelegateCommand<Window> LoginCommand => new DelegateCommand<Window>(LoginSystem);

        private void LoginSystem(Window loginView)
        {
            if (string.IsNullOrEmpty(SysUser.Account) || string.IsNullOrEmpty(SysUser.Password))
            {
                LoginErrorMsg = "账号和密码不能为空";
            }

            string md5Pwd = Md5Hepler.ComputeMD5Hash(SysUser.Password);
            var loginUser = _dataAccess.Login(SysUser.Account, md5Pwd);
            if (loginUser == null)
            {
                LoginErrorMsg = "账号或密码错误";
                return;
            }

            // 登录成功隐藏登录窗体
            loginView.Hide();

            #region 将登录信息传递给主界面
            DialogParameters dialogParameters = new DialogParameters();

            dialogParameters.Add("LoginUser", new SysUserModel
            {
                UserId = loginUser.UserId,
                RealName = loginUser.RealName,
                Department = loginUser.Department,
                Account = loginUser.Account,
                IsAdmin = loginUser.IsAdmin
            });

            _dialogService.ShowDialog("MainUCView", dialogParameters);
            #endregion
        }
        #endregion
    }
}
