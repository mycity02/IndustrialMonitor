namespace IndustrialMonitor.Data;

public interface IDataAccess
{
    SysUserEntity? Login(string account, string passwordHash);
}