using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUtilities
{
    public class WebClientException : InvalidOperationException
    {
        public IWebResponseMessage Response { get; }
        public Uri Uri { get; }

        public WebClientException(string message) : base(message)
        {

        }
        public WebClientException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public WebClientException(string message, Exception innerException, IWebResponseMessage response) 
            : base(message, innerException)
        {
            Response = response;
        }
    }
}
