﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;

namespace WebUtilities.WebWrapper
{
    public class WebClientResponseWrapper : IWebResponseMessage
    {
        private HttpWebResponse _response;
        private HttpWebRequest _request;

        public HttpStatusCode StatusCode { get { return _response.StatusCode; } }

        public bool IsSuccessStatusCode
        {
            get { return ((int)StatusCode >= 200) && ((int)StatusCode <= 299); }
        }

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

        public string ReasonPhrase { get { return _response.StatusDescription; } }

        public WebClientResponseWrapper(HttpWebResponse response, HttpWebRequest request)
        {
            _response = response;
            _request = request;
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
