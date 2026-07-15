using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Helper;
using IndustrialMonitor.Logger;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

public sealed class LoginViewModel : BindableBase
{
    private readonly IDataAccess _dataAccess;
    private readonly IDialogService _dialogService;
    private readonly ILoggerService<LoginViewModel> _logger;
    private readonly IWindowService _windowService;

    private string _loginErrorMsg = string.Empty;

    public SysUserModel SysUser { get; } = new();

    public string LoginErrorMsg
    {
        get => _loginErrorMsg;
        private set => SetProperty(ref _loginErrorMsg, value);
    }

    public DelegateCommand LoginCommand { get; }

    public LoginViewModel(
        IDataAccess dataAccess,
        IDialogService dialogService,
        ILoggerService<LoginViewModel> logger,
        IWindowService windowService)
    {
        _dataAccess = dataAccess;
        _dialogService = dialogService;
        _logger = logger;
        _windowService = windowService;
        LoginCommand = new DelegateCommand(Login);
    }

    private void Login()
    {
        if (string.IsNullOrWhiteSpace(SysUser.Account) || string.IsNullOrWhiteSpace(SysUser.Password))
        {
            LoginErrorMsg = "账号和密码不能为空";
            _logger.Warn("登录失败：账号或密码为空");
            return;
        }

        string passwordHash = Md5Hepler.ComputeMD5Hash(SysUser.Password);
        var loginUser = _dataAccess.Login(SysUser.Account, passwordHash);
        if (loginUser == null)
        {
            LoginErrorMsg = "账号或密码错误";
            _logger.Warn($"登录失败：账号 {SysUser.Account}");
            return;
        }

        _logger.Info($"用户 {loginUser.Account} 登录成功");
        _windowService.HideActiveWindow();

        var parameters = new DialogParameters
        {
            { "LoginUser", new SysUserModel { Account = loginUser.Account } }
        };
        _dialogService.ShowDialog("MainUCView", parameters);
    }
}