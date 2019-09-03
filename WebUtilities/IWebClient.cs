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
        /// Timeout for requests in milliseconds.
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
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(Uri uri);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="completeOnHeaders"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="completeOnHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(string url);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="completeOnHeaders"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="completeOnHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's an exception getting a response.</exception>
        Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken);

    }

    public enum ErrorHandling
    {
        ThrowOnException,
        ThrowOnWebFault,
        ReturnEmptyContent
    }
}
