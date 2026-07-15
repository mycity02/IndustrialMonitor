using IndustrialMonitor.DataEntities;
using Microsoft.EntityFrameworkCore;

namespace IndustrialMonitor.DBAcess;

public sealed class DataAccess : IDataAccess
{
    private readonly AppContext _context = new();

    public SysUserEntity? Login(string account, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return null;
        }

        return _context.SysUsers
            .AsNoTracking()
            .FirstOrDefault(user => user.Account == account && user.Password == passwordHash);
    }

    public List<DeviceEntity> GetDevices() => _context.Devices.AsNoTracking().ToList();

    public List<DevicePropEntity> GetDeviceProps() => _context.DeviceProps.AsNoTracking().ToList();

    public List<DeviceVarEntity> GetDeviceVarList() => _context.DeviceVars.AsNoTracking().ToList();

    public List<ManualControlEntity> GetManualControlList() =>
        _context.ManualControls.AsNoTracking().ToList();

    public List<VarAlarmConfEntity> GetVarAlarmConfList() =>
        _context.VarAlarmConfs.AsNoTracking().ToList();

    public bool SaveDevice(
        List<DeviceEntity> devices,
        List<DevicePropEntity> properties,
        List<DeviceVarEntity> variables,
        List<ManualControlEntity> controls,
        List<VarAlarmConfEntity> thresholdConfigurations)
    {
        try
        {
            _context.ChangeTracker.Clear();

            _context.VarAlarmConfs.RemoveRange(_context.VarAlarmConfs);
            _context.ManualControls.RemoveRange(_context.ManualControls);
            _context.DeviceVars.RemoveRange(_context.DeviceVars);
            _context.DeviceProps.RemoveRange(_context.DeviceProps);
            _context.Devices.RemoveRange(_context.Devices);

            _context.Devices.AddRange(devices);
            _context.DeviceProps.AddRange(properties);
            _context.DeviceVars.AddRange(variables);
            _context.ManualControls.AddRange(controls);
            _context.VarAlarmConfs.AddRange(thresholdConfigurations);

            _context.SaveChanges();
            return true;
        }
        catch (Exception)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public void SaveMonitorRecords(List<MonitorRecordEntity> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        _context.MonitorRecords.AddRange(records);
        _context.SaveChanges();
    }

    public List<MonitorRecordEntity> GetMonitorRecords() =>
        _context.MonitorRecords.AsNoTracking().ToList();
}