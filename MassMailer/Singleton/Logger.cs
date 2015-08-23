using NLog;

namespace MailService.Singleton
{
    /// <summary>
    /// Singleton: logging class
    /// </summary>
    static class Logger
    {
        public static NLog.Logger Log { get; private set; }

        static Logger()
        {
            Log = LogManager.GetLogger("Global");
        }
    }
}
