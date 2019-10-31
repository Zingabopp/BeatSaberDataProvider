﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;

namespace WebUtilities.WebWrapper
{
    public class WebClientContent : IWebResponseContent
    {
        private HttpWebResponse _response;
        public WebClientContent(HttpWebResponse response)
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

        protected Dictionary<string, IEnumerable<string>> _headers;
        public ReadOnlyDictionary<string, IEnumerable<string>> Headers
        {
            get { return new ReadOnlyDictionary<string, IEnumerable<string>>(_headers); }
        }

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

        public long? ContentLength { get; protected set; }

        public async Task<byte[]> ReadAsByteArrayAsync()
        {
            using (Stream stream = _response.GetResponseStream())
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    await Task.Yield();
                    await stream.CopyToAsync(memStream, int.MaxValue).ConfigureAwait(false);
                    return memStream.ToArray();
                }
            }
        }

        public Task<Stream> ReadAsStreamAsync()
        {
            return Task.Run(() => _response?.GetResponseStream());
        }

        public async Task<string> ReadAsStringAsync()
        {
            using (Stream stream = _response?.GetResponseStream())
            using (var sr = new StreamReader(stream))
            {
                await Task.Yield();
                return await sr.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Downloads the provided HttpContent to the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">Thrown when content or the filename are null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and a file at the provided path already exists.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory it's trying to save to doesn't exist.</exception>
        /// <exception cref="EndOfStreamException">Thrown when the downloaded file's size doesn't match the expected size</exception>
        /// <exception cref="IOException">Thrown when there's a problem writing the file.</exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Full path to the downloaded file</returns>
        public async Task<string> ReadAsFileAsync(string filePath, bool overwrite, CancellationToken cancellationToken)
        {
            if (_response == null)
                throw new ArgumentNullException(nameof(_response), "content cannot be null for HttpContent.ReadAsFileAsync");
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
                var responseStream = _response.GetResponseStream();
                long expectedLength = 0;
                if (_response.ContentLength > 0)
                    expectedLength = _response.ContentLength;
                // TODO: Timeouts don't seem to do anything.
                //responseStream.ReadTimeout = 1;
                //responseStream.WriteTimeout = 1;
                string downloadedPath = string.Empty;
                downloadedPath = await _response.GetResponseStream().CopyToAsync(fileStream, 81920, cancellationToken).ContinueWith(
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
