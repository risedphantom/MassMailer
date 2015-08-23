using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace MailService.Singleton
{
    /// <summary>
    /// Singleton: zabbix sender class
    /// </summary>
    static class Zabbix
    {
        public static ZabbixSender Sender { get; private set; }

        static Zabbix()
        {
            Sender = new ZabbixSender(Config.ZabbixServer, Config.ZabbixPort);
        }
    }

    struct ZabbixItem
    {
        public string Host;
        public string Key;
        public string Value;
    }

    class ZabbixSender
    {
        internal struct SendItem
        {
            // ReSharper disable InconsistentNaming - Zabbix is case sensitive
            public string host;
            public string key;
            public string value;
            public string clock;
            // ReSharper restore InconsistentNaming
        }

        #pragma warning disable 0649
        internal struct ZabbixResponse
        {
            public string Response;
            public string Info;
        }
        #pragma warning restore 0649

        #region --- Constants ---

        public const string DefaultHeader = "ZBXD\x01";
        public const string SendRequest = "sender data";
        public const int DefaultTimeout = 10000;

        #endregion

        #region --- Fields ---
        private readonly DateTime _dtUnixMinTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
        private readonly int _timeout;
        private readonly string _zabbixserver;
        private readonly int _zabbixport;
        #endregion

        #region --- Constructors ---

        public ZabbixSender(string zabbixserver, int zabbixport)
            : this(zabbixserver, zabbixport, DefaultTimeout)
        {
        }

        public ZabbixSender(string zabbixserver, int zabbixport, int timeout)
        {
            _zabbixserver = zabbixserver;
            _zabbixport = zabbixport;
            _timeout = timeout;
        }
        #endregion

        #region --- Methods ---

        public string SendData(ZabbixItem itm)
        {
            return SendData(new List<ZabbixItem>(1) { itm });
        }

        public string SendData(List<ZabbixItem> lstData)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                var values = new List<SendItem>(lstData.Count);

                values.AddRange(lstData.Select(itm => new SendItem
                {
                    host = itm.Host,
                    key = itm.Key,
                    value = itm.Value,
                    clock = Math.Floor((DateTime.Now.ToUniversalTime() - _dtUnixMinTime).TotalSeconds).ToString(CultureInfo.InvariantCulture)
                }));

                var json = serializer.Serialize(new
                {
                    request = SendRequest,
                    data = values.ToArray()
                });

                var header = Encoding.ASCII.GetBytes(DefaultHeader);
                var length = BitConverter.GetBytes((long)json.Length);
                var data = Encoding.ASCII.GetBytes(json);

                var packet = new byte[header.Length + length.Length + data.Length];
                Buffer.BlockCopy(header, 0, packet, 0, header.Length);
                Buffer.BlockCopy(length, 0, packet, header.Length, length.Length);
                Buffer.BlockCopy(data, 0, packet, header.Length + length.Length, data.Length);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(_zabbixserver, _zabbixport);

                    socket.Send(packet);

                    //Header
                    var buffer = new byte[5];
                    ReceivData(socket, buffer, 0, buffer.Length, _timeout);

                    if (DefaultHeader != Encoding.ASCII.GetString(buffer, 0, buffer.Length))
                        throw new Exception("Invalid header");

                    //Message length
                    buffer = new byte[8];
                    ReceivData(socket, buffer, 0, buffer.Length, _timeout);
                    var dataLength = BitConverter.ToInt32(buffer, 0);

                    if (dataLength == 0)
                        throw new Exception("Invalid data length");

                    //Message
                    buffer = new byte[dataLength];
                    ReceivData(socket, buffer, 0, buffer.Length, _timeout);

                    var response = serializer.Deserialize<ZabbixResponse>(Encoding.ASCII.GetString(buffer, 0, buffer.Length));
                    return string.Format("Response: {0}, Info: {1}", response.Response, response.Info);
                }
            }
            catch (Exception e)
            {
                return string.Format("Exception: {0}", e);
            }
        }

        private static void ReceivData(Socket pObjSocket, byte[] buffer, int offset, int size, int timeout)
        {
            var startTickCount = Environment.TickCount;
            var received = 0;
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new TimeoutException();

                try
                {
                    received += pObjSocket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.IOPending || ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                        Thread.Sleep(30);
                    else
                        throw;
                }
            } while (received < size);
        }

        #endregion
    }
}
