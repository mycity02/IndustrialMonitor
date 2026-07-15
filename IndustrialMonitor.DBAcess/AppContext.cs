using IndustrialMonitor.DataEntities;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace IndustrialMonitor.DBAcess;

public class AppContext : DbContext
{
    public DbSet<SysUserEntity> SysUsers { get; set; } = null!;
    public DbSet<DeviceEntity> Devices { get; set; } = null!;
    public DbSet<DevicePropEntity> DeviceProps { get; set; } = null!;
    public DbSet<DeviceVarEntity> DeviceVars { get; set; } = null!;
    public DbSet<ManualControlEntity> ManualControls { get; set; } = null!;
    public DbSet<VarAlarmConfEntity> VarAlarmConfs { get; set; } = null!;
    public DbSet<MonitorRecordEntity> MonitorRecords { get; set; } = null!;
    public DbSet<TrendEntity> Trends { get; set; } = null!;
    public DbSet<TrendAxisEntity> TrendAxises { get; set; } = null!;
    public DbSet<TrendSectionEntity> TrendSections { get; set; } = null!;
    public DbSet<TrendSeriesEntity> TrendSerieses { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        string connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
    }
}