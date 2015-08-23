using System;
using System.Globalization;
using System.Runtime;
using System.Threading;
using MailService.Singleton;

namespace MailService
{
    internal class MailWorker
    {
        public static long LastSendCount;
        public static long LastErrorCount;
        private static volatile bool _threadStop;
        private MassMailer _massMailer;

        public void Worker()
        {
            if (Config.CoreTest != 0)
                Logger.Log.Warn("Running in CoreTest mode ({0})", Config.CoreTest);

            SendCounter.Refresh();
            ErrorCounter.Refresh();
            LastSendCount = 0;
            LastErrorCount = 0;

            var timer = new Timer(GetStatus, null, 0, Config.NotifyPeriod * 1000);
            
            do
            {
                ProcessMassMail();
                Thread.Sleep(2000);
                
                //Trigger GC to reduce LOH fragmentation
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
            while (!_threadStop);

            timer.Dispose();
        }

        /// <summary>
        /// Метод для деликатной остановки рассылки
        /// </summary>
        public void RequestStop()
        {
            _threadStop = true;
            if (_massMailer != null)
                _massMailer.Stop();
        }

        /// <summary>
        /// Метод для выполнения массовых рассылок 
        /// </summary>
        public void ProcessMassMail()
        {
            //Инициализация 
            _massMailer = new MassMailer();
            _massMailer.MessagesSend += OnMessagesSend;
            _massMailer.Init();

            _massMailer.Run();
            _massMailer.MessagesSend -= OnMessagesSend;
        }

        /// <summary>
        /// Получает текущее значение счетчика отправленных писем
        /// </summary>
        private void GetStatus(object sender)
        {
            var curSendCount = SendCounter.Read();
            var curErrorCount = ErrorCounter.Read();
            //If massMail object was recreated
            LastSendCount = (curSendCount < LastSendCount) ? 0 : LastSendCount;
            LastErrorCount = (curErrorCount < LastErrorCount) ? 0 : LastErrorCount;
            Zabbix.Sender.SendData(new ZabbixItem { Host = Config.HostKey, Key = Config.SendCountKey, Value = (curSendCount - LastSendCount).ToString(CultureInfo.InvariantCulture) });
            Zabbix.Sender.SendData(new ZabbixItem { Host = Config.HostKey, Key = Config.ErrorCountKey, Value = (curErrorCount - LastErrorCount).ToString(CultureInfo.InvariantCulture) });
            Logger.Log.Debug("Send to zabbix - send: {0}, errors: {1}", curSendCount - LastSendCount, curErrorCount - LastErrorCount);
            Logger.Log.Debug("Total - send: {0}, errors: {1}", curSendCount, curErrorCount);
            LastSendCount = curSendCount;
            LastErrorCount = curErrorCount;
	    }

        /// <summary>
        /// Обработчик события об отправленных сообщениях
        /// </summary>
        public void OnMessagesSend(long sendCount, long errorCount)
        {
            SendCounter.Add(sendCount);
            ErrorCounter.Add(errorCount);
            Logger.Log.Debug("Messages processed - send: {0} + errors: {1}", SendCounter.Read(), ErrorCounter.Read());
        }
	}
}