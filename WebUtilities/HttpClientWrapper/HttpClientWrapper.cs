using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static WebUtilities.Utilities;

namespace WebUtilities.HttpClientWrapper
{
    /// <summary>
    /// <see cref="IWebClient"/> wrapper for a <see cref="HttpClient"/>.
    /// </summary>
    public class HttpClientWrapper : IWebClient
    {
        private HttpClient? httpClient;
        /// <inheritdoc/>
        public string? UserAgent { get; private set; }
        /// <summary>
        /// Creates a new <see cref="HttpClientWrapper"/> with a default <see cref="HttpClient"/>.
        /// </summary>
        public HttpClientWrapper()
        {
            httpClient = new HttpClient();
            ErrorHandling = ErrorHandling.ThrowOnException;
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientWrapper"/> with a default <see cref="HttpClient"/> and sets the provided <paramref name="userAgent"/>.
        /// </summary>
        /// <param name="userAgent"></param>
        public HttpClientWrapper(string userAgent)
            : this()
        {
            if (!string.IsNullOrEmpty(userAgent))
            {
                UserAgent = userAgent;
                SetUserAgent(UserAgent);
            }
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientWrapper"/> with the given <paramref name="client"/>.
        /// </summary>
        /// <param name="client"></param>
        public HttpClientWrapper(HttpClient client)
        {
            if (client == null)
                httpClient = new HttpClient();
            else
                httpClient = client;
            ErrorHandling = ErrorHandling.ThrowOnException;
            httpClient.Timeout = _timeout;
        }
        /// <inheritdoc/>
        public void SetUserAgent(string? userAgent)
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
        /// <inheritdoc/>
        public ErrorHandling ErrorHandling { get; set; }

        /// <inheritdoc/>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri), "uri cannot be null.");
            if (timeout < 0) throw new ArgumentOutOfRangeException(nameof(timeout), "timeout cannot be less than 0.");
            HttpClient client = httpClient ?? throw new InvalidOperationException($"{nameof(httpClient)} is null.");
            if (timeout == 0)
                timeout = Timeout;
            if (timeout == 0)
                timeout = (int)client.Timeout.TotalMilliseconds;
            using (var timeoutCts = new CancellationTokenSource(timeout))
            {
                var timeoutToken = timeoutCts.Token;
                using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken))
                {
                    HttpResponseMessage? response = null;
                    try
                    {
                        //TODO: Need testing for cancellation token
                        response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, linkedSource.Token).ConfigureAwait(false);
                        // response.EnsureSuccessStatusCode(); // Calling this disposes the content.

                        var wrappedResponse = new HttpResponseWrapper(response, uri);
                        if (ErrorHandling == ErrorHandling.ThrowOnException)
                            wrappedResponse.EnsureSuccessStatusCode();
                        return wrappedResponse;
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ErrorHandling == ErrorHandling.ThrowOnException)
                        {
                            throw new WebClientException(ex.Message, ex, new HttpResponseWrapper(response, uri, ex));
                        }
                        else
                        {
                            //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                            return new HttpResponseWrapper(response, uri, ex);
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
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
            var urlAsUri = new Uri(url);
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

        /// <summary>
        /// Disposes the wrapped <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="disposing"></param>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
