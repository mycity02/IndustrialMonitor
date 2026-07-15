namespace IndustrialMonitor.Models;

/// <summary>
/// 登录界面使用的账号模型。
/// </summary>
public class SysUserModel
{
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}