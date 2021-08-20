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
    /// <summary>
    /// Wrapper for the content from a <see cref="HttpResponseWrapper"/>.
    /// </summary>
    public class HttpContentWrapper : IWebResponseContent
    {
        private HttpContent? _content;

        /// <summary>
        /// Creates a new <see cref="HttpContentWrapper"/> from the provided <paramref name="content"/>.
        /// </summary>
        /// <param name="content"></param>
        public HttpContentWrapper(HttpContent? content)
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
        /// <summary>
        /// Response headers.
        /// </summary>
        protected Dictionary<string, IEnumerable<string>> _headers;
        /// <inheritdoc/>
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

        /// <inheritdoc/>
        public string ContentType { get { return _content?.Headers?.ContentType?.MediaType ?? string.Empty; } }

        /// <inheritdoc/>
        public long? ContentLength { get; protected set; }

        /// <inheritdoc/>
        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            HttpContent content = _content ?? throw new InvalidOperationException("There is no content to read.");
            return await content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<Stream> ReadAsStreamAsync()
        {
            HttpContent content = _content ?? throw new InvalidOperationException("There is no content to read.");
            return content.ReadAsStreamAsync();
        }

        /// <inheritdoc/>
        public async Task<string> ReadAsStringAsync()
        {
            HttpContent content = _content ?? throw new InvalidOperationException("There is no content to read.");
            return await content.ReadAsStringAsync().ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<string> ReadAsFileAsync(string filePath, bool overwrite, CancellationToken cancellationToken)
        {
            HttpContent? content = _content ?? throw new InvalidOperationException("There is no content to read.");
            if (string.IsNullOrEmpty(filePath?.Trim()))
                throw new ArgumentNullException(nameof(filePath), "filename cannot be null or empty for HttpContent.ReadAsFileAsync");
            string pathname = Path.GetFullPath(filePath);
            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            FileStream? fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                if (cancellationToken.CanBeCanceled)
                    cancellationToken.Register(() => fileStream.Close());
                long expectedLength = Math.Max(0, _content?.Headers?.ContentLength ?? 0);

                // TODO: Should this be awaited?
                string downloadedPath = await content.CopyToAsync(fileStream).ContinueWith(
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
