using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUtilities
{
    public class WebClientException : InvalidOperationException
    {
        public FaultedResponse Response { get; }
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
            Response = new FaultedResponse(response);
        }
    }

    public class FaultedResponse : IWebResponse
    {
        public int StatusCode { get; private set; }
        public string ReasonPhrase { get; private set; }
        public Exception Exception { get; private set; }
        public bool IsSuccessStatusCode
        {
            get { return (StatusCode >= 200) && (StatusCode <= 299); }
        }
        public Uri RequestUri { get; private set; }
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        public FaultedResponse(IWebResponseMessage response)
        {
            StatusCode = response.StatusCode;
            ReasonPhrase = response.ReasonPhrase;
            Exception = response.Exception;
            RequestUri = response.RequestUri;
            Headers = new ReadOnlyDictionary<string, IEnumerable<string>>(response.Headers);
        }

        public FaultedResponse(int statusCode, string reasonPhrase, Uri requestUri,
            IDictionary<string, IEnumerable<string>> headers, Exception exception)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Exception = exception;
            RequestUri = requestUri;
            Headers = new ReadOnlyDictionary<string, IEnumerable<string>>(headers);
        }
    }
}
