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
        Task<string> ReadAsFileAsync(string filePath, bool overwrite);

        string ContentType { get; }
        long? ContentLength { get; }
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    }
}
