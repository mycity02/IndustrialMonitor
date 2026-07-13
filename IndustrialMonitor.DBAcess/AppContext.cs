using Microsoft.EntityFrameworkCore;
using IndustrialMonitor.DataEntities;
using System.Configuration;

namespace IndustrialMonitor.DBAcess;

public class AppContext : DbContext
{
    public virtual DbSet<SysUserEntity> SysUsers { get; set; }

    /// <summary>
    /// 组件表
    /// </summary>
    public virtual DbSet<ComponentEntity> Components { get; set; }

    /// <summary>
    /// 设备表
    /// </summary>
    public virtual DbSet<DeviceEntity> Devices { get; set; }

    /// <summary>
    /// 组件属性表
    /// </summary>
    public virtual DbSet<PropEntity> Properties { get; set; }

    /// <summary>
    /// 设备属性表
    /// </summary>
    public virtual DbSet<DevicePropEntity> DeviceProps { get; set; }

    /// <summary>
    /// 设备变量表
    /// </summary>
    public virtual DbSet<DeviceVarEntity> DeviceVars { get; set; }

    /// <summary>
    /// 手动控制表
    /// </summary>
    public virtual DbSet<ManualControlEntity> ManualControls { get; set; }

    /// <summary>
    /// 变量报警配置表
    /// </summary>
    public virtual DbSet<VarAlarmConfEntity> VarAlarmConfs { get; set; }

    /// <summary>
    /// 设备报警信息表
    /// </summary>
    public virtual DbSet<DeviceAlarmEntity> DeviceAlarms { get; set; }

    /// <summary>
    /// 监控数据表
    /// </summary>
    public virtual DbSet<MonitorRecordEntity> MonitorRecords { get; set; }

    /// <summary>
    /// 近七日能耗电量
    /// </summary>
    public virtual DbSet<SevenPowerEntity> SevenPowers { get; set; }

    /// <summary>
    /// 近七日能耗用气量
    /// </summary>
    public virtual DbSet<SevenAirEntity> SevenAirs { get; set; }

    /// <summary>
    /// 近七日能耗泄露量
    /// </summary>
    public virtual DbSet<SevenLeakEntity> SevenLeaks { get; set; }

    /// <summary>
    /// 设备提醒表
    /// </summary>
    public virtual DbSet<DeviceWarningEntity> DeviceWarnings { get; set; }

    /// <summary>
    /// 用气排行榜
    /// </summary>
    public virtual DbSet<AirRankingEntity> AirRankings { get; set; }

    /// <summary>
    /// 趋势图表
    /// </summary>
    public virtual DbSet<TrendEntity> Trends { get; set; }

    /// <summary>
    /// 趋势坐标轴
    /// </summary>
    public virtual DbSet<TrendAxisEntity> TrendAxises { get; set; }

    /// <summary>
    /// 趋势预警线段
    /// </summary>
    public virtual DbSet<TrendSectionEntity> TrendSections { get; set; }

    /// <summary>
    /// 图表序列
    /// </summary>
    public virtual DbSet<TrendSeriesEntity> TrendSerieses { get; set; }

    /// <summary>
    /// 监控配置表
    /// </summary>
    public virtual DbSet<MonitorSettingEntity> MonitorSettings { get; set; }

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
