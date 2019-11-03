﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using WebUtilities;

namespace WebUtilities.WebWrapper
{
    public class WebClientWrapper : IWebClient, IDisposable
    {
        //public ILogger Logger;
        public string UserAgent { get; private set; }

        public WebClientWrapper()
        {
            //if (client == null)
            //    client = new WebClient();
            //else
            //    webClient = client;
            ErrorHandling = ErrorHandling.ThrowOnException;
            MaxConcurrentConnections = int.MaxValue;
        }

        public WebClientWrapper(int maxConnectionsPerServer)
            : this()
        {
            MaxConcurrentConnections = maxConnectionsPerServer > 0 ? maxConnectionsPerServer : 1;
        }

        public void SetUserAgent(string userAgent)
        {
            UserAgent = userAgent;
        }

        private int _timeout;
        /// <summary>
        /// Timeout in milliseconds
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                if (value <= 0)
                    value = 1;
                if (_timeout == value)
                    return;
                _timeout = value;
            }
        }

        private int _maxConcurrentConnections;
        public int MaxConcurrentConnections
        {
            get { return _maxConcurrentConnections; }
            set
            {
                _maxConcurrentConnections = value;
                ServicePointManager.DefaultConnectionLimit = value;
            }
        }
        public ErrorHandling ErrorHandling { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="completeOnHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when there's a WebException.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="uri"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified <paramref name="timeout"/> is less than 0.</exception>
        /// <exception cref="OperationCanceledException">Thrown when cancelled by caller.</exception>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri), "uri cannot be null.");
            if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout), "timeout cannot be less than 0.");
            if (timeout == 0)
                timeout = Timeout;
            var request = HttpWebRequest.CreateHttp(uri);
            if (!string.IsNullOrEmpty(UserAgent))
                request.UserAgent = UserAgent;
            Task cancelTask = null;
            if (timeout != 0)
            {
                request.Timeout = timeout;
                request.ReadWriteTimeout = timeout;
                // TODO: This doesn't seem to do anything.
                //request.ContinueTimeout = Timeout;
            }
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException($"GetAsync canceled for Uri {uri}", cancellationToken);
            try
            {
                //var getTask = request.GetResponseAsync();
                var getTask = Task.Run(() =>
                {
                    return request.GetResponse(); // Have to use synchronous call because GetResponseAsync() doesn't respect Timeout
                });
                //TODO: Need testing for cancellation token
                if (cancellationToken.CanBeCanceled)
                {
                    cancelTask = cancellationToken.AsTask();
                    await Task.WhenAny(getTask, cancelTask).ConfigureAwait(false);
                    if (!getTask.IsCompleted) // either getTask completed or cancelTask was triggered
                    {
                        throw new OperationCanceledException($"GetAsync canceled for Uri {uri}", cancellationToken);
                    }
                    //cancelTask?.Dispose(); Can't dispose of a task that isn't Completed
                }
                var response = await getTask.ConfigureAwait(false); // either there's no cancellationToken or it's already completed

                return new WebClientResponseWrapper(response as HttpWebResponse, request);
            }
            catch (ArgumentException ex)
            {
                if (ErrorHandling != ErrorHandling.ReturnEmptyContent)
                    throw;
                else
                {
                    return new WebClientResponseWrapper(null, null, ex);
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                var statusOverride = WebExceptionStatusToHttpStatus(ex.Status);
                // This is thrown by HttpWebRequest.GetResponseAsync(), so we can't throw the exception here or the calling code won't be able to decide how to handle it...sorta

                if (ErrorHandling == ErrorHandling.ThrowOnException)
                {
                    string message = string.Empty;
                    Exception retException = ex;
                    if (ex.Status == WebExceptionStatus.Timeout)
                    {
                        message = Utilities.GetTimeoutMessage(uri);
                        retException = new TimeoutException(message);
                    }
                    else
                        message = ex.Message;
                    throw new WebClientException(message, retException, new WebClientResponseWrapper(resp, request, retException, statusOverride));
                }
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(resp, request, ex, statusOverride);
                }
            }
            catch (OperationCanceledException ex) // Timeout, could also be caught by WebException
            {
                Exception retException = ex;
                if (ErrorHandling == ErrorHandling.ThrowOnException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw; // Cancelled by caller, rethrow
                    else
                    {
                        retException = new TimeoutException(Utilities.GetTimeoutMessage(uri));
                        throw new WebClientException(retException.Message, retException, new WebClientResponseWrapper(null, request, retException, 408));
                    }
                }
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(null, request, ex, 408);
                }
            }
            finally
            {
                //if (cancelTask != null)
                //    cancelTask.Dispose();
            }
        }

        private static int? WebExceptionStatusToHttpStatus(WebExceptionStatus status)
        {
            switch (status)
            {
                case WebExceptionStatus.Success:
                    return 200;
                case WebExceptionStatus.Timeout:
                    return 408;
                /*
            case WebExceptionStatus.NameResolutionFailure:
                break;
            case WebExceptionStatus.ConnectFailure:
                break;
            case WebExceptionStatus.ReceiveFailure:
                break;
            case WebExceptionStatus.SendFailure:
                break;
            case WebExceptionStatus.PipelineFailure:
                break;
            case WebExceptionStatus.RequestCanceled:
                break;
            case WebExceptionStatus.ProtocolError:
                break;
            case WebExceptionStatus.ConnectionClosed:
                break;
            case WebExceptionStatus.TrustFailure:
                break;
            case WebExceptionStatus.SecureChannelFailure:
                break;
            case WebExceptionStatus.ServerProtocolViolation:
                break;
            case WebExceptionStatus.KeepAliveFailure:
                break;
            case WebExceptionStatus.Pending:
                break;
            case WebExceptionStatus.ProxyNameResolutionFailure:
                break;
            case WebExceptionStatus.UnknownError:
                break;
            case WebExceptionStatus.MessageLengthLimitExceeded:
                break;
            case WebExceptionStatus.CacheEntryNotFound:
                break;
            case WebExceptionStatus.RequestProhibitedByCachePolicy:
                break;
            case WebExceptionStatus.RequestProhibitedByProxy:
                break;
                */
                default:
                    return null;
            }
        }

        #region GetAsyncOverloads

        public Task<IWebResponseMessage> GetAsync(Uri uri)
        {
            return GetAsync(uri, 0, CancellationToken.None);
        }

        public Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync(uri, 0, cancellationToken);
        }
        public Task<IWebResponseMessage> GetAsync(Uri uri, int timeout)
        {
            return GetAsync(uri, timeout, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, int timeout, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), $"Url cannot be null for GetAsync()");
            var urlAsUri = new Uri(url);
            return GetAsync(urlAsUri, timeout, cancellationToken);
        }
        public Task<IWebResponseMessage> GetAsync(string url)
        {
            return GetAsync(url, 0, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, int timeout)
        {
            return GetAsync(url, timeout, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return GetAsync(url, 0, cancellationToken);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
