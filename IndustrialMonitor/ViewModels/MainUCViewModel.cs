using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DBAcess;
using IndustrialMonitor.DeviceAccess;
using IndustrialMonitor.DeviceAccess.Base;
using IndustrialMonitor.Helper;
using IndustrialMonitor.Models;
using IndustrialMonitor.Models.Models;
using IndustrialMonitor.Views.DialogWin;
using LiveCharts;
using Platform.Helper;
using Platform.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace IndustrialMonitor.ViewModels
{
    public class MainUCViewModel : BindableBase, IDialogAware
    {
        public SysUserModel LoginUserModel { get; set; }
        public string Title { get; set; } = "主界面";
        public DialogCloseListener RequestClose { get; } = new();
        private IDataAccess _dataAccess;

        public MainUCViewModel(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;

            //初始化设备
            InitHadDevices();

            //直接连接PLC设备监控
            Monitor();
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 接收对话框传递过来的参数
            LoginUserModel = parameters.GetValue<SysUserModel>("LoginUser");

            InitMenu();
        }



        #region 左侧菜单
        private UserControl _viewFunc;//右侧功能对象

        public UserControl ViewFunc
        {
            get { return _viewFunc; }
            set
            {
                SetProperty(ref _viewFunc, value);
            }
        }

        private List<MenuModel> _menuList = new();

        public List<MenuModel> MenuList
        {
            get => _menuList;
            set => SetProperty(ref _menuList, value);
        }

        /// <summary>
        /// 左侧菜单初始化
        /// </summary>
        private void InitMenu()
        {
            //_loggerService.Info("左侧初始化菜单123");
            MenuList =
                [
                    new MenuModel{  IsSelected=true, MenuName="监控", MenuIcon="\ue639",TargetFunc="MonitorUC"},
                    new MenuModel{  MenuName="趋势", MenuIcon="\ue61a",TargetFunc="TrendUC"},
                    new MenuModel{  MenuName="报警", MenuIcon="\ue60b",TargetFunc="AlarmUC"},
                    new MenuModel{  MenuName="报表", MenuIcon="\ue703",TargetFunc="ReportUC"},
                    new MenuModel{  MenuName="配置", MenuIcon="\ue60f",TargetFunc="SettingsUC"},
                ];

            DoSwitchFunc(MenuList[0]); //默认第一个功能
        }

        //切换功能命令
        public DelegateCommand<MenuModel> SwitchFuncCommand => new DelegateCommand<MenuModel>(DoSwitchFunc);

        /// <summary>
        /// 切换功能
        /// </summary>
        /// <param name="menuModel"></param>
        private void DoSwitchFunc(MenuModel menuModel)
        {
            //1、只针对该功能实现
            //2、代码优化（封装） 后面 弹出窗体，在窗体上面有操作 dialogresult

            //如果不是管理员，并且不是监控功能
            if (!LoginUserModel.IsAdmin && menuModel.TargetFunc != "MonitorUC")
            {
                MenuList[0].IsSelected = true;//没有权限，默认还是选择监控功能
                RightRemindWin rightRemindWin = new RightRemindWin();
                bool? dialogResult = rightRemindWin.ShowDialog();
                if (ActionHelper.ExecuteAndResult<object>("ShowRight", null))
                {
                    //重新登录
                    DoReLogin();
                }
            }
            else
            {
                //和之前点击的功能是一样的，就不用再执行
                if (ViewFunc != null && ViewFunc.GetType().Name == menuModel.TargetFunc)
                {
                    return;
                }
                Type type = Assembly.Load("IndustrialMonitor").GetType("IndustrialMonitor.Views.FunctionUC." + menuModel.TargetFunc)!;
                ViewFunc = Activator.CreateInstance(type) as UserControl;
            }
        }
        #endregion

        #region
        /// <summary>
        /// 重新登录
        /// </summary>
        private void DoReLogin()
        {
            Process.Start("IndustrialMonitor.exe");
            App.Current.Shutdown();
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        public DelegateCommand LogoutCommand => new DelegateCommand(DoReLogin);

        /// <summary>
        /// 重置密码
        /// </summary>
        public DelegateCommand ResetPwdCommand => new DelegateCommand(() =>
        {
            if (MessageBox.Show("确定重置密码吗？", "温馨提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string newPwd = Md5Hepler.ComputeMD5Hash("123456");//将密码重置为123456
                int result = _dataAccess.ResetPassword(LoginUserModel.UserId, newPwd);
                if (result == 1)
                {
                    MessageBox.Show("重置密码成功");
                }
                else
                {
                    MessageBox.Show("重置密码失败");
                }
            }
        });

        #endregion

        #region 工艺链路编辑/组件编辑/设备编辑

        #region 模糊度
        private int _viewBlur = 0;//默认0，即清晰

        public int ViewBlur
        {
            get { return _viewBlur; }
            set { SetProperty(ref _viewBlur, value); }
        }
        #endregion

        /// <summary>
        /// 打开编辑命令
        /// </summary>
        public DelegateCommand OpenComponentsEditCommand => new DelegateCommand(DoOpenComponentsEdit);

        /// <summary>
        /// 打开编辑窗体
        /// </summary>
        private void DoOpenComponentsEdit()
        {
            if (LoginUserModel.IsAdmin)
            {
                this.ViewBlur = 5;//默认模糊
                //1、如果dialogresult=true
                if (ActionHelper.ExecuteAndResult<object>("ComponentsEdit", null))
                {
                    //1、取消task
                    cts.Cancel();

                    //2、等待之前所有的task完成  耗时操作
                    Task.WaitAll(taskList.ToArray());

                    //3、清空之前所有的task
                    taskList.Clear();

                    //4、重新实例一个取消发生器
                    cts = new CancellationTokenSource();

                    //初始化设备
                    InitHadDevices();

                    //直接连接PLC设备监控
                    Monitor();

                    //通过windows服务进行监控
                    //MonitorByService();

                }

                this.ViewBlur = 0;//变清晰
            }
            else
            {
                //如果没有权限重新登录
                if (ActionHelper.ExecuteAndResult<object>("ShowRight", null))
                {
                    //重新登录
                    DoReLogin();
                }
            }
        }

        #endregion

        #region 初始化设备

        private ObservableCollection<DeviceModel> _deviceList;

        public ObservableCollection<DeviceModel> DeviceList
        {
            get { return _deviceList; }
            set
            {
                SetProperty(ref _deviceList, value);
            }
        }


        private void InitHadDevices()
        {

            var deviceEntityList = _dataAccess.GetDevices();//获取所有设备
            var devicePropEntityList = _dataAccess.GetDeviceProps();//获取所有设备属性
            var deviceVarEntityList = _dataAccess.GetDeviceVarList();//获取所有设备变量
            var manControlList = _dataAccess.GetManualControlList();//获取所有手动控制
            var varAlarmConfList = _dataAccess.GetVarAlarmConfList();//获取所有变量报警配置

            var deviceList = deviceEntityList.Select(d => new DeviceModel
            {
                WriteAction = WriteBytes,//写（通过向服务端发送报文写）
                IsMonitor = true,//是否监控
                ComponentId = d.ComponentId,
                ComponentType = d.ComponentType,
                DeviceName = d.DeviceName,
                DeviceNum = d.DeviceNum,
                Width = d.Width,
                Height = d.Height,
                X = d.X,
                Y = d.Y,
                Z = d.Z,
                FlowDirection = d.FlowDirection,
                Rotate = d.Rotate,

                //获取该设备属性
                DevicePropList = new ObservableCollection<DevicePropModel>(devicePropEntityList.Where(dp => dp.DeviceNum == d.DeviceNum).Select(dp => new DevicePropModel { PropName = dp.PropName, PropValue = dp.PropValue })),

                //获取该设备变量
                DeviceVarList = new ObservableCollection<DeviceVarModel>(deviceVarEntityList.Where(dv => dv.DeviceNum == d.DeviceNum).Select(dv => new DeviceVarModel
                {
                    DeviceNum = dv.DeviceNum,
                    DeviceName = d.DeviceName,
                    VarNum = dv.VarNum,
                    VarName = dv.VarName,
                    VarAddress = dv.VarAddress,
                    Offset = dv.Offset,
                    Modulus = dv.Modulus,
                    VarType = dv.VarType,

                    //取出该变量的报警配置
                    VarAlarmConfList = new ObservableCollection<VarAlarmConfModel>(varAlarmConfList.Where(vac => vac.VarNum == dv.VarNum).Select(vac => new VarAlarmConfModel
                    {
                        ConfNum = vac.ConfNum,
                        Operator = vac.Operator,
                        CompareValue = vac.CompareValue,
                        AlarmContent = vac.AlarmContent,
                    }))
                })),

                //获取该设备的手动控制
                ManualControlList = new ObservableCollection<ManualControlModel>(manControlList.Where(mc => mc.DeviceNum == d.DeviceNum).Select(mc => new ManualControlModel
                {
                    ControlName = mc.ControlName,
                    ControlAddress = mc.ControlAddress,
                    ControlValue = mc.ControlValue,
                })),

            });

            DeviceList = new ObservableCollection<DeviceModel>(deviceList);


        }
        #endregion

        #region 监控

        private int _alarmDeviceCount;

        /// <summary>
        /// 报警设备量
        /// </summary>
        public int AlarmDeviceCount
        {
            get { return _alarmDeviceCount; }
            set
            {
                SetProperty(ref _alarmDeviceCount, value);
            }
        }


        CancellationTokenSource cts = new CancellationTokenSource();//取消发生器 需要取消取消的时候 调用Cancel()
        List<Task> taskList = new List<Task>();//保存所有的task

        DataTable computeDt = new DataTable();//用来计算的

        Communication communication = Communication.CreateInstance();//通信实例
        /// <summary>
        /// 向指定设备写入控制数据。
        /// </summary>
        private void WriteBytes(DeviceModel device, string address, byte[] writeBytes)
        {
            var executeResult = communication.GetExecuteObject(device.DevicePropList
                .Select(item => new DevicePropEntity { PropName = item.PropName, PropValue = item.PropValue })
                .ToList());

            if (!executeResult.Status)
            {
                device.IsWarning = true;
                device.WarningMsg = executeResult.Msg;
                Trace.TraceError($"设备{device.DeviceNum}写入前通信初始化失败：{executeResult.Msg}");
                return;
            }

            var writeResult = executeResult.Data.Write(new WriteDataInfo
            {
                StartAddr = address,
                ValueType = typeof(ushort),
                WriteBytes = writeBytes
            });

            if (!writeResult.Status)
            {
                device.IsWarning = true;
                device.WarningMsg = writeResult.Msg;
                Trace.TraceError($"设备{device.DeviceNum}写入失败：{writeResult.Msg}");
            }
        }
        /// <summary>
        /// 监控
        /// </summary>
        private void Monitor()
        {
            //1、每个设备循环一遍进行监控。问题
            //(1)设备比较多 慢
            //(2)中间某个设备有问题，后面设备一直等

            //2 每个设备开一个线程(用的这种)
            //(1) 串口.一个设备连接了com1,还没有执行完;另外一个设备也要连接com1.不同的设备采用的是同样的通信配置,那么只连接一次更好(通信里面使用同一个传输对象)
            //匹配  匹配条件 串口 COM1；网口 IP、端口号
            //(2) 加锁
            foreach (var deviceModel in DeviceList)
            {
                //通信配置 和 变量参数都必须配置了才监控
                if (deviceModel.DevicePropList.Count == 0 || deviceModel.DeviceVarList.Count == 0)
                {
                    Trace.TraceInformation($"设备{deviceModel.DeviceNum}没有属性或设备变量");
                    continue;
                }

                //获取执行对象  同一返回结果
                var resultEo = communication.GetExecuteObject(deviceModel.DevicePropList.Select(p => new DevicePropEntity { PropName = p.PropName, PropValue = p.PropValue }).ToList());

                if (!resultEo.Status)//有错就不执行了
                {
                    //_loggerService.Fatal();
                    deviceModel.IsWarning = true;//要报警了
                    deviceModel.WarningMsg = resultEo.Msg;//报警信息

                    string alarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                    SaveDeviceAlarm(alarmNum, deviceModel, null, null, null, resultEo.Msg);//记录报警

                    continue;
                }
                //deviceModel.IsWarning = false;//取消

                List<VariableProp> varList = deviceModel.DeviceVarList.Select(v => new VariableProp
                {
                    VarNum = v.VarNum,
                    VarAddr = v.VarAddress,
                    ValueType = Type.GetType("System." + v.VarType)//"UInt16"转成Type
                }).ToList();

                var resultGroupAddr = resultEo.Data.GroupAddress(varList);//分组
                if (!resultGroupAddr.Status)
                {
                    deviceModel.IsWarning = true;//要报警了
                    deviceModel.WarningMsg = resultGroupAddr.Msg;//报警信息

                    string alarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                    SaveDeviceAlarm(alarmNum, deviceModel, null, null, null, resultGroupAddr.Msg);//记录报警

                    continue;
                }
                //deviceModel.IsWarning = false;//取消

                Task task = Task.Run(async () =>
                {
                    #region 可以从配置读取延时
                    //int delay = 500;
                    //var refreshRate = deviceModel.DevicePropList.FirstOrDefault(p => p.PropName == "RefreshRate");
                    //if (refreshRate != null)
                    //{
                    //    delay = Convert.ToInt32(refreshRate.PropValue);
                    //}
                    #endregion

                    while (!cts.IsCancellationRequested)
                    {
                        #region 如果需要人工手动处理报警，则发现设备已经报警了，程序不要处理
                        if (deviceModel.IsWarning)
                        {
                            continue;
                        }
                        #endregion

                        await Task.Delay(500);//等待500毫秒 可以配置

                        var readResult = resultEo.Data.Read(resultGroupAddr.Data);//分组 按照功能码分组  每读一次就分组一次
                        if (!readResult.Status)
                        {
                            Trace.TraceError($"{resultEo.Data}读出错了，错误信息:{readResult.Msg}");
                            deviceModel.IsWarning = true;//要报警了
                            deviceModel.WarningMsg = "服务器繁忙";//报警信息

                            string alarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                            SaveDeviceAlarm(alarmNum, deviceModel, null, null, null, readResult.Msg);//记录报警

                            continue;
                        }
                        //deviceModel.IsWarning = false;//取消

                        #region 解析数据
                        bool isAlarm = false;//是否报警 默认没有报警
                        foreach (GroupAddress groupAddress in resultGroupAddr.Data)
                        {
                            foreach (VariableProp variableProp in groupAddress.VarPropList)//循环设备的变量
                            {
                                var resultConvertData = communication.ConvertType(variableProp.ReadBytes, variableProp.ValueType);
                                if (!resultConvertData.Status)
                                {
                                    deviceModel.IsWarning = true;//要报警了
                                    deviceModel.WarningMsg = resultConvertData.Msg;//报警信息

                                    string groupAlarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                                    SaveDeviceAlarm(groupAlarmNum, deviceModel, null, null, null, resultConvertData.Msg);//记录报警

                                    continue;
                                }
                                //deviceModel.IsWarning = false;//取消


                                DeviceVarModel deviceVarModel = deviceModel.DeviceVarList.First(dv => dv.VarNum == variableProp.VarNum);
                                object oldReadValue = deviceVarModel.ReadValue;//之前从plc读取的值 记录监控值需要判断
                                deviceVarModel.ReadValue = resultConvertData.Data;//将转换的结果放到变量业务模型 plc设备里面的原始数据
                                                                                  //计算结果
                                if (deviceVarModel.VarType != "Boolean")//bool类型不需要计算
                                {
                                    //表达式
                                    string exp = $"{deviceVarModel.ReadValue}*{deviceVarModel.Modulus}+{deviceVarModel.Offset}";
                                    deviceVarModel.ReadValue = computeDt.Compute(exp, "");//将计算结果重新赋值
                                }

                                #region 报警提示
                                string? alarmNum = null;//报警编号
                                foreach (VarAlarmConfModel curAlarm in deviceVarModel.VarAlarmConfList)
                                {
                                    //每个设备每次只能显示一个报警
                                    if (isAlarm)
                                    {
                                        break;
                                    }

                                    //50<5
                                    string exp = $"{deviceVarModel.ReadValue}{curAlarm.Operator}{curAlarm.CompareValue}";
                                    if (Boolean.TryParse(computeDt.Compute(exp, "").ToString(), out bool result) && result)
                                    {
                                        //需要报警
                                        isAlarm = true;
                                        deviceModel.WarningMsg = curAlarm.AlarmContent;

                                        alarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                                        SaveDeviceAlarm(alarmNum, deviceModel, deviceVarModel, curAlarm.ConfNum, deviceVarModel.ReadValue.ToString(), curAlarm.AlarmContent);//记录报警
                                    }
                                }
                                #endregion

                                #region 记录监控值到DB
                                //1、加缓存(如，redis)
                                //2、当需要记录的数量达到一定时(每积累到200条)，记录到db里一次。实时性有问题（采用）
                                //程序退出的时候，即使没有达到200条，也必须要记录，不能丢数据
                                if (deviceVarModel.VarType == "UInt16")//只记录数字  || deviceVarModel.VarType == "UInt32"
                                {
                                    if (!deviceVarModel.ReadValue.Equals(oldReadValue))//值与之前不一样才写
                                    {
                                        _dataAccess.SaveMonitorRecords(new List<MonitorRecordEntity>
                                        {
                                            new MonitorRecordEntity
                                            {
                                                DeviceNum = deviceModel.DeviceNum,
                                                DeviceName = deviceModel.DeviceName,
                                                VarNum = deviceVarModel.VarNum,
                                                VarName = deviceVarModel.VarName,
                                                RecordValue = Convert.ToDecimal(deviceVarModel.ReadValue),
                                                Account = LoginUserModel.Account,
                                                AlarmNum = alarmNum,
                                            }
                                        });
                                    }
                                }
                                #endregion

                                #region 监控数据上云
                                // MQTT 上云尚未配置服务地址、账号和客户端连接；暂不发布。
                                // 配置完成后，在此处通过已连接的 MQTT 客户端发布监控数据。
                                #endregion
                            }
                        }

                        deviceModel.IsWarning = isAlarm;//设置监控
                        #endregion
                    }

                    resultEo.Data.DisConnect();//断开连接
                }, cts.Token);

                taskList.Add(task);//保存task
            }
        }

        /// <summary>
        /// 记录报警信息
        /// </summary>
        /// <param name="alarmNum">报警编号</param>
        /// <param name="device">报警设备</param>
        /// <param name="deviceVarModel">报警变量(可能不需要)</param>
        /// <param name="confNum">报警变量配置编号(可能不需要)</param>
        /// <param name="alarmValue">报警值(可能不需要)</param>
        /// <param name="AlarmContent">报警提示</param>
        private void SaveDeviceAlarm(string alarmNum, DeviceModel device, DeviceVarModel? deviceVarModel, string? confNum, string? alarmValue, string AlarmContent)
        {
            _dataAccess.SaveDeviceAlarm(new DeviceAlarmEntity
            {
                AlarmNum = alarmNum,
                Account = LoginUserModel.Account,
                AlarmContent = AlarmContent,
                AlarmValue = alarmValue,
                ConfNum = confNum,
                DeviceNum = device.DeviceNum,
                DeviceName = device.DeviceName,
                VarNum = (deviceVarModel == null ? null : deviceVarModel.VarNum),
                VarName = (deviceVarModel == null ? null : deviceVarModel.VarName),
            });
        }
        #endregion

        #region 跳转到详情模块
        public DelegateCommand AlarmDetailCommand => new DelegateCommand(() =>
        {
            MenuList[2].IsSelected = true;//该菜单设置为已选中
            DoSwitchFunc(MenuList[2]);
        });
        #endregion

        #region 7日能耗 - 耗电量

        /// <summary>
        /// 耗电量
        /// </summary>
        public ChartValues<decimal> PowerLineValues { get; set; } = new ChartValues<decimal>();//Y轴

        /// <summary>
        /// 日期
        /// </summary>
        public List<string> PowerLineLabels { get; set; } = new List<string>();//X轴

        /// <summary>
        /// 初始化7日能耗 - 耗电量
        /// </summary>
        private void InitSevenPower()
        {
            var sevenPowerList = _dataAccess.GetSevenPowerList();

            foreach (var power in sevenPowerList)
            {
                PowerLineLabels.Add(power.Day);
                PowerLineValues.Add(power.Power);
            }
        }

        #endregion

        #region 7日能耗 - 用气量
        public ChartValues<decimal> AirLineValues { get; set; } = new ChartValues<decimal>();

        public List<string> AirLineLabels { get; set; } = new List<string>();//X轴

        /// <summary>
        /// 初始化7日能耗 - 用气量
        /// </summary>
        private void InitSevenAir()
        {
            var sevenAirList = _dataAccess.GetSevenAirList();

            foreach (var air in sevenAirList)
            {
                AirLineLabels.Add(air.Day);
                AirLineValues.Add(air.Air);
            }
        }
        #endregion

        #region 7日能耗 - 泄露量
        public ChartValues<decimal> LeakLineValues { get; set; } = new ChartValues<decimal>();

        public List<string> LeakLineLabels { get; set; } = new List<string>();//X轴

        /// <summary>
        /// 初始化7日能耗 - 泄露量
        /// </summary>
        private void InitSevenLeak()
        {
            var sevenLeakList = _dataAccess.GetSevenLeakList();

            foreach (var leak in sevenLeakList)
            {
                LeakLineLabels.Add(leak.Day);
                LeakLineValues.Add(leak.Leak);
            }
        }
        #endregion

        #region 用气排行
        public List<AirRankingModel> AirRankingList { get; set; }//用气量排行

        /// <summary>
        /// 初始化用气排行
        /// </summary>
        private void InitAirRanking()
        {
            AirRankingList = _dataAccess.GetAirRankings().Select(a => new AirRankingModel { WorkshopName = a.WorkshopName, PlanValue = a.PlanValue, FinishedValue = a.FinishedValue }).ToList();
        }

        #endregion

        #region 设备提醒

        public List<DeviceWarningModel> DeviceWarningList { get; set; }

        /// <summary>
        /// 初始化设备提醒
        /// </summary>
        private void InitDeviceWarning()
        {
            DeviceWarningList = _dataAccess.GetDeviceWarnings().Select(w => new DeviceWarningModel
            {
                Message = w.Message,
                DateTime = w.DateTime
            }).ToList();
        }

        #endregion

        #region 根据监控配置显示监控数据温度、湿度、PM25、压力、瞬时流量

        /// <summary>
        /// 温度
        /// </summary>
        public DeviceVarModel TemperatureVar { get; set; }

        /// <summary>
        /// 湿度
        /// </summary>
        public DeviceVarModel HumidityVar { get; set; }

        /// <summary>
        /// PM2.5
        /// </summary>
        public DeviceVarModel PM25Var { get; set; }//PM2.5

        /// <summary>
        /// 压力
        /// </summary>
        public DeviceVarModel PressureVar { get; set; }

        /// <summary>
        /// 瞬时流量
        /// </summary>
        public DeviceVarModel FlowRateVar { get; set; }

        /// <summary>
        /// 初始化监控配置的变量
        /// </summary>
        private void InitMonitorSettingVar()
        {
            var settingEntityList = _dataAccess.GetMonitorSettings();

            //温度
            var tempSetting = settingEntityList.FirstOrDefault(s => s.SettingNum == MonitorSettingNumEnum.TemperatureVar);
            if (tempSetting != null)
            {
                var device = DeviceList.FirstOrDefault(d => d.DeviceNum == tempSetting.DeviceNum);
                if (device != null)
                {
                    TemperatureVar = device.DeviceVarList.FirstOrDefault(v => v.VarNum == tempSetting.VarNum);
                }
            }

            //湿度
            var humiditySetting = settingEntityList.FirstOrDefault(s => s.SettingNum == MonitorSettingNumEnum.HumidityVar);
            if (humiditySetting != null)
            {
                var device = DeviceList.FirstOrDefault(d => d.DeviceNum == humiditySetting.DeviceNum);
                if (device != null)
                {
                    HumidityVar = device.DeviceVarList.FirstOrDefault(v => v.VarNum == humiditySetting.VarNum);
                }

            }

            //PM2.5
            var pm25Setting = settingEntityList.FirstOrDefault(s => s.SettingNum == MonitorSettingNumEnum.PM25Var);
            if (pm25Setting != null)
            {
                var device = DeviceList.FirstOrDefault(d => d.DeviceNum == pm25Setting.DeviceNum);
                if (device != null)
                {
                    PM25Var = device.DeviceVarList.FirstOrDefault(v => v.VarNum == pm25Setting.VarNum);
                }
            }

            //母管压力
            var pressureSetting = settingEntityList.FirstOrDefault(s => s.SettingNum == MonitorSettingNumEnum.PressureVar);
            if (pressureSetting != null)
            {
                var device = DeviceList.FirstOrDefault(d => d.DeviceNum == pressureSetting.DeviceNum);
                if (device != null)
                {
                    PressureVar = device.DeviceVarList.FirstOrDefault(v => v.VarNum == pressureSetting.VarNum);
                }
            }

            //瞬时流量
            var flowRateVarSetting = settingEntityList.FirstOrDefault(s => s.SettingNum == MonitorSettingNumEnum.FlowRateVar);
            if (flowRateVarSetting != null)
            {
                var device = DeviceList.FirstOrDefault(d => d.DeviceNum == flowRateVarSetting.DeviceNum);
                if (device != null)
                {
                    FlowRateVar = device.DeviceVarList.FirstOrDefault(v => v.VarNum == flowRateVarSetting.VarNum);
                }
            }
        }
        #endregion

        #region 通过windows服务监控

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private ushort clientId = 0;//客户端ID
        private ushort tid = 0;//事务id

        //服务器是否活着
        bool IsAlive = false;

        //用于线程间同步
        //AutoResetEvent
        //C#内置的线程同步信号类
        //作用：让一个线程等待，直到另一个线程发送信号，它才继续执行
        AutoResetEvent PingResetEvent = new AutoResetEvent(false);

        /// <summary>
        ///  通过windows服务监控
        /// </summary>
        private void MonitorByService()
        {
            try
            {
                // 1、TCP连接到服务器
                socket.Connect("127.0.0.1", 8899);
                socket.ReceiveTimeout = 5000;

                // 2、获取服务器颁发的ID
                byte[] iDBytes = this.ReceiveBytes(socket);

                if (iDBytes[4] == 0xFF)//表示出错
                {
                    return;
                }

                //客户端ID
                clientId = BitConverter.ToUInt16([iDBytes[8], iDBytes[7]]);

                //3、订阅读
                foreach (var device in DeviceList)
                {
                    if (device.DevicePropList.Count == 0 || device.DeviceVarList.Count == 0)
                    {
                        continue;
                    }

                    //(1)通讯配置拼接成string，string->byte[]
                    //Protocol:ModbusRTU,PortName:COM1,SlaveId:1
                    string propStr = string.Join(",", device.DevicePropList.Select(p => p.PropName + ":" + p.PropValue).ToArray());
                    byte[] propBytes = Encoding.Default.GetBytes(propStr); //通讯配置字节数组
                    //(2)所有设备所有的变量信息拼接成字符串，string->byte[]
                    //D20260127164838923-V20260215154934038:40001:UInt16,D20260127164838923 - V20260227180655031:40003:UInt16
                    var varStr = string.Join(",", device.DeviceVarList.Select(v => v.DeviceNum + "-" + v.VarNum + ":" + v.VarAddress + ":" + v.VarType).ToArray());
                    byte[] varBytes = Encoding.Default.GetBytes(varStr); //所有变量字节数组

                    //发送订阅读的报文
                    tid++;
                    tid %= 0xFFFF;
                    List<byte> subBytes = [
                        //事务ID
                        (byte)(tid/256),
                        (byte)(tid%256),

                        //客户端ID
                        (byte)(clientId/256),
                        (byte)(clientId%256),

                        0x03,//功能码 c-s 订阅读

                        //字节长度
                        (byte)((propBytes.Length + varBytes.Length + 4) / 256),
                        (byte)((propBytes.Length + varBytes.Length + 4) % 256),

                        //通讯配置的字节长度
                        (byte)((propBytes.Length) / 256),
                        (byte)((propBytes.Length) % 256)

                        ];

                    //添加通讯配置字节数据
                    subBytes.AddRange(propBytes);//假设20个字节  20

                    //变量字节长度
                    subBytes.Add((byte)((varBytes.Length) / 256));
                    subBytes.Add((byte)((varBytes.Length) % 256));

                    //添加变量字节数据
                    subBytes.AddRange(varBytes);//假设30个字节   30

                    // 发送订阅请求
                    socket.Send(subBytes.ToArray());
                }

                // 5、开启心跳/发送挺协调报文
                var taskHeart = Task.Run(async () =>
                {
                    //Socket s = socket;
                    while (!cts.IsCancellationRequested)
                    {
                        // 每隔一秒发一个心跳
                        await Task.Delay(1000);

                        IsAlive = false;
                        if (socket != null)
                        {
                            tid++;
                            tid %= 0xFFFF;
                            List<byte> subBytes = new List<byte> {
                                (byte)(tid / 256), (byte)(tid % 256),
                                (byte)(clientId/256),(byte)(clientId%256),
                                0x06 ,//心跳功能码0x06
                                0x00,0x00
                            };
                            socket.Send(subBytes.ToArray(), 0, subBytes.Count, SocketFlags.None);

                            //等待信号
                            PingResetEvent.WaitOne(3000);

                            // 扩展：尝试多次心跳，都不能正常，判断断开 ，执行重连
                            if (!IsAlive)
                            {
                                break;//终止
                            }
                        }
                    }
                }, cts.Token);
                taskList.Add(taskHeart);

                #region 接收服务器的报文并解析
                var task = Task.Run(async () =>
                {
                    //bool isAlarm = false;
                    while (!cts.IsCancellationRequested)
                    {
                        await Task.Delay(100);

                        try
                        {
                            byte[] respBytes = this.ReceiveBytes(socket);//服务器返回的完整报文

                            switch (respBytes[4])//功能码
                            {
                                //case 0x03://服务器告诉客户端已经收到了订阅读的数据
                                //    {
                                //        break;
                                //    }
                                case 0x04://服务器返回的读的数据
                                    {
                                        //数据字节
                                        var dataBytes = respBytes.ToList().GetRange(7, respBytes.Length - 7);

                                        //取出变量编号:设备编号-变量编号
                                        //varByteLen 变量字节长度
                                        ushort varByteLen = BitConverter.ToUInt16([dataBytes[1], dataBytes[0]]);

                                        //变量字节
                                        var varBytes = dataBytes.GetRange(2, varByteLen);
                                        //变量字符串
                                        string varStr = Encoding.Default.GetString(varBytes.ToArray());//设备编号-变量编号

                                        //设备编号和变量编号
                                        var deviceNum = varStr.Split('-')[0];
                                        var varNum = varStr.Split('-')[1];
                                        var deviceModel = DeviceList.FirstOrDefault(d => d.DeviceNum == deviceNum);
                                        if (deviceModel == null)
                                        {
                                            continue;
                                        }

                                        if (deviceModel.IsWarning)
                                        {
                                            continue;
                                        }

                                        var varModel = deviceModel.DeviceVarList.FirstOrDefault(v => v.VarNum == varNum);
                                        if (varModel == null)
                                        {
                                            continue;
                                        }

                                        //读取数据的字节长度
                                        ushort dataByteLen = BitConverter.ToUInt16([dataBytes[2 + varByteLen + 1], dataBytes[2 + varByteLen]]);

                                        //读取数据的字节
                                        var readDataBytes = dataBytes.ToList().GetRange(2 + varByteLen + 2, dataByteLen);

                                        ResultData<object> resultData = communication.ConvertType(readDataBytes.ToArray(), Type.GetType("System." + varModel.VarType));

                                        // 赋值
                                        var newValue = resultData.Data;
                                        if (varModel.VarType != "Boolean")
                                        {
                                            newValue = new DataTable().Compute(resultData.Data.ToString() + "*" + varModel.Modulus.ToString() + "+" + varModel.Offset.ToString(), "");
                                        }

                                        if (varModel.ReadValue == newValue)
                                        {
                                            continue;
                                        }
                                        varModel.ReadValue = newValue;

                                        //#region 报警提示

                                        //string? alarmNum = null;//报警编号
                                        //foreach (VarAlarmConfModel curAlarm in varModel.VarAlarmConfList)
                                        //{
                                        //    //每个设备每次只能显示一个报警
                                        //    if (isAlarm)
                                        //    {
                                        //        break;
                                        //    }

                                        //    //50<5
                                        //    string exp = $"{varModel.ReadValue}{curAlarm.Operator}{curAlarm.CompareValue}";
                                        //    if (Boolean.TryParse(computeDt.Compute(exp, "").ToString(), out bool result) && result)
                                        //    {
                                        //        //需要报警
                                        //        isAlarm = true;
                                        //        deviceModel.WarningMsg = curAlarm.AlarmContent;

                                        //        alarmNum = "A" + DateTime.Now.ToString("yyyyMMddHHmmssFFF");//报警编号
                                        //        SaveDeviceAlarm(alarmNum, deviceModel, varModel, curAlarm.ConfNum, varModel.ReadValue.ToString(), curAlarm.AlarmContent);//记录报警
                                        //    }
                                        //}

                                        //deviceModel.IsWarning = isAlarm;
                                        //#endregion
                                        break;
                                    }

                                //心跳报文
                                case 0x06:
                                    // 处理心跳
                                    IsAlive = true;
                                    //发送信号！唤醒等待的线程
                                    PingResetEvent.Set();
                                    //_loggerService.Info("接收到心跳数据了");
                                    break;

                                default: break;
                            }
                        }
                        catch (Exception)
                        {
                            //异常处理
                        }

                    }

                }, cts.Token);
                taskList.Add(task);
                #endregion

            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion

        #region 接受服务器的完整报文

        /// <summary>
        /// 接受服务器的完整报文 
        /// </summary>
        /// <param name="socket">客户端</param>
        /// <returns></returns>
        private byte[] ReceiveBytes(Socket socket)
        {
            byte[] subResp = new byte[7];
            socket.Receive(subResp, 0, 7, SocketFlags.None);//服务器端发送的报文前面7个字节固定，并含义一样
            ushort len = BitConverter.ToUInt16([subResp[6], subResp[5]]);//数据字节个数
            if (len == 0)//如果没有数据字节
            {
                return subResp;
            }

            //取数据部分字节
            byte[] pyload = new byte[len];
            socket.Receive(pyload, 0, len, SocketFlags.None);

            List<byte> bytes = new List<byte>();
            bytes.AddRange(subResp);
            bytes.AddRange(pyload);

            return bytes.ToArray();
        }
        #endregion

        
    }
}
