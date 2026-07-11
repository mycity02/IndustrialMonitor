# IndustrialMonitor

基于 WPF (.NET 10) 的工业上位机监控软件，采用分层架构。

## 项目简介

本项目是一个用于学习工业上位机开发的 WPF 应用程序。系统采用模块化的分层架构，方便后续按职责逐步扩展（数据采集、设备通信、数据持久化、UI 组件等）。

## 技术栈

- 平台：.NET 10.0 (Windows)
- UI 框架：WPF
- 目标框架：`net10.0-windows`
- 语言：C#

## 解决方案结构

```
IndustrialMonitor.slnx
├── IndustrialMonitor/                  WPF 主程序入口（App / MainWindow）
├── IndustrialMonitor.Models/           数据传输模型（DTO / ViewModel）
├── IndustrialMonitor.DataEntities/     实体类（与数据库表对应）
├── IndustrialMonitor.DBAcess/          数据访问层（数据库操作封装）
├── IndustrialMonitor.DeviceAccess/     设备通信层（PLC / 仪器仪表读写）
├── IndustrialMonitor.CommonResource/   公共资源（样式、模板、语言资源等）
├── IndustrialMonitor.Compenents/       自定义 UI 组件
├── IndustrialMonitor.Converter/        XAML 值转换器
└── IndustrialMonitor.Helper/           通用帮助类与扩展方法
```

## 环境要求

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 或更高版本

## 构建与运行

```powershell
# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行主程序
dotnet run --project IndustrialMonitor
```

## 学习规划（持续更新）

- [x] 解决方案分层结构搭建
- [ ] 引入 Prism 框架（区域、导航、依赖注入）
- [ ] 设备通信模块（串口 / TCP / Modbus）
- [ ] 实时数据展示（曲线、仪表盘）
- [ ] 数据持久化（MySQL / SQLite）
- [ ] 报警与历史记录
- [ ] SignalR 实时消息推送
