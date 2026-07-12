using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Models.Models;
using Platform.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace IndustrialMonitor.ViewModels
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public class LoginViewModel : BindableBase
    {
        private readonly IDataAccess _dataAccess;
        public LoginViewModel(IDataAccess dataAccess) 
        {
            _dataAccess = dataAccess;
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
        public DelegateCommand LoginCommand => new DelegateCommand(LoginSystem);

        private void LoginSystem()
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
            }
        }
        #endregion
    }
}
