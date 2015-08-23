using System;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using Encoding = RazorEngine.Encoding;

namespace MailService.Singleton
{
    enum MailStatusCode
    {
        FAILED = 900,
        RAZORERROR = 901,
        XMLERROR = 902
    }

    public class Mail
    {
        public long Id { get; set; }
        public long TemplateId { get; set; }
        public bool HasAttachment { get; set; }
        public bool StaticSubject { get; set; }
        public string ListId { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public dynamic Model { get; set; }
    }

    public class Template
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public string Guid { get; set; }
        public bool IsHtml { get; set; }
    }

    public class Attach
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }

    public class XmlException : Exception
    {
        public XmlException()
        {
        }

        public XmlException(string message)
            : base(message)
        {
        }

        public XmlException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Singleton: сервисный класс с функциями общего назначения
    /// </summary>
    class Functions
    {
        static Functions()
        {
            //Configure Razor
            var config = new TemplateServiceConfiguration
            {
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => {})
            };

            Engine.Razor = RazorEngineService.Create(config);
        }

        public static dynamic GetDynamicFromXml(string xml)
        {
            if (String.IsNullOrEmpty(xml))
                return "";

            try
            {
                dynamic model = new ExpandoObject();
                var xDoc = XDocument.Parse(xml);
                DynamicXml.Parse(model, xDoc.Elements().First());
                return model;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetMd5(string input)
        {
            using (var md5Hash = MD5.Create())
            {
                var data = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var builder = new StringBuilder();
 
                foreach (var bt in data)
                    builder.Append(bt.ToString("x2"));

                return builder.ToString();
            }
        }

        public static string RazorGetText(string template, string guid, dynamic model)
        {
            try
            {
                return model.ToString() == "" ? template : Engine.Razor.RunCompile(template, guid, null, (object)model);
            }
            catch (Exception ex)
            {
                throw new RazorException(string.Format("Unable to parse template {0}: {1}", guid, ex.Message));
            }
        }
    }
}
