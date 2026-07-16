# IndustrialMonitor

用于学习 **WPF、Prism、异步任务和 NModbus** 的工业通信项目——通过 Modbus TCP 轮询 6 台模拟从站，实时读写保持寄存器并展示在界面上。

## 技术栈

| 组件 | 用途 |
|------|------|
| .NET 10 / WPF | 桌面 UI |
| Prism 9.0 (DryIoc) | MVVM 框架、依赖注入、对话框管理 |
| NModbus 3.0 | Modbus TCP 通信 |
| EF Core + Pomelo MySQL | 数据库访问（登录校验） |

## 功能

- 登录：账号密码经 MD5 哈希后通过 EF Core/MySQL 校验
- 设备监控：6 台固定 Modbus TCP 模拟从站（站号 1～6）
- 异步轮询：每台从站一个独立 Task，间隔 500ms 读取保持寄存器 40001～40004
- 寄存器写回：界面输入 `0～65535`，通过功能码 06 写入单个保持寄存器
- 通信异常标记：通信失败时卡片变红并显示错误信息，顶部统计异常设备数
- 共享连接：6 个轮询 Task 共用一条 TCP 连接，`SemaphoreSlim(1,1)` 保证报文不冲突
- 自动重连：通信失败丢弃连接，下次请求自动重建
- 优雅取消：关闭窗口时 `CancellationToken` 统一终止所有轮询任务

## 项目结构

```text
IndustrialMonitor.slnx
└─ IndustrialMonitor/
   ├─ Data/              EF Core DbContext + 登录数据访问
   ├─ Services/          ModbusTcpService（核心通信）、WindowService
   ├─ Models/            DeviceModel、DeviceVarModel、ManualControlModel、SysUserModel
   ├─ ViewModels/        LoginViewModel、MainUCViewModel（核心业务逻辑）
   ├─ Views/             LoginView、MainUCView（对话框外壳）、MonitorUC（设备卡片）
   ├─ Helpers/           Md5Helper
   └─ Resources/         图标字体、设备图片（空压机/罐体/干燥机/过滤器）、按钮样式
```

## 架构分层

```
View (XAML)  ← 纯绑定，DataTrigger 控制状态变色
    ↕ 双向绑定
ViewModel (Prism BindableBase)  ← 轮询逻辑、地址转换、异常统计
    ↕ 委托注入
Service (ModbusTcpService)  ← TCP 连接管理、SemaphoreSlim 排队、自动重连
    ↕
NModbus  ← Modbus TCP 报文打包/解析
```

## Modbus 协议简述

Modbus 是请求-响应协议，主站主动发问，从站被动应答。设备内部有四张数据表：

| 表 | 地址范围 | 数据类型 | 读写 | 功能码 |
|----|---------|---------|------|--------|
| 线圈 (Coils) | 00001~ | 位（0/1） | 读写 | FC01/FC05 |
| 离散输入 (Discrete Inputs) | 10001~ | 位（0/1） | 只读 | FC02 |
| 输入寄存器 (Input Registers) | 30001~ | 16位字 | 只读 | FC04 |
| 保持寄存器 (Holding Registers) | 40001~ | 16位字 | 读写 | **FC03/FC06** |

本项目读写保持寄存器：功能码 03 读、06 写单寄存器。界面显示 40001，实际通信地址为 0（去掉前缀 4，减 1）。

## 核心调用链

### 读取链路

```text
OnDialogOpened
  → LoadFixedDevices()  —— new 6 个 DeviceModel，注入 WriteAction 委托
  → StartMonitoring()
       → foreach DeviceList → PollDeviceAsync(device, token) × 6

PollDeviceAsync（每台设备一个独立 Task）
  while (true):
    → ReadHoldingRegistersAsync(SlaveId, 0, 4, token)  // 异步读，ConfigureAwait(false) 切后台
    → RunOnUiAsync → ApplyReadValues()                   // 切回 UI 线程刷新界面
    → SetProperty 触发 WPF 绑定自动更新
    → Task.Delay(500, token)                             // 异步等待，不占线程

ModbusTcpService 内部：
  ExecuteAsync → WaitAsync(SemaphoreSlim) 排队 → GetMasterAsync() 拿连接
  → NModbus 发功能码 03 报文 → 返回 ushort[]
  → 失败则 CloseConnection()，下次自动重连
```

### 写入链路

```text
用户输入值 → 点"写入 4xxxx"
  → ManualControlCommand → ExecuteManualControl()
       → ushort.TryParse 校验（0～65535）
       → WriteAction(device, "40001", value)
            → WriteRegisterAsync()
                 → ParseHoldingRegisterAddress("40001") → 0
                 → WriteSingleRegisterAsync(SlaveId, 0, value, token)
                      → ExecuteAsync → 排队 → 拿连接 → 发功能码 06
                 → RunOnUiAsync → 清异常标记
（写入不轮询，一次调用即完；界面看到新值依赖读取轮询下一次循环）
```

## 关键设计决策

| 决策 | 原因 |
|------|------|
| `async/await` 而非同步 IO | 网络 IO 不阻塞线程，界面不卡死 |
| `SemaphoreSlim(1,1)` 排队 | 同一条 TCP 连接上多帧报文交叉会导致从站解析失败 |
| `ConfigureAwait(false)` | 轮询 Task 无需回 UI 线程，避免不必要的上下文切换 |
| `RunOnUiAsync`（Dispatcher） | WPF 只允许 UI 线程修改控件，后台线程必须投递操作 |
| `CancellationToken` | 关闭窗口时统一打断所有 await（Delay、网络读写），优雅退出 |
| 共享连接 + 自动重连 | 避免每轮询周期都三次握手；失败自动重建，上层无感 |
| `WriteAction` 委托注入 | ViewModel 方法通过委托注入 Model，解耦写入链路 |
| `RefreshAbnormalDeviceCount()` | 顶部"通信异常"是 6 台汇总统计，任何设备状态变化都重新计数 |

## 模拟器配置

`MainUCViewModel.cs` 顶部常量：

```text
IP: 127.0.0.1  |  Port: 502  |  SlaveId: 1～6  |  Registers: 40001～40004
```

模拟器需创建 6 个从站，站号 1～6，开放保持寄存器地址 0～3。

## 构建与运行

```powershell
dotnet restore
dotnet build IndustrialMonitor.slnx
dotnet run --project IndustrialMonitor
```
