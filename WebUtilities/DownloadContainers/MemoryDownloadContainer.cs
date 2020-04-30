using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    /// <summary>
    /// A <see cref="DownloadContainer"/> that stores the data in memory.
    /// </summary>
    public class MemoryDownloadContainer : DownloadContainer
    {
        private byte[]? _data;
        /// <summary>
        /// Set to true if data has been successfully received.
        /// </summary>
        protected bool dataReceived = false;
        /// <summary>
        /// Returns true if data was successfully received and is not null.
        /// </summary>
        public override bool ResultAvailable { get => dataReceived && _data != null; }

        /// <summary>
        /// Creates an empty <see cref="MemoryDownloadContainer"/> that can receive data.
        /// </summary>
        public MemoryDownloadContainer() { }
        /// <summary>
        /// Creates a new <see cref="MemoryDownloadContainer"/> with existing data.
        /// </summary>
        /// <param name="existingData"></param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="existingData"/> is null.</exception>
        public MemoryDownloadContainer(byte[] existingData)
        {
            _data = existingData ?? throw new ArgumentNullException(nameof(existingData), "Cannot create a MemoryDownloadContainer with null existingData");
            dataReceived = true;
        }

        /// <summary>
        /// Transfers the contents of <paramref name="inputStream"/> into this <see cref="MemoryDownloadContainer"/>.
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
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> into this <see cref="MemoryDownloadContainer"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="progress"></param>
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

        /// <summary>
        /// Returns a stream with the data contained in this <see cref="MemoryDownloadContainer"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no data to retrieve.</exception>
        public override Stream GetResultStream()
        {
            if (!ResultAvailable || _data == null)
                throw new InvalidOperationException("There is not data to retrieve.");
            return new MemoryStream(_data);
        }

        bool disposed;
        /// <summary>
        /// Disposes this <see cref="MemoryDownloadContainer"/>.
        /// </summary>
        /// <param name="disposing"></param>
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
