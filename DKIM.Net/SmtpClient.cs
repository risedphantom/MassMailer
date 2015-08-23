//
// System.Net.Mail.SmtpClient.cs
//
// Author:
//	Tim Coleman (tim@timcoleman.com)
//
// Copyright (C) Tim Coleman, 2004
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//
// Modified by Topten Software (contact@toptensoftware.com) to support
// DomainKey and DKIM signed emails.
//
// Domain key and DKIM implementation based on DKIM.NET by Damien McGivern
// https://github.com/dmcgiv/DKIM.Net
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Net.Configuration;
using System.Configuration;
using System.Net.Security;
using System.Net.Mail;
using System.Security.Authentication;

using X509CertificateCollection = System.Security.Cryptography.X509Certificates.X509CertificateCollection;

namespace DKIM
{
	public class SmtpClient
	: IDisposable
	{
		#region Fields

		string host;
		int port;
		int timeout = 100000;
		ICredentialsByHost credentials;
		string pickupDirectoryLocation;
		SmtpDeliveryMethod deliveryMethod;
		bool enableSsl;
		X509CertificateCollection clientCertificates;

		TcpClient client;
		Stream stream;
		StreamWriter writer;
		StreamReader reader;
		int boundaryIndex;
		MailAddress defaultFrom;

		MailMessage messageInProcess;

		BackgroundWorker worker;
		object user_async_state;

		[Flags]
		enum AuthMechs
		{
			None = 0,
			Login = 0x01,
			Plain = 0x02,
		}

		class CancellationException : Exception
		{
		}

		AuthMechs authMechs;
		Mutex mutex = new Mutex();

		#endregion // Fields


		#region Lifted helpers

		internal static string To2047(byte[] bytes)
		{
			System.IO.StringWriter writer = new System.IO.StringWriter();
			foreach (byte i in bytes)
			{
				if (i > 127 || i == '\t')
				{
					writer.Write("=");
					writer.Write(Convert.ToString(i, 16).ToUpper());
				}
				else
					writer.Write(Convert.ToChar(i));
			}
			return writer.GetStringBuilder().ToString();
		}

		internal static string EncodeSubjectRFC2047(string s, Encoding enc)
		{
			if (s == null || Encoding.ASCII.Equals(enc))
				return s;
			for (int i = 0; i < s.Length; i++)
				if (s[i] >= '\u0080')
				{
					string quoted = To2047(enc.GetBytes(s));
					return String.Concat("=?", enc.HeaderName, "?Q?", quoted, "?=");
				}
			return s;
		}

		internal static TransferEncoding GuessTransferEncoding(Encoding enc)
		{
			if (Encoding.ASCII.Equals(enc))
				return TransferEncoding.SevenBit;
			else if (Encoding.UTF8.CodePage == enc.CodePage || Encoding.Unicode.CodePage == enc.CodePage || Encoding.UTF32.CodePage == enc.CodePage)
				return TransferEncoding.Base64;
			else
				return TransferEncoding.QuotedPrintable;
		}



		internal static TransferEncoding GetContentTransferEncoding(MailMessage message)
		{
			return GuessTransferEncoding(message.BodyEncoding);
		}

		internal static ContentType GetBodyContentType(MailMessage message)
		{
			ContentType ct = new ContentType(message.IsBodyHtml ? "text/html" : "text/plain");
			ct.CharSet = (message.BodyEncoding ?? Encoding.ASCII).HeaderName;
			return ct;
		}

		#endregion

		#region Constructors

		public SmtpClient()
			: this(null, 0)
		{
		}

		public SmtpClient(string host)
			: this(host, 0)
		{
		}

		public SmtpClient(string host, int port)
		{
			SmtpSection cfg = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

			if (cfg != null)
			{
				this.host = cfg.Network.Host;
				this.port = cfg.Network.Port;
				this.enableSsl = cfg.Network.EnableSsl;
				TargetName = cfg.Network.TargetName;
				if (this.TargetName == null)
					TargetName = "SMTPSVC/" + (host != null ? host : "");


				if (cfg.Network.UserName != null)
				{
					string password = String.Empty;

					if (cfg.Network.Password != null)
						password = cfg.Network.Password;

					Credentials = new CCredentialsByHost(cfg.Network.UserName, password);
				}

				if (!String.IsNullOrEmpty(cfg.From))
					defaultFrom = new MailAddress(cfg.From);
			}

			if (!String.IsNullOrEmpty(host))
				this.host = host;

			if (port != 0)
				this.port = port;
		}

		#endregion // Constructors

		#region Properties

		public X509CertificateCollection ClientCertificates
		{
			get
			{
				if (clientCertificates == null)
					clientCertificates = new X509CertificateCollection();
				return clientCertificates;
			}
		}

		public string TargetName { get; set; }

		public ICredentialsByHost Credentials
		{
			get { return credentials; }
			set
			{
				CheckState();
				credentials = value;
			}
		}

		public SmtpDeliveryMethod DeliveryMethod
		{
			get { return deliveryMethod; }
			set
			{
				CheckState();
				deliveryMethod = value;
			}
		}

		public bool EnableSsl
		{
			get { return enableSsl; }
			set
			{
				CheckState();
				enableSsl = value;
			}
		}

		public string Host
		{
			get { return host; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");
				if (value.Length == 0)
					throw new ArgumentException("An empty string is not allowed.", "value");
				CheckState();
				host = value;
			}
		}

		public string PickupDirectoryLocation
		{
			get { return pickupDirectoryLocation; }
			set { pickupDirectoryLocation = value; }
		}

		public int Port
		{
			get { return port; }
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException("value");
				CheckState();
				port = value;
			}
		}

