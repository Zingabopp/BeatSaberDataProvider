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

        public int Timeout { get; set; }
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

        public async Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            var request = HttpWebRequest.CreateHttp(uri);
            if (cancellationToken.IsCancellationRequested)
                return null;
            try
            {
                var getTask = request.GetResponseAsync();
                if (cancellationToken != CancellationToken.None)
                {
                    var cancelTask = cancellationToken.AsTask();
                    await Task.WhenAny(getTask, cancelTask);
                    if (!getTask.IsCompleted)
                        return null;
                }
                var response = await getTask.ConfigureAwait(false) as HttpWebResponse;
                //TODO: Need testing for cancellation token
                return new WebClientResponseWrapper(response, request);
            }
            catch (ArgumentException ex)
            {
                if (ErrorHandling != ErrorHandling.ReturnEmptyContent)
                    throw;
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Invalid URL, {uri?.ToString()}, passed to GetAsync()\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(null, null);
                }
            }
            catch (WebException ex)
            {
                if (ErrorHandling == ErrorHandling.ThrowOnException)
                    throw;
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(null, null);
                }
            }

        }

        #region GetAsyncOverloads

        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            var urlAsUri = string.IsNullOrEmpty(url) ? null : new Uri(url);
            return GetAsync(urlAsUri, completeOnHeaders, cancellationToken);
        }
        public Task<IWebResponseMessage> GetAsync(string url)
        {
            return GetAsync(url, false, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, bool completeOnHeaders)
        {
            return GetAsync(url, completeOnHeaders, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
        {
            return GetAsync(url, false, cancellationToken);
        }

        public Task<IWebResponseMessage> GetAsync(Uri uri)
        {
            return GetAsync(uri, false, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders)
        {
            return GetAsync(uri, completeOnHeaders, CancellationToken.None);
        }
        public Task<IWebResponseMessage> GetAsync(Uri uri, CancellationToken cancellationToken)
        {
            return GetAsync(uri, false, cancellationToken);
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
