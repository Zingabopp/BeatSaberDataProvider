using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;

namespace WebUtilities.WebWrapper
{
    public class WebClientResponseWrapper : IWebResponseMessage
    {
        private HttpWebResponse _response;
        private HttpWebRequest _request;

        public int StatusCode { get { return (int)(_response?.StatusCode ?? 0); } }

        public bool IsSuccessStatusCode
        {
            get { return (StatusCode >= 200) && (StatusCode <= 299); }
        }

        public Exception Exception { get; protected set; }

        public IWebResponseMessage EnsureSuccessStatusCode()
        {
            if (!IsSuccessStatusCode)
                throw new WebException($"HttpStatus: {StatusCode.ToString()}, {ReasonPhrase} getting {_request?.RequestUri.ToString()}.");
            return this;
        }

        public IWebResponseContent Content { get; protected set; }

        private Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public string ReasonPhrase { get { return _response?.StatusDescription ?? Exception?.Message; } }

        public WebClientResponseWrapper(HttpWebResponse response, HttpWebRequest request, Exception exception = null)
        {
            _response = response;
            _request = request;
            Exception = exception;
            Content = new WebClientContent(_response);
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
