using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    public class FileDownloadContainer : DownloadContainer
    {
        public string FilePath { get; private set; }
        public bool Overwrite { get; private set; }

        protected bool dataReceived;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileDownloadContainer(string filePath, bool overwrite = true)
        {
            if(string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath), "filePath cannot be null.");
            FilePath = filePath;
            Overwrite = overwrite;
            dataReceived = false;
        }

        public override bool ResultAvailable { get => dataReceived && File.Exists(FilePath); }

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
            long totalBytesReceived = 0;
            try
            {
                if (inputStream.CanSeek)
                    ExpectedInputLength = inputStream.Length;
                FileStream? fileStream = null;
                FileMode fileMode = Overwrite ? FileMode.Create : FileMode.CreateNew;
                using (fileStream = new FileStream(FilePath, fileMode, FileAccess.Write, FileShare.None))
                {
                    inputStream.CopyTo(fileStream, 81920);
                    totalBytesReceived = fileStream.Length;
                }
                dataReceived = true;
                return totalBytesReceived;
            }
            catch
            {
                throw;
            }
            finally
            {
                ActualBytesReceived = totalBytesReceived;
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
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IOException"></exception>
        public async override Task<long> ReceiveDataAsync(Stream inputStream, bool disposeInput, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
        {
            try
            {
                FileStream? fileStream = null;
                if (inputStream.CanSeek)
                    ExpectedInputLength = inputStream.Length;
                FileMode fileMode = Overwrite ? FileMode.Create : FileMode.CreateNew;
                long fileStreamLength;
                using (fileStream = new FileStream(FilePath, fileMode, FileAccess.Write, FileShare.None))
                {
                    await ProcessContentStreamAsync(inputStream, fileStream, progress, cancellationToken).ConfigureAwait(false);
                    fileStreamLength = fileStream.Length;
                }
                fileStream.Close(); // Is this necessary with the using?
                dataReceived = true;
                return fileStreamLength;
            }
            catch
            {
                throw;
            }
            finally
            {
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
            return new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        }

        ~FileDownloadContainer()
        {
            Dispose(false);
        }
        bool disposed;
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposed)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
                try
                {
                    string file = FilePath;
                    if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                disposed = true;
            }
        }

    }

}
