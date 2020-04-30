using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities
{
    /// <summary>
    /// An interface defining content returned with an <see cref="IWebResponse"/>.
    /// </summary>
    public interface IWebResponseContent : IDisposable
    {
        /// <summary>
        /// Returns the content of the response as a string asynchronously.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no content to read.</exception>
        Task<string> ReadAsStringAsync();
        /// <summary>
        /// Returns the content of the response as a stream asynchronously.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no content to read.</exception>
        Task<Stream> ReadAsStreamAsync();
        /// <summary>
        /// Returns the content of the response in a byte array asynchronously.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no content to read.</exception>
        Task<byte[]> ReadAsByteArrayAsync();
        /// <summary>
        /// Writes the content of the response to a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the filename or response content are empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there is no content to read or overwrite is false and the file already exists.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory it's trying to save to doesn't exist.</exception>
        /// <exception cref="EndOfStreamException">Thrown when ContentLength is reported by the server and the file doesn't match it.</exception>
        /// <exception cref="IOException">Thrown when there's a problem writing the file.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the cancellationToken is triggered while downloading</exception>
        Task<string> ReadAsFileAsync(string filePath, bool overwrite, CancellationToken cancellationToken);

        /// <summary>
        /// Type of content reported by the response headers, if available.
        /// </summary>
        string? ContentType { get; }
        /// <summary>
        /// Length of the content in bytes reported by the response headers, if available.
        /// </summary>
        long? ContentLength { get; }
        /// <summary>
        /// The response content headers.
        /// </summary>
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    }
}
