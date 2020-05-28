using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;

namespace WebUtilities.WebWrapper
{
    /// <summary>
    /// Wrapper for the content of a <see cref="WebClientResponseWrapper"/>.
    /// </summary>
    public class WebClientContent : IWebResponseContent
    {
        private HttpWebResponse? _response;
        /// <summary>
        /// Creates a new <see cref="WebClientContent"/> from the <paramref name="response"/>.
        /// </summary>
        /// <param name="response"></param>
        public WebClientContent(HttpWebResponse? response)
        {
            _response = response;
            ContentLength = _response?.ContentLength ?? 0;
            if (ContentLength < 0)
                ContentLength = null;
            _headers = new Dictionary<string, IEnumerable<string>>();
            if (_response?.Headers != null)
            {
                foreach (var headerKey in _response.Headers.AllKeys)
                {
                    _headers.Add(headerKey, new string[] { _response.Headers[headerKey] });
                }
            }
        }

        /// <summary>
        /// Response headers.
        /// </summary>
        protected Dictionary<string, IEnumerable<string>> _headers;

        /// <summary>
        /// A ReadOnlyDictionary of the response headers.
        /// </summary>
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }
        /// <inheritdoc/>
        public string ContentType
        {
            get
            {
                if (_response == null)
                    return string.Empty;
                var cType = _response.ContentType ?? string.Empty;
                if (cType.Contains(";"))
                    cType = cType.Substring(0, cType.IndexOf(";"));
                return cType;
            }
        }
        /// <inheritdoc/>
        public long? ContentLength { get; protected set; }
        /// <inheritdoc/>
        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            using (Stream stream = _response?.GetResponseStream() ?? throw new InvalidOperationException("There is no content to read."))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream, int.MaxValue).ConfigureAwait(false);
                    return memStream.ToArray();
                }
            }
        }
        /// <inheritdoc/>
        public Task<Stream> ReadAsStreamAsync()
        {
            return Task.Run(() => _response?.GetResponseStream() ?? throw new InvalidOperationException("There is no content to read."));
        }
        /// <inheritdoc/>
        public async Task<string> ReadAsStringAsync()
        {
            using (Stream stream = _response?.GetResponseStream() ?? throw new InvalidOperationException("There is no content to read."))
            using (var sr = new StreamReader(stream))
            {
                return await sr.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory it's trying to save to doesn't exist.</exception>
        /// <exception cref="EndOfStreamException">Thrown when the downloaded file's size doesn't match the expected size</exception>
        /// <exception cref="IOException">Thrown when there's a problem writing the file.</exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Full path to the downloaded file</returns>
        public async Task<string> ReadAsFileAsync(string filePath, bool overwrite, CancellationToken cancellationToken)
        {
            HttpWebResponse response = _response ?? throw new InvalidOperationException("There is no content to read.");
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
                var responseStream = response.GetResponseStream();
                long expectedLength = 0;
                if (response.ContentLength > 0)
                    expectedLength = response.ContentLength;
                // TODO: Timeouts don't seem to do anything.
                //responseStream.ReadTimeout = 1;
                //responseStream.WriteTimeout = 1;
                string downloadedPath = string.Empty;
                downloadedPath = await response.GetResponseStream().CopyToAsync(fileStream, 81920, cancellationToken).ContinueWith(
                    (copyTask) =>
                    {
                        long fileStreamLength = fileStream.Length;

                        fileStream.Close();

                        if (expectedLength != 0 && fileStreamLength != ContentLength)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            throw new EndOfStreamException($"File content length of {fileStreamLength} didn't match expected size {expectedLength}");
                        }
                        return pathname;
                    }).ConfigureAwait(false);
                return downloadedPath;
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
        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
