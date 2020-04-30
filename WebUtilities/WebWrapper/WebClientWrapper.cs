using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities.WebWrapper
{
    /// <summary>
    /// An <see cref="IWebClient"/> that uses the <see cref="System.Net"/> library.
    /// </summary>
    public class WebClientWrapper : IWebClient, IDisposable
    {
        /// <inheritdoc/>
        public string? UserAgent { get; private set; }

        /// <summary>
        /// Creates a new <see cref="WebClientWrapper"/> with the default settings.
        /// </summary>
        public WebClientWrapper()
        {
            //if (client == null)
            //    client = new WebClient();
            //else
            //    webClient = client;
            ErrorHandling = ErrorHandling.ThrowOnException;
            MaxConcurrentConnections = int.MaxValue;
        }

        /// <summary>
        /// Creates a new <see cref="WebClientWrapper"/> with the given <paramref name="maxConnectionsPerServer"/>.
        /// </summary>
        /// <param name="maxConnectionsPerServer"></param>
        public WebClientWrapper(int maxConnectionsPerServer)
            : this()
        {
            MaxConcurrentConnections = maxConnectionsPerServer > 0 ? maxConnectionsPerServer : 1;
        }

        /// <inheritdoc/>
        public void SetUserAgent(string? userAgent)
        {
            UserAgent = userAgent;
        }

        private int _timeout = 30000;

        /// <inheritdoc/>
        public int Timeout
        {
            get { return _timeout; }
            set
            {
                if (value < 0)
                    value = 30000;
                if (_timeout == value)
                    return;
                _timeout = value;
            }
        }

        private int _maxConcurrentConnections;
        /// <summary>
        /// Maximum number of concurrent connections to a server.
        /// </summary>
        public int MaxConcurrentConnections
        {
            get { return _maxConcurrentConnections; }
            set
            {
                _maxConcurrentConnections = value;
                ServicePointManager.DefaultConnectionLimit = value;
            }
        }
        /// <inheritdoc/>
        public ErrorHandling ErrorHandling { get; set; }


        /// <inheritdoc/>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri), "uri cannot be null.");
            if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout), "timeout cannot be less than 0.");
            if (timeout == 0)
                timeout = Timeout;
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            if (!string.IsNullOrEmpty(UserAgent))
                request.UserAgent = UserAgent;
            CancellationTokenSource? timeoutSource = null;
            CancellationToken timeoutToken = CancellationToken.None;
            if (timeout != 0)
            {
                request.Timeout = timeout;
                request.ReadWriteTimeout = timeout;
                // TODO: This doesn't seem to do anything.
                //request.ContinueTimeout = Timeout;
            }
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException($"GetAsync canceled for Uri {uri}", cancellationToken);
            bool wasCanceled = false;
            //bool timedOut = false;
            try
            {
                Task<WebResponse> getTask = request.GetResponseAsync(cancellationToken);
                //var getTask = Task.Run(() =>
                //{
                //    return request.GetResponse(); // Have to use synchronous call because GetResponseAsync() doesn't respect Timeout
                //});
                //TODO: Need testing for cancellation token
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() =>
                    {
                        request.Abort();
                        wasCanceled = true;
                    });
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (timeout > 0)
                {
                    timeoutSource = new CancellationTokenSource();
                    timeoutToken = timeoutSource.Token;
                    timeoutToken.Register(() =>
                    {
                        request.Abort();
                        if (!wasCanceled)
                        {
                            wasCanceled = true;
                            //timedOut = true;
                        }
                    });
                    timeoutSource.CancelAfter(timeout);

                }
                WebResponse response = await getTask.ConfigureAwait(false);
                return new WebClientResponseWrapper(response as HttpWebResponse, request);

            }
            catch (ArgumentException ex)
            {
                if (ErrorHandling != ErrorHandling.ReturnEmptyContent)
                    throw;
                else
                {
                    return new WebClientResponseWrapper(null, request, ex);
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse? resp = ex.Response as HttpWebResponse;
                int? statusOverride = WebExceptionStatusToHttpStatus(ex.Status);
                // This is thrown by HttpWebRequest.GetResponseAsync(), so we can't throw the exception here or the calling code won't be able to decide how to handle it...sorta

                if (ErrorHandling == ErrorHandling.ThrowOnException)
                {
                    string message = string.Empty;
                    Exception retException = ex;
                    if (ex.Status == WebExceptionStatus.RequestCanceled && (wasCanceled || timeoutToken.IsCancellationRequested))
                    {
                        if (timeoutToken.IsCancellationRequested)
                        {
                            message = Utilities.GetTimeoutMessage(uri);
                            statusOverride = 408;
                            retException = new TimeoutException(message);
                        }
                        else
                            cancellationToken.ThrowIfCancellationRequested();
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
                    cancellationToken.ThrowIfCancellationRequested();
                    retException = new TimeoutException(Utilities.GetTimeoutMessage(uri));
                    throw new WebClientException(retException.Message, retException, new WebClientResponseWrapper(null, request, retException, 408));
                }
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(null, request, ex, 408);
                }
            }
            finally
            {
                if(timeoutSource != null)
                {
                    timeoutSource.Dispose();
                    timeoutSource = null;
                }
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

        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(Uri uri)
        {
            return GetAsync(uri, 0, CancellationToken.None);
        }

        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync(uri, 0, cancellationToken);
        }
        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(Uri uri, int timeout)
        {
            return GetAsync(uri, timeout, CancellationToken.None);
        }
        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(string url, int timeout, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url), $"Url cannot be null for GetAsync()");
            Uri urlAsUri = new Uri(url);
            return GetAsync(urlAsUri, timeout, cancellationToken);
        }
        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(string url)
        {
            return GetAsync(url, 0, CancellationToken.None);
        }
        /// <inheritdoc/>
        public Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return GetAsync(url, 0, cancellationToken);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
