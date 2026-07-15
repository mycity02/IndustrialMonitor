using IndustrialMonitor.DBAcess;
using IndustrialMonitor.Logger;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.ObjectModel;
using System.IO;

namespace IndustrialMonitor.ViewModels.FunctionUC;

/// <summary>
/// 将历史采集记录按“设备 + 变量”汇总，并支持导出 Excel。
/// </summary>
public sealed class MonitorDataUCViewModel : BindableBase
{
    private static readonly (string Header, Func<RecordStatModel, string> Value)[] ExportColumns =
    [
        ("设备编号", row => row.DeviceNum),
        ("设备名称", row => row.DeviceName),
        ("变量编号", row => row.VarNum),
        ("变量名称", row => row.VarName),
        ("平均值", row => row.AvgValue),
        ("最大值", row => row.MaxValue.ToString()),
        ("最小值", row => row.MinValue.ToString()),
        ("记录次数", row => row.RecordCount.ToString()),
        ("最后记录时间", row => row.LastTime)
    ];

    private readonly IDataAccess _dataAccess;
    private readonly ILoggerService<MonitorDataUCViewModel> _logger;
    private readonly IWindowService _windowService;
    private ObservableCollection<RecordStatModel> _allStatList = [];

    public MonitorDataUCViewModel(
        IDataAccess dataAccess,
        ILoggerService<MonitorDataUCViewModel> logger,
        IWindowService windowService)
    {
        _dataAccess = dataAccess;
        _logger = logger;
        _windowService = windowService;
        RefreshCommand = new DelegateCommand(Refresh);
        ExportCommand = new DelegateCommand(Export);
        Refresh();
    }

    public ObservableCollection<RecordStatModel> AllStatList
    {
        get => _allStatList;
        private set => SetProperty(ref _allStatList, value);
    }

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand ExportCommand { get; }

    private void Refresh()
    {
        var statistics = _dataAccess.GetMonitorRecords()
            .GroupBy(record => new
            {
                record.DeviceNum,
                record.DeviceName,
                record.VarNum,
                record.VarName
            })
            .Select(group => new RecordStatModel
            {
                DeviceNum = group.Key.DeviceNum,
                DeviceName = group.Key.DeviceName,
                VarNum = group.Key.VarNum,
                VarName = group.Key.VarName,
                LastTime = group.Max(item => item.RecordTime).ToString("yyyy-MM-dd HH:mm:ss"),
                AvgValue = group.Average(item => item.RecordValue).ToString("F2"),
                MaxValue = group.Max(item => item.RecordValue),
                MinValue = group.Min(item => item.RecordValue),
                RecordCount = group.Count()
            });

        AllStatList = new ObservableCollection<RecordStatModel>(statistics);
    }

    private void Export()
    {
        string? filePath = _windowService.ChooseSaveFile(
            "导出监控数据",
            "Excel 文件 (*.xlsx)|*.xlsx",
            $"监控数据-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");

        if (filePath == null)
        {
            return;
        }

        try
        {
            ExportToXlsx(filePath);
            _windowService.ShowInformation("监控数据已导出", "监控数据");
        }
        catch (Exception exception)
        {
            _logger.Error("导出监控数据失败", exception);
            _windowService.ShowError("导出失败，请检查保存路径或日志。", "监控数据");
        }
    }

    private void ExportToXlsx(string filePath)
    {
        IWorkbook workbook = new XSSFWorkbook();
        try
        {
            ISheet sheet = workbook.CreateSheet("监控数据");
            ICellStyle headerStyle = CreateCellStyle(workbook, bold: true);
            ICellStyle dataStyle = CreateCellStyle(workbook, bold: false);

            IRow headerRow = sheet.CreateRow(0);
            for (int columnIndex = 0; columnIndex < ExportColumns.Length; columnIndex++)
            {
                ICell cell = headerRow.CreateCell(columnIndex);
                cell.SetCellValue(ExportColumns[columnIndex].Header);
                cell.CellStyle = headerStyle;
            }

            for (int rowIndex = 0; rowIndex < AllStatList.Count; rowIndex++)
            {
                IRow row = sheet.CreateRow(rowIndex + 1);
                for (int columnIndex = 0; columnIndex < ExportColumns.Length; columnIndex++)
                {
                    ICell cell = row.CreateCell(columnIndex);
                    cell.SetCellValue(ExportColumns[columnIndex].Value(AllStatList[rowIndex]));
                    cell.CellStyle = dataStyle;
                }
            }

            for (int columnIndex = 0; columnIndex < ExportColumns.Length; columnIndex++)
            {
                sheet.AutoSizeColumn(columnIndex);
            }

            using FileStream stream = new(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(stream);
        }
        finally
        {
            workbook.Close();
        }
    }

    private static ICellStyle CreateCellStyle(IWorkbook workbook, bool bold)
    {
        ICellStyle style = workbook.CreateCellStyle();
        style.BorderTop = BorderStyle.Thin;
        style.BorderBottom = BorderStyle.Thin;
        style.BorderLeft = BorderStyle.Thin;
        style.BorderRight = BorderStyle.Thin;
        style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

        if (bold)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微软雅黑";
            font.FontHeightInPoints = 12;
            font.IsBold = true;
            style.SetFont(font);
        }

        return style;
    }
}