using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUtilities
{
#pragma warning disable CA2237 // Mark ISerializable types with serializable
    /// <summary>
    /// Exception that wraps almost all <see cref="Exception"/>s thrown by an <see cref="IWebClient"/>.
    /// </summary>
    public class WebClientException : InvalidOperationException
#pragma warning restore CA2237 // Mark ISerializable types with serializable
    {
        /// <summary>
        /// The <see cref="FaultedResponse"/>.
        /// </summary>
        public FaultedResponse? Response { get; }
        /// <summary>
        /// The Uri of the request that failed.
        /// </summary>
        public Uri? Uri { get; }

        /// <summary>
        /// Creates a new <see cref="WebClientException"/>.
        /// </summary>
        public WebClientException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="WebClientException"/> with a message.
        /// </summary>
        public WebClientException(string message) : base(message)
        {

        }
        /// <summary>
        /// Creates a new <see cref="WebClientException"/> with a message and inner exception.
        /// </summary>
        public WebClientException(string message, Exception innerException) : base(message, innerException)
        {

        }
        /// <summary>
        /// Creates a new <see cref="WebClientException"/> with a message, inner exception, and a <see cref="FaultedResponse"/>.
        /// </summary>
        public WebClientException(string message, Exception innerException, FaultedResponse response)
            : base(message, innerException)
        {
            Response = response;
        }

        /// <summary>
        /// Creates a new <see cref="WebClientException"/> with a message, inner exception, and the failed <see cref="IWebResponseMessage"/>.
        /// TODO: Bad to include the response in the exception, response could get disposed by a using
        /// </summary>
        public WebClientException(string message, Exception innerException, IWebResponseMessage response)
            : base(message, innerException)
        {
            Response = new FaultedResponse(response);
        }
    }

    /// <summary>
    /// A failed <see cref="IWebResponse"/> containing details about the request failure.
    /// </summary>
    public class FaultedResponse : IWebResponse
    {
        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; private set; }
        /// <summary>
        /// Reason phrase for the failure, if available.
        /// </summary>
        public string? ReasonPhrase { get; private set; }
        /// <summary>
        /// The <see cref="Exception"/> thrown by the response.
        /// </summary>
        public Exception? Exception { get; private set; }
        /// <summary>
        /// Returns true if the <see cref="StatusCode"/> indicated success.
        /// </summary>
        public bool IsSuccessStatusCode
        {
            get { return (StatusCode >= 200) && (StatusCode <= 299); }
        }
        /// <summary>
        /// <see cref="Uri"/> of the request, if available.
        /// </summary>
        public Uri RequestUri { get; private set; }
        /// <summary>
        /// Response headers, if available.
        /// </summary>
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        /// <summary>
        /// Creates a new <see cref="FaultedResponse"/> from an <see cref="IWebResponseMessage"/>.
        /// </summary>
        /// <param name="response"></param>
        public FaultedResponse(IWebResponseMessage response)
        {
            StatusCode = response.StatusCode;
            ReasonPhrase = response.ReasonPhrase;
            Exception = response.Exception;
            RequestUri = response.RequestUri;
            Headers = new ReadOnlyDictionary<string, IEnumerable<string>>(response.Headers);
        }

        /// <summary>
        /// Creates a new <see cref="FaultedResponse"/>.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="reasonPhrase"></param>
        /// <param name="requestUri"></param>
        /// <param name="headers"></param>
        /// <param name="exception"></param>
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
