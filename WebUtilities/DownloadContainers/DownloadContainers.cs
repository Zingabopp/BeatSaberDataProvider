using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities.DownloadContainers
{
    /// <summary>
    /// A container to house the contents of a web download. This is an abstract class.
    /// </summary>
    public abstract class DownloadContainer
        : IDisposable
    {
        /// <summary>
        /// Returns true if data was successfully received and the target file exists.
        /// </summary>
        public abstract bool ResultAvailable { get; }
        /// <summary>
        /// Returns true if the <see cref="DownloadContainer"/> was disposed.
        /// </summary>
        public bool IsDisposed { get; protected set; }
        /// <summary>
        /// The expected length in bytes of the content. May be null.
        /// </summary>
        public long? ExpectedInputLength { get; protected set; }
        /// <summary>
        /// The length in bytes of the content received.
        /// </summary>
        public long ActualBytesReceived { get; protected set; }
        private int _progressReportRate = 50;
        /// <summary>
        /// Report rate of the <see cref="ProgressChanged"/> handler and the progress callback. 0 to disable progress reporting.
        /// </summary>
        public int ProgressReportRate
        {
            get { return _progressReportRate; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(ProgressReportRate), $"{nameof(ProgressReportRate)} cannot be less than 0.");
            }
        }
        /// <summary>
        /// Event invoked for progress updates.
        /// </summary>
        public event EventHandler<DownloadProgress>? ProgressChanged;

        /// <summary>
        /// Asynchronously transfers the contents of <paramref name="contentStream"/> to <paramref name="destinationStream"/>.
        /// </summary>
        /// <param name="contentStream"></param>
        /// <param name="destinationStream"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        protected virtual async Task ProcessContentStreamAsync(Stream contentStream, Stream destinationStream, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long totalBytesRead = 0L;
            long readCount = 0L;
            long progressBytesRead = 0L;
            byte[] buffer = new byte[8192];
            bool isMoreToRead = true;
            try
            {
                do
                {
                    int bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    progressBytesRead += bytesRead;
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        TriggerProgressChanged(progressBytesRead, totalBytesRead, progress);
                        progressBytesRead = 0;
                        continue;
                    }

                    await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    totalBytesRead += bytesRead;
                    readCount += 1;

                    if (ProgressReportRate > 0 && (ProgressReportRate == 1 || readCount % ProgressReportRate == 0)) // No progress reports if the rate is 0.
                    {
                        TriggerProgressChanged(progressBytesRead, totalBytesRead, progress);
                        progressBytesRead = 0;
                    }
                }
                while (isMoreToRead);
            }
            catch { throw; }
            finally
            {
                ActualBytesReceived = totalBytesRead;
            }
        }

        /// <summary>
        /// Invokes the <see cref="ProgressChanged"/> event and the <paramref name="progress"/> if provided.
        /// </summary>
        /// <param name="bytesRead">Bytes read since the last progress change.</param>
        /// <param name="totalBytesRead">Total bytes transferred into the container.</param>
        /// <param name="progress">Progress callback to invoke.</param>
        protected virtual void TriggerProgressChanged(long bytesRead, long totalBytesRead, IProgress<DownloadProgress>? progress)
        {
            if (ProgressChanged == null && progress == null)
                return;
            DownloadProgress currentProgress = new DownloadProgress(ExpectedInputLength, bytesRead, totalBytesRead);

            progress?.Report(currentProgress);
            EventHandler<DownloadProgress>? progressHandler = ProgressChanged;
            progressHandler?.Invoke(this, currentProgress);
        }
        /// <summary>
        /// Transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public abstract long ReceiveData(Stream inputStream, bool disposeInput);

        /// <summary>
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public abstract Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously transfers the contents of the <paramref name="webResponseContent"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="webResponseContent"></param>
        /// <param name="disposeInput"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebClientException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public async virtual Task<long> ReceiveDataAsync(IWebResponseContent webResponseContent, bool disposeInput, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
        {
            if (webResponseContent == null)
                throw new ArgumentNullException(nameof(webResponseContent), $"{nameof(webResponseContent)} cannot be null for {nameof(ReceiveDataAsync)}.");
            ExpectedInputLength = webResponseContent.ContentLength;
            return await ReceiveDataAsync(await webResponseContent.ReadAsStreamAsync().ConfigureAwait(false), disposeInput, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a stream with the data contained in this <see cref="MemoryDownloadContainer"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no data to retrieve.</exception>
        public abstract Stream GetResultStream();

        /// <summary>
        /// Tries to get a stream with the data contained in this <see cref="MemoryDownloadContainer"/>. Returns false if there is no data.
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> with the data.</param>
        /// <param name="exception">Any <see cref="Exception"/> thrown when getting the data stream.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no data to retrieve.</exception>
        public virtual bool TryGetResultStream(out Stream? stream, out Exception? exception)
        {
            stream = null;
            try
            {
                stream = GetResultStream();
                exception = null;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        #region Overloads
        /// <summary>
        /// Transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public long ReceiveData(Stream inputStream) => ReceiveData(inputStream, false);
        /// <summary>
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream) => ReceiveDataAsync(inputStream, false, null, CancellationToken.None);
        /// <summary>
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream, CancellationToken cancellationToken) => ReceiveDataAsync(inputStream, false, null, cancellationToken);
        /// <summary>
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, CancellationToken cancellationToken) => ReceiveDataAsync(inputStream, disposeInput, null, cancellationToken);

        /// <summary>
        /// Asynchronously transfers the contents of the <paramref name="webResponseContent"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="webResponseContent"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebClientException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(IWebResponseContent webResponseContent) => ReceiveDataAsync(webResponseContent, false, null, CancellationToken.None);
        /// <summary>
        /// Asynchronously transfers the contents of the <paramref name="webResponseContent"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="webResponseContent"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebClientException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(IWebResponseContent webResponseContent, CancellationToken cancellationToken) => ReceiveDataAsync(webResponseContent, false, null, cancellationToken);
        /// <summary>
        /// Asynchronously transfers the contents of the <paramref name="webResponseContent"/> to the <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="webResponseContent"></param>
        /// <param name="disposeInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebClientException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(IWebResponseContent webResponseContent, bool disposeInput, CancellationToken cancellationToken) => ReceiveDataAsync(webResponseContent, disposeInput, null, cancellationToken);
        #endregion

        #region IDisposable
        /// <summary>
        /// Disposes of the <see cref="DownloadContainer"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Disposes of the <see cref="DownloadContainer"/>.
        /// </summary>
        protected abstract void Dispose(bool disposing);
        #endregion
    }
}
