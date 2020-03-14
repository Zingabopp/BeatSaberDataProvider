using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public struct DownloadProgress
    {
        public readonly long? TotalDownloadSize;
        public readonly long BytesRead;
        public readonly long TotalBytesDownloaded;
        public DownloadProgress(long? totalDownloadSize, long bytesRead, long totalBytesDownloaded)
        {
            TotalDownloadSize = totalDownloadSize;
            BytesRead = bytesRead;
            TotalBytesDownloaded = totalBytesDownloaded;
        }

        public double? ProgressPercent
        {
            get
            {
                if (TotalDownloadSize.HasValue && TotalDownloadSize > 0)
                    return Math.Round((double)TotalBytesDownloaded / TotalDownloadSize.Value * 100, 2);
                return null;
            }
        }

        public override string ToString()
        {
            if (TotalDownloadSize.HasValue)
                return $"{ProgressPercent}";
            else
                return $"{BytesRead} ({TotalBytesDownloaded})";
        }
    }
    /// <summary>
    /// From: https://stackoverflow.com/a/43169927
    /// </summary>
    public class DownloadWithProgress : IDisposable
    {
        private readonly Uri _downloadUri;
        private readonly string _destinationFilePath;
        private Stream _destinationStream;
        private readonly int ReportRate;
        public bool DownloadStarted { get; protected set; }
        protected CancellationTokenSource TokenSource { get; set; }
        public IWebClient WebClient { get; protected set; }

        public event EventHandler<DownloadProgress> ProgressChanged;

        public DownloadWithProgress(IWebClient client, Uri downloadUri, int reportRate = 50)
        {
            ReportRate = Math.Min(reportRate, 1);
            WebClient = client ?? throw new ArgumentNullException(nameof(client), "client cannot be null for HttpClientDownloadWithProgress.");
            _downloadUri = downloadUri ?? throw new ArgumentNullException(nameof(downloadUri), "downloadUri cannot be null for HttpClientDownloadWithProgress.");
        }
        public DownloadWithProgress(IWebClient client, Uri downloadUri, string targetFilePath, int reportRate = 50)
            : this(client, downloadUri, reportRate)
        {
            if (string.IsNullOrEmpty(targetFilePath?.Trim()))
                throw new ArgumentNullException(nameof(targetFilePath), $"{nameof(targetFilePath)} cannot be null or empty for HttpClientDownloadWithProgress.");
            _destinationFilePath = targetFilePath;
        }

        public DownloadWithProgress(IWebClient client, string downloadUrl, string targetFilePath, int reportRate = 50)
            : this(client, new Uri(downloadUrl), targetFilePath, reportRate)
        { }

        /// <summary>
        /// Starts the download using the provided <paramref name="targetStream"/> as the output.
        /// </summary>
        /// <param name="targetStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task StartDownload(Stream targetStream, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            if (DownloadStarted) return;
            DownloadStarted = true;
            TokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = TokenSource.Token;
            _destinationStream = targetStream;
            using (IWebResponseMessage response = await WebClient.GetAsync(_downloadUri, token).ConfigureAwait(false))
                await DownloadFileFromHttpResponseMessage(response, progress, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the download using the provided <paramref name="targetStream"/> as the output.
        /// </summary>
        /// <param name="targetStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task StartDownload(Stream targetStream, CancellationToken cancellationToken)
        {
            return StartDownload(targetStream, default(IProgress<DownloadProgress>), cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public virtual async Task StartDownload(IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            if (DownloadStarted) return;
            if (string.IsNullOrEmpty(_destinationFilePath))
                throw new InvalidOperationException("No valid target found.");
            using (FileStream targetStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                await StartDownload(targetStream, progress, cancellationToken).ConfigureAwait(false);
        }

        public virtual Task StartDownload(CancellationToken cancellationToken) => StartDownload(default(IProgress<DownloadProgress>), cancellationToken);

        public virtual Task StartDownload() => StartDownload(default(IProgress<DownloadProgress>), CancellationToken.None);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task DownloadFileFromHttpResponseMessage(IWebResponseMessage response, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.ContentLength;

            using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                await ProcessContentStream(totalBytes, contentStream, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalDownloadSize"></param>
        /// <param name="contentStream"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long totalBytesRead = 0L;
            long readCount = 0L;
            byte[] buffer = new byte[8192];
            long progressBytesRead = 0L;
            bool isMoreToRead = true;
            using (_destinationStream)
            {
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    progressBytesRead += bytesRead;
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(totalDownloadSize, progressBytesRead, totalBytesRead, progress);
                        continue;
                    }

                    await _destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (ReportRate == 1 || readCount % ReportRate == 0)
                    {
                        TriggerProgressChanged(totalDownloadSize, progressBytesRead, totalBytesRead, progress);
                        progressBytesRead = 0;
                    }
                }
                while (isMoreToRead);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long bytesRead, long totalBytesRead, IProgress<DownloadProgress> progress)
        {
            if (ProgressChanged == null && progress == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
            DownloadProgress currentProgress = new DownloadProgress(totalDownloadSize, bytesRead, totalBytesRead);

            progress?.Report(currentProgress);
            EventHandler<DownloadProgress> progressHandler = ProgressChanged;
            progressHandler?.Invoke(this, currentProgress);

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _destinationStream = null;
                    if (TokenSource != null)
                    {
                        TokenSource.Cancel();
                        TokenSource.Dispose();
                        TokenSource = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpClientDownloadWithProgress()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
