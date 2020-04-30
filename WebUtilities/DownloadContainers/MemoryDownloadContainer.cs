using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public class MemoryDownloadContainer : DownloadContainer
    {
        private byte[]? _data;

        public override bool ResultAvailable { get => _data != null; }

        public MemoryDownloadContainer() { }
        public MemoryDownloadContainer(byte[] existingData) => _data = existingData;

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
        public override long ReceiveData(Stream inputStream, bool disposeInput)
        {
            long actualBytesReceived = 0;
            try
            {
                if (inputStream is MemoryStream memoryStream)
                {
                    _data = memoryStream.ToArray();
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    _data = ms.ToArray();
                }
                actualBytesReceived = _data?.Length ?? 0;
                ExpectedInputLength = actualBytesReceived;
                TriggerProgressChanged(actualBytesReceived, actualBytesReceived, null);
                return actualBytesReceived;
            }
            catch
            {
                throw;
            }
            finally
            {
                ActualBytesReceived = actualBytesReceived;
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

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
        public async override Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
        {
            long actualBytesReceived = 0;
            try
            {
                if (inputStream is MemoryStream ms)
                {
                    _data = ms.ToArray();
                    actualBytesReceived = _data.Length;
                    ExpectedInputLength = actualBytesReceived;
                    TriggerProgressChanged(actualBytesReceived, actualBytesReceived, progress);
                    return actualBytesReceived;
                }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await ProcessContentStreamAsync(inputStream, memoryStream, progress, cancellationToken).ConfigureAwait(false);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    _data = memoryStream.ToArray();
                    actualBytesReceived = memoryStream.Length;
                    return actualBytesReceived;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                ActualBytesReceived = actualBytesReceived;
                try
                {
                    if (disposeInput)
                        inputStream.Dispose();
                }
                catch { }
            }
        }

        public override Stream GetResultStream()
        {
            return new MemoryStream(_data);
        }

        bool disposed;
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposed)
            {
                if (disposing)
                {

                }
                _data = null;
                disposed = true;
            }
        }
    }

}
