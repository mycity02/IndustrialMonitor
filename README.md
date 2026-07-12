# IndustrialMonitor

空压站数字化监控平台 —— 基于 WPF (.NET 10) + Prism 的工业上位机监控软件。

## 项目简介

本项目是一个用于学习工业上位机开发的 WPF 应用程序。系统采用 Prism + 模块化分层架构，目前已完成登录认证与 MySQL 数据持久化基础，后续将逐步扩展设备通信、实时数据展示、报警与历史记录等功能。

## 技术栈

- 平台：.NET 10.0 (Windows)
- UI 框架：WPF
- 应用框架：Prism 9 (DryIoc)
- ORM：Entity Framework Core 9
- 数据库：MySQL (Pomelo.EntityFrameworkCore.MySql 9)
- 语言：C#

## 解决方案结构

```
IndustrialMonitor.slnx
├── IndustrialMonitor/                  WPF 主程序入口（PrismApplication / Views / ViewModels）
├── IndustrialMonitor.Models/           数据传输模型（DTO / ViewModel）
├── IndustrialMonitor.DataEntities/     实体类（与数据库表对应）
├── IndustrialMonitor.DBAcess/          数据访问层（DbContext / IDataAccess）
├── IndustrialMonitor.DeviceAccess/     设备通信层（PLC / 仪器仪表读写）
├── IndustrialMonitor.CommonResource/   公共资源（字体、图片、样式）
├── IndustrialMonitor.Compenents/       自定义 UI 组件
├── IndustrialMonitor.Converter/        XAML 值转换器
└── IndustrialMonitor.Helper/           通用帮助类（MD5、密码框附加属性等）
```

## 当前已完成功能

- [x] 解决方案分层结构搭建
- [x] 引入 Prism 9 框架（PrismApplication、ViewModelLocator、DelegateCommand）
- [x] MySQL 数据库接入（EF Core 9 + Pomelo）
- [x] 登录认证模块（账号密码校验、MD5 加密）
- [x] 自定义密码框附加属性（支持双向绑定）
- [x] 公共资源库（字体、图标、按钮样式）

## 待实现功能

- [ ] 主窗口框架与区域导航
- [ ] 设备通信模块（串口 / TCP / Modbus）
- [ ] 实时数据展示（曲线、仪表盘）
- [ ] 报警与历史记录
- [ ] SignalR 实时消息推送

## 环境要求

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 或更高版本
- MySQL 8.x

## 数据库配置

在主程序配置文件（`App.config`）中配置连接字符串，例如：

```xml
<connectionStrings>
  <add name="Default" 
       connectionString="server=localhost;port=3306;database=industrial_monitor;uid=root;pwd=your_password;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

然后执行 EF Core 迁移命令：

```powershell
# 安装 EF Core 工具（如未安装）
dotnet tool install --global dotnet-ef

# 更新数据库
dotnet ef database update --project IndustrialMonitor.DBAcess
```

## 构建与运行

```powershell
# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行主程序
dotnet run --project IndustrialMonitor
```
