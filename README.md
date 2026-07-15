# IndustrialMonitor

这是一个基于 **WPF、Prism、EF Core 和 Modbus** 的工业设备监控学习项目。界面只保留三个模块：

- **监控**：设备状态、实时变量、手动写入和阈值提示。
- **趋势**：选择一个设备变量，每秒采样并显示最近 60 个数据点。
- **监控数据**：汇总历史采集记录并导出 Excel。

项目不使用 MQTT。Modbus RTU/TCP 使用 **NModbus 3.0.83** 完成组帧、CRC 和响应解析，项目代码只保留连接管理、设备轮询、地址转换和数据换算。

## 建议学习顺序

1. `LoginViewModel`：登录、传递用户参数、打开主窗口。
2. `MainUCViewModel`：加载设备、菜单状态、启动和停止采集任务。
3. `DeviceManageWinViewModel`：维护 RTU/TCP 参数、变量和手动控制。
4. `Communication`：按 `Protocol` 选择 Modbus RTU 或 Modbus TCP。
5. `TrendUCViewModel`：使用一个 `DispatcherTimer` 刷新单条实时曲线。
6. `MonitorDataUCViewModel`：统计历史记录并导出 Excel。

## 主要数据流

```text
MySQL
  ↓ IDataAccess
MainUCViewModel 加载设备配置
  ↓ Communication
Modbus RTU / Modbus TCP 读取设备
  ↓
DeviceModel.DeviceVarList 更新实时值
  ├─→ 监控页显示
  ├─→ 趋势页每秒取样
  └─→ MonitorRecordBuffer 批量写入历史记录
                                 ↓
                          监控数据页汇总/导出
```

## WPF 与 ViewModel 的分工

- `MainUCView.xaml` 固定声明三个页面，通过 `CurrentPage` 枚举控制显示。
- ViewModel 不再通过反射创建 `UserControl`，也不使用字符串回调的 `ActionHelper`。
- 普通窗口、消息提示和保存文件对话框统一通过 `IWindowService` 调用。
- 命令在构造函数中创建一次，不再在属性 getter 中重复 `new DelegateCommand`。
- 趋势页不保存复杂配置，只读取通信层已经更新的变量值。

## 解决方案结构

```text
IndustrialMonitor.slnx
├─ IndustrialMonitor/                 WPF 视图、ViewModel、窗口服务
├─ IndustrialMonitor.Models/          界面和业务模型
├─ IndustrialMonitor.DataEntities/    EF Core 数据库实体
├─ IndustrialMonitor.DBAcess/         数据访问和数据库迁移
├─ IndustrialMonitor.DeviceAccess/    NModbus RTU/TCP 适配与连接管理
├─ IndustrialMonitor.CommonResource/  公共样式、字体和图片
└─ IndustrialMonitor.Helper/          密码绑定和通用帮助类
```

## 通信与并发

- MainUCViewModel 为每台有效设备启动一个独立轮询任务。
- 不同串口或不同 TCP 端点可以并行通信。
- 共享同一串口或 TCP 连接的任务由 TransferObject 使用 SemaphoreSlim 排队，防止请求和响应串线。
- NModbus 负责功能码、RTU CRC、TCP MBAP 报文头、超时和协议重试。
- 所有轮询停止后统一释放共享连接，重新加载设备配置时重新创建连接。

通信调用链：

    MainUCViewModel.PollDeviceAsync
      → ExecuteObject.ReadAsync
      → ModbusExecuteObject
      → TransferObject.UseMasterAsync
      → SerialUnit / SocketTcpUnit
      → NModbus IModbusMaster

## 数据库表

当前模型只保留登录、设备通信和监控记录需要的表；实时趋势数据只保存在内存中：

- `monitor_SysUsers`
- `monitor_Devices`
- `monitor_DeviceProps`
- `monitor_DeviceVars`
- `monitor_ManualControls`
- `monitor_VarAlarmConfs`
- `monitor_MonitorRecords`

数据库连接在 `IndustrialMonitor/App.config` 中配置。迁移命令：

```powershell
dotnet ef database update --project IndustrialMonitor.DBAcess
```

## 构建与运行

```powershell
dotnet restore
dotnet build IndustrialMonitor.slnx
dotnet run --project IndustrialMonitor
```

当前使用旧版 `LiveCharts.Wpf 0.9.7`。在 .NET 10 下 NuGet 会给出 `NU1701` 兼容性提示，但项目可以正常编译；这是目前唯一保留的构建警告。