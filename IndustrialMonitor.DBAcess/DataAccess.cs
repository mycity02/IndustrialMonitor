using Platform.DataEntities;

namespace IndustrialMonitor.DBAcess;

public sealed class DataAccess : IDataAccess
{
    public SysUserEntity? Login(string account, string pwd)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(pwd))
            return null;

        using var appContext = new AppContext();

        return appContext.SysUsers.FirstOrDefault(u => u.Account == account && u.Password == pwd);
    }
}
