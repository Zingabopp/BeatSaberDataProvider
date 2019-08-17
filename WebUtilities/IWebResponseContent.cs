using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace WebUtilities
{
    public interface IWebResponseContent : IDisposable
    {

        Task<string> ReadAsStringAsync();
        Task<Stream> ReadAsStreamAsync();
        Task<byte[]> ReadAsByteArrayAsync();
        /// <summary>
        /// Writes the content of the response to a file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the filename or response content are empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when overwrite is false and the file already exists.</exception>
        Task<string> ReadAsFileAsync(string filePath, bool overwrite);

        string ContentType { get; }
        long? ContentLength { get; }
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    }
}
