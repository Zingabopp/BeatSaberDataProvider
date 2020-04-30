using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public abstract class DownloadContainer
        : IDisposable
    {
        public abstract bool ResultAvailable { get; }
        public bool IsDisposed { get; protected set; }
        public long? ExpectedInputLength { get; protected set; }
        public long ActualBytesReceived { get; protected set; }
        private int _progressReportRate = 50;
        public int ProgressReportRate
        {
            get { return _progressReportRate; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(ProgressReportRate), $"{nameof(ProgressReportRate)} cannot be less than 0.");
            }
        }
        public event EventHandler<DownloadProgress>? ProgressChanged;

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
        /// 
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
        /// 
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
        /// 
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
        public async virtual Task<long> ReceiveDataAsync(IWebResponseContent webResponseContent, bool disposeInput, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
        {
            if (webResponseContent == null)
                throw new ArgumentNullException(nameof(webResponseContent), $"{nameof(webResponseContent)} cannot be null for {nameof(ReceiveDataAsync)}.");
            ExpectedInputLength = webResponseContent.ContentLength;
            return await ReceiveDataAsync(await webResponseContent.ReadAsStreamAsync().ConfigureAwait(false), disposeInput, progress, cancellationToken).ConfigureAwait(false);
        }
        public abstract Stream GetResultStream();
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
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public long ReceiveData(Stream inputStream) => ReceiveData(inputStream, false);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public Task<long> ReceiveDataAsync(Stream inputStream) => ReceiveDataAsync(inputStream, false, null, CancellationToken.None);
        /// <summary>
        /// 
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
        /// 
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
        /// 
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
        /// 
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
        /// 
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected abstract void Dispose(bool disposing);
        #endregion
    }
}
