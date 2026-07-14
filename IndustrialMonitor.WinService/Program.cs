namespace IndustrialMonitor.WinService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ////1����������������
            //var builder = Host.CreateApplicationBuilder(args);

            ////2��ע���Զ���ĺ�̨����worker дҵ���߼���
            //builder.Services.AddHostedService<Worker>();

            ////3����������ʵ��
            //var host = builder.Build();

            ////4��������������к�̨����
            //host.Run();

            IHost host = Host.CreateDefaultBuilder(args)
              .UseWindowsService()
              .ConfigureServices(services =>
              {
                  services.AddHostedService<Worker>();
              })
              .Build();

            host.Run();
        }
    }
}