# IndustrialMonitor

C# | 工业数字化监控平台 —— 基于 WPF (.NET 10) + Prism 的工业上位机监控软件。

## 项目简介

本项目是一个用于学习工业上位机开发的 WPF 应用程序。系统采用 Prism + 模块化分层架构，目前已完成登录认证、主界面框架、监控面板、左侧菜单导航与 MySQL 数据持久化，后续将逐步完善趋势、报警、报表、配置等业务模块。

## 技术栈

- 平台：.NET 10.0 (Windows)
- UI 框架：WPF
- 应用框架：Prism 9 (DryIoc)
- 图表组件：LiveCharts.Wpf 0.9.7
- ORM：Entity Framework Core 9
- 数据库：MySQL (Pomelo.EntityFrameworkCore.MySql 9)
- 语言：C#

## 解决方案结构

```
IndustrialMonitor.slnx
├── IndustrialMonitor/                  WPF 主程序入口（PrismApplication / Views / ViewModels）
│   ├── Views/                          视图（LoginView / MainUCView / FunctionUC / DialogWin）
│   └── ViewModels/                     视图模型（LoginViewModel / MainUCViewModel）
├── IndustrialMonitor.Models/           数据传输模型（DTO / ViewModel）
├── IndustrialMonitor.DataEntities/     实体类（与数据库表对应）
├── IndustrialMonitor.DBAcess/          数据访问层（DbContext / IDataAccess / Migrations）
├── IndustrialMonitor.DeviceAccess/     设备通信层（PLC / 仪器仪表读写）
├── IndustrialMonitor.CommonResource/   公共资源（字体、图片、样式）
├── IndustrialMonitor.Compenents/       自定义 UI 组件（ComponentBase / Meter 仪表盘）
├── IndustrialMonitor.Converter/        XAML 值转换器（Device / UserType / RankingValue 等）
└── IndustrialMonitor.Helper/           通用帮助类（MD5、密码框附加属性、ActionHelper 等）
```

## 当前已完成功能

- [x] 解决方案分层结构搭建
- [x] 引入 Prism 9 框架（PrismApplication、ViewModelLocator、DelegateCommand、DialogService）
- [x] MySQL 数据库接入（EF Core 9 + Pomelo）
- [x] 登录认证模块（账号密码校验、MD5 加密、登录成功后打开主界面）
- [x] 自定义密码框附加属性（支持双向绑定）
- [x] 主界面框架（左侧菜单导航、右上角窗口控制）
- [x] 监控模块页面（环境监测、7日能耗曲线、用气排行、设备运行状态、管道信息、设备提醒、用户信息）
- [x] 自定义工业组件基类（ComponentBase：选中、缩放、流向、旋转、报警、手动控制）
- [x] 自定义仪表盘组件（Meter）
- [x] 动态设备转换器（DeviceConverter）
- [x] 功能模块页面搭建（趋势 / 报警 / 报表 / 配置）
- [x] 权限控制（非管理员仅可使用监控功能）
- [x] 通用弹窗与提示（DialogOuterWin / RightRemindWin，支持背景模糊效果）
- [x] 公共资源库（字体、图标、按钮 / 表格样式）

## 待实现功能

- [ ] 趋势模块历史曲线
- [ ] 报警模块报警记录与处理
- [ ] 报表模块数据导出
- [ ] 配置模块参数设置
- [ ] 设备通信模块（串口 / TCP / Modbus）
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

## 默认登录账号

可在数据库 `monitor_SysUsers` 表中预置账号，默认测试账号可在登录窗口直接输入（具体以实际数据为准）。
