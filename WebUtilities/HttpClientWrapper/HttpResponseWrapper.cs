using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;

namespace WebUtilities.HttpClientWrapper
{
    public class HttpResponseWrapper : IWebResponseMessage
    {
        private HttpResponseMessage _response;
        /// <summary>
        /// Returns 0 if response was null.
        /// </summary>
        public int StatusCode { get { return (int)(_response?.StatusCode ?? 0); } }

        public bool IsSuccessStatusCode { get { return _response?.IsSuccessStatusCode ?? false; } }

        public string ReasonPhrase { get { return _response?.ReasonPhrase ?? Exception?.Message; } }

        public Exception Exception { get; protected set; }

        public IWebResponseMessage EnsureSuccessStatusCode()
        {
            try
            {
                var _ = _response?.EnsureSuccessStatusCode() ?? throw new WebClientException("Response is null.");
            }
            catch (HttpRequestException ex)
            {
                throw new WebClientException(ex.Message, ex);
            }
            return this;
        }

        public IWebResponseContent Content { get; protected set; }

        private Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public HttpResponseWrapper(HttpResponseMessage response, Exception exception = null)
        {
            _response = response;
            Exception = exception;
            Content = new HttpContentWrapper(response?.Content);
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_response?.Headers != null)
            {
                foreach (var header in _response.Headers)
                {
                    _headers.Add(header.Key, header.Value);
                }
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Content != null)
                    {
                        Content.Dispose();
                        Content = null;
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
