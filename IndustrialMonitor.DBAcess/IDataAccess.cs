using IndustrialMonitor.DataEntities;

namespace IndustrialMonitor.DBAcess;

public interface IDataAccess
{
    SysUserEntity? Login(string account, string passwordHash);

    List<DeviceEntity> GetDevices();
    List<DevicePropEntity> GetDeviceProps();
    List<DeviceVarEntity> GetDeviceVarList();
    List<ManualControlEntity> GetManualControlList();
    List<VarAlarmConfEntity> GetVarAlarmConfList();

    bool SaveDevice(
        List<DeviceEntity> devices,
        List<DevicePropEntity> properties,
        List<DeviceVarEntity> variables,
        List<ManualControlEntity> controls,
        List<VarAlarmConfEntity> thresholdConfigurations);

    void SaveMonitorRecords(List<MonitorRecordEntity> records);
    List<MonitorRecordEntity> GetMonitorRecords();
}