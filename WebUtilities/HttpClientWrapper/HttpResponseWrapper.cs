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
        private HttpResponseMessage? _response;
        private int? _statusCodeOverride;
        /// <summary>
        /// Returns 0 if response was null and no status override was provided.
        /// </summary>
        public int StatusCode
        {
            get
            {
                if (_statusCodeOverride != null)
                    return _statusCodeOverride ?? 0;
                return (int)(_response?.StatusCode ?? 0);
            }
        }

        public bool IsSuccessStatusCode { get { return _response?.IsSuccessStatusCode ?? false; } }

        public string? ReasonPhrase { get { return _response?.ReasonPhrase ?? Exception?.Message; } }

        public Uri RequestUri { get; protected set; }

        public Exception? Exception { get; protected set; }

        public IWebResponseMessage EnsureSuccessStatusCode()
        {
            try
            {
                if(_response == null)
                    throw new WebClientException("Response is null.");
                if (!_response.IsSuccessStatusCode)
                {
                    HttpRequestException httpException;
                    if(((int)_response.StatusCode) > 0)
                        httpException = new HttpRequestException($"The remove server returned an error: ({(int)_response.StatusCode}) {_response.ReasonPhrase}.");
                    else
                        httpException = new HttpRequestException($"Error getting a response: {_response.ReasonPhrase}.");
                    var faultedResponse = new FaultedResponse(this);
                    _response.Dispose();
                    _response = null;
                    throw new WebClientException(httpException.Message, httpException, faultedResponse);
                }
            }
            catch (HttpRequestException ex)
            {
                var faultedResponse = new FaultedResponse(this);
                _response?.Dispose();
                _response = null;
                throw new WebClientException(ex.Message, ex, faultedResponse);
            }
            return this;
        }

        public IWebResponseContent? Content { get; protected set; }

        private Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public HttpResponseWrapper(HttpResponseMessage? response, Uri requestUri, Exception? exception = null, int? statusCodeOverride = null)
        {
            _response = response;
            _statusCodeOverride = statusCodeOverride;
            Exception = exception;
            RequestUri = requestUri;
            if(response?.Content != null)
                Content = new HttpContentWrapper(response.Content);
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
                    if (_response != null)
                    {
                        _response.Dispose();
                        _response = null;
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
