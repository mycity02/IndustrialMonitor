using NLog;

namespace IndustrialMonitor.Logger
{
    /// <summary>
    /// NLog日志服务
    /// </summary>
    public class NLogLoggerService<T> : ILoggerService<T>
    {
        private readonly ILogger _logger;

        public NLogLoggerService()
        {
            _logger = LogManager.GetLogger(typeof(T).FullName ?? typeof(T).Name);
        }

        public void Trace(string message) => _logger.Trace(message);
        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Warn(string message) => _logger.Warn(message);

        public void Error(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                _logger.Error(message);
            }
            else
            {
                _logger.Error(ex, message);
            }
        }

        public void Fatal(string message, Exception? ex = null)
        {
            if (ex == null)
            {
                _logger.Fatal(message);
            }
            else
            {
                _logger.Fatal(ex, message);
            }
        }
    }
}