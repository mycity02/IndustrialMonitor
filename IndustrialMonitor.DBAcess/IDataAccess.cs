using Platform.DataEntities;

namespace IndustrialMonitor.DBAcess;

public interface IDataAccess
{
    /// <summary>
    /// 登录查询
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="pwd">密码</param>
    /// <returns></returns>
    SysUserEntity? Login(string account, string pwd);
}
