using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static WebUtilities.Utilities;

namespace WebUtilities.HttpClientWrapper
{
    public class HttpClientWrapper : IWebClient
    {
        private HttpClient httpClient;
        public string UserAgent { get; private set; }
        //public ILogger Logger;
        public HttpClientWrapper()
        {
            if (httpClient == null)
                httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(UserAgent))
            {
                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent);
            }

            ErrorHandling = ErrorHandling.ThrowOnException;
        }

        public HttpClientWrapper(HttpClient client)
        {
            if (client == null)
                httpClient = new HttpClient();
            else
                httpClient = client;
            ErrorHandling = ErrorHandling.ThrowOnException;
            httpClient.Timeout = _timeout;
        }

        public void SetUserAgent(string userAgent)
        {

            if (httpClient != null)
            {
                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
                    UserAgent = userAgent;
            }
            else
            {
                UserAgent = userAgent;
            }
        }

        private TimeSpan _timeout;
        /// <summary>
        /// Timeout for the HttpClient in milliseconds. Default is 100,000 milliseconds.
        /// </summary>
        public int Timeout
        {
            get { return (int)_timeout.TotalMilliseconds; }
            set
            {
                if (value <= 0)
                    value = 1;
                if ((int)_timeout.TotalMilliseconds == value)
                    return;
                _timeout = new TimeSpan(0, 0, 0, 0, value);
                if (httpClient != null)
                    httpClient.Timeout = _timeout;
            }
        }

        public ErrorHandling ErrorHandling { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="completeOnHeaders"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="WebClientException">Thrown on errors from the web client.</exception>
        /// <returns></returns>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri), $"Uri cannot be null for GetAsync");
            if (timeout == 0)
                timeout = Timeout;
            if (timeout == 0)
                timeout = (int)httpClient.Timeout.TotalMilliseconds;
            var timeoutCts = new CancellationTokenSource(timeout);
            var timeoutToken = timeoutCts.Token;
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken))
            {
                HttpResponseMessage response = null;
                try
                {
                    //TODO: Need testing for cancellation token
                    response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, linkedSource.Token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return new HttpResponseWrapper(response, uri);
                }
                catch (HttpRequestException ex)
                {
                    timeoutCts.Dispose();
                    if (ErrorHandling == ErrorHandling.ThrowOnException)
                    {
                        throw new WebClientException(ex.Message, ex, new HttpResponseWrapper(response, uri, ex));
                    }
                    else
                    {
                        //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                        return new HttpResponseWrapper(null, uri, ex);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    timeoutCts.Dispose();
                    Exception retException = ex;
                    int? statusOverride = null;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        retException = new TimeoutException(GetTimeoutMessage(uri));
                        statusOverride = (int)HttpStatusCode.RequestTimeout;
                    }
                    else
                    {
                        throw new OperationCanceledException($"GetAsync canceled for Uri {uri}", cancellationToken);
                    }
                    if (ErrorHandling == ErrorHandling.ThrowOnException)
                    {
                        throw new WebClientException(retException.Message, retException, new HttpResponseWrapper(null, uri, retException, statusOverride));
                    }
                    else
                    {
                        //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                        return new HttpResponseWrapper(null, uri, retException, statusOverride);
                    }
                }
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
                    if (httpClient != null)
                    {
                        httpClient.Dispose();
                        httpClient = null;
                    }
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
