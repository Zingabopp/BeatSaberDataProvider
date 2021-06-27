using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;

namespace WebUtilities.WebWrapper
{
    /// <summary>
    /// Response wrapper for <see cref="WebClientWrapper"/>.
    /// </summary>
    public class WebClientResponseWrapper : IWebResponseMessage
    {
        private HttpWebResponse? _response;
        //private HttpWebRequest? _request;
        /// <summary>
        /// Returns 0 if response was null and no status override was provided.
        /// </summary>
        private int? _statusCodeOverride;

        /// <inheritdoc/>
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
        public bool IsSuccessStatusCode
        {
            get { return (StatusCode >= 200) && (StatusCode <= 299); }
        }

        /// <inheritdoc/>
        public Exception? Exception { get; protected set; }

        /// <inheritdoc/>
        public Uri RequestUri { get; protected set; }

        /// <inheritdoc/>
        public IWebResponseMessage EnsureSuccessStatusCode()
        {
            if (_response == null)
                throw new WebClientException("Response is null.");
            if (!IsSuccessStatusCode)
            {
                WebException webException;
                if (((int)_response.StatusCode) == 0 && Exception == null)
                    webException = new WebException($"Error getting a response: {ReasonPhrase}.");
                else if (Exception is WebException wException)
                    webException = wException;
                else
                    webException = new WebException($"The remote server returned an error: ({(int)_response.StatusCode}) {ReasonPhrase}.");
                var faultedResponse = new FaultedResponse(this);
                _response.Dispose();
                _response = null;
                throw new WebClientException(webException.Message, webException, faultedResponse);
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

        /// <inheritdoc/>
        public string ReasonPhrase { get { return _response?.StatusDescription ?? Exception?.Message ?? "Unknown Error"; } }
        /// <summary>
        /// Creates a new <see cref="WebClientResponseWrapper"/> with the given <see cref="HttpWebResponse"/> and <see cref="HttpWebRequest"/>.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="request"></param>
        /// <param name="exception"></param>
        /// <param name="statusCodeOverride"></param>
        public WebClientResponseWrapper(HttpWebResponse? response, HttpWebRequest request, Exception? exception = null, int? statusCodeOverride = null)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request), $"Request cannot be null when creating a {nameof(WebClientResponseWrapper)}");
            _response = response;
            _statusCodeOverride = statusCodeOverride;
            Exception = exception;
            RequestUri = request.RequestUri;
            if(response != null)
                Content = new WebClientContent(response);
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_response?.Headers != null)
            {
                foreach (var headerKey in _response.Headers.AllKeys)
                {
                    _headers.Add(headerKey, new string[] { _response.Headers[headerKey] });
                }
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes of the <see cref="Content"/> and wrapped response, if they exist.
        /// </summary>
        /// <param name="disposing"></param>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
