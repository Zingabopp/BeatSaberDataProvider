using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace WebUtilities.HttpClientWrapper
{
    public class HttpContentWrapper : IWebResponseContent
    {
        private HttpContent _content;

        public HttpContentWrapper(HttpContent content)
        {
            _content = content;
            ContentLength = content?.Headers?.ContentLength;
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_content?.Headers != null)
            {
                foreach (var header in _content.Headers)
                {
                    _headers.Add(header.Key, header.Value);
                }
            }
        }

        protected Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public string ContentType { get { return _content?.Headers?.ContentType?.MediaType; } }

        public long? ContentLength { get; protected set; }

        public Task<byte[]> ReadAsByteArrayAsync()
        { 
            return _content?.ReadAsByteArrayAsync();
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            return _content?.ReadAsStreamAsync();
        }

        public Task<string> ReadAsStringAsync()
        {
            return _content?.ReadAsStringAsync();
        }

        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <returns>Full path to the downloaded file</returns>
        public Task<string> ReadAsFileAsync(string filePath, bool overwrite)
        {
            if (_content == null)
                throw new ArgumentNullException(nameof(_content), "content cannot be null for HttpContent.ReadAsFileAsync");
            if (string.IsNullOrEmpty(filePath?.Trim()))
                throw new ArgumentNullException(nameof(filePath), "filename cannot be null or empty for HttpContent.ReadAsFileAsync");
            string pathname = Path.GetFullPath(filePath);
            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                long expectedLength = 0;
                if ((_content?.Headers?.ContentLength ?? 0) > 0)
                    expectedLength = _content.Headers.ContentLength ?? 0;
                // TODO: Should this be awaited?
                return _content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        long fileStreamLength = fileStream.Length;

                        fileStream.Close();
                        if (expectedLength != 0 && fileStreamLength != ContentLength)
                            throw new EndOfStreamException($"File content length of {fileStreamLength} didn't match expected size {expectedLength}");
                        return pathname;
                    });
            }
            catch
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }

                throw;
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
                    if (_content != null)
                    {
                        _content.Dispose();
                        _content = null;
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
