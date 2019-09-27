using System;
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
        /// <exception cref="ArgumentException">Thrown when there's a WebException.</exception>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            if (timeout == 0)
                timeout = Timeout;
            var request = HttpWebRequest.CreateHttp(uri);
            if (!string.IsNullOrEmpty(UserAgent))
                request.UserAgent = UserAgent;
            Task cancelTask;
            if (timeout != 0)
            {
                // TODO: This doesn't seem to do anything.
                //request.ContinueTimeout = Timeout;
                request.Timeout = timeout;
                request.ReadWriteTimeout = timeout;
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
                    cancelTask?.Dispose();
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
                    throw new WebClientException(ex.Message, ex, new WebClientResponseWrapper(resp, request, ex, statusOverride));
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(resp, request, ex, statusOverride);
                }
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
            return null;
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
