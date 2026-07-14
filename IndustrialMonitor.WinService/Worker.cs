using IndustrialMonitor.DataEntities;
using IndustrialMonitor.DeviceAccess;
using IndustrialMonitor.DeviceAccess.Base;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IndustrialMonitor.WinService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// ���ʱִ��
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    if (_logger.IsEnabled(LogLevel.Information))
            //    {
            //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    }
            //    await Task.Delay(1000, stoppingToken);
            //}

            StartListen();
        }

        ///// <summary>
        /////  host.Run()���Զ�ִ��
        ///// </summary>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //public override Task StartAsync(CancellationToken cancellationToken)
        //{
        //    return Task.Run(() =>
        //    {
        //        StartListen();
        //    }, cancellationToken);
        //}

        /// <summary>
        /// ֹͣ����
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ֹͣ����");
            return Task.Run(() =>
            {
                Stop();
            }, cancellationToken);
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        List<Task> TaskList = new List<Task>();

        #region �������

        Socket server;

        /// <summary>
        /// �������
        /// </summary>
        private void StartListen()
        {
            try
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, 8899));//ȷ���˿ں�û��ռ��
                server.Listen(10);

                _logger.LogInformation("TCPԶ�̷���������������ȴ��ͻ��˽���....");

                AcceptClient(server);//���ܿͻ�������


                //���ͻ����Ƿ����
                CheckAlive();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("TCPԶ�̷������ʧ�ܡ�" + ex.Message);
            }
        }
        #endregion

        #region ���ܿͻ��˽��룬���ҷַ�һ���ͻ���ID�����տͻ������ݣ����������Ķ�������

        Random random = new Random();

        List<ClientModel> ClientList = new List<ClientModel>();//�ͻ��˼���

        /// <summary>
        /// ���ܿͻ��˽��룬���ҷַ�һ���ͻ���ID�����տͻ������ݣ����������Ķ�������
        /// </summary>
        /// <param name="socket"></param>
        private void AcceptClient(Socket socket)
        {
            var t = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    var client = socket.Accept();//client�������ӵĿͻ���

                    try
                    {
                        // �����ͻ���ID
                        ushort clientId = (ushort)random.Next(0, ushort.MaxValue);//������ɿͻ���ID
                        while (ClientList.Exists(c => c.ClientId == clientId))//id���ظ�������ظ�����������
                        {
                            clientId = (ushort)random.Next(0, ushort.MaxValue);
                        }

                        //���ͻ��˷��͵İ䷢�ͻ���ID�ı���
                        byte[] regBytes = [
                            0x00,0x00,0x00,0x00,0x01,0x00,0x02,(byte)(clientId/256),(byte)(clientId%256)
                        ];

                        client.Send(regBytes, 0, regBytes.Length, SocketFlags.None);

                        //��client��ӵ�clientlist
                        ClientModel clientModel = new ClientModel { ClientId = clientId, Client = client, LifeTime = DateTime.Now.AddSeconds(20) };
                        ClientList.Add(clientModel);

                        _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} �ͻ���{clientId}�Ѿ�����");

                        // ��ʼ���տͻ�����Ϣ
                        Receive(client);

                        //���Ķ����ݵĻ�ȡ������
                        MonitorData(clientModel);

                        //����д��ִ��
                        var taskWrite = Task.Run(async () =>
                        {
                            while (!cts.IsCancellationRequested)
                            {
                                await Task.Delay(100);
                                ReceiveWrite(client);
                            }
                        }, cts.Token);
                        TaskList.Add(taskWrite);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("���ܿͻ��˽����쳣 - " + ex.Message);
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        client.Dispose();
                    }

                }
            }, cts.Token);

            TaskList.Add(t);
        }
        #endregion

        #region ���ݽ��ն��Ķ�/����

        /// <summary>
        /// ���ݽ��ն��Ķ�/����
        /// </summary>
        /// <param name="client"></param>
        private void Receive(Socket client)
        {
            var t = Task.Run(() =>
            {
                client.ReceiveTimeout = 1000;
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        List<byte> totalBytes = new List<byte>();//���յ������ֽ�

                        //1������ǰ��7���ֽ�
                        byte[] respBytes = new byte[7];
                        client.Receive(respBytes, 0, 7, SocketFlags.None);
                        totalBytes.AddRange(respBytes);//�Ƚ����յ�7���ֽڷŽ���

                        //��ȡ������ֽڳ���
                        ushort dataLen = BitConverter.ToUInt16([respBytes[6], respBytes[5]]);
                        if (dataLen > 0)
                        {
                            byte[] dataBytes = new byte[dataLen];//�����ֽ�
                            client.Receive(dataBytes, 0, dataLen, SocketFlags.None);
                            totalBytes.AddRange(dataBytes);
                        }

                        // �ͻ��˱��
                        ushort clientId = BitConverter.ToUInt16([totalBytes[3], totalBytes[2]]);
                        var clientModel = ClientList.FirstOrDefault(c => c.ClientId == clientId);//���ݿͻ��˱�Ż�ȡ�ͻ���
                        if (clientModel == null)//���û���ҵ��ͻ��ˣ��Ͳ�����
                        {
                            continue;
                        }

                        clientModel.LifeTime = DateTime.Now.AddSeconds(20);//��Ч���ӳ�20��

                        List<byte> byteList = new List<byte>();//����ȷ���ֽ�
                        byteList.AddRange(totalBytes.GetRange(0, 5));

                        if (totalBytes[4] == 0x03)//���Ķ�
                        {
                            _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ���յ����Ķ���Ϣ");
                            // ������Ϣ   �����豸ͨ�Ų��������
                            var infoBytes = totalBytes.GetRange(7, totalBytes.Count - 7);//�����ֽ�

                            #region ��������

                            //����ͨѶ����
                            ushort propLen = BitConverter.ToUInt16([infoBytes[1], infoBytes[0]]);//ͨѶ�����ֽ���
                            var propBytes = infoBytes.GetRange(2, propLen);//ͨѶ���������ֽ�

                            string propStr = Encoding.Default.GetString(propBytes.ToArray());//ͨѶ�����ַ���

                            _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ���յ���ͨ�������ַ���:{propStr}");

                            //����ͨѶ���ý���Ž���
                            clientModel.PropList.Add(propStr.Split(','));
                            //List[0]  ["Protocol:ModbusRTU","PortName:COM1",""]
                            //List[1]  ["Protocol:ModbusRTU","PortName:COM1",""]

                            //��������
                            int varStartIndex = 2 + propLen;//�����ֽ���ʼindex
                            ushort varLen = BitConverter.ToUInt16([infoBytes[varStartIndex + 1], infoBytes[varStartIndex]]);//��������

                            //�����ֽ�����
                            var varBytes = infoBytes.GetRange(varStartIndex + 2, varLen);
                            string varStr = Encoding.Default.GetString(varBytes.ToArray());//�����ַ���
                            _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ���յ��ı����ַ���:{varStr}");
                            clientModel.VarList.Add(varStr.Split(','));
                            //List[0]  ["D2026........-V2026................:40001:UInt16","D2026........-V2026................:40002:UInt16"]
                            //List[1]  ["D2026.....5943-V2026............1773:40001:UInt16","D2026........-V2026................:40002:UInt16"]

                            #endregion

                            byteList.Add(0x00);
                            byteList.Add(0x00);
                            client.Send(byteList.ToArray(), 0, byteList.Count, SocketFlags.None);
                        }

                        //��������
                        if (totalBytes[4] == 0x06)
                        {
                            //clientModel.LifeTime = DateTime.Now.AddSeconds(20);//��Ч���ӳ�20��
                            _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ����������Ϣ");

                            byteList.Add(0x00);
                            byteList.Add(0x00);

                            //���ͻ�����Ӧ
                            client.Send(byteList.ToArray(), 0, byteList.Count, SocketFlags.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }, cts.Token);

            TaskList.Add(t);
        }
        #endregion

        #region ���Ķ����ݵĻ�ȡ������

        /// <summary>
        /// ���Ķ����ݵĻ�ȡ������
        /// </summary>
        /// <param name="clientModel">�ͻ���model</param>
        private void MonitorData(ClientModel clientModel)
        {
            Communication communication = Communication.CreateInstance();

            var t = Task.Run(() =>
            {
                _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}��ʼ���Ķ����� �ͻ���ID:{clientModel.ClientId}");

                int deviceIndex = 0;//�豸�±�
                while (!cts.IsCancellationRequested)
                {
                    if (clientModel.PropList.Count == 0 || clientModel.VarList.Count == 0)
                    {
                        continue;
                    }

                    var prop = clientModel.PropList[deviceIndex];//ͨѶ����

                    var resultEo = communication.GetExecuteObject(
                        prop.Select(p =>
                        new DevicePropEntity { PropName = p.Split(":")[0], PropValue = p.Split(":")[1] }
                        ).ToList());
                    if (!resultEo.Status)
                    {
                        // �����쳣����
                        _logger.LogInformation($"��ȡͨѶִ�ж���ʧ��,ԭ��:{resultEo.Msg}");
                        continue;
                    }

                    var variable = clientModel.VarList[deviceIndex];//�豸����

                    deviceIndex++;
                    deviceIndex %= clientModel.PropList.Count;//�±����С���豸����

                    var varList = variable.Select(v => new VariableProp
                    {
                        VarNum = v.Split(":")[0],//D20260127164838923-V20260215154934038
                        VarAddr = v.Split(":")[1],
                        ValueType = Type.GetType("System." + v.Split(":")[2])
                    }).ToList();

                    var resultGroupAddr = resultEo.Data.GroupAddress(varList);
                    if (!resultGroupAddr.Status)
                    {
                        // �����쳣����
                        _logger.LogInformation($"�����������,ԭ��:{resultGroupAddr.Msg}");
                        continue;
                    }

                    try
                    {
                        var resultRead = resultEo.Data.Read(resultGroupAddr.Data);
                        if (!resultRead.Status)
                        {
                            // �����쳣����

                            _logger.LogInformation($"��ȡ���ݳ���,ԭ��:{resultRead.Msg}");
                            continue;
                        }
                        foreach (var varProp in varList)
                        {
                            //��ʼ��
                            if (!clientModel.Values.ContainsKey(varProp.VarNum))
                            {
                                clientModel.Values.Add(varProp.VarNum, []);
                            }

                            //�ȶ� ��һ�¾�֪ͨ
                            if (!clientModel.Values[varProp.VarNum].SequenceEqual(varProp.ReadBytes))
                            {
                                _logger.LogInformation("��ȡ�������ݣ�������");
                                // ֪ͨ��ֵ�仯
                                clientModel.Values[varProp.VarNum] = varProp.ReadBytes;

                                //������� D20260127164838923-V20260215154934038
                                byte[] varBytes = Encoding.Default.GetBytes(varProp.VarNum);

                                //���ͻ��˷��������ݵı���
                                List<byte> sendBytes =
                                [
                                    0x00,0x00,//����ID
                                     (byte)(clientModel.ClientId/256),(byte)(clientModel.ClientId%256),//�ͻ���ID
                                        0x04,//������ �̶������������ͻ��˷��Ӷ�������

                                        //���ݳ���
                                        (byte)((varBytes.Length+varProp.ReadBytes.Length+4)/256),
                                        (byte)((varBytes.Length+varProp.ReadBytes.Length+4)%256),

                                        //�����ֽ���
                                        (byte)(varBytes.Length/256),
                                        (byte)(varBytes.Length%256),
                                    ];

                                sendBytes.AddRange(varBytes);//����

                                //��ȡ���ݵ��ֽ���
                                sendBytes.Add((byte)(varProp.ReadBytes.Length / 256));
                                sendBytes.Add((byte)(varProp.ReadBytes.Length % 256));

                                sendBytes.AddRange(varProp.ReadBytes);//��ȡ�������ֽ�

                                clientModel.Client.Send(sendBytes.ToArray(), 0, sendBytes.Count, SocketFlags.None);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }, cts.Token);

            TaskList.Add(t);

        }
        #endregion

        #region ����д��ִ��
        private void ReceiveWrite(Socket client)
        {
            try
            {
                List<byte> totalBytes = new List<byte>();//���յ������ֽ�

                //1������ǰ��7���ֽ�
                byte[] respBytes = new byte[7];
                client.Receive(respBytes, 0, 7, SocketFlags.None);
                totalBytes.AddRange(respBytes);//�Ƚ����յ�7���ֽڷŽ���

                //��ȡ������ֽڳ���
                ushort dataLen = BitConverter.ToUInt16([respBytes[6], respBytes[5]]);
                if (dataLen > 0)
                {
                    byte[] dataBytes = new byte[dataLen];//�����ֽ�
                    client.Receive(dataBytes, 0, dataLen, SocketFlags.None);
                    totalBytes.AddRange(dataBytes);
                }

                // �ͻ��˱��
                ushort clientId = BitConverter.ToUInt16([totalBytes[3], totalBytes[2]]);
                var clientModel = ClientList.FirstOrDefault(c => c.ClientId == clientId);//���ݿͻ��˱�Ż�ȡ�ͻ���
                if (clientModel == null)//���û���ҵ��ͻ��ˣ��Ͳ�����
                {
                    return;
                }

                clientModel.LifeTime = DateTime.Now.AddSeconds(20);//��Ч���ӳ�20��

                List<byte> byteList = new List<byte>();//����ȷ���ֽ�
                byteList.AddRange(totalBytes.GetRange(0, 5));

                if (totalBytes[4] == 0x08)//ʵʱд
                {


                    _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ���յ����Ķ���Ϣ");
                    // ������Ϣ   �����豸ͨ�Ų��������
                    var infoBytes = totalBytes.GetRange(7, totalBytes.Count - 7);//�����ֽ�

                    #region �������Ĳ�ִ��

                    Communication communication = Communication.CreateInstance();//���õ�������

                    #region ����ͨѶ���ò���ȡִ�ж���
                    //����ͨѶ����
                    ushort propLen = BitConverter.ToUInt16([infoBytes[1], infoBytes[0]]);//ͨѶ�����ֽ���
                    var propBytes = infoBytes.GetRange(2, propLen);//ͨѶ���������ֽ�

                    string propStr = Encoding.Default.GetString(propBytes.ToArray());//ͨѶ�����ַ���

                    _logger.LogInformation($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} ʵʱд���յ���ͨ�������ַ���:{propStr}");

                    //����ͨѶ���ý���Ž���
                    //List[0]  ["Protocol:ModbusRTU","PortName:COM1",""]
                    //List[1]  ["Protocol:ModbusRTU","PortName:COM1",""]

                    var prop = propStr.Split(',');
                    var resultEo = communication.GetExecuteObject(prop.Select(p =>
                      new DevicePropEntity { PropName = p.Split(":")[0], PropValue = p.Split(":")[1] }
                      ).ToList());
                    if (!resultEo.Status)
                    {
                        // �����쳣����
                        _logger.LogInformation($"ʵʱд��ʱ�򣬻�ȡͨѶִ�ж���ʧ��,ԭ��:{resultEo.Msg}");
                        return;
                    }
                    #endregion

                    #region ��ȡ��д�ĵ�ַ
                    ushort addByteLen = BitConverter.ToUInt16([infoBytes[2 + propLen + 1], infoBytes[2 + propLen]]);
                    List<byte> addrBytes = infoBytes.GetRange(2 + propLen + 2, addByteLen);
                    string addrStr = Encoding.Default.GetString(addrBytes.ToArray());

                    _logger.LogInformation($"ʵʱд��ʱ�򣬻�ȡ�ĵ�ַ{addrStr}");

                    #endregion

                    #region ��ȡд���ֽ�
                    ushort writeByteLen = BitConverter.ToUInt16([infoBytes[2 + propLen + 2 + addByteLen + 1], infoBytes[2 + propLen + 2 + addByteLen]]);
                    List<byte> writeBytes = infoBytes.GetRange(2 + propLen + 2 + addByteLen + 2, writeByteLen);
                    #endregion

                    //ִ��д
                    var writeRst = resultEo.Data.Write(new WriteDataInfo { StartAddr = addrStr, ValueType = typeof(UInt16), WriteBytes = writeBytes.ToArray() });
                    if (writeRst.Status)//д�ɹ�
                    {
                        //���ͻ��˷���
                        List<byte> subClientBytes = totalBytes.GetRange(0, 4);

                        subClientBytes.AddRange([0x09, 0x00, 0x01, 0x00]);

                        client.Send(subClientBytes.ToArray(), 0, subClientBytes.Count, SocketFlags.None);
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {

            }

        }
        #endregion

        #region ���ͻ��˻�Ծ�ԣ���������

        /// <summary>
        ///  ���ͻ��˻�Ծ�ԣ���������
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void CheckAlive()
        {
            var t = Task.Run(async () =>
            {
                int index = 0;//�ͻ����±�
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(5000);

                    if (ClientList.Count == 0)
                    {
                        continue;
                    }

                    //������
                    if (ClientList[index].LifeTime < DateTime.Now)
                    {
                        ClientList[index].Client.Shutdown(SocketShutdown.Both);
                        ClientList[index].Client.Close();
                        ClientList[index].Client.Dispose();

                        ClientList.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }

                    index %= ClientList.Count;
                }
            }, cts.Token);

            TaskList.Add(t);
        }
        #endregion

        #region ֹͣ����

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation("ֹͣ����");
            cts.Cancel();
            Task.WaitAll(TaskList.ToArray());

            server.Shutdown(SocketShutdown.Both);
            server.Close();
            server.Dispose();
        }
        #endregion
    }
}
