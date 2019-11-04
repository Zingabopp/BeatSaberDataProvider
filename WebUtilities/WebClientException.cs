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

        // TODO: Bad to include the response in the exception, response could get disposed by a using
        public WebClientException(string message, Exception innerException, IWebResponseMessage response) 
            : base(message, innerException)
        {
            Response = response;
        }
    }
}
