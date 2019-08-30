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
        private int _timeout;
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
        /// <exception cref="WebException">Thrown when there's a WebException.</exception>
        public async Task<IWebResponseMessage> GetAsync(Uri uri, bool completeOnHeaders, CancellationToken cancellationToken)
        {
            var request = HttpWebRequest.CreateHttp(uri);
            if (Timeout != 0)
            {
                // TODO: This doesn't seem to do anything.
                request.ContinueTimeout = Timeout;
                request.Timeout = Timeout;
                request.ReadWriteTimeout = Timeout;
            }
            if (cancellationToken.IsCancellationRequested)
                return null;
            try
            {
                var getTask = request.GetResponseAsync();
                //TODO: Need testing for cancellation token
                if (cancellationToken.CanBeCanceled)
                {
                    var cancelTask = cancellationToken.AsTask();
                    await Task.WhenAny(getTask, cancelTask).ConfigureAwait(false);
                    if (!getTask.IsCompleted) // either getTask completed or cancelTask was triggered
                    {
                        return null;
                    }
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

                // This is thrown by HttpWebRequest.GetResponseAsync(), so we can't throw the exception here or the calling code won't be able to decide how to handle it...sorta
                if (ErrorHandling == ErrorHandling.ThrowOnException)
                    throw new WebClientException(ex.Message, ex, uri, new WebClientResponseWrapper(resp, request, ex));
                else
                {
                    //Logger?.Log(LogLevel.Error, $"Exception getting {uri?.ToString()}\n{ex.Message}\n{ex.StackTrace}");
                    return new WebClientResponseWrapper(resp, request, ex);
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
