using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public interface IWebClient : IDisposable
    {
        /// <summary>
        /// Default timeout for requests in milliseconds.
        /// </summary>
        int Timeout { get; set; }
        /// <summary>
        /// The UserAgent string the client sends with request headers.
        /// </summary>
        string UserAgent { get; }
        /// <summary>
        /// How the WebClient handles errors. TODO: May not be fully implemented.
        /// </summary>
        ErrorHandling ErrorHandling { get; set; }
        /// <summary>
        /// Sets the UserAgent the client sends in the request headers.
        /// Should be in the format: Product/Version.
        /// </summary>
        /// <param name="userAgent"></param>
        void SetUserAgent(string userAgent);

        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation. If the server doesn't respond inside the provided timeout (milliseconds) or the provided CancellationToken is triggered, the operation is canceled.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">The provided Uri is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <exception cref="OperationCanceledException">The provided cancellationToken was triggered.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The provided Uri is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        //Task<IWebResponseMessage> GetAsync(Uri uri);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation. If the provided CancellationToken is triggered, the operation is canceled.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">The provided Uri is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation. If the server doesn't respond inside the provided timeout (milliseconds), the operation is canceled.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <exception cref="ArgumentNullException">The provided Uri is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(Uri uri, int timeout);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="ArgumentNullException">The provided Url is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(string url);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation. If the provided CancellationToken is triggered, the operation is canceled.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">The provided Url is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <exception cref="OperationCanceledException">The provided cancellationToken was triggered.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
        /// <summary>
        /// Send a GET request to the specified Uri as an asynchronous operation. If the server doesn't respond inside the provided timeout (milliseconds) or the provided CancellationToken is triggered, the operation is canceled.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">The provided Url is null.</exception>
        /// <exception cref="WebClientException">Thrown when an error occurs in the web client or the request times out.</exception>
        /// <exception cref="OperationCanceledException">The provided cancellationToken was triggered.</exception>
        /// <returns></returns>
        Task<IWebResponseMessage> GetAsync(string url, int timeout, CancellationToken cancellationToken);

    }

    public enum ErrorHandling
    {
        ThrowOnException,
        ThrowOnWebFault,
        ReturnEmptyContent
    }
}
