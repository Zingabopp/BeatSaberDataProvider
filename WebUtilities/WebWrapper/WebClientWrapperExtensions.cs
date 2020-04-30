using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebUtilities.WebWrapper
{
    /// <summary>
    /// From: https://stackoverflow.com/a/19215782
    /// </summary>
    public static class WebClientWrapperExtensions
    {
        /// <summary>
        /// Gets a response from the <paramref name="request"/> asynchronously, allowing the use of a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<WebResponse> GetResponseAsync(this HttpWebRequest request, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => request.Abort(), useSynchronizationContext: false))
            {
                try
                {
                    var response = await request.GetResponseAsync().ConfigureAwait(false); ;
                    return response;
                }
                catch (WebException ex)
                {
                    
                    // WebException is thrown when request.Abort() is called,
                    // but there may be many other reasons,
                    // propagate the WebException to the caller correctly
                    if (ex.Status == WebExceptionStatus.RequestCanceled && cancellationToken.IsCancellationRequested)
                    {
                        // the WebException will be available as Exception.InnerException
                        throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                    }

                    // cancellation hasn't been requested, rethrow the original WebException
                    throw;
                }
            }
        }
    }
}
