using IndustrialMonitor.DataEntities;

namespace IndustrialMonitor.DBAcess;

public sealed class DataAccess : IDataAccess
{
    AppContext appContext = new AppContext();

    public SysUserEntity? Login(string account, string pwd)
    {
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(pwd))
            return null;


        return appContext.SysUsers.FirstOrDefault(u => u.Account == account && u.Password == pwd);
    }

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="newPwd">新密码</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public int ResetPassword(int userId, string newPwd)
    {
        SysUserEntity sysUserEntity = appContext.SysUsers.FirstOrDefault(u => u.UserId == userId);
        if (sysUserEntity != null)
        {
            sysUserEntity.Password = newPwd;

            return appContext.SaveChanges();
        }
        return 0;
    }

    /// <summary>
    /// 取出所有的组件
    /// </summary>
    /// <returns></returns>
    public List<ComponentEntity> GetComponents()
    {
        return appContext.Components.ToList();
    }

    /// <summary>
    /// 获取所有设备
    /// </summary>
    /// <returns></returns>
    public List<DeviceEntity> GetDevices()
    {
        //ChangeTracker 作用实体对象的状态变化(增删改、没有改变)

        appContext.ChangeTracker.Clear();//不跟踪实体对象
        return appContext.Devices.ToList();
    }

    /// <summary>
    /// 保存设备
    /// </summary>
    /// <param name="deviceList">需要保存的设备集合</param>
    /// <param name="devicePropList">设备属性集合</param>
    /// <param name="deviceVarList">设备变量集合</param>
    /// <param name="manualControlList">手动控制集合</param>
    /// <param name="varAlarmConfList">变量报警配置集合</param>
    /// <returns>保存是否成功</returns>
    public bool SaveDevice(List<DeviceEntity> deviceList, List<DevicePropEntity> devicePropList = null, List<DeviceVarEntity> deviceVarList = null, List<ManualControlEntity> manualControlList = null, List<VarAlarmConfEntity> varAlarmConfList = null)
    {
        try
        {
            #region 保存设备基本信息
            //先删除数据库所有数据，然后再添加deviceList
            appContext.Devices.RemoveRange(appContext.Devices);//删除所有

            appContext.Devices.AddRange(deviceList);
            #endregion

            #region 保存设备属性
            if (devicePropList != null)//如果设备属性不是null
            {
                //1、删除所有设备属性数据
                appContext.DeviceProps.RemoveRange(appContext.DeviceProps);
                //2、保存传过来的数据
                appContext.DeviceProps.AddRange(devicePropList);
            }
            #endregion

            #region 保存设备变量
            if (deviceVarList != null)//如果设备变量不是null
            {
                //1、删除所有设备变量数据
                appContext.DeviceVars.RemoveRange(appContext.DeviceVars);
                //2、保存传过来的数据
                appContext.DeviceVars.AddRange(deviceVarList);
            }
            #endregion

            #region 保存手动空置
            if (manualControlList != null)
            {
                //1、删除
                appContext.ManualControls.RemoveRange(appContext.ManualControls);
                //2、保存
                appContext.ManualControls.AddRange(manualControlList);
            }
            #endregion

            #region 保存变量报警配置
            if (varAlarmConfList != null)
            {
                //1、删除
                appContext.VarAlarmConfs.RemoveRange(appContext.VarAlarmConfs);
                //2、保存
                appContext.VarAlarmConfs.AddRange(varAlarmConfList);
            }
            #endregion

            int row = appContext.SaveChanges();

            //return row > 0;
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取所有属性
    /// </summary>
    /// <returns></returns>
    public List<PropEntity> GetPropList()
    {
        return appContext.Properties.ToList();
    }

    /// <summary>
    /// 获取所有设备属性
    /// </summary>
    /// <returns></returns>
    public List<DevicePropEntity> GetDeviceProps()
    {
        return appContext.DeviceProps.ToList();
    }

    /// <summary>
    /// 获取所有设备变量
    /// </summary>
    /// <returns></returns>
    public List<DeviceVarEntity> GetDeviceVarList()
    {
        return appContext.DeviceVars.ToList();
    }

    /// <summary>
    /// 获取所有手动控制
    /// </summary>
    /// <returns></returns>
    public List<ManualControlEntity> GetManualControlList()
    {
        return appContext.ManualControls.ToList();
    }

    /// <summary>
    /// 获取所有变量报警配置
    /// </summary>
    /// <returns></returns>
    public List<VarAlarmConfEntity> GetVarAlarmConfList()
    {
        return appContext.VarAlarmConfs.ToList();
    }

    /// <summary>
    /// 保存设备报警信息
    /// </summary>
    /// <param name="deviceAlarmEntity">设备报警信息</param>
    public void SaveDeviceAlarm(DeviceAlarmEntity deviceAlarmEntity)
    {
        appContext.DeviceAlarms.Add(deviceAlarmEntity);

        appContext.SaveChanges();
    }

    /// <summary>
    /// 记录监控数据
    /// </summary>
    /// <param name="monitorRecordList">监控数据集合</param>
    public void SaveMonitorRecords(List<MonitorRecordEntity> monitorRecordList)
    {
        appContext.MonitorRecords.AddRange(monitorRecordList);

        appContext.SaveChanges();
    }

    /// <summary>
    /// 获取七日耗电量
    /// </summary>
    /// <returns></returns>
    public List<SevenPowerEntity> GetSevenPowerList()
    {
        return appContext.SevenPowers.ToList();
    }

    /// <summary>
    /// 获取七日耗能-用气量
    /// </summary>
    /// <returns></returns>
    public List<SevenAirEntity> GetSevenAirList()
    {
        return appContext.SevenAirs.ToList();
    }

    /// <summary>
    /// 获取七日耗能-泄露量
    /// </summary>
    /// <returns></returns>
    public List<SevenLeakEntity> GetSevenLeakList()
    {
        return appContext.SevenLeaks.ToList();
    }

    /// <summary>
    /// 获取设备提醒
    /// </summary>
    /// <returns></returns>
    public List<DeviceWarningEntity> GetDeviceWarnings()
    {
        return appContext.DeviceWarnings.ToList();
    }

    /// <summary>
    /// 获取用气量排行
    /// </summary>
    /// <returns></returns>
    public List<AirRankingEntity> GetAirRankings()
    {
        return appContext.AirRankings.ToList();
    }

    /// <summary>
    /// 保存趋势信息
    /// </summary>
    /// <param name="trendList">趋势集合</param>
    /// <param name="axisList">纵轴集合</param>
    /// <param name="sectionList">预警集合</param>
    /// <param name="seriesList">序列集合</param>
    public void SaveTrend(List<TrendEntity> trendList, List<TrendAxisEntity>? axisList = null, List<TrendSectionEntity>? sectionList = null, List<TrendSeriesEntity>? seriesList = null)
    {
        //保存趋势
        appContext.Trends.RemoveRange(appContext.Trends);
        appContext.Trends.AddRange(trendList);

        //保存纵轴信息
        if (axisList != null)
        {
            appContext.TrendAxises.RemoveRange(appContext.TrendAxises);
            appContext.TrendAxises.AddRange(axisList);
        }

        //保存预警线信息
        if (sectionList != null)
        {
            appContext.TrendSections.RemoveRange(appContext.TrendSections);
            appContext.TrendSections.AddRange(sectionList);
        }

        //保存图表序列
        if (seriesList != null)
        {
            appContext.TrendSerieses.RemoveRange(appContext.TrendSerieses);
            appContext.TrendSerieses.AddRange(seriesList);
        }

        appContext.SaveChanges();
    }

    /// <summary>
    /// 获取所有趋势
    /// </summary>
    public List<TrendEntity> GetTrends()
    {
        return appContext.Trends.ToList();
    }

    /// <summary>
    /// 获取所有纵轴
    /// </summary>
    public List<TrendAxisEntity> GetTrendAxises()
    {
        return appContext.TrendAxises.ToList();
    }

    /// <summary>
    /// 获取所有预警
    /// </summary>
    public List<TrendSectionEntity> GetTrendSections()
    {
        return appContext.TrendSections.ToList();
    }

    /// <summary>
    /// 获取图表序列
    /// </summary>
    public List<TrendSeriesEntity> GetTrendSerieses()
    {
        return appContext.TrendSerieses.ToList();
    }

    /// <summary>
    /// 根据关键词查询报警信息
    /// </summary>
    /// <param name="keyWord">关键词</param>
    /// <returns></returns>
    public List<DeviceAlarmEntity> GetDeviceAlarmList(string keyWord)
    {
        if (string.IsNullOrEmpty(keyWord))
        {
            return appContext.DeviceAlarms.OrderByDescending(a => a.RecordTime).ToList();//根据记录时间降序
        }
        else
        {
            return appContext.DeviceAlarms.Where(a => a.DeviceNum.Contains(keyWord) || a.DeviceName.Contains(keyWord) || a.VarNum.Contains(keyWord) || a.VarName.Contains(keyWord) || a.AlarmContent.Contains(keyWord)).OrderByDescending(a => a.RecordTime).ToList();
        }
    }

    /// <summary>
    /// 处理报警
    /// </summary>
    /// <param name="alarNum">报警编号</param>
    /// <param name="solveTime">解决时间</param>
    public void HandleAlarmState(string alarNum, DateTime solveTime)
    {
        var alarm = appContext.DeviceAlarms.FirstOrDefault(t => t.AlarmNum == alarNum);
        if (alarm != null)
        {
            alarm.State = 1;
            alarm.SolveTime = solveTime;
            appContext.SaveChanges();
        }
    }

    /// <summary>
    /// 获取所有监控数据
    /// </summary>
    /// <returns></returns>
    public List<MonitorRecordEntity> GetMonitorRecords()
    {
        return appContext.MonitorRecords.ToList();
    }

    /// <summary>
    /// 获取所有监控配置
    /// </summary>
    /// <returns></returns>
    public List<MonitorSettingEntity> GetMonitorSettings()
    {
        return appContext.MonitorSettings.ToList();
    }

    /// <summary>
    /// 保存监测配置
    /// </summary>
    /// <param name="monitorSettingList"></param>
    public void SaveMonitorSets(List<MonitorSettingEntity> monitorSettingList)
    {
        if (monitorSettingList != null)
        {
            appContext.MonitorSettings.RemoveRange(appContext.MonitorSettings);
            appContext.MonitorSettings.AddRange(monitorSettingList);
            appContext.SaveChanges();
        }
    }

    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns></returns>
    public List<SysUserEntity> GetSysUserList()
    {
        return appContext.SysUsers.ToList();
    }

    /// <summary>
    /// 保存用户配置
    /// </summary>
    /// <param name="userList"></param>
    public void SaveUserSets(List<SysUserEntity> userList)
    {
        if (userList != null)
        {
            appContext.SysUsers.RemoveRange(appContext.SysUsers);
            appContext.SysUsers.AddRange(userList);
            appContext.SaveChanges();
        }
    }
}
