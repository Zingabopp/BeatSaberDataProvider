using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WebUtilities
{
    public interface IWebResponseMessage : IDisposable
    {
        /// <summary>
        /// Http Status code of the response.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Reason phrase associated with the status code.
        /// </summary>
        string ReasonPhrase { get; }

        /// <summary>
        /// If an exception is thrown getting the response, store it here.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Returns true if the Http Status Code indicates success.
        /// </summary>
        bool IsSuccessStatusCode { get; }

        /// <summary>
        /// Content of the response.
        /// </summary>
        IWebResponseContent Content { get; }

        /// <summary>
        /// Throws an exception if there wasn't a successful response.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="WebClientException">Thrown when IsSuccessStatusCode is false or the response is null.</exception>
        IWebResponseMessage EnsureSuccessStatusCode();

        /// <summary>
        /// Headers associated with the response.
        /// </summary>
        ReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
    }


}
