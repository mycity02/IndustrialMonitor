# IndustrialMonitor

C# | 工业数字化监控平台 —— 基于 WPF (.NET 10) + Prism 的工业上位机监控软件。

## 项目简介

本项目是一个用于学习工业上位机开发的 WPF 应用程序。系统采用 Prism + 模块化分层架构，已构建出较为完整的监控与工艺链路编辑能力：登录认证、主界面导航、实时数据面板、工业组件库、工艺链路可视化编辑、变量报警配置、设备通信框架（ModbusRTU / ModbusTCP / S7COMM）以及基于 MySQL 的数据持久化。

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
├── IndustrialMonitor/                  WPF 主程序入口
│   ├── Views/                          视图（登录 / 主界面 / 功能模块 / 弹窗 / 工艺链路编辑）
│   └── ViewModels/                     视图模型
├── IndustrialMonitor.Models/           数据传输模型（DTO / ViewModel）
├── IndustrialMonitor.DataEntities/     实体类（用户、设备、变量、报警、趋势、能耗等）
├── IndustrialMonitor.DBAcess/          数据访问层（DbContext / IDataAccess / Migrations）
├── IndustrialMonitor.DeviceAccess/     设备通信层（PLC / 仪器仪表读写）
├── IndustrialMonitor.CommonResource/   公共资源（字体、图片、样式）
├── IndustrialMonitor.Compenents/       自定义工业 UI 组件
│   ├── ComponentBase.cs                组件基类（选中、缩放、流向、旋转、报警、手动控制）
│   ├── Meter.xaml                      仪表盘组件
│   ├── AirCompressorUC.xaml            空压机组件
│   ├── AdsorptionDryerUC.xaml          吸附式干燥机组件
│   ├── FilterUC.xaml                   过滤器组件
│   ├── FreezeDryerUC.xaml              冷冻式干燥机组件
│   ├── GasTankUC.xaml                  储气罐组件
│   ├── HorizontalPipelineUC.xaml       水平管道组件
│   ├── VerticalPipelineUC.xaml         垂直管道组件
│   ├── RAJointsUC.xaml                 弯头接头组件
│   ├── TeeJointsUC.xaml                三通接头组件
│   └── WidthRule / HeightRule.xaml     对齐辅助线
├── IndustrialMonitor.Converter/        XAML 值转换器
│   ├── DeviceConverter.cs              动态工业组件转换器
│   ├── UserTypeConverter.cs            用户类型转换器
│   ├── RankingValueConverter.cs        用气排行宽度计算转换器
│   ├── FocuseToSelectedConverter.cs    焦点选中转换器
│   └── RowNumberConverter.cs           序号转换器
└── IndustrialMonitor.Helper/           通用帮助类（MD5、密码框附加属性、ActionHelper 等）
```

## 当前已完成功能

### 基础框架

- [x] 解决方案分层结构搭建
- [x] 引入 Prism 9 框架（PrismApplication、ViewModelLocator、DelegateCommand、DialogService）
- [x] MySQL 数据库接入（EF Core 9 + Pomelo）
- [x] 登录认证模块（账号密码校验、MD5 加密、登录成功后打开主界面）
- [x] 自定义密码框附加属性（支持双向绑定）
- [x] 主界面框架（左侧菜单导航、右上角窗口控制）
- [x] 权限控制（非管理员仅可使用监控功能）
- [x] 通用弹窗与提示（DialogOuterWin / RightRemindWin，支持背景模糊效果）
- [x] 公共资源库（字体、图标、按钮 / 表格 / 下拉框样式）

### 监控模块

- [x] 监控面板完整 UI（环境监测、7日能耗曲线、用气排行、设备运行状态、管道信息、设备提醒、用户信息）
- [x] LiveCharts 图表集成（折线图、柱状图）
- [x] 自定义仪表盘组件（Meter）显示母管压力、瞬时流量等

### 工艺链路编辑

- [x] 工艺链路编辑窗口（ComponentEditWin）
- [x] 左侧组件库（空压机、管道、接头等）拖拽到画布
- [x] 设备在画布中自由移动、缩放、旋转
- [x] 水平 / 垂直 / 宽度 / 高度对齐线与吸附
- [x] 设备右键菜单（旋转、改变流向、层级、删除）
- [x] 设备通信参数配置（协议、串口、波特率、数据位、校验、停止位）
- [x] 设备变量配置（变量名、地址、数据类型、偏移量、系数）
- [x] 手动控制选项配置
- [x] 变量报警配置（大于 / 小于 / 等于 / 大于等于 / 小于等于 / 不等于）
- [x] 工艺链路数据保存到数据库

### 数据模型

- [x] 用户、设备、组件、设备属性、设备变量
- [x] 手动控制、变量报警配置、设备报警
- [x] 监控记录、设备提醒、用气排行
- [x] 近七日能耗（电量 / 用气量 / 泄露量）
- [x] 趋势图表、趋势坐标轴、趋势预警线段、图表序列
- [x] 监控配置

### 自定义组件与转换器

- [x] 自定义工业组件基类 ComponentBase
- [x] 具体工业组件：空压机、吸附式干燥机、过滤器、冷冻式干燥机、储气罐、水平/垂直管道、弯头/三通接头、对齐辅助线
- [x] 动态设备转换器 DeviceConverter
- [x] 用气排行宽度计算 RankingValueConverter
- [x] 用户类型显示 UserTypeConverter
- [x] 焦点选中 FocuseToSelectedConverter
- [x] 序号 RowNumberConverter
- [x] 信息按钮可见性 InfoButtonVisibilityValueConverter

### 设备通信

- [x] 通信框架搭建（Communication 单例 + ExecuteObject 抽象基类）
- [x] 传输层：SerialUnit（串口）、SocketTcpUnit（TCP）
- [x] 协议实现：ModbusRTU、ModbusTCP、S7COMM
- [x] 变量地址分组、CRC16 校验、异常响应解析
- [x] 基础读写接口（Read / Write / DisConnect）

## 待实现功能

- [ ] 趋势模块历史曲线
- [ ] 报警模块报警记录与处理
- [ ] 报表模块数据导出
- [ ] 配置模块参数设置
- [ ] 设备实时通信调度与后台轮询
- [ ] FINS / MC 协议扩展
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

## 使用说明

1. 运行程序后进入登录界面，输入账号密码进入主界面。
2. 主界面左侧为功能菜单，默认展示“监控”面板。
3. 管理员点击“工艺链路编辑”可打开链路编辑窗口：
   - 从左侧组件库拖拽组件到画布；
   - 选中设备后可在右侧配置通信参数、变量、手动控制与报警条件；
   - 支持拖拽移动、缩放、旋转、对齐吸附；
   - 编辑完成后点击“保存”将链路数据写入数据库。
