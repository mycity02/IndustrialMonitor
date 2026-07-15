using Microsoft.EntityFrameworkCore;

namespace IndustrialMonitor.Data;

public sealed class DataAccess : IDataAccess
{
    public SysUserEntity? Login(string account, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return null;
        }

        using var context = new MonitorDbContext();
        return context.SysUsers
            .AsNoTracking()
            .FirstOrDefault(user => user.Account == account && user.Password == passwordHash);
    }
}