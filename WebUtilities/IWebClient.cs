﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public interface IWebClient : IDisposable
    {
        int Timeout { get; set; }
        ErrorHandling ErrorHandling { get; set; }
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
