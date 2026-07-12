using Microsoft.EntityFrameworkCore;
using Platform.DataEntities;
using System.Configuration;

namespace IndustrialMonitor.DBAcess;

public class AppContext : DbContext
{
    public virtual DbSet<SysUserEntity> SysUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        string connectionString = ConfigurationManager
            .ConnectionStrings["Default"]
            .ConnectionString;

        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString));
    }
}
