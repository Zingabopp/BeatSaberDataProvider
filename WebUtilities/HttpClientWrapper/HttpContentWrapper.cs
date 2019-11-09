using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Threading;

namespace WebUtilities.HttpClientWrapper
{
    public class HttpContentWrapper : IWebResponseContent
    {
        private HttpContent _content;

        public HttpContentWrapper(HttpContent content)
        {
            _content = content;
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_content?.Headers != null)
            {
                foreach (var header in _content.Headers)
                {
                    _headers.Add(header.Key, header.Value);
                }
            }
            try
            {
                ContentLength = content?.Headers?.ContentLength;
            }
            catch (ObjectDisposedException)
            {
                _content = null;
            }

        }

        protected Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        public string ContentType { get { return _content?.Headers?.ContentType?.MediaType ?? string.Empty; } }

        public long? ContentLength { get; protected set; }

        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            if (_content == null)
                return null;
            return await _content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        public async Task<Stream> ReadAsStreamAsync()
        {
            if (_content == null)
                return null;
            return await _content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public async Task<string> ReadAsStringAsync()
        {
            if (_content == null)
                return null;
            return await _content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Full path to the downloaded file</returns>
        public async Task<string> ReadAsFileAsync(string filePath, bool overwrite, CancellationToken cancellationToken)
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
                if (cancellationToken.CanBeCanceled)
                    cancellationToken.Register(() => fileStream.Close());
                long expectedLength = 0;
                if ((_content?.Headers?.ContentLength ?? 0) > 0)
                    expectedLength = _content.Headers.ContentLength ?? 0;
                // TODO: Should this be awaited?
                string downloadedPath = await _content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        long fileStreamLength = fileStream.Length;

                        fileStream.Close();
                        if (expectedLength != 0 && fileStreamLength != ContentLength)
                            throw new EndOfStreamException($"File content length of {fileStreamLength} didn't match expected size {expectedLength}");
                        return pathname;
                    });
                return downloadedPath;
            }
            catch (ObjectDisposedException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (Exception)
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
                        ContentLength = null;
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
