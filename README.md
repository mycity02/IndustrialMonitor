# IndustrialMonitor

这是一个用于学习 **WPF、Prism、异步任务和 NModbus** 的精简工业通信项目。

## 当前功能

- 登录：用户信息通过 EF Core/MySQL 校验。
- 设备监控：固定 6 个 Modbus TCP 模拟从站。
- 异步轮询：每个从站一个异步任务，每 500 ms 读取一次。
- 寄存器读取：一次读取保持寄存器 `40001` 到 `40004`。
- 寄存器写回：界面可以写入 `0` 到 `65535`。
- 共享连接：同一个 TCP 连接上的 NModbus 命令通过 `SemaphoreSlim` 排队。
- 设备图片：每台从站显示对应的工业组件图标（`Resources/Images/Components/`）。
- 异常统计：监控页顶部显示当前通信异常设备数。

趋势、日志、报警、历史记录、Excel 导出、设备管理和 Modbus RTU 已删除。

## 单项目结构

```text
IndustrialMonitor.slnx
└─ IndustrialMonitor/
   ├─ Data/          登录数据访问
   ├─ Services/      窗口服务和单一 ModbusTcpService
   ├─ Models/        设备和界面模型
   ├─ Helpers/       WPF 辅助类
   ├─ Resources/     字体、图片和样式
   ├─ ViewModels/
   └─ Views/
```

整个解决方案只有 `IndustrialMonitor.csproj`，没有项目间引用。

## 模拟器配置

默认配置位于 `IndustrialMonitor/ViewModels/MainUCViewModel.cs` 顶部：

```text
IP: 127.0.0.1
Port: 502
SlaveId: 1 到 6
Holding Registers: 40001 到 40004
```

模拟器需要创建 6 个从站，站号分别为 1、2、3、4、5、6，并开放保持寄存器相对地址 0 到 3。

## 核心调用链

```text
MainUCViewModel.PollDeviceAsync
  → ModbusTcpService.ReadHoldingRegistersAsync
  → NModbus ReadHoldingRegistersAsync
  → Dispatcher 更新 DeviceVarModel.ReadValue
  → WPF Binding 刷新界面
```

写回调用链：

```text
写入按钮
  → DeviceModel.ManualControlCommand
  → MainUCViewModel.WriteRegisterAsync
  → ModbusTcpService.WriteSingleRegisterAsync
  → NModbus WriteSingleRegisterAsync
```

## 构建

```powershell
dotnet restore
dotnet build IndustrialMonitor.slnx
dotnet run --project IndustrialMonitor
```
