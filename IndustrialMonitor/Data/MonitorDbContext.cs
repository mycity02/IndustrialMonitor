using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace IndustrialMonitor.Data;

public sealed class MonitorDbContext : DbContext
{
    public DbSet<SysUserEntity> SysUsers => Set<SysUserEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        string connectionString =
            ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 0)));
    }
}