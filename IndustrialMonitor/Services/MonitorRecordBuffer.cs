using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DBAcess;

namespace IndustrialMonitor.Services;

/// <summary>
/// 批量缓存监控记录，减少每次采集都写数据库的开销。
/// </summary>
public static class MonitorRecordBuffer
{
    private const int BatchSize = 200;
    private static readonly object SyncRoot = new();
    private static readonly List<MonitorRecordEntity> Records = [];

    public static void Add(IDataAccess dataAccess, MonitorRecordEntity record)
    {
        lock (SyncRoot)
        {
            Records.Add(record);
            if (Records.Count >= BatchSize)
            {
                FlushCore(dataAccess);
            }
        }
    }

    public static void Flush(IDataAccess dataAccess)
    {
        lock (SyncRoot)
        {
            FlushCore(dataAccess);
        }
    }

    private static void FlushCore(IDataAccess dataAccess)
    {
        if (Records.Count == 0)
        {
            return;
        }

        dataAccess.SaveMonitorRecords(Records);
        Records.Clear();
    }
}