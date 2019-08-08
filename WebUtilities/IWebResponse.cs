﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

namespace WebUtilities
{
    public interface IWebResponseMessage : IDisposable
    {
        int StatusCode { get; }
        string ReasonPhrase { get; }
        bool IsSuccessStatusCode { get; }
        IWebResponseContent Content { get; }

        IWebResponseMessage EnsureSuccessStatusCode();
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
    }

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
