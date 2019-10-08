using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace SongFeedReadersTests.MockClasses
{
    public class MockWebClient : IWebClient
    {
        public int Timeout { get; set; }
        public string UserAgent { get; private set; }

        public ErrorHandling ErrorHandling { get; set; }

        public void SetUserAgent(string userAgent)
        {
            UserAgent = userAgent;
        }

        public Task<IWebResponseMessage> GetAsync(Uri uri, int timeout, CancellationToken cancellationToken)
        {
            //var content = new MockHttpContent(url);
#pragma warning disable CA2000 // Dispose objects before losing scope
            var response = new MockHttpResponse(uri);
#pragma warning restore CA2000 // Dispose objects before losing scope
            return Task.Run(() => { return (IWebResponseMessage)response; });
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
                    //if (httpClient != null)
                    //{
                    //    httpClient.Dispose();
                    //    httpClient = null;
                    //}
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