		public ServicePoint ServicePoint
		{
			get { throw new NotImplementedException(); }
		}

		public int Timeout
		{
			get { return timeout; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value");
				CheckState();
				timeout = value;
			}
		}

		public bool UseDefaultCredentials
		{
			get { return false; }
			set
			{
				if (value)
					throw new NotImplementedException("Default credentials are not supported");
				CheckState();
			}
		}

		#endregion // Properties

		#region Events

		public event SendCompletedEventHandler SendCompleted;

		#endregion // Events

		#region Methods
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			// TODO: We should close all the connections and abort any async operations here
		}

		private void CheckState()
		{
			if (messageInProcess != null)
				throw new InvalidOperationException("Cannot set Timeout while Sending a message");
		}

		private static string EncodeAddress(MailAddress address)
		{
			if (!String.IsNullOrEmpty(address.DisplayName))
			{
				string encodedDisplayName = EncodeSubjectRFC2047(address.DisplayName, Encoding.UTF8);
				return "\"" + encodedDisplayName + "\" <" + address.Address + ">";
			}
			return address.ToString();
		}

		private static string EncodeAddresses(MailAddressCollection addresses)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (MailAddress address in addresses)
			{
				if (!first)
				{
					sb.Append(", ");
				}
				sb.Append(EncodeAddress(address));
				first = false;
			}
			return sb.ToString();
		}

		private string EncodeSubjectRFC2047(MailMessage message)
		{
			return EncodeSubjectRFC2047(message.Subject, message.SubjectEncoding);
		}

		private string EncodeBody(MailMessage message)
		{
			string body = message.Body;
			Encoding encoding = message.BodyEncoding;
			// RFC 2045 encoding
			switch (GetContentTransferEncoding(message))
			{
				case TransferEncoding.SevenBit:
					return body;
				case TransferEncoding.Base64:
					return Convert.ToBase64String(encoding.GetBytes(body), Base64FormattingOptions.InsertLineBreaks);
				default:
					return ToQuotedPrintable(body, encoding);
			}
		}

		private string EncodeBody(AlternateView av)
		{
			//Encoding encoding = av.ContentType.CharSet != null ? Encoding.GetEncoding (av.ContentType.CharSet) : Encoding.UTF8;

			byte[] bytes = new byte[av.ContentStream.Length];
			av.ContentStream.Read(bytes, 0, bytes.Length);

			// RFC 2045 encoding
			switch (av.TransferEncoding)
			{
				case TransferEncoding.SevenBit:
					return Encoding.ASCII.GetString(bytes);
				case TransferEncoding.Base64:
					return Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);
				default:
					return ToQuotedPrintable(bytes);
			}
		}


		private void EndSection(string section)
		{
			SendData(String.Format("--{0}--", section));
			SendData(string.Empty);
		}

		private string GenerateBoundary()
		{
			string output = GenerateBoundary(boundaryIndex);
			boundaryIndex += 1;
			return output;
		}

		private static string GenerateBoundary(int index)
		{
			return String.Format("--boundary_{0}_{1}", index, Guid.NewGuid().ToString("D"));
		}

		private bool IsError(SmtpResponse status)
		{
			return ((int)status.StatusCode) >= 400;
		}

		protected void OnSendCompleted(AsyncCompletedEventArgs e)
		{
			try
			{
				if (SendCompleted != null)
					SendCompleted(this, e);
			}
			finally
			{
				worker = null;
				user_async_state = null;
			}
		}

		private void CheckCancellation()
		{
			if (worker != null && worker.CancellationPending)
				throw new CancellationException();
		}

		private SmtpResponse Read()
		{
			byte[] buffer = new byte[512];
			int position = 0;
			bool lastLine = false;

			do
			{
				CheckCancellation();

				int readLength = stream.Read(buffer, position, buffer.Length - position);
				if (readLength > 0)
				{
					int available = position + readLength - 1;
					if (available > 4 && (buffer[available] == '\n' || buffer[available] == '\r'))
						for (int index = available - 3; ; index--)
						{
							if (index < 0 || buffer[index] == '\n' || buffer[index] == '\r')
							{
								lastLine = buffer[index + 4] == ' ';
								break;
							}
						}

					// move position
					position += readLength;

					// check if buffer is full
					if (position == buffer.Length)
					{
						byte[] newBuffer = new byte[buffer.Length * 2];
						Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);
						buffer = newBuffer;
					}
				}
				else
				{
					break;
				}
			} while (!lastLine);

			if (position > 0)
			{
				Encoding encoding = new ASCIIEncoding();

				string line = encoding.GetString(buffer, 0, position - 1);

				// parse the line to the lastResponse object
				SmtpResponse response = SmtpResponse.Parse(line);

				return response;
			}
			else
			{
				throw new System.IO.IOException("Connection closed");
			}
		}

		void ResetExtensions()
		{
			authMechs = AuthMechs.None;
		}

		void ParseExtensions(string extens)
		{
			string[] parts = extens.Split('\n');

			foreach (string part in parts)
			{
				if (part.Length < 4)
					continue;

				string start = part.Substring(4);
				if (start.StartsWith("AUTH ", StringComparison.Ordinal))
				{
					string[] options = start.Split(' ');
					for (int k = 1; k < options.Length; k++)
					{
						string option = options[k].Trim();
						// GSSAPI, KERBEROS_V4, NTLM not supported
						switch (option)
						{
							/*
							case "CRAM-MD5":
								authMechs |= AuthMechs.CramMD5;
								break;
							case "DIGEST-MD5":
								authMechs |= AuthMechs.DigestMD5;
								break;
							*/
							case "LOGIN":
								authMechs |= AuthMechs.Login;
								break;
							case "PLAIN":
								authMechs |= AuthMechs.Plain;
								break;
						}
					}
				}
			}
		}

		public void Send(MailMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (deliveryMethod == SmtpDeliveryMethod.Network && (Host == null || Host.Trim().Length == 0))
				throw new InvalidOperationException("The SMTP host was not specified");
			else if (deliveryMethod == SmtpDeliveryMethod.PickupDirectoryFromIis)
				throw new NotSupportedException("IIS delivery is not supported");

			if (port == 0)
				port = 25;

			// Block while sending
			mutex.WaitOne();
			try
			{
				messageInProcess = message;
				if (deliveryMethod == SmtpDeliveryMethod.SpecifiedPickupDirectory)
					SendToFile(message);
				else
					SendInternal(message);
			}
			catch (CancellationException)
			{
				// This exception is introduced for convenient cancellation process.
			}
			catch (SmtpException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new SmtpException("Message could not be sent.", ex);
			}
			finally
			{
				// Release the mutex to allow other threads access
				mutex.ReleaseMutex();
				messageInProcess = null;
			}
		}

		private void SendInternal(MailMessage message)
		{
			CheckCancellation();

			try
			{
				client = new TcpClient(host, port);
				stream = client.GetStream();
				// FIXME: this StreamWriter creation is bogus.
				// It expects as if a Stream were able to switch to SSL
				// mode (such behavior is only in Mainsoft Socket API).
				writer = new StreamWriter(stream);
				reader = new StreamReader(stream);

				SendCore(message);
			}
			finally
			{
				if (writer != null)
					writer.Close();
				if (reader != null)
					reader.Close();
				if (stream != null)
					stream.Close();
				if (client != null)
					client.Close();
			}
		}

		public DomainKeySigner DomainKeysSigner { get; set; }
		public DkimSigner DkimSigner { get; set; }

		enum CollectMode
		{
			None,
			Headers,
			Content,
		};

		CollectMode m_CollectMode=CollectMode.None;

		List<EmailHeader> m_collectedHeaders;
		StringBuilder m_collectedContent;

		void StartCollecting()
		{
			if (DomainKeysSigner == null && DkimSigner == null)
			{
				m_CollectMode = CollectMode.None;
				return;
			}

			m_CollectMode = CollectMode.Headers;
			m_collectedHeaders = new List<EmailHeader>();
			m_collectedContent = new StringBuilder();
		}

		void FinishCollecting()
		{
			if (m_CollectMode == CollectMode.None)
				return;

			var e = new Email() 
			{ 
				Headers = m_collectedHeaders, 
				Body = m_collectedContent.ToString(),
				Encoding = writer.Encoding
			};

			// Generate Dkim signature
			if (DkimSigner != null)
			{
				m_collectedHeaders.Insert(0, DkimSigner.SignMessage(e));
			}

			// Generate domain keys signature
			if (DomainKeysSigner != null)
			{
				m_collectedHeaders.Insert(0, DomainKeysSigner.SignMessage(e));
			}

			m_CollectMode = CollectMode.None;

			// Write all headers
			foreach (var h in m_collectedHeaders)
			{
				SendHeader(h.Key, h.Value);
			}

			// Write the separator
			SendData(string.Empty);

			// Write the content
			SendData(e.Body);
		}

		private void SendToWriter(MailMessage message)
		{
			StartCollecting();

			// Send message headers
			string dt = DateTime.Now.ToString("ddd, dd MMM yyyy HH':'mm':'ss zzz", DateTimeFormatInfo.InvariantInfo);
			// remove ':' from time zone offset (e.g. from "+01:00")
			dt = dt.Remove(dt.Length - 3, 1);
			SendHeader(HeaderName.Date, dt);

			MailAddress from = message.From;
			if (from == null)
				from = defaultFrom;

			SendHeader(HeaderName.From, EncodeAddress(from));
			SendHeader(HeaderName.To, EncodeAddresses(message.To));
			if (message.CC.Count > 0)
				SendHeader(HeaderName.Cc, EncodeAddresses(message.CC));
			SendHeader(HeaderName.Subject, EncodeSubjectRFC2047(message));

			string v = "normal";

			switch (message.Priority)
			{
				case MailPriority.Normal:
					v = "normal";
					break;

				case MailPriority.Low:
					v = "non-urgent";
					break;

				case MailPriority.High:
					v = "urgent";
					break;
			}
			SendHeader("Priority", v);
			if (message.Sender != null)
				SendHeader("Sender", EncodeAddress(message.From));
			if (message.ReplyToList.Count > 0)
				SendHeader("Reply-To", EncodeAddresses(message.ReplyToList));

			foreach (string s in message.Headers.AllKeys)
				SendHeader(s, EncodeSubjectRFC2047(message.Headers[s], message.HeadersEncoding));

			AddPriorityHeader(message);

			boundaryIndex = 0;
			if (message.Attachments.Count > 0)
				SendWithAttachments(message);
			else
				SendWithoutAttachments(message, null, false);

			FinishCollecting();
		}

		// FIXME: simple implementation, could be brushed up.
		private void SendToFile(MailMessage message)
		{
			if (!Path.IsPathRooted(pickupDirectoryLocation))
				throw new SmtpException("Only absolute directories are allowed for pickup directory.");

			string filename = Path.Combine(pickupDirectoryLocation,
				Guid.NewGuid() + ".eml");

			try
			{
				writer = new StreamWriter(filename);
				SendToWriter(message);

			}
			finally
			{
				if (writer != null) writer.Close(); writer = null;
			}
		}

		public string SendToString(MailMessage message)
		{
			try
			{
				var ms = new MemoryStream();
				writer = new StreamWriter(ms);
				SendToWriter(message);
				writer.Flush();
				ms.Flush();
				return Encoding.UTF8.GetString(ms.GetBuffer());
			}
			finally
			{
				if (writer != null) writer.Close(); writer = null;
			}
		}


		private void SendCore(MailMessage message)
		{
			SmtpResponse status;

			status = Read();
			if (IsError(status))
				throw new SmtpException(status.StatusCode, status.Description);

			// EHLO

			// FIXME: parse the list of extensions so we don't bother wasting
			// our time trying commands if they aren't supported.
			status = SendCommand("EHLO " + Dns.GetHostName());

			if (IsError(status))
			{
				status = SendCommand("HELO " + Dns.GetHostName());

				if (IsError(status))
					throw new SmtpException(status.StatusCode, status.Description);
			}
			else
			{
				// Parse ESMTP extensions
				string extens = status.Description;

				if (extens != null)
					ParseExtensions(extens);
			}

			if (enableSsl)
			{
				InitiateSecureConnection();
				ResetExtensions();
				writer = new StreamWriter(stream);
				reader = new StreamReader(stream);
				status = SendCommand("EHLO " + Dns.GetHostName());

				if (IsError(status))
				{
					status = SendCommand("HELO " + Dns.GetHostName());

					if (IsError(status))
						throw new SmtpException(status.StatusCode, status.Description);
				}
				else
				{
					// Parse ESMTP extensions
					string extens = status.Description;
					if (extens != null)
						ParseExtensions(extens);
				}
			}

			if (authMechs != AuthMechs.None)
				Authenticate();

			// The envelope sender: use 'Sender:' in preference of 'From:'
			MailAddress sender = message.Sender;
			if (sender == null)
				sender = message.From;
			if (sender == null)
				sender = defaultFrom;

			// MAIL FROM:
			status = SendCommand("MAIL FROM:<" + sender.Address + '>');
			if (IsError(status))
			{
				throw new SmtpException(status.StatusCode, status.Description);
			}

			// Send RCPT TO: for all recipients
			List<SmtpFailedRecipientException> sfre = new List<SmtpFailedRecipientException>();

			for (int i = 0; i < message.To.Count; i++)
			{
				status = SendCommand("RCPT TO:<" + message.To[i].Address + '>');
				if (IsError(status))
					sfre.Add(new SmtpFailedRecipientException(status.StatusCode, message.To[i].Address));
			}
			for (int i = 0; i < message.CC.Count; i++)
			{
				status = SendCommand("RCPT TO:<" + message.CC[i].Address + '>');
				if (IsError(status))
					sfre.Add(new SmtpFailedRecipientException(status.StatusCode, message.CC[i].Address));
			}
			for (int i = 0; i < message.Bcc.Count; i++)
			{
				status = SendCommand("RCPT TO:<" + message.Bcc[i].Address + '>');
				if (IsError(status))
					sfre.Add(new SmtpFailedRecipientException(status.StatusCode, message.Bcc[i].Address));
			}

			if (sfre.Count > 0)
				throw new SmtpFailedRecipientsException("failed recipients", sfre.ToArray());

			// DATA
			status = SendCommand("DATA");
			if (IsError(status))
				throw new SmtpException(status.StatusCode, status.Description);

			SendToWriter(message);

			SendDot();

			status = Read();
			if (IsError(status))
				throw new SmtpException(status.StatusCode, status.Description);

			try
			{
				status = SendCommand("QUIT");
			}
			catch (System.IO.IOException)
			{
				// We excuse server for the rude connection closing as a response to QUIT
			}
		}

		public void Send(string from, string to, string subject, string body)
		{
			Send(new MailMessage(from, to, subject, body));
		}

		private void SendDot()
		{
			writer.Write(".\r\n");
			writer.Flush();
		}

		private void SendData(string data)
		{
			// Check for end of headers
			if (m_CollectMode == CollectMode.Headers && data == String.Empty)
			{
				m_CollectMode = CollectMode.Content;
				return;
			}

			// Collecting content
			if (m_CollectMode==CollectMode.Content)
			{
				if (!String.IsNullOrEmpty(data))
					m_collectedContent.Append(data);
				m_collectedContent.Append("\r\n");
				return;
			}

			// Write it
			if (String.IsNullOrEmpty(data))
			{
				writer.Write("\r\n");
				writer.Flush();
				return;
			}

			StringReader sr = new StringReader(data);
			string line;
			bool escapeDots = deliveryMethod == SmtpDeliveryMethod.Network;
			while ((line = sr.ReadLine()) != null)
			{
				CheckCancellation();

				if (escapeDots && line.StartsWith("."))
				{
					line = "." + line;
				}
				writer.Write(line);
				writer.Write("\r\n");
			}
			writer.Flush();
		}

		public void SendAsync(MailMessage message, object userToken)
		{
			if (worker != null)
				throw new InvalidOperationException("Another SendAsync operation is in progress");

			worker = new BackgroundWorker();
			worker.DoWork += delegate(object o, DoWorkEventArgs ea)
			{
				try
				{
					user_async_state = ea.Argument;
					Send(message);
				}
				catch (Exception ex)
				{
					ea.Result = ex;
					throw ex;
				}
			};
			worker.WorkerSupportsCancellation = true;
			worker.RunWorkerCompleted += delegate(object o, RunWorkerCompletedEventArgs ea)
			{
				// Note that RunWorkerCompletedEventArgs.UserState cannot be used (LAMESPEC)
				OnSendCompleted(new AsyncCompletedEventArgs(ea.Error, ea.Cancelled, user_async_state));
			};
			worker.RunWorkerAsync(userToken);
		}

		public void SendAsync(string from, string to, string subject, string body, object userToken)
		{
			SendAsync(new MailMessage(from, to, subject, body), userToken);
		}

		public void SendAsyncCancel()
		{
			if (worker == null)
				throw new InvalidOperationException("SendAsync operation is not in progress");
			worker.CancelAsync();
		}

		private void AddPriorityHeader(MailMessage message)
		{
			switch (message.Priority)
			{
				case MailPriority.High:
					SendHeader(HeaderName.Priority, "Urgent");
					SendHeader(HeaderName.Importance, "high");
					SendHeader(HeaderName.XPriority, "1");
					break;
				case MailPriority.Low:
					SendHeader(HeaderName.Priority, "Non-Urgent");
					SendHeader(HeaderName.Importance, "low");
					SendHeader(HeaderName.XPriority, "5");
					break;
			}
		}

		private void SendSimpleBody(MailMessage message)
		{
			SendHeader(HeaderName.ContentType, GetBodyContentType(message).ToString());
			if (GetContentTransferEncoding(message) != TransferEncoding.SevenBit)
				SendHeader(HeaderName.ContentTransferEncoding, GetTransferEncodingName(GetContentTransferEncoding(message)));
			SendData(string.Empty);

			SendData(EncodeBody(message));
		}

		private void SendBodylessSingleAlternate(AlternateView av)
		{
			SendHeader(HeaderName.ContentType, av.ContentType.ToString());
			if (av.TransferEncoding != TransferEncoding.SevenBit)
				SendHeader(HeaderName.ContentTransferEncoding, GetTransferEncodingName(av.TransferEncoding));
			SendData(string.Empty);

			SendData(EncodeBody(av));
		}

		private void SendWithoutAttachments(MailMessage message, string boundary, bool attachmentExists)
		{
			if (message.Body == null && message.AlternateViews.Count == 1)
				SendBodylessSingleAlternate(message.AlternateViews[0]);
			else if (message.AlternateViews.Count > 0)
				SendBodyWithAlternateViews(message, boundary, attachmentExists);
			else
				SendSimpleBody(message);
		}


		private void SendWithAttachments(MailMessage message)
		{
			string boundary = GenerateBoundary();

			// first "multipart/mixed"
			ContentType messageContentType = new ContentType();
			messageContentType.Boundary = boundary;
			messageContentType.MediaType = "multipart/mixed";
			messageContentType.CharSet = null;

			SendHeader(HeaderName.ContentType, messageContentType.ToString());
			SendData(String.Empty);

			// body section
			Attachment body = null;

			if (message.AlternateViews.Count > 0)
				SendWithoutAttachments(message, boundary, true);
			else
			{
				body = Attachment.CreateAttachmentFromString(message.Body, null, message.BodyEncoding, message.IsBodyHtml ? "text/html" : "text/plain");
				message.Attachments.Insert(0, body);
			}

			try
			{
				SendAttachments(message, body, boundary);
			}
			finally
			{
				if (body != null)
					message.Attachments.Remove(body);
			}

			EndSection(boundary);
		}

		private void SendBodyWithAlternateViews(MailMessage message, string boundary, bool attachmentExists)
		{
			AlternateViewCollection alternateViews = message.AlternateViews;

			string inner_boundary = GenerateBoundary();

			ContentType messageContentType = new ContentType();
			messageContentType.Boundary = inner_boundary;
			messageContentType.MediaType = "multipart/alternative";

			if (!attachmentExists)
			{
				SendHeader(HeaderName.ContentType, messageContentType.ToString());
				SendData(String.Empty);
			}

			// body section
			AlternateView body = null;
			if (message.Body != null)
			{
				body = AlternateView.CreateAlternateViewFromString(message.Body, message.BodyEncoding, message.IsBodyHtml ? "text/html" : "text/plain");
				alternateViews.Insert(0, body);
				StartSection(boundary, messageContentType);
			}

			try
			{
				// alternate view sections
				foreach (AlternateView av in alternateViews)
				{

					string alt_boundary = null;
					ContentType contentType;
					if (av.LinkedResources.Count > 0)
					{
						alt_boundary = GenerateBoundary();
						contentType = new ContentType("multipart/related");
						contentType.Boundary = alt_boundary;

						contentType.Parameters["type"] = av.ContentType.ToString();
						StartSection(inner_boundary, contentType);
						StartSection(alt_boundary, av.ContentType, av.TransferEncoding);
					}
					else
					{
						contentType = new ContentType(av.ContentType.ToString());
						StartSection(inner_boundary, contentType, av.TransferEncoding);
					}

					switch (av.TransferEncoding)
					{
						case TransferEncoding.Base64:
							byte[] content = new byte[av.ContentStream.Length];
							av.ContentStream.Read(content, 0, content.Length);
							SendData(Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks));
							break;
						case TransferEncoding.QuotedPrintable:
							byte[] bytes = new byte[av.ContentStream.Length];
							av.ContentStream.Read(bytes, 0, bytes.Length);
							SendData(ToQuotedPrintable(bytes));
							break;
						case TransferEncoding.SevenBit:
						case TransferEncoding.Unknown:
							content = new byte[av.ContentStream.Length];
							av.ContentStream.Read(content, 0, content.Length);
							SendData(Encoding.ASCII.GetString(content));
							break;
					}

					if (av.LinkedResources.Count > 0)
					{
						SendLinkedResources(message, av.LinkedResources, alt_boundary);
						EndSection(alt_boundary);
					}

					if (!attachmentExists)
						SendData(string.Empty);
				}

			}
			finally
			{
				if (body != null)
					alternateViews.Remove(body);
			}
			EndSection(inner_boundary);
		}

		private void SendLinkedResources(MailMessage message, LinkedResourceCollection resources, string boundary)
		{
			foreach (LinkedResource lr in resources)
			{
				StartSection(boundary, lr.ContentType, lr.TransferEncoding, lr);

				switch (lr.TransferEncoding)
				{
					case TransferEncoding.Base64:
						byte[] content = new byte[lr.ContentStream.Length];
						lr.ContentStream.Read(content, 0, content.Length);
						SendData(Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks));
						break;
					case TransferEncoding.QuotedPrintable:
						byte[] bytes = new byte[lr.ContentStream.Length];
						lr.ContentStream.Read(bytes, 0, bytes.Length);
						SendData(ToQuotedPrintable(bytes));
						break;
					case TransferEncoding.SevenBit:
					case TransferEncoding.Unknown:
						content = new byte[lr.ContentStream.Length];
						lr.ContentStream.Read(content, 0, content.Length);
						SendData(Encoding.ASCII.GetString(content));
						break;
				}
			}
		}

		private void SendAttachments(MailMessage message, Attachment body, string boundary)
		{
			foreach (Attachment att in message.Attachments)
			{
				ContentType contentType = new ContentType(att.ContentType.ToString());
				if (att.Name != null)
				{
					contentType.Name = att.Name;
					if (att.NameEncoding != null)
						contentType.CharSet = att.NameEncoding.HeaderName;
					att.ContentDisposition.FileName = att.Name;
				}
				StartSection(boundary, contentType, att.TransferEncoding, att == body ? null : att.ContentDisposition);

				byte[] content = new byte[att.ContentStream.Length];
				att.ContentStream.Read(content, 0, content.Length);
				switch (att.TransferEncoding)
				{
					case TransferEncoding.Base64:
						SendData(Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks));
						break;
					case TransferEncoding.QuotedPrintable:
						SendData(ToQuotedPrintable(content));
						break;
					case TransferEncoding.SevenBit:
					case TransferEncoding.Unknown:
						SendData(Encoding.ASCII.GetString(content));
						break;
				}

				SendData(string.Empty);
			}
		}

		private SmtpResponse SendCommand(string command)
		{
			writer.Write(command);
			// Certain SMTP servers will reject mail sent with unix line-endings; see http://cr.yp.to/docs/smtplf.html
			writer.Write("\r\n");
			writer.Flush();
			return Read();
		}

		private void SendHeader(string name, string value)
		{
			if (m_CollectMode==CollectMode.Headers)
			{
				m_collectedHeaders.Add(new EmailHeader() { Key = name, Value = value });
			}
			else
			{
				SendData(String.Format("{0}: {1}", name, value));
			}
		}

		private void StartSection(string section, ContentType sectionContentType)
		{
			SendData(String.Format("--{0}", section));
			SendHeader("content-type", sectionContentType.ToString());
			SendData(string.Empty);
		}

		private void StartSection(string section, ContentType sectionContentType, TransferEncoding transferEncoding)
		{
			SendData(String.Format("--{0}", section));
			SendHeader("content-type", sectionContentType.ToString());
			SendHeader("content-transfer-encoding", GetTransferEncodingName(transferEncoding));
			SendData(string.Empty);
		}

		private void StartSection(string section, ContentType sectionContentType, TransferEncoding transferEncoding, LinkedResource lr)
		{
			SendData(String.Format("--{0}", section));
			SendHeader("content-type", sectionContentType.ToString());
			SendHeader("content-transfer-encoding", GetTransferEncodingName(transferEncoding));

			if (lr.ContentId != null && lr.ContentId.Length > 0)
				SendHeader("content-ID", "<" + lr.ContentId + ">");

			SendData(string.Empty);
		}

		private void StartSection(string section, ContentType sectionContentType, TransferEncoding transferEncoding, ContentDisposition contentDisposition)
		{
			SendData(String.Format("--{0}", section));
			SendHeader("content-type", sectionContentType.ToString());
			SendHeader("content-transfer-encoding", GetTransferEncodingName(transferEncoding));
			if (contentDisposition != null)
				SendHeader("content-disposition", contentDisposition.ToString());
			SendData(string.Empty);
		}

		// use proper encoding to escape input
		private string ToQuotedPrintable(string input, Encoding enc)
		{
			byte[] bytes = enc.GetBytes(input);
			return ToQuotedPrintable(bytes);
		}

		private string ToQuotedPrintable(byte[] bytes)
		{
			StringWriter writer = new StringWriter();
			int charsInLine = 0;
			int curLen;
			StringBuilder sb = new StringBuilder("=", 3);
			byte equalSign = (byte)'=';
			char c = (char)0;

			foreach (byte i in bytes)
			{
				if (i > 127 || i == equalSign)
				{
					sb.Length = 1;
					sb.Append(Convert.ToString(i, 16).ToUpperInvariant());
					curLen = 3;
				}
				else
				{
					c = Convert.ToChar(i);
					if (c == '\r' || c == '\n')
					{
						writer.Write(c);
						charsInLine = 0;
						continue;
					}
					curLen = 1;
				}

				charsInLine += curLen;
				if (charsInLine > 75)
				{
					writer.Write("=\r\n");
					charsInLine = curLen;
				}
				if (curLen == 1)
					writer.Write(c);
				else
					writer.Write(sb.ToString());
			}

			return writer.ToString();
		}
		private static string GetTransferEncodingName(TransferEncoding encoding)
		{
			switch (encoding)
			{
				case TransferEncoding.QuotedPrintable:
					return "quoted-printable";
				case TransferEncoding.SevenBit:
					return "7bit";
				case TransferEncoding.Base64:
					return "base64";
			}
			return "unknown";
		}

		RemoteCertificateValidationCallback callback = delegate(object sender,
									 X509Certificate certificate,
									 X509Chain chain,
									 SslPolicyErrors sslPolicyErrors)
		{
			// honor any exciting callback defined on ServicePointManager
			if (ServicePointManager.ServerCertificateValidationCallback != null)
				return ServicePointManager.ServerCertificateValidationCallback(sender, certificate, chain, sslPolicyErrors);
			// otherwise provide our own
			if (sslPolicyErrors != SslPolicyErrors.None)
				throw new InvalidOperationException("SSL authentication error: " + sslPolicyErrors);
			return true;
		};

		private void InitiateSecureConnection()
		{
			SmtpResponse response = SendCommand("STARTTLS");

			if (IsError(response))
			{
				throw new SmtpException(SmtpStatusCode.GeneralFailure, "Server does not support secure connections.");
			}

			SslStream sslStream = new SslStream(stream, false, callback, null);
			CheckCancellation();
			sslStream.AuthenticateAsClient(Host, this.ClientCertificates, SslProtocols.Default, false);
			stream = sslStream;
		}

		void Authenticate()
		{
			string user = null, pass = null;

			if (UseDefaultCredentials)
			{
				user = CredentialCache.DefaultCredentials.GetCredential(new System.Uri("smtp://" + host), "basic").UserName;
				pass = CredentialCache.DefaultCredentials.GetCredential(new System.Uri("smtp://" + host), "basic").Password;
			}
			else if (Credentials != null)
			{
				user = Credentials.GetCredential(host, port, "smtp").UserName;
				pass = Credentials.GetCredential(host, port, "smtp").Password;
			}
			else
			{
				return;
			}

			Authenticate(user, pass);
		}

		void CheckStatus(SmtpResponse status, int i)
		{
			if (((int)status.StatusCode) != i)
				throw new SmtpException(status.StatusCode, status.Description);
		}

		void ThrowIfError(SmtpResponse status)
		{
			if (IsError(status))
				throw new SmtpException(status.StatusCode, status.Description);
		}

		void Authenticate(string user, string password)
		{
			if (authMechs == AuthMechs.None)
				return;

			SmtpResponse status;
			/*
			if ((authMechs & AuthMechs.DigestMD5) != 0) {
				status = SendCommand ("AUTH DIGEST-MD5");
				CheckStatus (status, 334);
				string challenge = Encoding.ASCII.GetString (Convert.FromBase64String (status.Description.Substring (4)));
				Console.WriteLine ("CHALLENGE: {0}", challenge);
				DigestSession session = new DigestSession ();
				session.Parse (false, challenge);
				string response = session.Authenticate (this, user, password);
				status = SendCommand (Convert.ToBase64String (Encoding.UTF8.GetBytes (response)));
				CheckStatus (status, 235);
			} else */
			if ((authMechs & AuthMechs.Login) != 0)
			{
				status = SendCommand("AUTH LOGIN");
				CheckStatus(status, 334);
				status = SendCommand(Convert.ToBase64String(Encoding.UTF8.GetBytes(user)));
				CheckStatus(status, 334);
				status = SendCommand(Convert.ToBase64String(Encoding.UTF8.GetBytes(password)));
				CheckStatus(status, 235);
			}
			else if ((authMechs & AuthMechs.Plain) != 0)
			{
				string s = String.Format("\0{0}\0{1}", user, password);
				s = Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
				status = SendCommand("AUTH PLAIN " + s);
				CheckStatus(status, 235);
			}
			else
			{
				throw new SmtpException("AUTH types PLAIN, LOGIN not supported by the server");
			}
		}

		#endregion // Methods

		// The HeaderName struct is used to store constant string values representing mail headers.
		private struct HeaderName
		{
			public const string ContentTransferEncoding = "Content-Transfer-Encoding";
			public const string ContentType = "Content-Type";
			public const string Bcc = "Bcc";
			public const string Cc = "Cc";
			public const string From = "From";
			public const string Subject = "Subject";
			public const string To = "To";
			public const string MimeVersion = "MIME-Version";
			public const string MessageId = "Message-ID";
			public const string Priority = "Priority";
			public const string Importance = "Importance";
			public const string XPriority = "X-Priority";
			public const string Date = "Date";
		}

		// This object encapsulates the status code and description of an SMTP response.
		private struct SmtpResponse
		{
			public SmtpStatusCode StatusCode;
			public string Description;

			public static SmtpResponse Parse(string line)
			{
				SmtpResponse response = new SmtpResponse();

				if (line.Length < 4)
					throw new SmtpException("Response is to short " +
								 line.Length + ".");

				if ((line[3] != ' ') && (line[3] != '-'))
					throw new SmtpException("Response format is wrong.(" +
								 line + ")");

				// parse the response code
				response.StatusCode = (SmtpStatusCode)Int32.Parse(line.Substring(0, 3));

				// set the raw response
				response.Description = line;

				return response;
			}
		}
	}

	class CCredentialsByHost : ICredentialsByHost
	{
		public CCredentialsByHost(string userName, string password)
		{
			this.userName = userName;
			this.password = password;
		}

		public NetworkCredential GetCredential(string host, int port, string authenticationType)
		{
			return new NetworkCredential(userName, password);
		}

		private string userName;
		private string password;
	}
}

