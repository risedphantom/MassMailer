using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using DKIM;
using MailService.Singleton;

namespace MailService
{
    class MassMailer
    {
        private volatile bool _stopPipeline;
        private MassMail _massMail;
        private TransformBlock<DataTable, List<Mail>> _parseXmlDataBlock;
        private TransformBlock<List<Mail>, DataTable> _sendEmailsBlock;
        private BatchBlock<DataTable> _batchResultBlock;
        private ActionBlock<DataTable[]> _writeResultsBlock;
        
        //Cache
        private static ConcurrentDictionary<long, Lazy<Template>> _templateCache;
        private static ConcurrentDictionary<long, Lazy<Attach>> _attachmentCache;
        private static ConcurrentDictionary<string, DkimSigner> _dkimSignerCache;
        private static ConcurrentDictionary<string, DomainKeySigner> _domailKeySignerCache;

        //Fire statistics event
        public delegate void OnMessagesSend(long messagesSend, long errorCount);

        public event OnMessagesSend MessagesSend;

        public void Init()
        {
            _massMail = new MassMail(Config.BlockSize, Config.UserAgent, Config.ConnectionString, Config.Mode);
            _templateCache = new ConcurrentDictionary<long, Lazy<Template>>();
            _attachmentCache = new ConcurrentDictionary<long, Lazy<Attach>>();
            _dkimSignerCache = new ConcurrentDictionary<string, DkimSigner>();
            _domailKeySignerCache = new ConcurrentDictionary<string, DomainKeySigner>();
            
            //Get all private keys
            GetDkimSigners();

            //*** Create pipeline ***
            //Create TransformBlock that gets table of client data and make a list of objects from them.
            _parseXmlDataBlock = new TransformBlock<DataTable, List<Mail>>(sendData => ParseXmlData(sendData),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Config.ParseXmlMaxdop,
                    BoundedCapacity = Config.ParseXmlBufferSize
                });
            //Create TransformBlock that gets a list of client objects, send them email, and stores result in DataTable.
            _sendEmailsBlock = new TransformBlock<List<Mail>, DataTable>(mails => SendEmails(_massMail, mails),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Config.SendEmailsMaxdop,
                    BoundedCapacity = Config.SendEmailsMaxdop
                });
            //Create BatchBlock that holds several DataTable and then propagates them out as an array.
            _batchResultBlock = new BatchBlock<DataTable>(Config.BatchSize,
                new GroupingDataflowBlockOptions
                {
                    BoundedCapacity = Config.BatchSize
                });
            //Create ActionBlock that writes result into DB
            _writeResultsBlock = new ActionBlock<DataTable[]>(results => WriteResults(_massMail, results),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1
                });

            //*** Build pipeline ***
            // POST --> _parseXmlDataBlock --> _sendEmailsBlock --> _batchResultBlock --> _writeResultsBlock
            _parseXmlDataBlock.LinkTo(_sendEmailsBlock);
            _sendEmailsBlock.LinkTo(_batchResultBlock);
            _batchResultBlock.LinkTo(_writeResultsBlock);

            _parseXmlDataBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_sendEmailsBlock).Fault(t.Exception);
                else _sendEmailsBlock.Complete();
            });
            _sendEmailsBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_batchResultBlock).Fault(t.Exception);
                else _batchResultBlock.Complete();
            });
            _batchResultBlock.Completion.ContinueWith(t =>
            {
                if (t.IsFaulted) ((IDataflowBlock)_writeResultsBlock).Fault(t.Exception);
                else _writeResultsBlock.Complete();
            });
        }

        public void Run()
        {
            try
            {
                //*** Start pipeline ***
                var data = _massMail.GetBatch();
                while (data != null)
                {
                    _parseXmlDataBlock.SendAsync(data).Wait();
                    
                    if (_stopPipeline)
                        break;

                    data = _massMail.GetBatch();
                }

                //*** Shut down ***
                _parseXmlDataBlock.Complete();
                _writeResultsBlock.Completion.Wait();
                if (_stopPipeline)
                    Logger.Log.Info("Pipeline has been stopped by user");
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Exception ocured: {0}", ex.Message);
            }
        }

        public void Stop()
        {
            _stopPipeline = true;
        }

        private static void GetDkimSigners()
        {
            try
            {
                var files = new DirectoryInfo(Config.PrivateKeyFolder).GetFiles("*.pem");
                
                if (files.Length == 0)
                    throw new Exception("No private key files (*.pem) found");

                foreach (var file in files)
                {
                    var key = file.Name.Replace(".pem", "");
                    var selector = key.Split('@')[0];
                    var domain = key.Split('@')[1];

                    var pKeySig = PrivateKeySigner.LoadFromFile(file.FullName);
                    var dkimSig = new DkimSigner(pKeySig, domain, selector, new[] { "From", "To", "Subject" });
                    var domainKeySig = new DomainKeySigner(pKeySig, domain, selector, new[] { "From", "To", "Subject" });

                    dkimSig.HeaderCanonicalization = DkimCanonicalizationAlgorithm.RELAXED;
                    dkimSig.BodyCanonicalization = DkimCanonicalizationAlgorithm.RELAXED;
                    
                    _dkimSignerCache.TryAdd(key, dkimSig);
                    _domailKeySignerCache.TryAdd(key, domainKeySig);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Warn("Unable to turn on DKIM: {0}", ex.Message);
            }
        }

        private static List<Mail> ParseXmlData(DataTable sendData)
        {
            var mails = new List<Mail>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (DataRow sendRow in sendData.Rows)
            {
                var mail = new Mail
                {
                    StaticSubject = (bool)sendRow["StaticSubject"],
                    HasAttachment = (bool)sendRow["HasAttachment"],
                    TemplateId = (long)sendRow["TemplateID"],
                    Id = (long)sendRow["ID"],
                    ListId = sendRow["ListID"].ToString(),
                    Subject = sendRow["Subject"].ToString(),
                    From = sendRow["AddressFrom"].ToString(),
                    To = sendRow["AddressTo"].ToString(),
                    Cc = sendRow["AddressCC"].ToString(),
                    Model = Functions.GetDynamicFromXml(sendRow["XMLData"].ToString())
                };
                mails.Add(mail);
            }
            return mails;
        }

        private DataTable SendEmails(MassMail massMail, IEnumerable<Mail> mails)
        {
            var mailClient = new DKIM.SmtpClient(Config.MailServer, Config.MailPort);
            var resultTable = new DataTable();
            var processedCount = 0;
            var errorCount = 0;
            
            resultTable.Columns.Add("ID");
            resultTable.Columns.Add("Status");
            resultTable.Columns.Add("SendMoment");

            foreach (var mail in mails)
            {
                var result = 0;
                var attachs = new List<Attach>();

                try
                {
                    if (mail.Model == null)
                        throw new XmlException(String.Format("error when creating model for ID:[{0}]", mail.Id));

                    //Load template
                    var id = mail.TemplateId;
                    var template = _templateCache.GetOrAdd(id, new Lazy<Template>(() => massMail.GetTemplate(id))).Value;
                    var body = Functions.RazorGetText(template.Body, template.Guid, mail.Model);

                    //Parse subject
                    var subject = mail.Subject;
                    if (!mail.StaticSubject)
                        subject = Functions.RazorGetText(mail.Subject, Functions.GetMd5(mail.Subject), mail.Model);

                    
                    //Get attachments
                    if (mail.HasAttachment)
                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach (DataRow attachRow in massMail.GetAttachments(mail.Id).Rows)
                        {
                            var attachId = (long)attachRow["AttachmentID"];
                            attachs.Add(_attachmentCache.GetOrAdd(attachId, new Lazy<Attach>(() => massMail.GetAttachment(attachId))).Value);
                        }

                    result = SendMail(mailClient, mail.From, mail.To, mail.Cc, subject, mail.ListId, body, template.IsHtml, attachs);
                    processedCount++;
                }
                catch (XmlException ex)
                {
                    Logger.Log.Error("Unable to parse xml: {0}", ex.Message);
                    errorCount++;
                    result = (int)MailStatusCode.XMLERROR;
                }
                catch (RazorException ex)
                {
                    Logger.Log.Error("Razor error: {0}", ex.Message);
                    errorCount++;
                    result = (int)MailStatusCode.RAZORERROR;
                }
                catch (SmtpFailedRecipientsException ex)
                {
                    Logger.Log.Error("SMTP exception: {0}", ex.Message);
                    errorCount++;
                    result = (int)ex.StatusCode;
                }
                catch (Exception ex)
                {
                    Logger.Log.Error("Unable to send email: {0}", ex.Message);
                    errorCount++;
                    result = (int)MailStatusCode.FAILED;
                }
                finally
                {
                    resultTable.Rows.Add(new object[] { mail.Id, result, DateTime.Now });
                }
            }
            MessagesSend(processedCount, errorCount);
            mailClient.Dispose();
            
            return resultTable;
        }

        private int SendMail(DKIM.SmtpClient smtpClient, string from, string to, string cc, string subject, string listId, string body, bool isHtml, List<Attach> attachs)
        {
            using (var message = new MailMessage())
            {
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = isHtml;
                message.Subject = subject;
                message.Body = body;
                message.Sender = new MailAddress(Config.ReturnPath); 
                message.From = new MailAddress(from);
                message.To.Add(to);
                if (!String.IsNullOrEmpty(listId))
                    message.Headers.Add("List-Id", listId);
                
                //Send copy if needed
                if (!String.IsNullOrEmpty(cc))
                    message.CC.Add(cc);

                //Attach files if exists
                if (attachs.Count != 0)
                    foreach (var attach in attachs)
                        message.Attachments.Add(new Attachment(new MemoryStream(attach.Data), attach.Name));

                //Sign message if available
                DkimSigner dkimSig;
                DomainKeySigner domainKeySig;

                _dkimSignerCache.TryGetValue(message.From.Address, out dkimSig);
                _domailKeySignerCache.TryGetValue(message.From.Address, out domainKeySig);
                smtpClient.DkimSigner = dkimSig;
                smtpClient.DomainKeysSigner = domainKeySig;

                if (Config.CoreTest == 0)
                    smtpClient.Send(message);
                else if (Config.CoreTest > 0)
                    Thread.Sleep(Config.CoreTest);
            }
            
            return (int)SmtpStatusCode.Ok;
        }

        private static void WriteResults(MassMail massMail, IEnumerable<DataTable> results)
        {
            try
            {
                var dataTable = new DataTable();
                foreach (var result in results)
                    dataTable.Merge(result);

                massMail.UpdateBlock(dataTable);
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Unable to write batch: {0}", ex.Message);
            }
        }
    }
}