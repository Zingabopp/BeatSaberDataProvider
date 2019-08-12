using System;
using System.Collections.Generic;
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


}
