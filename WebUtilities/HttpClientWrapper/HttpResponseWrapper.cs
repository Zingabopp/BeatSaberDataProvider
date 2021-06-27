using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;

namespace WebUtilities.HttpClientWrapper
{
    /// <summary>
    /// Wrapper for the response from <see cref="HttpClientWrapper"/>.
    /// </summary>
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

        /// <inheritdoc/>
        public bool IsSuccessStatusCode { get { return _response?.IsSuccessStatusCode ?? false; } }

        /// <inheritdoc/>
        public string? ReasonPhrase { get { return _response?.ReasonPhrase ?? Exception?.Message; } }

        /// <inheritdoc/>
        public Uri RequestUri { get; protected set; }

        /// <inheritdoc/>
        public Exception? Exception { get; protected set; }

        /// <inheritdoc/>
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
                        httpException = new HttpRequestException($"The remote server returned an error: ({(int)_response.StatusCode}) {_response.ReasonPhrase}.");
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

        /// <inheritdoc/>
        public IWebResponseContent? Content { get; protected set; }

        private Dictionary<string, IEnumerable<string>> _headers;
        /// <inheritdoc/>
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        /// <summary>
        /// Creates a new <see cref="HttpResponseWrapper"/> from the given <paramref name="response"/>.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="requestUri"></param>
        /// <param name="exception"></param>
        /// <param name="statusCodeOverride"></param>
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

        /// <inheritdoc/>
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
                        // TODO: Should I be disposing of the response here? Not the content's responsibility?
                        _response.Dispose();
                        _response = null;
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
