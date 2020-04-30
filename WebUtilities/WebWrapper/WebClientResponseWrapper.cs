﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;

namespace WebUtilities.WebWrapper
{
    public class WebClientResponseWrapper : IWebResponseMessage
    {
        private HttpWebResponse? _response;
        //private HttpWebRequest? _request;
        /// <summary>
        /// Returns 0 if response was null and no status override was provided.
        /// </summary>
        private int? _statusCodeOverride;

        public int StatusCode
        {
            get
            {
                if (_statusCodeOverride != null)
                    return _statusCodeOverride ?? 0;
                return (int)(_response?.StatusCode ?? 0);
            }
        }

        public bool IsSuccessStatusCode
        {
            get { return (StatusCode >= 200) && (StatusCode <= 299); }
        }

        public Exception? Exception { get; protected set; }

        public Uri RequestUri { get; protected set; }

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
                    webException = new WebException($"The remove server returned an error: ({(int)_response.StatusCode}) {ReasonPhrase}.");
                var faultedResponse = new FaultedResponse(this);
                _response.Dispose();
                _response = null;
                throw new WebClientException(webException.Message, webException, faultedResponse);
            }
            return this;
        }

        public IWebResponseContent? Content { get; protected set; }

        private Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public string ReasonPhrase { get { return _response?.StatusDescription ?? Exception?.Message ?? "Unknown Error"; } }

        public WebClientResponseWrapper(HttpWebResponse? response, HttpWebRequest request, Exception? exception = null, int? statusCodeOverride = null)
        {
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
