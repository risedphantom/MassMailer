using System;

namespace MailService
{
    public class RazorException : Exception
    {
        public RazorException()
        {
        }

        public RazorException(string message)
            : base(message)
        {
        }

        public RazorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
