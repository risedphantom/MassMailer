using System;
using System.Reflection;
using System.Threading;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using MailService.Singleton;
using ThreadState = System.Threading.ThreadState;

namespace MailService
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int serviceType;
        public int currentState;
        public int controlsAccepted;
        public int win32ExitCode;
        public int serviceSpecificExitCode;
        public int checkPoint;
        public int waitHint;
    }

    public enum State
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    // Custom commands
    public enum CustomCommand
    {
        DELICATE_STOP = 0x00000080
    }

    public class MailService : ServiceBase
    {
        [DllImport("ADVAPI32.DLL", EntryPoint = "SetServiceStatus")]
        public static extern bool SetServiceStatus(IntPtr hServiceStatus, ServiceStatus lpServiceStatus);
        private ServiceStatus _myServiceStatus;
        private static readonly ManualResetEvent Pause = new ManualResetEvent(false);
        private MailWorker _mailWorker;
        private Thread _workerThread;

        static void Main()
        {
            var services = new ServiceBase[]
			{
				new MailService()
			};

            Run(services);
        }

        // Start the service.
        protected override void OnStart(string[] args)
        {
            try
            {
                var handle = ServiceHandle;
                _myServiceStatus.currentState = (int)State.SERVICE_START_PENDING;
                SetServiceStatus(handle, _myServiceStatus);

                // Start a separate thread that does the actual work.
                if ((_workerThread == null) || ((_workerThread.ThreadState & (ThreadState.Unstarted | ThreadState.Stopped)) != 0))
                {
                    Logger.Log.Info("-=START=-");
                    // Build status info
                    var statusInfo = string.Format(@"
                        <html>MODE: <font size='3' color='red'>{0}</font><br>
                            VERSION: <font size='3' color='red'>{1}</font>
                        </html>", Config.Mode, Assembly.GetEntryAssembly().GetName().Version);
                    Zabbix.Sender.SendData(new ZabbixItem { Host = Config.HostKey, Key = Config.StatusKey, Value = statusInfo });
                    
                    _mailWorker = new MailWorker();
                    _workerThread = new Thread(_mailWorker.Worker);
                    _workerThread.Start();
                }
                if (_workerThread != null)
                    Logger.Log.Debug("Worker thread state = {0}", _workerThread.ThreadState.ToString());

                _myServiceStatus.currentState = (int)State.SERVICE_RUNNING;
                SetServiceStatus(handle, _myServiceStatus);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Error on start: {0}", ex.Message);
            }
        }

        protected override void OnStop()
        {
            Logger.Log.Info("-=STOP=-");
            if ((_workerThread != null) && (_workerThread.IsAlive))
            {
                Pause.Reset();
                Thread.Sleep(1000);
                _workerThread.Abort();
            }
            ExitCode = 0;
        }

        protected override void OnCustomCommand(int command)
        {
            switch (command)
            {
                case (int)CustomCommand.DELICATE_STOP:
                    Logger.Log.Info("-=DELICATE STOP=-");
                    _mailWorker.RequestStop();

                    while (_workerThread != null && _workerThread.IsAlive)
                        Thread.Sleep(100);

                    Stop();
                    break;
            }
        }
    }
}