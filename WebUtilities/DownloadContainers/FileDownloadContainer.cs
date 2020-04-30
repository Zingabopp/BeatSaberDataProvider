using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    /// <summary>
    /// A <see cref="DownloadContainer"/> that stores the data in a file.
    /// </summary>
    public class FileDownloadContainer : DownloadContainer
    {
        /// <summary>
        /// Path to the file.
        /// </summary>
        public string FilePath { get; protected set; }
        /// <summary>
        /// If true, overwrites any existing file.
        /// </summary>
        public bool Overwrite { get; protected set; }
        /// <summary>
        /// If true, the target file is deleted when the <see cref="FileDownloadContainer"/> is disposed.
        /// </summary>
        public bool DeleteOnDispose { get; set; }
        /// <summary>
        /// Set to true if data has been successfully received.
        /// </summary>
        protected bool dataReceived;
        /// <summary>
        /// Creates a new <see cref="FileDownloadContainer"/> that targets the given <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Path to the target file.</param>
        /// <param name="overwrite">If true, overwrites any existing file when data is received.</param>
        /// <param name="deleteOnDispose">If true, the target file is deleted when the <see cref="FileDownloadContainer"/> is disposed.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileDownloadContainer(string filePath, bool overwrite = true, bool deleteOnDispose = true)
        {
            if(string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath), "filePath cannot be null.");
            FilePath = filePath;
            Overwrite = overwrite;
            DeleteOnDispose = deleteOnDispose;
            dataReceived = false;
        }
        /// <summary>
        /// Returns true if data was successfully received and the target file exists.
        /// </summary>
        public override bool ResultAvailable { get => dataReceived && File.Exists(FilePath); }

        /// <summary>
        /// Transfers the contents of <paramref name="inputStream"/> to the target file.
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
        /// Asynchronously transfers the contents of <paramref name="inputStream"/> to the target file.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="disposeInput"></param>
        /// <param name="progress"></param>
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
        /// <summary>
        /// Returns a stream with the data contained in the target file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no data to retrieve.</exception>
        public override Stream GetResultStream()
        {
            if (!ResultAvailable)
                throw new InvalidOperationException("There is not data to retrieve.");
            return new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        }

        /// <summary>
        /// Deconstructor for <see cref="FileDownloadContainer"/>.
        /// </summary>
        ~FileDownloadContainer()
        {
            Dispose(false);
        }

        bool disposed;
        /// <summary>
        /// Disposes of the <see cref="FileDownloadContainer"/>, deleting the file.
        /// </summary>
        /// <param name="disposing"></param>
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
                    if (DeleteOnDispose && !string.IsNullOrEmpty(file) && File.Exists(file))
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
