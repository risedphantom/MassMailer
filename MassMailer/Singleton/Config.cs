using System;
using System.Configuration;

namespace MailService.Singleton
{
    /// <summary>
    /// Singleton: configuration class
    /// </summary>
    static class Config
    {
        #region --- Strongly typed settings ---

        public static string Mode { get; private set; }
        public static string ConnectionString { get; private set; }
        public static string MailServer { get; private set; }
        public static string ZabbixServer { get; private set; }
        public static string SendCountKey { get; private set; }
        public static string ErrorCountKey { get; private set; }
        public static string StatusKey { get; private set; }
        public static string HostKey { get; private set; }
        public static string UserAgent { get; private set; }
        public static string PrivateKeyFolder { get; private set; }
        public static string ReturnPath { get; private set; }
        public static int ParseXmlBufferSize { get; private set; }
        public static int ParseXmlMaxdop { get; private set; }
        public static int SendEmailsMaxdop { get; private set; }
        public static int BatchSize { get; private set; }
        public static int BlockSize { get; private set; }
        public static int NotifyAfter { get; private set; }
        public static int CoreTest { get; private set; }
        public static int MailPort { get; private set; }
        public static int ZabbixPort { get; private set; }
        public static int NotifyPeriod { get; private set; }
        
        #endregion

        static Config()
        {
            try
			{
                ConnectionString = ConfigurationManager.ConnectionStrings["connection"].ConnectionString;
                MailServer = ConfigurationManager.AppSettings["mail.server"];
                MailPort = Convert.ToInt32(ConfigurationManager.AppSettings["mail.port"]);
                Mode = ConfigurationManager.AppSettings["mode"];
                UserAgent = ConfigurationManager.AppSettings["useragent"];
                PrivateKeyFolder = ConfigurationManager.AppSettings["privatekeyfolder"];
                ReturnPath = ConfigurationManager.AppSettings["returnpath"];

                ParseXmlBufferSize = Convert.ToInt32(ConfigurationManager.AppSettings["parsexmlbuffersize"]);
                ParseXmlMaxdop = Convert.ToInt32(ConfigurationManager.AppSettings["parsexmlmaxdop"]);
                SendEmailsMaxdop = Convert.ToInt32(ConfigurationManager.AppSettings["sendemailsmaxdop"]);
                BatchSize = Convert.ToInt32(ConfigurationManager.AppSettings["batchsize"]);
                BlockSize = Convert.ToInt32(ConfigurationManager.AppSettings["blocksize"]);
                NotifyAfter = Convert.ToInt32(ConfigurationManager.AppSettings["notifyafter"]);
                CoreTest = Convert.ToInt32(ConfigurationManager.AppSettings["coretest"]);

                //параметры для Zabbix
                ZabbixServer = ConfigurationManager.AppSettings["zabbix.server"];
                ZabbixPort = Convert.ToInt32(ConfigurationManager.AppSettings["zabbix.port"]);
                HostKey = ConfigurationManager.AppSettings["hostkey"];
                NotifyPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["notifyperiod"]);
                SendCountKey = ConfigurationManager.AppSettings["sendcountkey"] + NotifyPeriod;
                ErrorCountKey = ConfigurationManager.AppSettings["errorcountkey"] + NotifyPeriod;
                StatusKey = ConfigurationManager.AppSettings["statuskey"];
            }
            catch (Exception ex)
            {
                Logger.Log.Fatal("Error at reading config: {0}", ex.Message);
                throw;
            }
        }
    }
}
